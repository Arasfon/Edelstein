using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Builders;

public interface IResourceAdditionBuilder
{
    public ResourceConfigurer AddCoinPoints(int amount, bool hidden = false);
    public ResourceConfigurer AddPoints(PointType type, int amount, bool hidden = false);

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

    public void AddGemsAsGift(string reason, int amount, long? expirationTimestamp = null);

    public void AddCoinPointsAsGift(string reason, int amount, long? expirationTimestamp = null);
    public void AddPointsAsGift(string reason, PointType type, int amount, long? expirationTimestamp = null);

    public void AddItemAsGift(string reason, uint itemId, int amount, long? expirationTimestamp = null);
    public void AddCardAsGift(string reason, uint cardId, long? expirationTimestamp = null);

    public void AddGift(string reason, RewardType type, uint itemId, int amount, long? expirationTimestamp = null);

    public ResourceConfigurer FinishDeferred(DeferredResourceConfigurer deferredResourceConfigurer);

    public ResourcesModificationResult Build();
}
