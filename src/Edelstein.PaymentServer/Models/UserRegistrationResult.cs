using Edelstein.Data.Models;

namespace Edelstein.PaymentServer.Models;

public record UserRegistrationResult(
    AuthenticationData AuthenticationData,
    UserData UserData
);
