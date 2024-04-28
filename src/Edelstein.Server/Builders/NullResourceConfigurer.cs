using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Builders;

public class NullResourceConfigurer : ResourceConfigurer
{
    public NullResourceConfigurer() : base(null!, false) { }

    public override ResourceConfigurer SetDropInfo(DropInfo dropInfo) =>
        this;
}
