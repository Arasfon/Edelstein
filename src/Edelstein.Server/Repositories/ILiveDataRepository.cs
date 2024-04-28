using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;

namespace Edelstein.Server.Repositories;

public interface ILiveDataRepository
{
    public Task<UserData> UpdateAfterFinishedLive(ulong xuid, long currentTimestamp, List<Live> lives,
        LinkedList<Point> points, LinkedList<Item> items, Stamina stamina, int experience, Gem gem,
        List<Character> characters, List<LiveMission> liveMissions, HashSet<uint> newStampIds, LinkedList<Gift> gifts);
}
