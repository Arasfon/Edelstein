namespace Edelstein.GameServer.Models.Endpoints.Chat;

public record ChatTalkStartRequestData(
    uint ChatId,
    uint RoomId,
    uint ChapterId
);
