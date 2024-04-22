using Edelstein.Data.Models.Components;
using Edelstein.GameServer.Models;

namespace Edelstein.GameServer.Services;

public interface IDefaultGroupCardsFactoryService
{
    public Task<DefaultCardRetrievalResult> GetOrCreate(BandCategory group, List<Card> existingCards);
}
