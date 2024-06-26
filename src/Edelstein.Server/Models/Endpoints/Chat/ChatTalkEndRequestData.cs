namespace Edelstein.Server.Models.Endpoints.Chat;

public record ChatTalkEndRequestData(
    uint ChatId,
    uint RoomId,
    uint ChapterId,
    List<string> SelectTalkIdList
);
