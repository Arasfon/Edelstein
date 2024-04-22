using Edelstein.Data.Models;
using Edelstein.Data.Models.Components;
using Edelstein.Server.Models;
using Edelstein.Server.Repositories;

namespace Edelstein.Server.Services;

public class ChatService : IChatService
{
    private readonly IUserInitializationDataRepository _userInitializationDataRepository;
    private readonly IUserHomeRepository _userHomeRepository;

    public ChatService(IUserInitializationDataRepository userInitializationDataRepository, IUserHomeRepository userHomeRepository)
    {
        _userInitializationDataRepository = userInitializationDataRepository;
        _userHomeRepository = userHomeRepository;
    }

    public async Task<ChatProgressDocument> GetChat(ulong xuid, uint chatId, uint roomId, uint chapterId) =>
        await _userHomeRepository.GetChat(xuid, chatId, roomId, chapterId);

    public async Task<AllChatsResult> GetAll(ulong xuid) =>
        await _userHomeRepository.GetAllChats(xuid);

    public async Task AddTutorialChat(ulong xuid)
    {
        UserInitializationData initializationData = await _userInitializationDataRepository.GetByXuid(xuid);

        uint masterChatRoomId = initializationData.FavoriteCharacterMasterId * 1000 + 1;

        await AddChat(xuid, initializationData.FavoriteCharacterMasterId, masterChatRoomId, 1, masterChatRoomId * 100 + 1);
    }

    public async Task<UserHomeDocument> AddChat(ulong xuid, uint chatId, uint masterChatRoomId, uint roomId, uint chapterId,
        ChatStorageDocument? existingChatStorage = null)
    {
        UserHomeDocument? userHomeDocument = null;

        if (existingChatStorage is null)
        {
            userHomeDocument = await _userHomeRepository.GetByXuid(xuid);
            existingChatStorage = userHomeDocument!.ChatStorage;
        }

        if (existingChatStorage.ChatRoomIds.Contains(masterChatRoomId))
            return userHomeDocument ?? (await _userHomeRepository.GetByXuid(xuid))!;

        ChatProgressDocument chatProgressDocument = new()
        {
            ChatProgress = new ChatProgress
            {
                ChatId = chatId,
                RoomId = roomId,
                ChapterId = chapterId,
                CreatedAt = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                IsRead = false
            }
        };

        return await _userHomeRepository.AddChat(xuid, masterChatRoomId, chatProgressDocument);
    }

    public async Task EndTalk(ulong xuid, uint chatId, uint roomId, uint chapterId,
        List<string> selectTalkIdList) =>
        await _userHomeRepository.MarkReadedAndSaveChatTalkIdList(xuid, chatId, roomId, chapterId, selectTalkIdList);
}
