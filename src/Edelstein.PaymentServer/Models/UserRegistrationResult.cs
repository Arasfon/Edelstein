using Edelstein.Data.Models;

namespace Edelstein.PaymentServer.Models;

public readonly record struct UserRegistrationResult(
    AuthenticationData AuthenticationData,
    UserData UserData
);
