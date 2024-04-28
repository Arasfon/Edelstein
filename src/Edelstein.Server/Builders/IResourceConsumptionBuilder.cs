using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Builders;

public interface IResourceConsumptionBuilder
{
    public bool TryDistributeConsumeGems(int amount);
    public bool TryConsumeFreeGems(int amount);
    public bool TryConsumePaidGems(int amount);
    public bool TryConsumeItems(uint itemId, int amount);

    public bool TryConsumePoints(PointType type, int amount);

    public bool TryConsume(ConsumeType consumeType, uint itemId, int amount);

    public ResourcesModificationResult Build();
}
