using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;

namespace Edelstein.Server.Builders;

public interface IResourceAdditionBuilder
{
    public ResourceConfigurer AddCoinPoints(int amount, bool hidden = false, bool preventGiftConversionOnLimit = false);
    public ResourceConfigurer AddPoints(PointType type, int amount, bool hidden = false, bool preventGiftConversionOnLimit = false);

    public ResourceConfigurer AddFreeGems(int amount, bool preventGiftConversionOnLimit = false);
    public ResourceConfigurer AddPaidGems(int amount, bool preventGiftConversionOnLimit = false);

    public ResourceConfigurer AddItem(uint itemId, int amount, long? expirationTimestamp = null, bool preventGiftConversionOnLimit = false);
    public DeferredItemResourceConfigurer AddItemDeferred(uint itemId, int amount, long? expirationTimestamp = null);

    public ResourceConfigurer AddCard(uint masterCardId, Rarity rarity, bool preventGiftConversionOnLimit = false);
    public DeferredCardResourceConfigurer AddCardDeferred(uint masterCardId, Rarity rarity);
    public ResourceConfigurer AddCard(CardMst cardMst, bool preventGiftConversionOnLimit = false);
    public DeferredCardResourceConfigurer AddCardDeferred(CardMst cardMst);

    public ResourceConfigurer AddChatStamp(uint chatStampId);
    public DeferredResourceConfigurer AddChatStampDeferred(uint chatStampId);

    public ResourceConfigurer Add(RewardType rewardType, uint itemId, int amount, Rarity? cardRarity = null,
        long? itemExpirationTimestamp = null, bool preventGiftConversionOnLimit = false);

    public ResourceConfigurer AddGemsAsGift(string reason, int amount, long? expirationTimestamp = null);

    public ResourceConfigurer AddCoinPointsAsGift(string reason, int amount, long? expirationTimestamp = null);
    public ResourceConfigurer AddPointsAsGift(string reason, PointType type, int amount, long? expirationTimestamp = null);

    public ResourceConfigurer AddItemAsGift(string reason, uint itemId, int amount, long? expirationTimestamp = null);
    public ResourceConfigurer AddCardAsGift(string reason, uint cardId, long? expirationTimestamp = null);

    public ResourceConfigurer AddGift(string reason, RewardType type, uint itemId, int amount, long? expirationTimestamp = null,
        bool isGiftConversion = false);

    public ResourceConfigurer ClaimGift(Gift gift, Rarity? cardRarity = null, long? itemExpirationTimestamp = null);

    public ResourceConfigurer FinishDeferred(DeferredResourceConfigurer deferredResourceConfigurer);

    public ResourcesModificationResult Build();
}
