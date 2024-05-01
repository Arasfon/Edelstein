using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

using System.Diagnostics;

namespace Edelstein.Server.Builders;

public class ResourceConsumptionBuilder : IResourceConsumptionBuilder
{
    protected readonly EfficientUpdatedValueList UpdatedValueList = new();

    protected readonly LinkedList<Point> AllPoints;
    protected readonly LinkedList<Item> AllItems;
    protected readonly Gem Gem;

    protected readonly Dictionary<PointType, Point> AllExistingUserPoints;
    protected readonly Dictionary<uint, Item> AllExistingUserItems;

    protected readonly long CurrentTimestamp;

    public ResourceConsumptionBuilder(LinkedList<Point> allPoints, LinkedList<Item> allItems, Gem gem, long currentTimestamp)
    {
        AllPoints = allPoints;
        AllItems = allItems;
        Gem = gem;

        CurrentTimestamp = currentTimestamp;

        AllExistingUserPoints = AllPoints.ToDictionary(x => x.Type);
        AllExistingUserItems = AllItems.ToDictionary(x => x.MasterItemId);
    }

    public ResourceConsumptionBuilder(UserData userData, long currentTimestamp) : this(userData.PointList,
        userData.ItemList, userData.Gem, currentTimestamp) { }

    public bool TryDistributeConsumeGems(int amount)
    {
        (bool enoughGems, int freeCharge, int paidCharge) = TryDistributeConsumeGems(Gem, amount);

        if (!enoughGems)
            return false;

        Gem.Free -= freeCharge;
        Gem.Charge -= paidCharge;

        Gem.Total = Gem.Free + Gem.Total;

        UpdatedValueList.Gem ??= Gem;

        return true;
    }

    public static (bool EnoughGems, int Free, int Paid) TryDistributeConsumeGems(Gem gem, int amount)
    {
        if (gem.Total - amount < 0)
            return (false, 0, 0);

        int freeCharge = (int)Math.Min(gem.Free, amount);

        amount -= freeCharge;

        int paidCharge = (int)Math.Min(gem.Charge, amount);

        amount -= paidCharge;

        Debug.Assert(amount == 0);

        return (true, freeCharge, paidCharge);
    }

    public bool TryConsumeFreeGems(int amount)
    {
        if (Gem.Free - amount < 0)
            return false;

        Gem.Free -= amount;
        Gem.Total -= amount;

        UpdatedValueList.Gem ??= Gem;

        return true;
    }

    public bool TryConsumePaidGems(int amount)
    {
        if (Gem.Charge - amount < 0)
            return false;

        Gem.Charge -= amount;
        Gem.Total -= amount;

        UpdatedValueList.Gem ??= Gem;

        return true;
    }

    public bool TryConsumeItems(uint itemId, int amount)
    {
        if (UpdatedValueList.ItemList.TryGetValue(itemId, out Item? item))
        {
            if (item.Amount - amount < 0)
                return false;

            if (item.ExpireDateTime <= CurrentTimestamp)
                return false;

            item.Amount -= amount;
            return true;
        }

        if (!AllExistingUserItems.TryGetValue(itemId, out item))
            return false;

        if (item.Amount - amount < 0)
            return false;

        if (item.ExpireDateTime <= CurrentTimestamp)
            return false;

        item.Amount -= amount;

        UpdatedValueList.ItemList.Add(itemId, item);
        return true;
    }

    public bool TryConsumePoints(PointType type, int amount)
    {
        if (UpdatedValueList.PointList.TryGetValue(type, out Point? point))
        {
            if (point.Amount - amount < 0)
                return false;

            point.Amount -= amount;
            return true;
        }

        if (!AllExistingUserPoints.TryGetValue(type, out point))
            return false;

        if (point.Amount - amount < 0)
            return false;

        point.Amount -= amount;

        UpdatedValueList.PointList.Add(type, point);
        return true;
    }

    public bool TryConsume(ConsumeType consumeType, uint itemId, int amount)
    {
        switch (consumeType)
        {
            case ConsumeType.None:
                return true;
            case ConsumeType.Gem:
                return TryConsumeFreeGems(amount);
            case ConsumeType.ChargeGem:
                return TryConsumePaidGems(amount);
            case ConsumeType.Item:
                return TryConsumeItems(itemId, amount);
            case ConsumeType.Point:
                return TryConsumePoints((PointType)itemId, amount);
            case ConsumeType.EventPoint:
            case ConsumeType.Card:
            default:
                throw new NotImplementedException();
        }
    }

    public ResourceAdditionBuilder ToResourceAdditionBuilder(UserData userData, bool calculateRewards = false) =>
        new(UpdatedValueList, AllPoints, AllExistingUserPoints, AllItems, AllExistingUserItems,
            Gem, userData, CurrentTimestamp, calculateRewards);

    public ResourcesModificationResult Build() =>
        new()
        {
            Updates = UpdatedValueList.ToUpdatedValueList(),
            Rewards = null,
            Gifts = null,
            Gem = UpdatedValueList.Gem,
            Points = AllPoints,
            Cards = null!,
            Items = AllItems
        };
}
