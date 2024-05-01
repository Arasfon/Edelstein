using Edelstein.Data.Constants;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Server.Builders;
using Edelstein.Server.Models;
using Edelstein.Server.Random;

using Microsoft.EntityFrameworkCore;

using System.Globalization;

using BandCategory = Edelstein.Data.Msts.BandCategory;

namespace Edelstein.Server.Services;

public class LotteryService : ILotteryService
{
    private readonly MstDbContext _mstDbContext;
    private readonly IUserService _userService;

    public LotteryService(MstDbContext mstDbContext, IUserService userService)
    {
        _mstDbContext = mstDbContext;
        _userService = userService;
    }

    public async Task<List<Lottery>> GetAndRefreshUserLotteriesData(ulong xuid)
    {
        UserData? userData = await _userService.GetUserDataByXuid(xuid);

        DateTimeOffset lastReset = ResetDate.Last;

        bool hasRefreshed = false;

        foreach (Lottery lottery in userData!.LotteryList)
        {
            DateTimeOffset lastCountDate = DateTimeOffset.ParseExact(lottery.LastCountDate, "yyyy-MM-dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

            if (lastCountDate < lastReset && lottery.DailyCount != 0)
            {
                lottery.DailyCount = 0;
                hasRefreshed = true;
            }
        }

        if (hasRefreshed)
            await _userService.UpdateUserLotteries(xuid, userData.LotteryList);

        return userData.LotteryList;
    }

    public Task<Lottery> GetTutorialLotteryByMasterCharacterId(uint masterCharacterId)
    {
        BandCategory group = (BandCategory)(masterCharacterId / 1000);

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        uint masterLotteryId = group switch
        {
            BandCategory.Muse => 0,
            BandCategory.Aqours => 9,
            BandCategory.Nijigaku => 9 + 9,
            BandCategory.Liella => 9 + 9 + 12,
            _ => throw new ArgumentException()
        };

        masterLotteryId += 9110000 + masterCharacterId % 100;

        return Task.FromResult(new Lottery
        {
            MasterLotteryId = masterLotteryId,
            MasterLotteryPriceNumber = 1,
            Count = 0,
            DailyCount = 0
        });
    }

    public async Task<LotteryDrawResult> Draw(ulong xuid, Lottery lottery)
    {
        DateTimeOffset currentDateTimeOffset = DateTimeOffset.UtcNow;
        long currentTimestamp = currentDateTimeOffset.ToUnixTimeSeconds();

        UserData userData = (await _userService.GetUserDataByXuid(xuid))!;

        Lottery? lotteryRecord = userData.LotteryList.FirstOrDefault(x =>
            x.MasterLotteryId == lottery.MasterLotteryId && x.MasterLotteryPriceNumber == lottery.MasterLotteryPriceNumber);

        // Get lottery price mst, using that MasterLotteryPriceId = Lottery.Id in msts
        LotteryPriceMst priceMst =
            await _mstDbContext.LotteryPriceMsts.FirstAsync(x =>
                x.Id == lottery.MasterLotteryId && x.Number == lottery.MasterLotteryPriceNumber);

        // Check daily/total limits
        if (lotteryRecord is null)
        {
            lotteryRecord = new Lottery
            {
                MasterLotteryId = lottery.MasterLotteryId,
                MasterLotteryPriceNumber = lottery.MasterLotteryPriceNumber,
                Count = 1,
                DailyCount = 1,
                LastCountDate = currentDateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss")
            };

            userData.LotteryList.Add(lotteryRecord);
        }
        else
        {
            if (priceMst.LimitCount > 0)
            {
                if (lotteryRecord.Count + 1 > priceMst.LimitCount)
                    return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);
            }

            if (priceMst.DailyLimitCount > 0)
            {
                DateTimeOffset lastCountDate = DateTimeOffset.ParseExact(lotteryRecord.LastCountDate, "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

                if (lastCountDate < ResetDate.LastOf(currentDateTimeOffset))
                    lotteryRecord.DailyCount = 0;

                if (lotteryRecord.DailyCount > priceMst.DailyLimitCount)
                    return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);
            }

            lotteryRecord.Count++;
            lotteryRecord.DailyCount++;
            lotteryRecord.LastCountDate = currentDateTimeOffset.ToString("yyyy-MM-dd HH:mm:ss");
        }

        // Consume resources
        ResourceConsumptionBuilder resourceConsumptionBuilder = new(userData, currentTimestamp);

        if (!resourceConsumptionBuilder.TryConsume(priceMst.ConsumeType, priceMst.MasterItemId, priceMst.Price))
            return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);

        // Create resource addition builder
        ResourceAdditionBuilder resourceAdditionBuilder = resourceConsumptionBuilder.ToResourceAdditionBuilder(userData);

        // Query to get all drawable items, using that MasterLotteryRarityId = Lottery.Id in msts
        IQueryable<JoinedLotteryItem> query = from r in _mstDbContext.LotteryRarityMsts
            join i in _mstDbContext.LotteryItemMsts on r.MasterLotteryItemId equals i.Id
            where r.Id == lottery.MasterLotteryId
            select new JoinedLotteryItem(i.Id,
                i.Number,
                i.Type,
                i.Value,
                i.Amount,
                r.Rarity,
                r.Ratio,
                r.Ensured);

        // Query and group drawable items by their Ensured status.
        // Within each Ensured group, further group items by their Id (rarity bucket).
        // Also, collect Ratios for each Ensured group.
        // After all operations index 0 should contain items with Ensured = 0, and index 1 with Ensured = 1
        var bucketsByEnsurance = (await query.ToListAsync())
            .GroupBy(x => x.Ensured)
            .OrderBy(x => x.Key)
            .Select(g =>
            {
                List<IGrouping<uint, JoinedLotteryItem>> itemBuckets = g
                    .GroupBy(item => item.Id)
                    .ToList();

                return new
                {
                    Ensured = g.Key,
                    Items = itemBuckets.Select(x => x.ToList()).ToList(),
                    Ratios = itemBuckets.Select(x => x.First().Ratio).ToList()
                };
            })
            .ToList();

        // Draw items
        List<JoinedLotteryItem> drawedDefaultItems;
        List<JoinedLotteryItem> drawedEnsuredItems;

        // If drawing 10 items
        if (priceMst.Count == 10)
        {
            // If there is some ensured items
            if (bucketsByEnsurance.Count > 1 && bucketsByEnsurance[1].Items.Count > 0)
            {
                BucketRandom<JoinedLotteryItem> defaultBucketRandom = new(bucketsByEnsurance[0].Ratios, bucketsByEnsurance[0].Items);
                BucketRandom<JoinedLotteryItem> ensuredBucketRandom = new(bucketsByEnsurance[1].Ratios, bucketsByEnsurance[1].Items);

                drawedDefaultItems = defaultBucketRandom.GetNextRange(9).ToList();
                drawedEnsuredItems = ensuredBucketRandom.GetNextRange(1).ToList();
            }
            // If there is none of ensured items
            else
            {
                BucketRandom<JoinedLotteryItem> defaultBucketRandom = new(bucketsByEnsurance[0].Ratios, bucketsByEnsurance[0].Items);

                drawedDefaultItems = defaultBucketRandom.GetNextRange(10).ToList();
                drawedEnsuredItems = [];
            }
        }
        // If drawing one item
        else
        {
            BucketRandom<JoinedLotteryItem> defaultBucketRandom = new(bucketsByEnsurance[0].Ratios, bucketsByEnsurance[0].Items);

            drawedDefaultItems = [defaultBucketRandom.GetNext()];
            drawedEnsuredItems = [];
        }

        // Build list of LotteryItem to return
        LinkedList<LotteryItem> lotteryItems = [];

        foreach (JoinedLotteryItem joinedLotteryItem in drawedDefaultItems.Concat(drawedEnsuredItems))
        {
            ResourceConfigurer resourceConfigurer = resourceAdditionBuilder.Add(joinedLotteryItem.Type, joinedLotteryItem.Value,
                joinedLotteryItem.Amount, joinedLotteryItem.Rarity);

            lotteryItems.AddLast(new LotteryItem
            {
                MasterLotteryItemId = joinedLotteryItem.Id,
                MasterLotteryItemNumber = joinedLotteryItem.Number,
                IsNew = resourceConfigurer.IsResourceNew,
                ExchangeItem = resourceConfigurer.ExchangeItem
            });
        }

        // Build modifications
        ResourcesModificationResult resourcesModificationResult = resourceAdditionBuilder.Build();

        // Update full cards, items, points and lotteries of a user, creating ids for cards and items, if they do not have it.
        // As cards/items are updated in-place, then there is no need to make some complex adjustments after an update.
        // uvl and userData.*List store references to the same objects, so everything is updated respectively.
        await _userService.SetGemsCardsItemsPointsLotteriesCreatingIds(xuid, resourcesModificationResult.Gem ?? userData.Gem,
            resourcesModificationResult.Cards, resourcesModificationResult.Items, resourcesModificationResult.Points, userData.LotteryList);

        return new LotteryDrawResult(LotteryDrawResultStatus.Success, lotteryItems, resourcesModificationResult.Updates);
    }

    private record JoinedLotteryItem(
        uint Id,
        uint Number,
        RewardType Type,
        uint Value,
        int Amount,
        Rarity Rarity,
        int Ratio,
        uint Ensured
    );

    public Task<bool> IsTutorial(Lottery lottery) =>
        Task.FromResult(lottery.MasterLotteryId > 9110000);
}
