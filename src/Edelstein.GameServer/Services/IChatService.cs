using Edelstein.Data.Models;
using Edelstein.GameServer.Models;

namespace Edelstein.GameServer.Services;

public interface IChatService
{
    public Task<ChatProgressDocument> GetChat(ulong xuid, uint chatId, uint roomId, uint chapterId);
    public Task<AllChatsResult> GetAll(ulong xuid);
    public Task AddTutorialChat(ulong xuid);

    public Task<UserHomeDocument> AddChat(ulong xuid, uint chatId, uint masterChatRoomId, uint roomId, uint chapterId,
        ChatStorageDocument? existingChatStorage = null);

    public Task EndTalk(ulong xuid, uint chatId, uint roomId, uint chapterId, List<string> selectTalkIdList);
}
