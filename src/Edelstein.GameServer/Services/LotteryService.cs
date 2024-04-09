using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Repositories;

using OneOf;

namespace Edelstein.GameServer.Services;

public class LotteryService : ILotteryService
{
    private readonly ISequenceRepository<ulong> _sequenceRepository;

    public LotteryService(ISequenceRepository<ulong> sequenceRepository) =>
        _sequenceRepository = sequenceRepository;

    public Task<Lottery> GetTutorialLotteryByMasterCharacterId(uint masterCharacterId)
    {
        BandCategory group = (BandCategory)(masterCharacterId / 1000);

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        uint masterLotteryId = group switch
        {
            BandCategory.Muse => 0,
            BandCategory.Aqours => 9,
            BandCategory.Nijigaku => 9 + 9,
            BandCategory.Liella => 9 + 9 + 12,
            _ => throw new ArgumentException()
        };

        masterLotteryId += 9110000 + masterCharacterId % 100;

        return Task.FromResult(new Lottery
        {
            MasterLotteryId = masterLotteryId,
            MasterLotteryPriceNumber = 1,
            Count = 0,
            DailyCount = 0
        });
    }

    public async Task<OneOf<LotteryDrawResult, TutorialLotteryDrawResult>> Draw(Lottery lottery)
    {
        if (IsTutorial(lottery))
            return await DrawTutorialLottery(lottery);

        // HACK: Dummy
        return new LotteryDrawResult(null!, null!);
    }

    private async Task<TutorialLotteryDrawResult> DrawTutorialLottery(Lottery lottery)
    {
        // HACK: Dummy data for Ren Hazuki

        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        List<LotteryItem> lotteryItems =
        [
            new LotteryItem
            {
                MasterLotteryItemId = 100001,
                MasterLotteryItemNumber = 70,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 100001,
                MasterLotteryItemNumber = 49,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 200001,
                MasterLotteryItemNumber = 48,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 200001,
                MasterLotteryItemNumber = 14,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 300001,
                MasterLotteryItemNumber = 13,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 100001,
                MasterLotteryItemNumber = 125,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 100001,
                MasterLotteryItemNumber = 111,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 200001,
                MasterLotteryItemNumber = 16,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 100001,
                MasterLotteryItemNumber = 58,
                IsNew = 1
            },
            new LotteryItem
            {
                MasterLotteryItemId = 911003501,
                MasterLotteryItemNumber = 1,
                IsNew = 1
            }
        ];

        // TODO: Note: All other cards are from other groups (can even be UR, check mst)
        // TODO: Note: UR favorite character card is always last

        ulong[] cardIdRange = (await _sequenceRepository.GetNextRangeById(SequenceNames.CardIds, 10)).ToArray();

        List<Card> cards =
        [
            new Card
            {
                Id = cardIdRange[0],
                MasterCardId = 10080007,
                CreatedDateTime = currentTimestamp
            },
            new Card
            {
                Id = cardIdRange[1],
                MasterCardId = 10060004,
                CreatedDateTime = currentTimestamp
            },
            new Card
            {
                Id = cardIdRange[2],
                MasterCardId = 20070010,
                CreatedDateTime = currentTimestamp
            },
            new Card
            {
                Id = cardIdRange[3],
                MasterCardId = 10050011,
                CreatedDateTime = currentTimestamp
            },
            new Card
            {
                Id = cardIdRange[4],
                MasterCardId = 20040011,
                CreatedDateTime = currentTimestamp
            },
            new Card
            {
                Id = cardIdRange[5],
                MasterCardId = 20070002,
                CreatedDateTime = currentTimestamp
            },
            new Card
            {
                Id = cardIdRange[6],
                MasterCardId = 20050002,
                CreatedDateTime = currentTimestamp
            },
            new Card
            {
                Id = cardIdRange[7],
                MasterCardId = 10060010,
                CreatedDateTime = currentTimestamp
            },
            new Card
            {
                Id = cardIdRange[8],
                MasterCardId = 10070004,
                CreatedDateTime = currentTimestamp
            },
            new Card // UR card
            {
                Id = cardIdRange[9],
                MasterCardId = 40050007,
                CreatedDateTime = currentTimestamp
            }
        ];

        return new TutorialLotteryDrawResult(lotteryItems, cards, 40050007, cardIdRange[9]);
    }

    private static bool IsTutorial(Lottery lottery) =>
        lottery.MasterLotteryId > 9110000;
}
