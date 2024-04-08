using Edelstein.Data.Models;

namespace Edelstein.GameServer.Models.Endpoints.Live;

public record LiveGuestResponseData(List<Friend> GuestList);
