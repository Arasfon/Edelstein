using Edelstein.Data.Models;

namespace Edelstein.Server.Models;

public readonly record struct UserRegistrationResult(
    AuthenticationData AuthenticationData,
    UserData UserData
);
