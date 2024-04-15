using Edelstein.Data.Constants;
using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Data.Repositories;
using Edelstein.GameServer.Random;

using Microsoft.EntityFrameworkCore;

using System.Globalization;

using BandCategory = Edelstein.Data.Msts.BandCategory;
using RewardType = Edelstein.Data.Msts.RewardType;

namespace Edelstein.GameServer.Services;

public class LotteryService : ILotteryService
{
    private readonly ISequenceRepository<ulong> _sequenceRepository;
    private readonly MstDbContext _mstDbContext;
    private readonly IUserService _userService;

    public LotteryService(ISequenceRepository<ulong> sequenceRepository, MstDbContext mstDbContext, IUserService userService)
    {
        _sequenceRepository = sequenceRepository;
        _mstDbContext = mstDbContext;
        _userService = userService;
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
                LastCountDate = DateTimeOffset.FromUnixTimeSeconds(currentTimestamp).ToString("yyyy-MM-dd HH:mm:ss")
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

                if (lastCountDate < ResetDate.PreviousOf(currentDateTimeOffset))
                    lotteryRecord.DailyCount = 0;

                if (lotteryRecord.DailyCount > priceMst.DailyLimitCount)
                    return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);

                lotteryRecord.DailyCount++;
            }

            lotteryRecord.Count++;
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
                break;
            }
            case ConsumeType.ChargeGem:
            {
                if (userData.Gem.Charge - priceMst.Price < 0)
                    return new LotteryDrawResult(LotteryDrawResultStatus.NotEnoughItems, [], null!);

                uvl.Gem = userData.Gem;
                uvl.Gem.Charge -= priceMst.Price;
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
            if (bucketsByEnsurance[1].Items.Count > 0)
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

        // It is updated in UpdateRewardsAndReturnIsNew() if duplicate cards are found
        int penlightCount = 0;

        // Build list of LotteryItem to return
        List<LotteryItem> lotteryItems = drawedDefaultItems.Concat(drawedEnsuredItems)
            .Select(x => new LotteryItem
            {
                MasterLotteryItemId = x.Id,
                MasterLotteryItemNumber = x.Number,
                IsNew = UpdateRewardsAndReturnIsNew(x)
            })
            .ToList();

        // Add penlights if they were produced
        if (penlightCount != 0)
        {
            const uint penlightId = 19100001;

            if (allUserItems.TryGetValue(penlightId, out Item? item))
            {
                item.Amount += penlightCount;
                uvl.ItemList.Add(item);
            }
            else
            {
                item = new Item
                {
                    MasterItemId = penlightId,
                    Amount = penlightCount
                };
                userData.ItemList.Add(item);
                uvl.ItemList.Add(item);
            }
        }

        // Update full cards, items, points and lotteries of a user, creating ids for cards and items, if they do not have it.
        // As cards/items are updated in-place, then there is no need to make some complex adjustments after an update.
        // uvl and userData.*List store references to the same objects, so everything is updated respectively.
        await _userService.SetCardsItemsPointsLotteriesCreatingIds(xuid, userData.CardList, userData.ItemList, userData.PointList,
            userData.LotteryList);

        return new LotteryDrawResult(LotteryDrawResultStatus.Success, lotteryItems, uvl);

        bool UpdateRewardsAndReturnIsNew(JoinedLotteryItem joinedLotteryItem)
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
                        penlightCount += joinedLotteryItem.Rarity switch
                        {
                            Rarity.R => 20,
                            Rarity.Sr => 50,
                            Rarity.Ur => 500,
                            _ => throw new ArgumentOutOfRangeException()
                        };
                        return false;
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
                        penlightCount += joinedLotteryItem.Rarity switch
                        {
                            Rarity.R => 20,
                            Rarity.Sr => 50,
                            Rarity.Ur => 500,
                            _ => throw new ArgumentOutOfRangeException()
                        };

                        return false;
                    }

                    return isNew;
                }
                case RewardType.Item:
                {
                    Item? drawnDuplicate = uvl.ItemList.FirstOrDefault(x => x.MasterItemId == joinedLotteryItem.Value);

                    if (drawnDuplicate is not null)
                    {
                        drawnDuplicate.Amount += joinedLotteryItem.Amount;
                        return false;
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

                    return isNew;
                }
                case RewardType.Point:
                {
                    Point? drawnDuplicate = uvl.PointList.FirstOrDefault(x => x.Type == (PointType)joinedLotteryItem.Value);

                    if (drawnDuplicate is not null)
                    {
                        drawnDuplicate.Amount += joinedLotteryItem.Amount;
                        return false;
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

                    return isNew;
                }
                default:
                    // Nothing else should not be possible (at least it does not exist in msts)
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
