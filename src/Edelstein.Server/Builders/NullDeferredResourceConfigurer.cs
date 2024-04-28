using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class NullDeferredResourceConfigurer : DeferredResourceConfigurer
{
    public NullDeferredResourceConfigurer() : base(null!, false, null!) { }

    public override DeferredResourceConfigurer SetDropInfo(DropInfo dropInfo) =>
        this;

    public override DeferredResourceConfigurer MakeLimited(List<LimitedReward> limitedRewards, int maxAmount, int firstReward = 0) =>
        this;

    public override ResourceConfigurer Finish() =>
        this;
}
