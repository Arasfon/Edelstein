using Edelstein.Data.Configuration;
using Edelstein.Data.Models;
using Edelstein.GameServer.Models;

using Microsoft.Extensions.Options;

using MongoDB.Driver;
using MongoDB.Driver.Linq;

namespace Edelstein.GameServer.Repositories;

public class UserHomeRepository : IUserHomeRepository
{
    private readonly IMongoCollection<UserHomeDocument> _userHomeCollection;

    public UserHomeRepository(IOptions<DatabaseOptions> databaseOptions, IMongoClient mongoClient)
    {
        IMongoDatabase mongoDatabase = mongoClient.GetDatabase(databaseOptions.Value.Name);
        _userHomeCollection = mongoDatabase.GetCollection<UserHomeDocument>(CollectionNames.UserHome);
    }

    public async Task<UserHomeDocument?> GetByXuid(ulong xuid) =>
        await _userHomeCollection.Find(x => x.Xuid == xuid).FirstOrDefaultAsync();

    public async Task InitializePresets(ulong xuid, uint masterCardId)
    {
        FilterDefinition<UserHomeDocument> filterDefinition =
            Builders<UserHomeDocument>.Filter.Eq(x => x.Xuid, xuid) &
            Builders<UserHomeDocument>.Filter.ElemMatch(x => x.Home.PresetSetting, x => x.Slot == 1);

        UpdateDefinition<UserHomeDocument> updateDefinition =
            Builders<UserHomeDocument>.Update.Set(x => x.Home.PresetSetting.FirstMatchingElement().IllustMasterCardId, masterCardId);

        await _userHomeCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }

    public async Task<ChatProgressDocument> GetChat(ulong xuid, uint chatId, uint roomId, uint chapterId)
    {
        FilterDefinition<UserHomeDocument> filterDefinition = Builders<UserHomeDocument>.Filter.Eq(x => x.Xuid, xuid);

        return (await _userHomeCollection.Aggregate()
            .Match(filterDefinition)
            .Project(x => new UserHomeGetChatProjection(x.ChatStorage.Chats))
            .Unwind<UserHomeGetChatProjection, UserHomeUnwindedGetChatProjection>(x => x.Chat)
            .Match(x => x.Chat.ChatProgress.ChatId == chatId && x.Chat.ChatProgress.RoomId == roomId &&
                x.Chat.ChatProgress.ChapterId == chapterId)
            .FirstOrDefaultAsync()).Chat;
    }

    private record UserHomeGetChatProjection(List<ChatProgressDocument> Chat);

    private record UserHomeUnwindedGetChatProjection(ChatProgressDocument Chat);

    public async Task<UserHomeDocument> AddChat(ulong xuid, uint masterChatRoomId, ChatProgressDocument chatProgressDocument)
    {
        FilterDefinition<UserHomeDocument> filterDefinition = Builders<UserHomeDocument>.Filter.Eq(x => x.Xuid, xuid);

        UpdateDefinition<UserHomeDocument> updateDefinition = Builders<UserHomeDocument>.Update
            .AddToSet(x => x.ChatStorage.Chats, chatProgressDocument)
            .AddToSet(x => x.ChatStorage.ChatRoomIds, masterChatRoomId)
            .Inc(x => x.Home.UnreadChatCount, 1);

        return await _userHomeCollection.FindOneAndUpdateAsync(filterDefinition, updateDefinition,
            new FindOneAndUpdateOptions<UserHomeDocument> { ReturnDocument = ReturnDocument.After });
    }

    public async Task<AllChatsResult> GetAllChats(ulong xuid)
    {
        FilterDefinition<UserHomeDocument> filterDefinition = Builders<UserHomeDocument>.Filter.Eq(x => x.Xuid, xuid);

        return await _userHomeCollection.Find(filterDefinition)
            .Project(x => new AllChatsResult(x.ChatStorage.Chats.Select(x => x.ChatProgress).ToList(), x.ChatStorage.ChatRoomIds))
            .FirstOrDefaultAsync();
    }

    public async Task MarkReadedAndSaveChatTalkIdList(ulong xuid, uint chatId, uint roomId, uint chapterId, List<string> selectTalkIdList)
    {
        FilterDefinition<UserHomeDocument> filterDefinition = Builders<UserHomeDocument>.Filter.Eq(x => x.Xuid, xuid) &
            Builders<UserHomeDocument>.Filter.ElemMatch(x => x.ChatStorage.Chats,
                x => x.ChatProgress.ChatId == chatId && x.ChatProgress.RoomId == roomId && x.ChatProgress.ChapterId == chapterId);

        UpdateDefinition<UserHomeDocument> updateDefinition =
            Builders<UserHomeDocument>.Update.Set(x => x.ChatStorage.Chats.FirstMatchingElement().SelectTalkIdList, selectTalkIdList)
                .Set(x => x.ChatStorage.Chats.FirstMatchingElement().ChatProgress.IsRead, true)
                .Inc(x => x.Home.UnreadChatCount, -1);

        await _userHomeCollection.UpdateOneAsync(filterDefinition, updateDefinition);
    }
}
