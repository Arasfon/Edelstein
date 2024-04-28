using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Builders;

public class ResourceAdditionBuilder : IResourceAdditionBuilder
{
    protected readonly EfficientUpdatedValueList UpdatedValueList = new();

    protected readonly LinkedList<Reward>? Rewards;
    protected readonly LinkedList<Gift> Gifts = [];

    protected readonly LinkedList<Point> AllPoints;
    protected readonly LinkedList<Item> AllItems;
    protected readonly LinkedList<Card> AllCards;
    protected readonly Gem Gem;

    protected readonly Dictionary<PointType, Point> AllExistingUserPoints;
    protected readonly Dictionary<uint, Item> AllExistingUserItems;
    protected readonly HashSet<uint> AllExistingUserCardIds;
    protected readonly HashSet<uint> AllExistingChatStampIds;

    public DateTimeOffset CurrentDateTimeOffset { get; } = DateTimeOffset.UtcNow;
    public long CurrentTimestamp { get; }

    public ResourceAdditionBuilder(EfficientUpdatedValueList updatedValueList, LinkedList<Point> allPoints,
        Dictionary<PointType, Point> allExistingUserPoints, LinkedList<Item> allItems, Dictionary<uint, Item> allExistingUserItems, Gem gem,
        UserData userData, long? currentTimestamp = null, bool calculateRewards = false)
    {
        UpdatedValueList = updatedValueList;
        AllPoints = allPoints;
        AllItems = allItems;
        AllCards = new LinkedList<Card>(userData.CardList);
        Gem = gem;

        AllExistingUserPoints = allExistingUserPoints;
        AllExistingUserItems = allExistingUserItems;
        AllExistingUserCardIds = userData.CardList.Select(x => x.MasterCardId).ToHashSet();
        AllExistingChatStampIds = userData.MasterStampIds.Select(x => x).ToHashSet();

        if (currentTimestamp is not null)
            CurrentDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(currentTimestamp.Value);

        CurrentTimestamp = CurrentDateTimeOffset.ToUnixTimeSeconds();

        if (calculateRewards)
            Rewards = [];
    }

    public ResourceAdditionBuilder(UserData userData, long? currentTimestamp = null, bool calculateRewards = false)
    {
        AllPoints = userData.PointList;
        AllItems = userData.ItemList;
        AllCards = new LinkedList<Card>(userData.CardList);
        Gem = userData.Gem;

        AllExistingUserPoints = AllPoints.ToDictionary(x => x.Type);
        AllExistingUserItems = AllItems.ToDictionary(x => x.MasterItemId);
        AllExistingUserCardIds = userData.CardList.Select(x => x.MasterCardId).ToHashSet();
        AllExistingChatStampIds = userData.MasterStampIds.Select(x => x).ToHashSet();

        if (currentTimestamp is not null)
            CurrentDateTimeOffset = DateTimeOffset.FromUnixTimeSeconds(currentTimestamp.Value);

        CurrentTimestamp = CurrentDateTimeOffset.ToUnixTimeSeconds();

        if (calculateRewards)
            Rewards = [];
    }

    public ResourceConfigurer AddCoinPoints(int amount, bool hidden = false) =>
        AddPoints(PointType.Coin, amount, hidden);

    public ResourceConfigurer AddPoints(PointType type, int amount, bool hidden = false)
    {
        if (amount == 0)
            return new NullResourceConfigurer();

        Reward reward = new()
        {
            Type = RewardType.Point,
            Value = (uint)type,
            Amount = amount
        };

        if (!hidden)
            Rewards?.AddLast(reward);

        if (UpdatedValueList.PointList.TryGetValue(type, out Point? point))
        {
            point.Amount += amount;
            return new ResourceConfigurer(this, false, reward);
        }

        if (!AllExistingUserPoints.TryGetValue(type, out point))
        {
            point = new Point
            {
                Type = type,
                Amount = amount
            };
            AllPoints.AddLast(point);
            UpdatedValueList.PointList.Add(type, point);

            return new ResourceConfigurer(this, true, reward);
        }

        point.Amount += amount;
        UpdatedValueList.PointList.Add(type, point);

        return new ResourceConfigurer(this, false, reward);
    }

    public ResourceConfigurer AddFreeGems(int amount)
    {
        if (amount == 0)
            return new NullResourceConfigurer();

        Reward reward = new()
        {
            Type = RewardType.Gem,
            Value = 1,
            Amount = amount
        };

        Rewards?.AddLast(reward);

        Gem.Free += amount;
        Gem.Total += amount;

        return new ResourceConfigurer(this, false, reward);
    }

    public ResourceConfigurer AddPaidGems(int amount)
    {
        if (amount == 0)
            return new NullResourceConfigurer();

        Reward reward = new()
        {
            Type = RewardType.Gem,
            Value = 1,
            Amount = amount
        };

        Rewards?.AddLast(reward);

        Gem.Charge += amount;
        Gem.Total += amount;

        return new ResourceConfigurer(this, false, reward);
    }

    public ResourceConfigurer AddItem(uint itemId, int amount, long? expirationTimestamp = null) =>
        AddItem(new Reward
        {
            Type = RewardType.Item,
            Value = itemId,
            Amount = amount
        }, expirationTimestamp);

    public ResourceConfigurer AddItem(Reward reward, long? expirationTimestamp)
    {
        if (reward.Amount == 0)
            return new NullResourceConfigurer();

        Rewards?.AddLast(reward);

        if (UpdatedValueList.ItemList.TryGetValue(reward.Value, out Item? item))
        {
            item.Amount += reward.Amount;
            return new ResourceConfigurer(this, false, reward);
        }

        if (!AllExistingUserItems.TryGetValue(reward.Value, out item))
        {
            item = new Item
            {
                MasterItemId = reward.Value,
                Amount = reward.Amount,
                ExpireDateTime = expirationTimestamp
            };
            AllItems.AddLast(item);
            UpdatedValueList.ItemList.Add(reward.Value, item);

            return new ResourceConfigurer(this, true, reward);
        }

        item.Amount += reward.Amount;
        UpdatedValueList.ItemList.Add(reward.Value, item);

        return new ResourceConfigurer(this, false, reward);
    }

    public DeferredItemResourceConfigurer AddItemDeferred(uint itemId, int amount, long? expirationTimestamp = null) =>
        new(this, true, new Reward
        {
            Type = RewardType.Item,
            Value = itemId,
            Amount = amount
        }, expirationTimestamp);

    public ResourceConfigurer AddCard(uint masterCardId, Rarity rarity) =>
        AddCard(new Reward
        {
            Type = RewardType.Card,
            Value = masterCardId,
            Amount = 1
        }, rarity);

    private ResourceConfigurer AddCard(Reward reward, Rarity rarity)
    {
        if (reward.Amount == 0)
            return new NullResourceConfigurer();

        Rewards?.AddLast(reward);

        const uint penlightMasterItemId = 19100001;

        if (UpdatedValueList.CardList.TryGetValue(reward.Value, out Card? card))
        {
            // Rarity.None is not possible in cards
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            int cardPenlightSubstitution = rarity switch
            {
                Rarity.R => 20,
                Rarity.Sr => 50,
                Rarity.Ur => 500,
                _ => throw new ArgumentOutOfRangeException()
            };

            AddItem(penlightMasterItemId, cardPenlightSubstitution);

            ExchangeItem exchangeItem = new()
            {
                MasterItemId = penlightMasterItemId,
                Amount = cardPenlightSubstitution
            };

            reward.ExchangeItem = exchangeItem;

            return new ResourceConfigurer(this, false, reward, exchangeItem);
        }

        if (!AllExistingUserCardIds.Contains(reward.Value))
        {
            card = new Card
            {
                MasterCardId = reward.Value,
                CreatedDateTime = CurrentTimestamp
            };

            AllCards.AddLast(card);
            UpdatedValueList.CardList.Add(reward.Value, card);

            return new ResourceConfigurer(this, true, reward);
        }
        else
        {
            // Rarity.None is not possible in cards
            // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
            int cardPenlightSubstitution = rarity switch
            {
                Rarity.R => 20,
                Rarity.Sr => 50,
                Rarity.Ur => 500,
                _ => throw new ArgumentOutOfRangeException()
            };

            AddItem(penlightMasterItemId, cardPenlightSubstitution);

            ExchangeItem exchangeItem = new()
            {
                MasterItemId = penlightMasterItemId,
                Amount = cardPenlightSubstitution
            };

            reward.ExchangeItem = exchangeItem;

            return new ResourceConfigurer(this, false, reward, exchangeItem);
        }
    }

    public DeferredCardResourceConfigurer AddCardDeferred(uint masterCardId, Rarity rarity) =>
        new(this, true, new Reward
        {
            Type = RewardType.Card,
            Value = masterCardId,
            Amount = 1
        }, rarity);

    public ResourceConfigurer AddCard(CardMst cardMst) =>
        AddCard(cardMst.Id, cardMst.Rarity);

    public DeferredCardResourceConfigurer AddCardDeferred(CardMst cardMst) =>
        AddCardDeferred(cardMst.Id, cardMst.Rarity);

    public ResourceConfigurer AddChatStamp(uint chatStampId) =>
        AddChatStamp(new Reward
        {
            Type = RewardType.ChatStamp,
            Value = chatStampId,
            Amount = 1
        });

    private ResourceConfigurer AddChatStamp(Reward reward)
    {
        Rewards?.AddLast(reward);

        if (AllExistingChatStampIds.Contains(reward.Value))
            return new ResourceConfigurer(this, false, reward);

        UpdatedValueList.MasterStampIds.Add(reward.Value);

        return new ResourceConfigurer(this, true, reward);
    }

    public DeferredResourceConfigurer AddChatStampDeferred(uint chatStampId) =>
        new(this, true, new Reward
        {
            Type = RewardType.ChatStamp,
            Value = chatStampId,
            Amount = 1
        });

    public ResourceConfigurer Add(RewardType rewardType, uint itemId, int amount, Rarity? rarity = null)
    {
        if (amount == 0)
            return new NullResourceConfigurer();

        switch (rewardType)
        {
            case RewardType.Gem:
                return AddFreeGems(amount);
            case RewardType.Card:
            {
                if (rarity is null)
                    throw new ArgumentNullException(nameof(rarity));

                return AddCard(itemId, rarity.Value);
            }
            case RewardType.Item:
                return AddItem(itemId, amount);
            case RewardType.Point:
                return AddPoints((PointType)itemId, amount);
            case RewardType.ChatStamp:
                return AddChatStamp(itemId);
            case RewardType.None:
            default:
                throw new NotImplementedException();
        }
    }

    public void AddGemsAsGift(string reason, int amount, long? expirationTimestamp = null) =>
        AddGift(reason, RewardType.Gem, 1, amount, expirationTimestamp);

    public void AddCoinPointsAsGift(string reason, int amount, long? expirationTimestamp = null) =>
        AddPointsAsGift(reason, PointType.Coin, amount, expirationTimestamp);

    public void AddPointsAsGift(string reason, PointType type, int amount, long? expirationTimestamp = null) =>
        AddGift(reason, RewardType.Point, (uint)type, amount, expirationTimestamp);

    public void AddItemAsGift(string reason, uint itemId, int amount, long? expirationTimestamp = null) =>
        AddGift(reason, RewardType.Item, itemId, amount, expirationTimestamp);

    public void AddCardAsGift(string reason, uint cardId, long? expirationTimestamp = null) =>
        AddGift(reason, RewardType.Card, cardId, 1, expirationTimestamp);

    public void AddGift(string reason, RewardType type, uint itemId, int amount, long? expirationTimestamp = null) =>
        Gifts.AddLast(new Gift
        {
            IsReceive = false,
            ReasonText = reason,
            RewardType = type,
            Value = itemId,
            Amount = amount,
            CreatedDateTime = CurrentTimestamp,
            ExpireDateTime = expirationTimestamp ?? CurrentDateTimeOffset.AddYears(1).ToUnixTimeSeconds()
        });

    public ResourceConfigurer FinishDeferred(DeferredResourceConfigurer deferredResourceConfigurer)
    {
        switch (deferredResourceConfigurer.ConfiguredReward.Type)
        {
            case RewardType.Card:
            {
                if (deferredResourceConfigurer is not DeferredCardResourceConfigurer deferredCardResourceConfigurer)
                    throw new Exception("Invalid deferred resource configurer type");

                return AddCard(deferredResourceConfigurer.ConfiguredReward, deferredCardResourceConfigurer.Rarity);
            }
            case RewardType.Item:
            {
                if (deferredResourceConfigurer is not DeferredItemResourceConfigurer deferredItemResourceConfigurer)
                    throw new Exception("Invalid deferred resource configurer type");

                return AddItem(deferredResourceConfigurer.ConfiguredReward, deferredItemResourceConfigurer.ExpirationTimestmap);
            }
            case RewardType.ChatStamp:
                return AddChatStamp(deferredResourceConfigurer.ConfiguredReward);
            default:
                throw new NotImplementedException();
        }
    }

    public ResourcesModificationResult Build() =>
        new()
        {
            Updates = UpdatedValueList.ToUpdatedValueList(),
            Rewards = Rewards,
            Gifts = Gifts,
            Gem = Gem,
            Points = AllPoints,
            Cards = AllCards,
            Items = AllItems
        };
}
