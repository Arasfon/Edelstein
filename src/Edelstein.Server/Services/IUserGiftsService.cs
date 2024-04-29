using Edelstein.Data.Models;
using Edelstein.Server.Models;

namespace Edelstein.Server.Services;

public interface IUserGiftsService
{
    public Task<IEnumerable<Gift>> GetAllGifts(ulong xuid);

    public Task<GiftClaimResult> ClaimGifts(ulong xuid, HashSet<ulong> giftIds);
}
