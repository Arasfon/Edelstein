using Edelstein.Data.Models.Components;
using Edelstein.Server.Models;

namespace Edelstein.Server.Services;

public interface IDefaultGroupCardsFactoryService
{
    public Task<DefaultCardRetrievalResult> GetOrCreate(BandCategory group, LinkedList<Card> existingCards);
}
