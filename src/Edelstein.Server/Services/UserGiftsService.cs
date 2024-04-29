using Edelstein.Data.Models;
using Edelstein.Data.Msts;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Server.Builders;
using Edelstein.Server.Models;
using Edelstein.Server.Repositories;

using Microsoft.EntityFrameworkCore;

using System.Globalization;

namespace Edelstein.Server.Services;

public class UserGiftsService : IUserGiftsService
{
    private readonly IUserGiftsRepository _userGiftsRepository;
    private readonly IUserService _userService;
    private readonly MstDbContext _mstDbContext;

    public UserGiftsService(IUserGiftsRepository userGiftsRepository, IUserService userService, MstDbContext mstDbContext)
    {
        _userGiftsRepository = userGiftsRepository;
        _userService = userService;
        _mstDbContext = mstDbContext;
    }

    public async Task<IEnumerable<Gift>> GetAllGifts(ulong xuid) =>
        await _userGiftsRepository.GetAllByXuid(xuid);

    public async Task<GiftClaimResult> ClaimGifts(ulong xuid, HashSet<ulong> giftIds)
    {
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        LinkedList<ulong> failedGiftIds = [];

        UserData userData = (await _userService.GetUserDataByXuid(xuid))!;

        ResourceAdditionBuilder resourceAdditionBuilder = new(userData, currentTimestamp, true);

        List<Gift> gifts = await _userGiftsRepository.GetManyByIds(giftIds, currentTimestamp);

        List<uint> cardGiftsCardIds = gifts.Where(x => x.RewardType == RewardType.Card).Select(x => x.Value).ToList();
        Dictionary<uint, CardMst> cardMsts =
            await _mstDbContext.CardMsts.Where(x => cardGiftsCardIds.Contains(x.Id)).Distinct().ToDictionaryAsync(x => x.Id);

        List<uint> itemGiftsItemIds = gifts.Where(x => x.RewardType == RewardType.Item).Select(x => x.Value).ToList();
        Dictionary<uint, ItemMst> itemMsts =
            await _mstDbContext.ItemMsts.Where(x => itemGiftsItemIds.Contains(x.Id)).Distinct().ToDictionaryAsync(x => x.Id);

        foreach (Gift gift in gifts)
        {
            ResourceConfigurer resourceConfigurer;

            switch (gift.RewardType)
            {
                case RewardType.Card:
                {
                    resourceConfigurer = resourceAdditionBuilder.ClaimGift(gift, cardMsts[gift.Value].Rarity);
                    break;
                }
                case RewardType.Item:
                {
                    string expireDateString = itemMsts[gift.Value].ExpireDate;
                    long? expirationTimestamp = null;

                    if (!String.IsNullOrEmpty(expireDateString))
                    {
                        expirationTimestamp = DateTimeOffset.ParseExact(expireDateString, "yyyy-MM-dd HH:mm:ss",
                                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal)
                            .ToUnixTimeSeconds();
                    }

                    resourceConfigurer = resourceAdditionBuilder.ClaimGift(gift, itemExpirationTimestamp: expirationTimestamp);
                    break;
                }
                default:
                {
                    resourceConfigurer = resourceAdditionBuilder.ClaimGift(gift);
                    break;
                }
            }

            resourceConfigurer = resourceConfigurer.SetGiveType(GiveType.Direct);

            if (resourceConfigurer is not { IsAdded: true, IsResourceConvertedToGift: false })
            {
                failedGiftIds.AddLast(gift.Id);
                giftIds.Remove(gift.Id);
            }
        }

        await _userGiftsRepository.MarkAsClaimed(xuid, giftIds, currentTimestamp);

        ResourcesModificationResult resourcesModificationResult = resourceAdditionBuilder.Build();

        return new GiftClaimResult(failedGiftIds, resourcesModificationResult.Updates, resourcesModificationResult.Rewards ?? []);
    }
}
