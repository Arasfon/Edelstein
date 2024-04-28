using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Builders;

public class DeferredCardResourceConfigurer : DeferredResourceConfigurer
{
    public Rarity Rarity { get; protected set; }

    public DeferredCardResourceConfigurer(IResourceAdditionBuilder resourceAdditionBuilder, bool isResourceNew, Reward reward,
        Rarity rarity, ExchangeItem? exchangeItem = null) : base(resourceAdditionBuilder, isResourceNew, reward, exchangeItem) =>
        Rarity = rarity;
}
