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
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        uint[] groupCardsMasterIds = group switch
        {
            BandCategory.Muse => [10010001, 10020001, 10030001, 10040001, 10050001, 10060001, 10070001, 10080001, 10090001],
            BandCategory.Aqours => [20010001, 20020001, 20030001, 20040001, 20050001, 20060001, 20070001, 20080001, 20090001],
            BandCategory.Nijigaku =>
            [
                30010001, 30020001, 30030001, 30040001, 30050001, 30060001, 30070001, 30080001, 30090001, 30100001,
                30110001
            ],
            BandCategory.Liella => [40010001, 40020001, 40030001, 40040001, 40050001, 40060001, 40070001, 40080001, 40090001],
            _ => []
        };

        ulong[] cardIdRange =
            (await _sequenceRepository.GetNextRangeById(SequenceNames.CardIds, (ulong)groupCardsMasterIds.Length)).ToArray();

        return groupCardsMasterIds.Select((x, i) => new Card
            {
                Id = cardIdRange[i],
                MasterCardId = x,
                CreatedDateTime = currentTimestamp
            })
            .ToList();
    }
}
