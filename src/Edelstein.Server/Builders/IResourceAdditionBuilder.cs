using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Builders;

public interface IResourceAdditionBuilder
{
    public ResourceConfigurer AddCoinPoints(int amount);
    public ResourceConfigurer AddPoints(PointType type, int amount);

    public ResourceConfigurer AddFreeGems(int amount);
    public ResourceConfigurer AddPaidGems(int amount);

    public ResourceConfigurer AddItem(uint itemId, int amount, long? expirationTimestamp = null);
    public DeferredItemResourceConfigurer AddItemDeferred(uint itemId, int amount, long? expirationTimestamp = null);

    public ResourceConfigurer AddCard(uint masterCardId, Rarity rarity);
    public DeferredCardResourceConfigurer AddCardDeferred(uint masterCardId, Rarity rarity);
    public ResourceConfigurer AddCard(CardMst cardMst);
    public DeferredCardResourceConfigurer AddCardDeferred(CardMst cardMst);

    public ResourceConfigurer AddChatStamp(uint chatStampId);
    public DeferredResourceConfigurer AddChatStampDeferred(uint chatStampId);

    public void AddCoinPointsAsGift(int amount, long? expirationTimestamp = null);
    public void AddPointsAsGift(PointType type, int amount, long? expirationTimestamp = null);

    public void AddGemsAsGift(int amount, long? expirationTimestamp = null);

    public void AddItemAsGift(uint itemId, int amount, long? expirationTimestamp = null);

    public void AddCardAsGift(uint cardId, long? expirationTimestamp = null);

    public ResourceConfigurer FinishDeferred(DeferredResourceConfigurer deferredResourceConfigurer);

    public ResourcesModificationResult Build();
}
