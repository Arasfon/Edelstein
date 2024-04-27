using Edelstein.Data.Models;

using MongoDB.Driver;

namespace Edelstein.Server.Repositories;

public interface IUserGiftsRepository
{
    public Task AddGift(ulong xuid, Gift gift);
    public Task AddGifts(ulong xuid, List<Gift> gift);

    public Task AddGift(IClientSessionHandle session, ulong xuid, Gift gift);
    public Task AddGifts(IClientSessionHandle session, ulong xuid, List<Gift> gift);

    public Task ClaimGift(ulong xuid, Gift gift);
    public Task ClaimAll(ulong xuid);

    public Task<IEnumerable<Gift>> GetAllByXuid(ulong xuid, long? currentTimestamp = null);
}
