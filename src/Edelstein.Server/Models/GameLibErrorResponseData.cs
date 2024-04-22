using Edelstein.Data.Transport;

namespace Edelstein.Server.Models;

public record GameLibErrorResponseData(
    GameLibErrorCode Code,
    string Message,
    string Result
);
