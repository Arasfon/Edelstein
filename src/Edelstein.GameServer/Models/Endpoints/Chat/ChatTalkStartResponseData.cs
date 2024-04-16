namespace Edelstein.GameServer.Models.Endpoints.Chat;

public record ChatTalkStartResponseData(
    List<string> SelectTalkIdList,
    List<uint> GetItemList,
    bool IsRead
);
