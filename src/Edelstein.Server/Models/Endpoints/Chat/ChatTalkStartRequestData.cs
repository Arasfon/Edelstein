namespace Edelstein.Server.Models.Endpoints.Chat;

public record ChatTalkStartRequestData(
    uint ChatId,
    uint RoomId,
    uint ChapterId
);
