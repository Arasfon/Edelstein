using Edelstein.Data.Models;

namespace Edelstein.Server.Repositories;

public interface IUserInitializationDataRepository
{
    public Task<UserInitializationData> GetByXuid(ulong xuid);

    public Task CreateForCharacter(ulong xuid, uint favoriteCharacterMasterId);
    public Task UpdateWithDrawedCard(ulong xuid, uint favoriteCharacterMasterCardId, ulong favoriteCharacterCardId);
    public Task Delete(ulong xuid);
}
