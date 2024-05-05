using Edelstein.Data.Models;
using Edelstein.Server.Models;

namespace Edelstein.Server.Services;

public interface IUserGiftsService
{
    public Task<IEnumerable<Gift>> GetAllGifts(ulong xuid);

    /// <summary>
    /// Add gifts to the user assigning <see cref="Gift.UserId"/> and creating <see cref="Gift.Id"/>
    /// </summary>
    /// <param name="xuid">Xiud of a user</param>
    /// <param name="gifts">Gifts to be added</param>
    public Task AddGifts(ulong xuid, LinkedList<Gift> gifts);

    public Task<GiftClaimResult> ClaimGifts(ulong xuid, HashSet<ulong> giftIds);
}
