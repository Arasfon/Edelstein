using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Repositories;

namespace Edelstein.GameServer.Services;

public class DefaultGroupCardsFactoryService : IDefaultGroupCardsFactoryService
{
    private readonly ISequenceRepository<ulong> _sequenceRepository;

    public DefaultGroupCardsFactoryService(ISequenceRepository<ulong> sequenceRepository) =>
        _sequenceRepository = sequenceRepository;

    public async Task<List<Card>> Create(BandCategory group)
    {
        switch (group)
        {
            case BandCategory.Liella:
            {
                ulong[] cardIdRange = (await _sequenceRepository.GetNextRangeById(SequenceNames.CardIds, 9)).ToArray();

                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                List<Card> groupCards =
                [
                    new Card
                    {
                        Id = cardIdRange[0],
                        MasterCardId = 40010001,
                        CreatedDateTime = currentTimestamp
                    },
                    new Card
                    {
                        Id = cardIdRange[1],
                        MasterCardId = 40020001,
                        CreatedDateTime = currentTimestamp
                    },
                    new Card
                    {
                        Id = cardIdRange[2],
                        MasterCardId = 40030001,
                        CreatedDateTime = currentTimestamp
                    },
                    new Card
                    {
                        Id = cardIdRange[3],
                        MasterCardId = 40040001,
                        CreatedDateTime = currentTimestamp
                    },
                    new Card
                    {
                        Id = cardIdRange[4],
                        MasterCardId = 40050001,
                        CreatedDateTime = currentTimestamp
                    },
                    new Card
                    {
                        Id = cardIdRange[5],
                        MasterCardId = 40060001,
                        CreatedDateTime = currentTimestamp
                    },
                    new Card
                    {
                        Id = cardIdRange[6],
                        MasterCardId = 40070001,
                        CreatedDateTime = currentTimestamp
                    },
                    new Card
                    {
                        Id = cardIdRange[7],
                        MasterCardId = 40080001,
                        CreatedDateTime = currentTimestamp
                    },
                    new Card
                    {
                        Id = cardIdRange[8],
                        MasterCardId = 40090001,
                        CreatedDateTime = currentTimestamp
                    }
                ];

                return groupCards;
            }
            default:
                throw new NotImplementedException();
        }
    }
}
