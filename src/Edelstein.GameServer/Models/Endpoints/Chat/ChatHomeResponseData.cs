using Edelstein.Data.Models.Components;

namespace Edelstein.GameServer.Models.Endpoints.Chat;

public record ChatHomeResponseData(
    List<ChatProgress> ProgressList,
    List<uint> MasterChatRoomIds,
    IEnumerable<uint> MasterChatStampIds,
    IEnumerable<uint> MasterChatAttachmentIds
);
