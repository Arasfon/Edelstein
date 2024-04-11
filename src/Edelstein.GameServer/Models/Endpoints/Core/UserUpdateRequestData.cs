namespace Edelstein.GameServer.Models.Endpoints.Core;

public record UserUpdateRequestData(
    string? Name,
    string? Comment,
    uint? FavoriteMasterCardId,
    uint? GuestSmileMasterCardId,
    uint? GuestPureMasterCardId,
    uint? GuestCoolMasterCardId,
    bool? FriendRequestDisabled
);
