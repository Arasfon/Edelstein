using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Builders;

public class ResourceConfigurer
{
    public bool IsResourceNew { get; protected set; }
    public ExchangeItem? ExchangeItem { get; protected set; }

    public bool IsAdded { get; protected set; }
    public bool IsResourceConvertedToGift { get; protected set; }

    protected readonly IResourceAdditionBuilder ResourceAdditionBuilder;
    protected readonly Reward? Reward;

    public ResourceConfigurer(IResourceAdditionBuilder resourceAdditionBuilder, bool isResourceNew, Reward? reward = null,
        ExchangeItem? exchangeItem = null, bool isAdded = true, bool isResourceConvertedToGift = false)
    {
        IsResourceNew = isResourceNew;
        ResourceAdditionBuilder = resourceAdditionBuilder;
        Reward = reward;
        ExchangeItem = exchangeItem;
        IsAdded = isAdded;
        IsResourceConvertedToGift = isResourceConvertedToGift;
    }

    public virtual ResourceConfigurer SetDropInfo(DropInfo dropInfo)
    {
        if (Reward is null)
            return this;

        Reward.DropInfo = dropInfo;

        return this;
    }

    public virtual ResourceConfigurer SetGiveType(GiveType giveType)
    {
        if (Reward is null)
            return this;

        Reward.GiveType = giveType;

        return this;
    }

    public virtual IResourceAdditionBuilder Chain() =>
        ResourceAdditionBuilder;
}
