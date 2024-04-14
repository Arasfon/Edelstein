using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.GameServer.Extensions;
using Edelstein.GameServer.Models;
using Edelstein.GameServer.Models.Endpoints.Live;
using Edelstein.GameServer.Repositories;

using System.Collections.Concurrent;
using System.Diagnostics;

namespace Edelstein.GameServer.Services;

public class LiveService : ILiveService
{
    // TODO: Replace with cache (invalidation after e.g. 15 minutes)
    private static readonly ConcurrentDictionary<ulong, (LiveStartRequestData Data, List<Character> DeckCharacters, UserData UserData)>
        CurrentLives = [];

    private readonly ILiveDataRepository _liveDataRepository;
    private readonly IUserDataRepository _userDataRepository;

    public LiveService(ILiveDataRepository liveDataRepository, IUserDataRepository userDataRepository)
    {
        _liveDataRepository = liveDataRepository;
        _userDataRepository = userDataRepository;
    }

    public async Task StartLive(ulong xuid, LiveStartRequestData liveStartData)
    {
        UserData? userData = await _userDataRepository.GetByXuid(xuid);

        List<Character> characters = await _userDataRepository.GetDeckCharactersFromUserData(userData, liveStartData.DeckSlot);

        CurrentLives[xuid] = (liveStartData, characters, userData!);
    }

    public Task RetireLive(ulong xuid, LiveRetireRequestData liveRetireData)
    {
        CurrentLives.TryRemove(xuid, out _);
        return Task.CompletedTask;
    }

    public async Task<LiveFinishResult> FinishLive(ulong xuid, LiveEndRequestData liveFinishData)
    {
        bool removed = CurrentLives.TryRemove(xuid,
            out (LiveStartRequestData Data, List<Character> DeckCharacters, UserData UserData) currentLiveData);

        Debug.Assert(removed);

        (LiveStartRequestData data, List<Character> deckCharacters, UserData userData) = currentLiveData;

        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        List<Item> items = [];
        List<Reward> rewards = [];
        List<Gift> gifts = [];
        List<Point> points = []; // TODO: Use user's point list

        int multiplier = (int)liveFinishData.UseLp / 10;

        // Simple values and their initialization
        int staminaChange = -(int)liveFinishData.UseLp;
        int coinChange = 750 * multiplier;
        int expChange = 10 * multiplier;
        int gemChange = 0;

        // Live
        Live updatedLive = userData.LiveList.FirstOrDefault(x => x.MasterLiveId == liveFinishData.MasterLiveId) ?? new Live
        {
            MasterLiveId = liveFinishData.MasterLiveId,
            Level = liveFinishData.Level,
            AutoEnable = true
        };

        updatedLive.ClearCount++;
        updatedLive.HighScore = Math.Max(updatedLive.HighScore, liveFinishData.LiveScore.Score);
        updatedLive.MaxCombo = Math.Max(updatedLive.MaxCombo, liveFinishData.LiveScore.MaxCombo);
        updatedLive.UpdatedTime = (int)currentTimestamp;

        // TODO: Use msts
        // First completion reward
        if (updatedLive.ClearCount == 1)
        {
            gemChange += 60;
            rewards.Add(new Reward
            {
                Type = RewardType.Gem,
                Value = 60,
                Amount = gemChange,
                DropInfo = new DropInfo
                {
                    GetableCount = 1,
                    FirstReward = 1,
                    RemainingGetableCount = 0
                }
            });
        }

        // TODO: Guaranteed reward

        // TODO: Random reward

        // Live missions
        // TODO: Check for completed LIVE missions (msts)
        LiveMission liveMission = userData.LiveMissionList.FirstOrDefault(x => x.MasterLiveId == liveFinishData.MasterLiveId) ??
            new LiveMission
            {
                MasterLiveId = liveFinishData.MasterLiveId,
                ClearMasterLiveMissionIds = []
            };
        // e.g. liveMission.ClearMasterLiveMissionIds.Add(x);
        // e.g. coinChange += x;

        // Missions
        // TODO: Check for completed NORMAL missions (mission service? should be quite big...)
        List<uint> clearedMissionIds = [];
        // e.g. coinChange += x;
        // TODO: Add gifts here

        // Character experience
        foreach (Character character in deckCharacters[..2].Concat(deckCharacters[^2..]))
        {
            character.BeforeExp = character.Exp;
            character.Exp += 1 * (ulong)multiplier;
        }

        foreach (Character character in deckCharacters[2..4].Concat(deckCharacters[^4..^2]))
        {
            character.BeforeExp = character.Exp;
            character.Exp += 2 * (ulong)multiplier;
        }

        deckCharacters[4].BeforeExp = deckCharacters[4].Exp;
        deckCharacters[4].Exp += 5 * (ulong)multiplier;

        // TODO: Calculate events

        // Last preparations to store
        Point updatedCoinsPoint = userData.PointList.FirstOrDefault(x => x.Type == PointType.Coin) ?? new Point
        {
            Type = PointType.Coin,
            Amount = 0
        };
        updatedCoinsPoint.Amount += coinChange;
        // TODO: Add or replace point
        points.Add(updatedCoinsPoint);

        // TODO: In tutorial ignore character exp & first clear reward (if failed?), events

        UserData responseUserData = await _liveDataRepository.UpdateAfterLive(xuid, updatedLive, points, items, staminaChange,
            expChange, gemChange, userData.CharacterList.ExceptBy(deckCharacters, x => x.MasterCharacterId).Concat(deckCharacters).ToList(),
            gifts, liveMission,
            clearedMissionIds, [], []);

        return new LiveFinishResult
        {
            ChangedGem = responseUserData.Gem,
            ChangedItems = items,
            ChangedPoints = responseUserData.PointList,
            PreviousLiveData = updatedLive,
            ClearedMasterLiveMissionIds = liveMission.ClearMasterLiveMissionIds,
            UpdatedUserData = responseUserData,
            DeckCharacters = deckCharacters,
            Rewards = rewards,
            NewGifts = gifts,
            ClearedMissionIds = clearedMissionIds,
            EventPointRewards = [],
            RankingChange = null!,
            EventMember = null,
            EventRankingData = null!
        };
    }
}
