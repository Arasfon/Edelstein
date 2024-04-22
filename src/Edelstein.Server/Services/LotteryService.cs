using Edelstein.Data.Constants;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Server.Random;

using Microsoft.EntityFrameworkCore;

using System.Globalization;

using BandCategory = Edelstein.Data.Msts.BandCategory;
using RewardType = Edelstein.Data.Msts.RewardType;

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
        UpdatedValueList uvl = new();

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

        // Check if enough consumables
        switch (priceMst.ConsumeType)
        {
            case ConsumeType.None:
                break;
            case ConsumeType.Gem:
            {
                if (userData.Gem.Free - priceMst.Price < 0)
                    return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);

                uvl.Gem = userData.Gem;
                uvl.Gem.Free -= priceMst.Price;
                uvl.Gem.Total -= priceMst.Price;
                break;
            }
            case ConsumeType.ChargeGem:
            {
                if (userData.Gem.Charge - priceMst.Price < 0)
                    return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);

                uvl.Gem = userData.Gem;
                uvl.Gem.Charge -= priceMst.Price;
                uvl.Gem.Total -= priceMst.Price;
                break;
            }
            case ConsumeType.Item:
            {
                Item? item = userData.ItemList.FirstOrDefault(x => x.MasterItemId == priceMst.MasterItemId);

                if (item is null)
                    return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);

                if (item.Amount - priceMst.Price < 0)
                    return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);

                uvl.ItemList.Add(item);
                item.Amount -= priceMst.Price;

                break;
            }
            default:
                // It is not possible to consume cards or points here
                throw new ArgumentOutOfRangeException();
        }

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
        // Also, collect distinct Ratios for each Ensured group.
        // After all operations index 0 should contain items with Ensured = 0, and index 1 with Ensured = 1
        var bucketsByEnsurance = (await query.ToListAsync())
            .GroupBy(x => x.Ensured)
            .OrderBy(x => x.Key)
            .Select(g => new
            {
                Ensured = g.Key,
                Items = g.GroupBy(x => x.Id)
                    .Select(x => x.ToList())
                    .ToList(),
                Ratios = g.Select(x => x.Ratio).Distinct().ToList()
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

        // Build HashSet/Dictionary for efficient lookup and existance checking
        HashSet<uint> allUserCards = userData.CardList.Select(x => x.MasterCardId).ToHashSet();
        Dictionary<uint, Item> allUserItems = userData.ItemList.ToDictionary(x => x.MasterItemId);
        Dictionary<PointType, Point> allUserPoints = userData.PointList.ToDictionary(x => x.Type);

        // It is updated in AddOrUpdateReward() if duplicate cards are found
        int penlightCount = 0;

        // Build list of LotteryItem to return
        List<LotteryItem> lotteryItems = [];

        foreach (JoinedLotteryItem joinedLotteryItem in drawedDefaultItems.Concat(drawedEnsuredItems))
        {
            (bool isNew, ExchangeItem? exchangeItem) = AddOrUpdateReward(joinedLotteryItem);

            lotteryItems.Add(new LotteryItem
            {
                MasterLotteryItemId = joinedLotteryItem.Id,
                MasterLotteryItemNumber = joinedLotteryItem.Number,
                IsNew = isNew,
                ExchangeItem = exchangeItem
            });
        }

        // Add penlights if they were produced
        const uint penlightMasterItemId = 19100001;

        if (penlightCount != 0)
        {
            AddOrUpdateReward(new JoinedLotteryItem(0, 0, RewardType.Item, penlightMasterItemId, penlightCount,
                Rarity.None, 0, 0));
        }

        // Update full cards, items, points and lotteries of a user, creating ids for cards and items, if they do not have it.
        // As cards/items are updated in-place, then there is no need to make some complex adjustments after an update.
        // uvl and userData.*List store references to the same objects, so everything is updated respectively.
        await _userService.SetGemsCardsItemsPointsLotteriesCreatingIds(xuid, userData.Gem, userData.CardList, userData.ItemList,
            userData.PointList, userData.LotteryList);

        return new LotteryDrawResult(LotteryDrawResultStatus.Success, lotteryItems, uvl);

        (bool IsNew, ExchangeItem? ExchangeItem) AddOrUpdateReward(JoinedLotteryItem joinedLotteryItem)
        {
            switch (joinedLotteryItem.Type)
            {
                case RewardType.Card:
                {
                    Card? drawnDuplicate = uvl.CardList.FirstOrDefault(x => x.MasterCardId == joinedLotteryItem.Value);

                    if (drawnDuplicate is not null)
                    {
                        // Rarity.None is not possible in cards
                        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                        int cardPenlightSubstitution = joinedLotteryItem.Rarity switch
                        {
                            Rarity.R => 20,
                            Rarity.Sr => 50,
                            Rarity.Ur => 500,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        penlightCount += cardPenlightSubstitution;

                        return (false, new ExchangeItem
                        {
                            MasterItemId = penlightMasterItemId,
                            Amount = cardPenlightSubstitution
                        });
                    }

                    bool isNew = !allUserCards.Contains(joinedLotteryItem.Value);

                    if (isNew)
                    {
                        Card card = new()
                        {
                            MasterCardId = joinedLotteryItem.Value,
                            CreatedDateTime = currentTimestamp
                        };
                        userData.CardList.Add(card);
                        uvl.CardList.Add(card);
                    }
                    else
                    {
                        // Rarity.None is not possible in cards
                        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
                        int cardPenlightSubstitution = joinedLotteryItem.Rarity switch
                        {
                            Rarity.R => 20,
                            Rarity.Sr => 50,
                            Rarity.Ur => 500,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        penlightCount += cardPenlightSubstitution;

                        return (false, new ExchangeItem
                        {
                            MasterItemId = penlightMasterItemId,
                            Amount = cardPenlightSubstitution
                        });
                    }

                    return (isNew, null);
                }
                case RewardType.Item:
                {
                    Item? drawnDuplicate = uvl.ItemList.FirstOrDefault(x => x.MasterItemId == joinedLotteryItem.Value);

                    if (drawnDuplicate is not null)
                    {
                        drawnDuplicate.Amount += joinedLotteryItem.Amount;
                        return (false, null);
                    }

                    bool isNew = !allUserItems.TryGetValue(joinedLotteryItem.Value, out Item? item);

                    if (isNew)
                    {
                        item = new Item
                        {
                            MasterItemId = joinedLotteryItem.Value,
                            Amount = joinedLotteryItem.Amount,
                            ExpireDateTime = null
                        };
                        userData.ItemList.Add(item);
                        uvl.ItemList.Add(item);
                    }
                    else
                    {
                        item!.Amount += joinedLotteryItem.Amount;
                        uvl.ItemList.Add(item);
                    }

                    return (isNew, null);
                }
                case RewardType.Point:
                {
                    Point? drawnDuplicate = uvl.PointList.FirstOrDefault(x => x.Type == (PointType)joinedLotteryItem.Value);

                    if (drawnDuplicate is not null)
                    {
                        drawnDuplicate.Amount += joinedLotteryItem.Amount;
                        return (false, null);
                    }

                    bool isNew = !allUserPoints.TryGetValue((PointType)joinedLotteryItem.Value, out Point? point);

                    if (isNew)
                    {
                        point = new Point
                        {
                            Type = (PointType)joinedLotteryItem.Value,
                            Amount = joinedLotteryItem.Amount
                        };
                        userData.PointList.Add(point);
                        uvl.PointList.Add(point);
                    }
                    else
                    {
                        point!.Amount += joinedLotteryItem.Amount;
                        uvl.PointList.Add(point);
                    }

                    return (isNew, null);
                }
                default:
                    // Nothing else should be possible (at least it does not exist in msts)
                    throw new ArgumentOutOfRangeException();
            }
        }
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
