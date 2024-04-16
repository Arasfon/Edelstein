using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Services;

public interface IDefaultGroupCardsFactoryService
{
    public Task<List<Card>> GetOrCreate(BandCategory group, List<Card> existingCards);
}
