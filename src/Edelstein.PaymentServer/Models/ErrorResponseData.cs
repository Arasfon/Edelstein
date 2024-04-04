using Edelstein.Data.Transport;

namespace Edelstein.PaymentServer.Models;

public record ErrorResponseData(
    GameLibErrorCode Code,
    string Message,
    string Result
);
