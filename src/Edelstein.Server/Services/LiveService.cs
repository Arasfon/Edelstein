using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Server.Builders;
using Edelstein.Server.Models;
using Edelstein.Server.Models.Endpoints.Live;
using Edelstein.Server.Random;
using Edelstein.Server.Repositories;

using Microsoft.EntityFrameworkCore;

using System.Diagnostics.CodeAnalysis;

namespace Edelstein.Server.Services;

public class LiveService : ILiveService
{
    private readonly ILiveDataRepository _liveDataRepository;
    private readonly IUserDataRepository _userDataRepository;
    private readonly ITutorialService _tutorialService;
    private readonly MstDbContext _mstDbContext;
    private readonly IUserService _userService;
    private readonly ISequenceRepository<ulong> _sequenceRepository;

    public LiveService(ILiveDataRepository liveDataRepository, IUserDataRepository userDataRepository, ITutorialService tutorialService,
        MstDbContext mstDbContext, IUserService userService, ISequenceRepository<ulong> sequenceRepository)
    {
        _liveDataRepository = liveDataRepository;
        _userDataRepository = userDataRepository;
        _tutorialService = tutorialService;
        _mstDbContext = mstDbContext;
        _userService = userService;
        _sequenceRepository = sequenceRepository;
    }

    public async Task StartLive(ulong xuid, LiveStartRequestData liveStartData) =>
        await _userDataRepository.SetCurrentLiveData(xuid, liveStartData.ToCurrentLiveData());

    // TODO: Add global action filter that will retire live automatically if request other than live/retire, live/end, live/continue is received
    // How should it check for live? resort back to cache with e.g. 15 min timeout? make it complement current storage technique to reduce database queries! that will allow finishing live after inadequate retry amount
    public async Task RetireLive(ulong xuid, LiveRetireRequestData liveRetireData) =>
        await _userDataRepository.SetCurrentLiveData(xuid, null);

    public async Task<LiveFinishResult> SkipLive(ulong xuid, LiveSkipRequestData liveSkipData)
    {
        UserData userData = (await _userDataRepository.GetByXuid(xuid))!;

        Deck? deck = userData.DeckList.FirstOrDefault(x => x.Slot == liveSkipData.DeckSlot);

        if (deck is null || deck.MainCardIds.Contains(0))
            throw new Exception("Invalid deck");

        List<Character> deckCharacters = await _userService.GetDeckCharactersFromUserData(userData, deck);

        DateTimeOffset currentDateTimeOffset = DateTimeOffset.UtcNow;
        long currentTimestamp = currentDateTimeOffset.ToUnixTimeSeconds();

        // Simple values and their initialization
        int multiplier = liveSkipData.LiveBoost;

        int staminaDecrease = 10 * multiplier;
        int expChange = 10 * multiplier;
        int nextUserExp = userData.User.Exp + expChange;

        // Calculate stamina
        (bool isEnoughStamina, Stamina updatedStamina) = await CalculateStamina(userData.User.Exp,
            nextUserExp, userData.Stamina, staminaDecrease, currentTimestamp);

        if (!isEnoughStamina)
            return new LiveFinishResult(LiveFinishResultStatus.NotEnoughResources);

        // Consume tickets
        ResourceConsumptionBuilder resourceConsumptionBuilder = new(userData, currentTimestamp);

        // Charge skip tickets
        const uint ticketItemId = 21000001;
        if (!resourceConsumptionBuilder.TryConsumeItems(ticketItemId, liveSkipData.LiveBoost))
            return new LiveFinishResult(LiveFinishResultStatus.NotEnoughResources);

        // Create resource addition builder
        ResourceAdditionBuilder resourceAdditionBuilder =
            resourceConsumptionBuilder.ToResourceAdditionBuilder(userData, true);

        // Update coins
        resourceAdditionBuilder.AddCoinPoints(750 * multiplier, true);

        // Guaranteed reward
        const uint guaranteedItemId = 16005001;
        int guaranteedItemAmount = 5 * multiplier;
        resourceAdditionBuilder.AddItem(guaranteedItemId, guaranteedItemAmount);

        // Live
        Live updatedLive = UpdateLive();

        // Add random rewards
        await AddRandomRewards();

        // Live missions
        HashSet<uint> newCompletedLiveMissions = [];

        LiveMission? storedliveMissionData = userData.LiveMissionList.FirstOrDefault(x => x.MasterLiveId == liveSkipData.MasterLiveId);

        if (storedliveMissionData is null)
        {
            storedliveMissionData = new LiveMission { MasterLiveId = liveSkipData.MasterLiveId };

            userData.LiveMissionList.Add(storedliveMissionData);
        }

        await AddLiveMissionRewards();

        // Character experience
        UpdateCharacterExperience();

        // Build modifications
        ResourcesModificationResult resourcesModificationResult = resourceAdditionBuilder.Build();

        UserData resultUserData =
            await UpdateUserDataAfterLiveCreatingGiftIds(xuid, currentTimestamp,
                userData.LiveList, resourcesModificationResult.Points, resourcesModificationResult.Items,
                updatedStamina, nextUserExp, resourcesModificationResult.Gem ?? userData.Gem,
                userData.CharacterList, userData.LiveMissionList, resourcesModificationResult.Updates.MasterStampIds,
                resourcesModificationResult.Gifts ?? []);

        return new LiveFinishResult
        {
            Status = LiveFinishResultStatus.Success,
            ChangedGem = resourcesModificationResult.Gem,
            ChangedItems = resourcesModificationResult.Updates.ItemList,
            ChangedPoints = resourcesModificationResult.Updates.PointList,
            FinishedLiveData = updatedLive,
            ClearedMasterLiveMissionIds = newCompletedLiveMissions,
            UpdatedUserData = resultUserData,
            UpdatedCharacters = deckCharacters,
            Rewards = resourcesModificationResult.Rewards ?? [],
            Gifts = resourcesModificationResult.Gifts ?? [],
            ClearedMissionIds = [],
            EventPointRewards = [],
            RankingChange = null!,
            EventMember = null,
            EventRankingData = null!
        };

        Live UpdateLive()
        {
            Live? live = userData.LiveList.FirstOrDefault(x => x.MasterLiveId == liveSkipData.MasterLiveId);

            if (live is null)
                throw new Exception("Skip is not available before initial live completion");

            live.ClearCount += liveSkipData.LiveBoost;
            live.UpdatedTime = (int)currentTimestamp;

            return live;
        }

        async Task AddRandomRewards()
        {
            resourceAdditionBuilder.AddItemDeferred(19100001, BinaryRandom.NextMultipleCount(multiplier, 1, 1))
                .MakeLimited(updatedLive.LimitedRewards, 100)
                .Finish();

            resourceAdditionBuilder.AddItem(17001001, BinaryRandom.NextMultipleCount(multiplier, 1, 1));

            List<LiveClearRewardMst> liveClearRewardMsts = await _mstDbContext.LiveClearRewardMsts
                .Where(x => x.MasterLiveId == liveSkipData.MasterLiveId && x.MasterReleaseLabelId == 1)
                .ToListAsync();

            foreach (LiveClearRewardMst liveClearRewardMst in liveClearRewardMsts)
            {
                int amount = BinaryRandom.NextMultipleCount(multiplier, 1, 1);

                if (amount == 0)
                    continue;

                switch (liveClearRewardMst.Type)
                {
                    case RewardType.ChatStamp:
                    {
                        resourceAdditionBuilder.AddChatStampDeferred(liveClearRewardMst.Value)
                            .MakeLimited(updatedLive.LimitedRewards, 1)
                            .Finish();
                        break;
                    }
                    case RewardType.Item:
                    case RewardType.Gem:
                        break;
                    default:
                        // Everything else should not be possible
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        async Task AddLiveMissionRewards()
        {
            // TODO: Consider loading to consts
            List<LiveMissionMst> liveMissionsMsts = await _mstDbContext.LiveMissionMsts.ToListAsync();

            Dictionary<uint, LiveMissionRewardMst> liveMissionsRewardsMsts = (await _mstDbContext.LiveMissionRewardMsts.ToListAsync())
                .ToDictionary(x => x.Id);

            CalculateClearCountMissions();

            void CalculateClearCountMissions()
            {
                List<LiveMissionMst> remainingMissions = liveMissionsMsts
                    .Where(x => x.Type == LiveMissionType.ClearCount)
                    .OrderBy(x => x.Id)
                    .ExceptBy(storedliveMissionData.ClearMasterLiveMissionIds, x => x.Id)
                    .ToList();

                if (remainingMissions.Count == 0)
                    return;

                foreach (LiveMissionMst liveMissionMst in remainingMissions)
                {
                    if (updatedLive.ClearCount < Int32.Parse(liveMissionMst.Value))
                        continue;

                    LiveMissionRewardMst rewardMst = liveMissionsRewardsMsts[liveMissionMst.MasterLiveMissionRewardId];
                    if (rewardMst.GiveType == GiveType.Gift)
                        resourceAdditionBuilder.AddGift("ライブミッション完了報酬", rewardMst.Type, rewardMst.Value, rewardMst.Amount);
                    else
                        throw new NotImplementedException();

                    storedliveMissionData.ClearMasterLiveMissionIds.Add(liveMissionMst.Id);
                    newCompletedLiveMissions.Add(liveMissionMst.Id);
                }
            }
        }

        void UpdateCharacterExperience()
        {
            Dictionary<uint, (ulong ExpIncrease, Character Character)> characterIdToExpIncreaseMap = new();

            foreach (Character character in deckCharacters[..2].Concat(deckCharacters[^2..]))
                characterIdToExpIncreaseMap[character.MasterCharacterId] = (1 * (ulong)multiplier, character);

            foreach (Character character in deckCharacters[2..4].Concat(deckCharacters[^4..^2]))
                characterIdToExpIncreaseMap[character.MasterCharacterId] = (2 * (ulong)multiplier, character);

            characterIdToExpIncreaseMap[deckCharacters[4].MasterCharacterId] = (5 * (ulong)multiplier, deckCharacters[4]);

            deckCharacters = characterIdToExpIncreaseMap.Values.Select(x =>
                {
                    x.Character.BeforeExp = x.Character.Exp;
                    x.Character.Exp += x.ExpIncrease;
                    return x.Character;
                })
                .ToList();
        }
    }

    public async Task<LiveFinishResult> FinishLive(ulong xuid, LiveEndRequestData liveFinishData)
    {
        UserData userData = await _userDataRepository.GetByXuidRemovingCurrentLiveData(xuid);

        if (userData.CurrentLive is null)
            throw new Exception("Live has not been started");

        if (userData.CurrentLive.MasterLiveId != liveFinishData.MasterLiveId)
            throw new Exception("Wrong live finished");

        if (userData.CurrentLive.Level != liveFinishData.Level)
            throw new Exception("Wrong live level finished");

        Deck? deck = userData.DeckList.FirstOrDefault(x => x.Slot == userData.CurrentLive.DeckSlot);

        if (deck is null || deck.MainCardIds.Contains(0))
            throw new Exception("Invalid deck");

        List<Character> deckCharacters = await _userService.GetDeckCharactersFromUserData(userData, deck);

        DateTimeOffset currentDateTimeOffset = DateTimeOffset.UtcNow;
        long currentTimestamp = currentDateTimeOffset.ToUnixTimeSeconds();

        bool isInTutorial = await _tutorialService.IsTutorialInProgress(xuid);

        // Simple values and their initialization
        int multiplier = userData.CurrentLive.LiveBoost;

        int staminaDecrease = isInTutorial ? 0 : (int)liveFinishData.UseLp;
        int expChange = 10 * multiplier;
        int nextUserExp = userData.User.Exp + expChange;

        // Calculate stamina
        (bool isEnoughStamina, Stamina updatedStamina) = await CalculateStamina(userData.User.Exp,
            nextUserExp, userData.Stamina, staminaDecrease, currentTimestamp);

        if (!isEnoughStamina)
            return new LiveFinishResult(LiveFinishResultStatus.NotEnoughResources);

        // Build item dictionary for efficient lookup
        ResourceAdditionBuilder resourceAdditionBuilder = new(userData, currentTimestamp, true);

        // Update coins
        resourceAdditionBuilder.AddCoinPoints(750 * multiplier, true);

        // Guaranteed reward
        const uint guaranteedItemId = 16005001;
        int guaranteedItemAmount = 5 * multiplier;
        resourceAdditionBuilder.AddItem(guaranteedItemId, guaranteedItemAmount);

        if (isInTutorial)
        {
            ResourcesModificationResult tutorialResourcesModificationResult = resourceAdditionBuilder.Build();

            UserData tutorialResultUserData =
                await UpdateUserDataAfterLiveCreatingGiftIds(xuid, currentTimestamp,
                    userData.LiveList, tutorialResourcesModificationResult.Points, tutorialResourcesModificationResult.Items,
                    updatedStamina, nextUserExp, tutorialResourcesModificationResult.Gem ?? userData.Gem,
                    userData.CharacterList, userData.LiveMissionList, tutorialResourcesModificationResult.Updates.MasterStampIds,
                    tutorialResourcesModificationResult.Gifts ?? []);

            return new LiveFinishResult
            {
                Status = LiveFinishResultStatus.Success,
                ChangedGem = tutorialResourcesModificationResult.Gem,
                ChangedItems = tutorialResourcesModificationResult.Updates.ItemList,
                ChangedPoints = tutorialResourcesModificationResult.Updates.PointList,
                FinishedLiveData = null,
                ClearedMasterLiveMissionIds = [],
                UpdatedUserData = tutorialResultUserData,
                UpdatedCharacters = deckCharacters,
                Rewards = tutorialResourcesModificationResult.Rewards ?? [],
                Gifts = tutorialResourcesModificationResult.Gifts ?? [],
                ClearedMissionIds = [],
                EventPointRewards = [],
                RankingChange = null!,
                EventMember = null,
                EventRankingData = null!
            };
        }

        // Live
        Live updatedLive = UpdateLive();

        // Guaranteed first clear gem reward
        AddFirstClearRewards();

        // Add random rewards
        await AddRandomRewards();

        // Live missions
        HashSet<uint> newCompletedLiveMissions = [];

        LiveMission? storedliveMissionData = userData.LiveMissionList.FirstOrDefault(x => x.MasterLiveId == liveFinishData.MasterLiveId);

        if (storedliveMissionData is null)
        {
            storedliveMissionData = new LiveMission { MasterLiveId = liveFinishData.MasterLiveId };

            userData.LiveMissionList.Add(storedliveMissionData);
        }

        await AddLiveMissionRewards();

        // TODO: Missions

        // TODO: Events

        // Character experience
        UpdateCharacterExperience();

        // Build modifications
        ResourcesModificationResult resourcesModificationResult = resourceAdditionBuilder.Build();

        UserData resultUserData =
            await UpdateUserDataAfterLiveCreatingGiftIds(xuid, currentTimestamp,
                userData.LiveList, resourcesModificationResult.Points, resourcesModificationResult.Items,
                updatedStamina, nextUserExp, resourcesModificationResult.Gem ?? userData.Gem,
                userData.CharacterList, userData.LiveMissionList, resourcesModificationResult.Updates.MasterStampIds,
                resourcesModificationResult.Gifts ?? []);

        return new LiveFinishResult
        {
            Status = LiveFinishResultStatus.Success,
            ChangedGem = resourcesModificationResult.Gem,
            ChangedItems = resourcesModificationResult.Updates.ItemList,
            ChangedPoints = resourcesModificationResult.Updates.PointList,
            FinishedLiveData = updatedLive,
            ClearedMasterLiveMissionIds = newCompletedLiveMissions,
            UpdatedUserData = resultUserData,
            UpdatedCharacters = deckCharacters,
            Rewards = resourcesModificationResult.Rewards ?? [],
            Gifts = resourcesModificationResult.Gifts ?? [],
            ClearedMissionIds = [],
            EventPointRewards = [],
            RankingChange = null!,
            EventMember = null,
            EventRankingData = null!
        };

        Live UpdateLive()
        {
            Live? live = userData.LiveList.FirstOrDefault(x => x.MasterLiveId == liveFinishData.MasterLiveId);

            if (live is null)
            {
                live = new Live
                {
                    MasterLiveId = liveFinishData.MasterLiveId,
                    Level = liveFinishData.Level,
                    AutoEnable = true
                };

                userData.LiveList.Add(live);
            }

            live.ClearCount++;
            live.HighScore = Math.Max(live.HighScore, liveFinishData.LiveScore.Score);
            live.MaxCombo = Math.Max(live.MaxCombo, liveFinishData.LiveScore.MaxCombo);
            live.UpdatedTime = (int)currentTimestamp;

            return live;
        }

        async Task AddRandomRewards()
        {
            resourceAdditionBuilder.AddItemDeferred(19100001, BinaryRandom.NextMultipleCount(multiplier, 1, 1))
                .MakeLimited(updatedLive.LimitedRewards, 100)
                .Finish();

            resourceAdditionBuilder.AddItem(17001001, BinaryRandom.NextMultipleCount(multiplier, 1, 1));

            List<LiveClearRewardMst> liveClearRewardMsts = await _mstDbContext.LiveClearRewardMsts
                .Where(x => x.MasterLiveId == liveFinishData.MasterLiveId && x.MasterReleaseLabelId == 1)
                .ToListAsync();

            foreach (LiveClearRewardMst liveClearRewardMst in liveClearRewardMsts)
            {
                int amount = BinaryRandom.NextMultipleCount(multiplier, 1, 1);

                if (amount == 0)
                    continue;

                switch (liveClearRewardMst.Type)
                {
                    case RewardType.ChatStamp:
                    {
                        resourceAdditionBuilder.AddChatStampDeferred(liveClearRewardMst.Value)
                            .MakeLimited(updatedLive.LimitedRewards, 1)
                            .Finish();
                        break;
                    }
                    case RewardType.Item:
                    case RewardType.Gem:
                        break;
                    default:
                        // Everything else should not be possible
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        void AddFirstClearRewards()
        {
            if (updatedLive.ClearCount != 1)
                return;

            resourceAdditionBuilder.AddFreeGems(60)
                .SetDropInfo(new DropInfo
                {
                    GetableCount = 1,
                    FirstReward = 1,
                    RemainingGetableCount = 0
                });
        }

        async Task AddLiveMissionRewards()
        {
            // TODO: Consider loading to consts
            List<LiveMissionMst> liveMissionsMsts = await _mstDbContext.LiveMissionMsts.ToListAsync();

            Dictionary<uint, LiveMissionRewardMst> liveMissionsRewardsMsts = (await _mstDbContext.LiveMissionRewardMsts.ToListAsync())
                .ToDictionary(x => x.Id);

            IQueryable<JoinedLiveMst> liveMstQuery = from liveMstQ in _mstDbContext.LiveMsts
                join missionComboMst in _mstDbContext.LiveMissionComboMsts on liveMstQ.MasterMusicId equals missionComboMst.MasterMusicId
                join musicLevelMst in _mstDbContext.MusicLevelMsts on new
                {
                    liveMstQ.MasterMusicId,
                    liveFinishData.Level
                } equals new
                {
                    musicLevelMst.MasterMusicId,
                    musicLevelMst.Level
                }
                where liveMstQ.Id == liveFinishData.MasterLiveId
                select new JoinedLiveMst
                {
                    Id = liveMstQ.Id,
                    MasterMusicId = liveMstQ.MasterMusicId,
                    ScoreC = liveMstQ.ScoreC,
                    ScoreB = liveMstQ.ScoreB,
                    ScoreA = liveMstQ.ScoreA,
                    ScoreS = liveMstQ.ScoreS,
                    MultiScoreC = liveMstQ.MultiScoreC,
                    MultiScoreB = liveMstQ.MultiScoreB,
                    MultiScoreA = liveMstQ.MultiScoreA,
                    MultiScoreS = liveMstQ.MultiScoreS,
                    ComboMissionRequirements = missionComboMst.ValueList,
                    FullCombo = musicLevelMst.FullCombo
                };

            JoinedLiveMst liveMst = await liveMstQuery.FirstAsync();

            CalculateScoreMissions();
            CalculateMaxComboMissions();
            CalculateFullComboMissions();
            CalculateClearCountMissions();
            CalculatePerfectFullComboMissions();

            void CalculateScoreMissions()
            {
                List<LiveMissionMst> remainingMissions = liveMissionsMsts
                    .Where(x => x.Type == LiveMissionType.Score)
                    .OrderBy(x => x.Id)
                    .ExceptBy(storedliveMissionData.ClearMasterLiveMissionIds, x => x.Id)
                    .ToList();

                if (remainingMissions.Count == 0)
                    return;

                foreach (LiveMissionMst liveMissionMst in remainingMissions)
                {
                    if (liveFinishData.LiveScore.Score < liveMst.GetScoreByLetter(liveMissionMst.Value))
                        continue;

                    AddLiveMissionReward(liveMissionsRewardsMsts[liveMissionMst.MasterLiveMissionRewardId]);
                    storedliveMissionData.ClearMasterLiveMissionIds.Add(liveMissionMst.Id);
                    newCompletedLiveMissions.Add(liveMissionMst.Id);
                }
            }

            void CalculateMaxComboMissions()
            {
                List<LiveMissionMst> remainingMissions = liveMissionsMsts
                    .Where(x => x.Type == LiveMissionType.Combo)
                    .OrderBy(x => x.Id)
                    .ExceptBy(storedliveMissionData.ClearMasterLiveMissionIds, x => x.Id)
                    .ToList();

                if (remainingMissions.Count == 0)
                    return;

                foreach (LiveMissionMst liveMissionMst in remainingMissions)
                {
                    if (liveFinishData.LiveScore.MaxCombo < liveMst.ComboMissionRequirements[liveMissionMst.Id % 10 - 1])
                        continue;

                    AddLiveMissionReward(liveMissionsRewardsMsts[liveMissionMst.MasterLiveMissionRewardId]);
                    storedliveMissionData.ClearMasterLiveMissionIds.Add(liveMissionMst.Id);
                    newCompletedLiveMissions.Add(liveMissionMst.Id);
                }
            }

            void CalculateFullComboMissions()
            {
                LiveMissionMst fullComboMission = liveMissionsMsts
                    .Where(x => x.Type == LiveMissionType.FullCombo)
                    .First(x => x.Level == liveFinishData.Level);

                if (liveFinishData.LiveScore.MaxCombo != liveMst.FullCombo)
                    return;

                AddLiveMissionReward(liveMissionsRewardsMsts[fullComboMission.MasterLiveMissionRewardId]);
                storedliveMissionData.ClearMasterLiveMissionIds.Add(fullComboMission.Id);
            }

            void CalculateClearCountMissions()
            {
                List<LiveMissionMst> remainingMissions = liveMissionsMsts
                    .Where(x => x.Type == LiveMissionType.ClearCount)
                    .OrderBy(x => x.Id)
                    .ExceptBy(storedliveMissionData.ClearMasterLiveMissionIds, x => x.Id)
                    .ToList();

                if (remainingMissions.Count == 0)
                    return;

                foreach (LiveMissionMst liveMissionMst in remainingMissions)
                {
                    if (updatedLive.ClearCount < Int32.Parse(liveMissionMst.Value))
                        continue;

                    AddLiveMissionReward(liveMissionsRewardsMsts[liveMissionMst.MasterLiveMissionRewardId]);
                    storedliveMissionData.ClearMasterLiveMissionIds.Add(liveMissionMst.Id);
                    newCompletedLiveMissions.Add(liveMissionMst.Id);
                }
            }

            void CalculatePerfectFullComboMissions()
            {
                LiveMissionMst perfectFullComboMission = liveMissionsMsts
                    .Where(x => x.Type == LiveMissionType.FullCombo)
                    .First(x => x.Level == liveFinishData.Level);

                if (liveFinishData.LiveScore.MaxCombo != liveMst.FullCombo ||
                    liveFinishData.LiveScore.Perfect != liveMst.FullCombo)
                    return;

                AddLiveMissionReward(liveMissionsRewardsMsts[perfectFullComboMission.MasterLiveMissionRewardId]);
                storedliveMissionData.ClearMasterLiveMissionIds.Add(perfectFullComboMission.Id);
            }

            void AddLiveMissionReward(LiveMissionRewardMst liveMissionRewardMst)
            {
                if (liveMissionRewardMst.GiveType == GiveType.Gift)
                {
                    resourceAdditionBuilder.AddGift("ライブミッション完了報酬", liveMissionRewardMst.Type, liveMissionRewardMst.Value,
                        liveMissionRewardMst.Amount);
                }
                else
                    throw new NotImplementedException();
            }
        }

        void UpdateCharacterExperience()
        {
            Dictionary<uint, (ulong ExpIncrease, Character Character)> characterIdToExpIncreaseMap = new();

            foreach (Character character in deckCharacters[..2].Concat(deckCharacters[^2..]))
                characterIdToExpIncreaseMap[character.MasterCharacterId] = (1 * (ulong)multiplier, character);

            foreach (Character character in deckCharacters[2..4].Concat(deckCharacters[^4..^2]))
                characterIdToExpIncreaseMap[character.MasterCharacterId] = (2 * (ulong)multiplier, character);

            characterIdToExpIncreaseMap[deckCharacters[4].MasterCharacterId] = (5 * (ulong)multiplier, deckCharacters[4]);

            deckCharacters = characterIdToExpIncreaseMap.Values.Select(x =>
                {
                    x.Character.BeforeExp = x.Character.Exp;
                    x.Character.Exp += x.ExpIncrease;
                    return x.Character;
                })
                .ToList();
        }
    }

    public async Task<LiveRewardsRetrievalResult> GetLiveRewards(ulong xuid, uint masterLiveId)
    {
        UserData? userData = await _userService.GetUserDataByXuid(xuid);

        if (userData is null)
            return new LiveRewardsRetrievalResult();

        Live? live = userData.LiveList.FirstOrDefault(x => x.MasterLiveId == masterLiveId);

        live ??= new Live { ClearCount = 0 };

        List<LiveClearRewardMst> liveClearRewardMsts =
            await _mstDbContext.LiveClearRewardMsts.Where(x => x.MasterLiveId == masterLiveId).ToListAsync();

        List<LiveReward> ensuredRewards = [];
        List<LiveReward> randomRewards = [];

        foreach (LiveClearRewardMst liveClearRewardMst in liveClearRewardMsts)
        {
            switch (liveClearRewardMst.Type)
            {
                case RewardType.Gem:
                {
                    ensuredRewards.Add(new LiveReward
                    {
                        MasterLiveClearRewardId = liveClearRewardMst.Id,
                        Type = liveClearRewardMst.Type,
                        Value = liveClearRewardMst.Value,
                        Amount = liveClearRewardMst.Amount,
                        GetableCount = liveClearRewardMst.GetableCount,
                        ReceivedCount = live.ClearCount > 0 ? 1 : 0
                    });
                    break;
                }
                case RewardType.Item when liveClearRewardMst.MasterReleaseLabelId == 1:
                case RewardType.ChatStamp:
                {
                    // TODO: Use actual live clear reward ids
                    randomRewards.Add(new LiveReward
                    {
                        MasterLiveClearRewardId = liveClearRewardMst.Id,
                        Type = liveClearRewardMst.Type,
                        Value = liveClearRewardMst.Value,
                        Amount = liveClearRewardMst.Amount,
                        GetableCount = liveClearRewardMst.GetableCount,
                        ReceivedCount = liveClearRewardMst.GetableCount -
                            live.LimitedRewards.FirstOrDefault(x => x.MasterRewardId == liveClearRewardMst.Value)?.Remaining ?? 0
                    });
                    break;
                }
            }
        }

        return new LiveRewardsRetrievalResult
        {
            EnsuredRewards = ensuredRewards,
            RandomRewards = randomRewards
        };
    }

    public async Task<Gem?> ContinueLive(ulong xuid, uint masterLiveId, LiveLevel liveLevel)
    {
        const int gemCharge = 100;

        Gem? gem = await _userService.ChargeGems(xuid, gemCharge);

        return gem;
    }

    private class JoinedLiveMst : LiveMst
    {
        [SetsRequiredMembers]
        public JoinedLiveMst()
        {
            Type = CardType.None;
            RehearsalImagePath = null!;
        }

        public uint[] ComboMissionRequirements { get; init; } = [];
        public int FullCombo { get; init; }
    }

    private async Task<(bool IsEnoughToDecrease, Stamina UpdatedStamina)> CalculateStamina(int currentUserExp, int nextUserExp,
        Stamina storedStamina, int staminaDecrease, long? atTimestamp = null)
    {
        atTimestamp ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (atTimestamp < storedStamina.LastUpdatedTime)
            throw new ArgumentOutOfRangeException(nameof(atTimestamp));

        long passedTime = atTimestamp.Value - storedStamina.LastUpdatedTime;
        long recoveredStamina = passedTime / 300;

        (RankLpMst currentRank, List<RankLpMst> rankUps) = await GetNearestRanksData(currentUserExp, nextUserExp);

        int newStaminaValue;

        // Claculation of current stamina
        // If excess
        if (storedStamina.StaminaValue >= currentRank.MaxLp)
            newStaminaValue = storedStamina.StaminaValue;
        else // If under maximum
            newStaminaValue = (int)Math.Min(storedStamina.StaminaValue + recoveredStamina, currentRank.MaxLp);

        // Decrease stamina after live
        newStaminaValue -= staminaDecrease;

        // Check if current stamina is enough
        if (newStaminaValue < 0)
            return (false, null!);

        // Rank-up lp bonus
        if (rankUps.Count > 0)
            newStaminaValue += rankUps.Sum(x => x.MaxLp);

        return (true, new Stamina
        {
            StaminaValue = newStaminaValue,
            LastUpdatedTime = atTimestamp.Value
        });
    }

    private async Task<(RankLpMst CurrentRank, List<RankLpMst> RankUps)> GetNearestRanksData(int bottomExpLimit, int topExpLimit)
    {
        // TODO: Consider loading to consts
        List<RankLpMst> userRankMsts = await _mstDbContext.UserRankMsts
            .Select(x => new RankLpMst(x.Exp, x.MaxLp))
            .ToListAsync();

        RankLpMst currentRankLp = userRankMsts
            .Where(x => x.Exp <= bottomExpLimit)
            .MaxBy(x => x.Exp)!;

        List<RankLpMst> rankUps = userRankMsts
            .Where(x => x.Exp > bottomExpLimit && x.Exp <= topExpLimit)
            .ToList();

        return (currentRankLp, rankUps);
    }

    private record RankLpMst(
        int Exp,
        int MaxLp
    );

    private async Task<UserData> UpdateUserDataAfterLiveCreatingGiftIds(ulong xuid, long currentTimestamp, List<Live> lives,
        List<Point> points, List<Item> items, Stamina stamina, int experience, Gem gem,
        List<Character> characters, List<LiveMission> liveMissions, HashSet<uint> newStampIds, List<Gift> gifts)
    {
        // Add gifts if any
        await _userService.AddGifts(xuid, gifts);

        // Add item ids for new items
        List<Item> itemsWithoutIds = items.Where(x => x.Id == 0).ToList();
        List<ulong> itemIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.ItemIds, (ulong)itemsWithoutIds.Count)).ToList();
        for (int i = 0; i < itemsWithoutIds.Count; i++)
            itemsWithoutIds[i].Id = itemIds[i];

        return await _liveDataRepository.UpdateAfterFinishedLive(xuid, currentTimestamp, lives, points, items,
            stamina, experience, gem, characters, liveMissions,
            newStampIds);
    }
}
