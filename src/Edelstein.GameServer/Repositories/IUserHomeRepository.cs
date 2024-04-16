using Edelstein.Data.Models;
using Edelstein.GameServer.Models;

namespace Edelstein.GameServer.Repositories;

public interface IUserHomeRepository
{
    public Task<UserHomeDocument?> GetByXuid(ulong xuid);
    public Task InitializePresets(ulong xuid, uint masterCardId);
    public Task<ChatProgressDocument> GetChat(ulong xuid, uint chatId, uint roomId, uint chapterId);
    public Task<UserHomeDocument> AddChat(ulong xuid, uint masterChatRoomId, ChatProgressDocument chatProgress);
    public Task<AllChatsResult> GetAllChats(ulong xuid);
    public Task MarkReadedAndSaveChatTalkIdList(ulong xuid, uint chatId, uint roomId, uint chapterId, List<string> selectTalkIdList);
}
