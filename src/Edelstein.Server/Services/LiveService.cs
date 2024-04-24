using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Data.Msts;
using Edelstein.Data.Msts.Persistence;
using Edelstein.Server.Models;
using Edelstein.Server.Models.Endpoints.Live;
using Edelstein.Server.Random;
using Edelstein.Server.Repositories;

using Microsoft.EntityFrameworkCore;

using System.Diagnostics.CodeAnalysis;

using RewardType = Edelstein.Data.Models.Components.RewardType;

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

    public async Task<LiveFinishResult> FinishLive(ulong xuid, LiveEndRequestData liveFinishData)
    {
        UserData userData = await _userDataRepository.GetByXuidRemovingCurrentLiveData(xuid);

        if (userData.CurrentLive is null)
            throw new Exception("Live has not been started");

        if (userData.CurrentLive.MasterLiveId != liveFinishData.MasterLiveId)
            throw new Exception("Wrong live finished");

        if (userData.CurrentLive.Level != liveFinishData.Level)
            throw new Exception("Wrong live level finished");

        DateTimeOffset currentDateTimeOffset = DateTimeOffset.UtcNow;
        long currentTimestamp = currentDateTimeOffset.ToUnixTimeSeconds();

        bool isInTutorial = await _tutorialService.IsTutorialInProgress(xuid);

        UpdatedValueList uvl = new();
        List<Reward> rewards = [];
        List<Gift> gifts = [];
        List<uint> clearedMissionIds = [];

        // Build item dictionary for efficient lookup
        Dictionary<uint, Item> allUserItems = userData.ItemList.ToDictionary(x => x.MasterItemId);

        // Simple values and their initialization
        int multiplier = (int)liveFinishData.UseLp / 10;

        int staminaDecrease = isInTutorial ? 0 : (int)liveFinishData.UseLp;
        int coinChange = 750 * multiplier;
        int expChange = 10 * multiplier;
        int nextUserExp = userData.User.Exp + expChange;
        int gemChange = 0;

        // Calculate stamina
        (bool isEnoughStamina, Stamina updatedStamina) = await CalculateStamina(userData.User.Exp,
            nextUserExp, userData.Stamina, staminaDecrease, currentTimestamp);

        if (!isEnoughStamina)
            return new LiveFinishResult(LiveFinishResultStatus.NotEnoughStamina);

        List<Character> deckCharacters = await _userService.GetDeckCharactersFromUserData(userData, userData.CurrentLive!.DeckSlot);

        // Guaranteed reward
        AddGuaranteedRewards();

        if (isInTutorial)
        {
            UpdateCoins();

            UserData tutorialResultUserData =
                await UpdateUserDataAfterLiveCreatingGiftIds(xuid, currentTimestamp,
                    userData.LiveList, userData.PointList, userData.ItemList,
                    updatedStamina, nextUserExp, userData.Gem,
                    userData.CharacterList, userData.LiveMissionList, userData.MasterStampIds,
                    gifts, clearedMissionIds);

            return new LiveFinishResult
            {
                Status = LiveFinishResultStatus.Success,
                ChangedGem = null,
                ChangedItems = uvl.ItemList,
                ChangedPoints = uvl.PointList,
                FinishedLiveData = null,
                ClearedMasterLiveMissionIds = [],
                UpdatedUserData = tutorialResultUserData,
                UpdatedCharacters = deckCharacters,
                Rewards = rewards,
                Gifts = gifts,
                ClearedMissionIds = [],
                EventPointRewards = [],
                RankingChange = null!,
                EventMember = null,
                EventRankingData = null!
            };
        }

        // Live
        Live updatedLive = UpdateLive();

        // Add random rewards
        await AddRandomRewards();

        // Live missions
        LiveMission? storedliveMissionData = userData.LiveMissionList.FirstOrDefault(x => x.MasterLiveId == liveFinishData.MasterLiveId);

        if (storedliveMissionData is null)
        {
            storedliveMissionData = new LiveMission { MasterLiveId = liveFinishData.MasterLiveId };

            userData.LiveMissionList.Add(storedliveMissionData);
        }

        await AddLiveMissionRewards();

        // Guaranteed first clear gem reward
        AddFirstClearRewards();

        // TODO: Missions

        // TODO: Events

        // Character experience
        UpdateCharacterExperience();

        // Update coins
        UpdateCoins();

        // Update gems
        UpdateGems();

        UserData resultUserData =
            await UpdateUserDataAfterLiveCreatingGiftIds(xuid, currentTimestamp,
                userData.LiveList, userData.PointList, userData.ItemList,
                updatedStamina, nextUserExp, userData.Gem,
                userData.CharacterList, userData.LiveMissionList, userData.MasterStampIds,
                gifts, clearedMissionIds);

        return new LiveFinishResult
        {
            Status = LiveFinishResultStatus.Success,
            ChangedGem = userData.Gem,
            ChangedItems = uvl.ItemList,
            ChangedPoints = uvl.PointList,
            FinishedLiveData = updatedLive,
            ClearedMasterLiveMissionIds = storedliveMissionData.ClearMasterLiveMissionIds,
            UpdatedUserData = resultUserData,
            UpdatedCharacters = deckCharacters,
            Rewards = rewards,
            Gifts = gifts,
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

        void AddGuaranteedRewards()
        {
            const uint guaranteedItemId = 16005001;
            int guaranteedItemAmount = 5 * multiplier;

            rewards.Add(new Reward
            {
                Type = RewardType.Item,
                Value = guaranteedItemId,
                Amount = guaranteedItemAmount
            });

            Item? itemDuplicate = uvl.ItemList.FirstOrDefault(x => x.MasterItemId == guaranteedItemId);

            if (itemDuplicate is not null)
            {
                itemDuplicate.Amount += guaranteedItemAmount;
                return;
            }

            if (allUserItems.TryGetValue(guaranteedItemId, out Item? item))
            {
                item.Amount += guaranteedItemAmount;
                uvl.ItemList.Add(item);
            }
            else
            {
                item = new Item
                {
                    MasterItemId = guaranteedItemId,
                    Amount = guaranteedItemAmount,
                    ExpireDateTime = null
                };
                userData.ItemList.Add(item);
                uvl.ItemList.Add(item);
            }
        }

        async Task AddRandomRewards()
        {
            AddLimitedItem(19100001, 1, 100, 1, 1);

            AddItem(17001001, 1, 1, 1);

            List<LiveClearRewardMst> liveClearRewardMsts = await _mstDbContext.LiveClearRewardMsts
                .Where(x => x.MasterLiveId == liveFinishData.MasterLiveId && x.MasterReleaseLabelId == 1)
                .ToListAsync();

            foreach (LiveClearRewardMst liveClearRewardMst in liveClearRewardMsts)
            {
                switch (liveClearRewardMst.Type)
                {
                    case Data.Msts.RewardType.Item:
                    {
                        AddItem(liveClearRewardMst.Value, 1, 1, 1);
                        break;
                    }
                    case Data.Msts.RewardType.ChatStamp:
                    {
                        AddStamp(liveClearRewardMst.Value);
                        break;
                    }
                    default:
                        // Everything else should not be possible
                        throw new ArgumentOutOfRangeException();
                }
            }

            void AddLimitedItem(uint itemId, int dropAmount, int maxDropAmount, int dropRateNumerator, int dropRateDenumerator)
            {
                int amount = dropAmount * BinaryRandom.NextMultipleCount(multiplier, dropRateNumerator, dropRateDenumerator);

                if (amount == 0)
                    return;

                LimitedReward? limitedReward = updatedLive.LimitedRewards.FirstOrDefault(x => x.MasterRewardId == itemId);

                if (limitedReward is null)
                {
                    rewards.Add(new Reward
                    {
                        Type = RewardType.Item,
                        Value = itemId,
                        Amount = amount,
                        DropInfo = new DropInfo
                        {
                            FirstReward = 0,
                            GetableCount = amount,
                            RemainingGetableCount = maxDropAmount - amount
                        }
                    });

                    updatedLive.LimitedRewards.Add(new LimitedReward
                    {
                        MasterRewardId = itemId,
                        Remaining = maxDropAmount - amount
                    });
                }
                else
                {
                    if (limitedReward.Remaining == 0)
                        return;

                    if (limitedReward.Remaining - amount < 0)
                        amount = limitedReward.Remaining;

                    limitedReward.Remaining -= amount;

                    rewards.Add(new Reward
                    {
                        Type = RewardType.Item,
                        Value = itemId,
                        Amount = amount,
                        DropInfo = new DropInfo
                        {
                            FirstReward = 0,
                            GetableCount = maxDropAmount - limitedReward.Remaining,
                            RemainingGetableCount = limitedReward.Remaining
                        }
                    });
                }

                Item? itemDuplicate = uvl.ItemList.FirstOrDefault(x => x.MasterItemId == itemId);

                if (itemDuplicate is not null)
                {
                    itemDuplicate.Amount += amount;
                    return;
                }

                if (allUserItems.TryGetValue(itemId, out Item? item))
                {
                    item.Amount += amount;
                    uvl.ItemList.Add(item);
                }
                else
                {
                    item = new Item
                    {
                        MasterItemId = itemId,
                        Amount = amount,
                        ExpireDateTime = null
                    };
                    userData.ItemList.Add(item);
                    uvl.ItemList.Add(item);
                }
            }

            void AddItem(uint itemId, int dropAmount, int dropRateNumerator, int dropRateDenumerator)
            {
                int amount = dropAmount * BinaryRandom.NextMultipleCount(multiplier, dropRateNumerator, dropRateDenumerator);

                if (amount == 0)
                    return;

                rewards.Add(new Reward
                {
                    Type = RewardType.Item,
                    Value = itemId,
                    Amount = amount
                });

                Item? itemDuplicate = uvl.ItemList.FirstOrDefault(x => x.MasterItemId == itemId);

                if (itemDuplicate is not null)
                {
                    itemDuplicate.Amount += amount;
                    return;
                }

                if (allUserItems.TryGetValue(itemId, out Item? item))
                {
                    item.Amount += amount;
                    uvl.ItemList.Add(item);
                }
                else
                {
                    item = new Item
                    {
                        MasterItemId = itemId,
                        Amount = amount,
                        ExpireDateTime = null
                    };
                    userData.ItemList.Add(item);
                    uvl.ItemList.Add(item);
                }
            }

            void AddStamp(uint stampId)
            {
                const int maxDropAmount = 1;

                int amount = BinaryRandom.NextMultipleCount(multiplier, 1, 1);

                if (amount == 0)
                    return;

                // Can be dropped only once
                amount = 1;

                LimitedReward? limitedReward = updatedLive.LimitedRewards.FirstOrDefault(x => x.MasterRewardId == stampId);

                if (limitedReward is null)
                {
                    rewards.Add(new Reward
                    {
                        Type = RewardType.Item,
                        Value = stampId,
                        Amount = amount,
                        DropInfo = new DropInfo
                        {
                            FirstReward = 0,
                            GetableCount = amount,
                            RemainingGetableCount = maxDropAmount - amount
                        }
                    });

                    updatedLive.LimitedRewards.Add(new LimitedReward
                    {
                        MasterRewardId = stampId,
                        Remaining = maxDropAmount - amount
                    });
                }
                else
                {
                    if (limitedReward.Remaining == 0)
                        return;

                    if (limitedReward.Remaining - amount < 0)
                        amount = limitedReward.Remaining;

                    limitedReward.Remaining -= amount;

                    rewards.Add(new Reward
                    {
                        Type = RewardType.Item,
                        Value = stampId,
                        Amount = amount,
                        DropInfo = new DropInfo
                        {
                            FirstReward = 0,
                            GetableCount = maxDropAmount - limitedReward.Remaining,
                            RemainingGetableCount = limitedReward.Remaining
                        }
                    });
                }

                if (userData.MasterStampIds.Contains(stampId))
                    return;

                if (!uvl.MasterStampIds.Contains(stampId))
                    uvl.MasterStampIds.Add(stampId);

                userData.MasterStampIds.Add(stampId);
            }
        }

        void AddFirstClearRewards()
        {
            if (updatedLive.ClearCount != 1)
                return;

            gemChange += 60;
            rewards.Add(new Reward
            {
                Type = RewardType.Gem,
                Value = 1,
                Amount = 60,
                DropInfo = new DropInfo
                {
                    GetableCount = 1,
                    FirstReward = 1,
                    RemainingGetableCount = 0
                }
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
                    Gift gift = new()
                    {
                        IsReceive = false,
                        ReasonText = "Live mission completion reward",
                        Value = liveMissionRewardMst.Value,
                        Amount = liveMissionRewardMst.Amount,
                        RewardType = liveMissionRewardMst.Type,
                        CreatedDateTime = currentTimestamp,
                        ExpireDateTime = currentDateTimeOffset.AddYears(1).ToUnixTimeSeconds()
                    };

                    gifts.Add(gift);
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

        void UpdateCoins()
        {
            Point? coinsPoint = userData.PointList.FirstOrDefault(x => x.Type == PointType.Coin);

            if (coinsPoint is null)
            {
                coinsPoint = new Point
                {
                    Type = PointType.Coin,
                    Amount = 0
                };

                userData.PointList.Add(coinsPoint);
            }

            coinsPoint.Amount += coinChange;
            uvl.PointList.Add(coinsPoint);
        }

        void UpdateGems()
        {
            userData.Gem.Free += gemChange;
            userData.Gem.Total += gemChange;
            uvl.Gem = userData.Gem;
        }
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

        Console.WriteLine($"{storedStamina.StaminaValue} | {storedStamina.LastUpdatedTime} | {newStaminaValue} | {atTimestamp.Value}");

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
        List<Character> characters, List<LiveMission> liveMissions, List<uint> stampIds, List<Gift> gifts, List<uint> clearedMissionIds)
    {
        // Add item ids for new items
        List<Item> itemsWithoutIds = items.Where(x => x.Id == 0).ToList();
        ulong[] itemIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.ItemIds, (ulong)itemsWithoutIds.Count)).ToArray();
        for (int i = 0; i < itemsWithoutIds.Count; i++)
            itemsWithoutIds[i].Id = itemIds[i];

        // Add ids for gifts
        List<Gift> giftsWithoutIds = gifts.Where(x => x.Id == 0).ToList();
        ulong[] giftIds = (await _sequenceRepository.GetNextRangeById(SequenceNames.GiftIds, (ulong)giftsWithoutIds.Count)).ToArray();
        for (int i = 0; i < giftsWithoutIds.Count; i++)
            giftsWithoutIds[i].Id = giftIds[i];

        return await _liveDataRepository.UpdateAfterFinishedLive(xuid, currentTimestamp, lives, points, items,
            stamina, experience, gem, characters, liveMissions,
            stampIds, gifts, clearedMissionIds);
    }
}
