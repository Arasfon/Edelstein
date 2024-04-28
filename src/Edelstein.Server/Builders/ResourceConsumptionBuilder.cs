using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

using System.Diagnostics;

namespace Edelstein.Server.Builders;

public class ResourceConsumptionBuilder : IResourceConsumptionBuilder
{
    private readonly EfficientUpdatedValueList _updatedValueList = new();

    private readonly LinkedList<Point> _allPoints;
    private readonly LinkedList<Item> _allItems;
    private readonly Gem _gem;

    private readonly Dictionary<PointType, Point> _allExistingUserPoints;
    private readonly Dictionary<uint, Item> _allExistingUserItems;

    public ResourceConsumptionBuilder(LinkedList<Point> allPoints, LinkedList<Item> allItems, Gem gem)
    {
        _allPoints = allPoints;
        _allItems = allItems;
        _gem = gem;

        _allExistingUserPoints = _allPoints.ToDictionary(x => x.Type);
        _allExistingUserItems = _allItems.ToDictionary(x => x.MasterItemId);
    }

    public ResourceConsumptionBuilder(UserData userData) : this(new LinkedList<Point>(userData.PointList),
        new LinkedList<Item>(userData.ItemList), userData.Gem) { }

    public bool TryDistributeConsumeGems(int amount)
    {
        (bool enoughGems, int freeCharge, int paidCharge) = TryDistributeConsumeGems(_gem, amount);

        if (!enoughGems)
            return false;

        _gem.Free -= freeCharge;
        _gem.Charge -= paidCharge;

        _gem.Total = _gem.Free + _gem.Total;

        _updatedValueList.Gem ??= _gem;

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
        if (_gem.Free - amount < 0)
            return false;

        _gem.Free -= amount;
        _gem.Total -= amount;

        _updatedValueList.Gem ??= _gem;

        return true;
    }

    public bool TryConsumePaidGems(int amount)
    {
        if (_gem.Charge - amount < 0)
            return false;

        _gem.Charge -= amount;
        _gem.Total -= amount;

        _updatedValueList.Gem ??= _gem;

        return true;
    }

    public bool TryConsumeItems(uint itemId, int amount)
    {
        if (!_allExistingUserItems.TryGetValue(itemId, out Item? item))
            return false;

        if (item.Amount - amount < 0)
            return false;

        _updatedValueList.ItemList.Add(itemId, item);
        item.Amount -= amount;

        return true;
    }

    public bool TryConsumePoints(PointType type, int amount)
    {
        if (!_allExistingUserPoints.TryGetValue(type, out Point? point))
            return false;

        if (point.Amount - amount < 0)
            return false;

        _updatedValueList.PointList.Add(type, point);
        point.Amount -= amount;

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

    public ResourceAdditionBuilder ToResourceAdditionBuilder(UserData userData, long? currentTimestamp = null, bool calculateRewards = false) =>
        new(_updatedValueList, _allPoints, _allExistingUserPoints, _allItems, _allExistingUserItems,
            _gem, userData, currentTimestamp, calculateRewards);

    public ResourcesModificationResult Build() =>
        new()
        {
            Updates = _updatedValueList.ToUpdatedValueList(),
            Rewards = null,
            Gifts = null,
            Gem = _updatedValueList.Gem,
            Points = _allPoints,
            Cards = null!,
            Items = _allItems
        };
}
