namespace Edelstein.PaymentServer.Repositories;

public interface IUserMissionsRepository
{
    Task Create(ulong xuid);
}
