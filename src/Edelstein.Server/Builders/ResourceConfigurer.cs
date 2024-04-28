using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class ResourceConfigurer
{
    public bool IsResourceNew { get; protected set; }
    public ExchangeItem? ExchangeItem { get; protected set; }

    protected readonly IResourceAdditionBuilder ResourceAdditionBuilder;
    protected readonly Reward? Reward;

    public ResourceConfigurer(IResourceAdditionBuilder resourceAdditionBuilder, bool isResourceNew, Reward? reward = null, ExchangeItem? exchangeItem = null)
    {
        IsResourceNew = isResourceNew;
        ResourceAdditionBuilder = resourceAdditionBuilder;
        Reward = reward;
        ExchangeItem = exchangeItem;
    }

    public virtual ResourceConfigurer SetDropInfo(DropInfo dropInfo)
    {
        if (Reward is null)
            return this;

        Reward.DropInfo = dropInfo;

        return this;
    }
}
