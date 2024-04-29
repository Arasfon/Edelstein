using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Builders;

public class NullResourceConfigurer : ResourceConfigurer
{
    public NullResourceConfigurer() : base(null!, false) { }

    public override ResourceConfigurer SetDropInfo(DropInfo dropInfo) =>
        this;

    public override ResourceConfigurer SetGiveType(GiveType giveType) =>
        this;
}
