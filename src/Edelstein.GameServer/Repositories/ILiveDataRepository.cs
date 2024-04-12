using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Repositories;

public interface ILiveDataRepository
{
    public Task<UserData> UpdateAfterLive(ulong xuid, Live updatedLive, List<Point> updatedPoints, List<Item> updatedItems,
        int staminaChange, int experienceChange, int gemChange, List<Character> updatedCharacters, List<Gift> newGifts,
        LiveMission liveMission, List<uint> clearedMissionIds, List<EventPoint> eventPointUpdates, List<Reward> eventPointRewards,
        RankingChange? rankingChange = null, EventMember? eventMember = null, EventRankingData? eventRankingData = null);
}
