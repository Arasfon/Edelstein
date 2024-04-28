using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class DeferredItemResourceConfigurer : DeferredResourceConfigurer
{
    public long? ExpirationTimestmap { get; protected set; }

    public DeferredItemResourceConfigurer(IResourceAdditionBuilder resourceAdditionBuilder, bool isResourceNew, Reward reward,
        long? expirationTimestamp,
        ExchangeItem? exchangeItem = null) : base(resourceAdditionBuilder, isResourceNew, reward, exchangeItem) =>
        ExpirationTimestmap = expirationTimestamp;
}
