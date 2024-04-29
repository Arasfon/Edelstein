using Edelstein.Data.Models;

namespace Edelstein.Server.Repositories;

public interface IUserGiftsRepository
{
    public Task AddGift(Gift gift);
    public Task AddGifts(IEnumerable<Gift> gifts);

    public Task MarkAsClaimed(ulong xuid, IEnumerable<ulong> giftIds, long? currentTimestamp = null);

    public Task<IEnumerable<Gift>> GetAllByXuid(ulong xuid, long? currentTimestamp = null);
    public Task<List<Gift>> GetManyByIds(IEnumerable<ulong> ids, long? currentTimestamp = null);
}
