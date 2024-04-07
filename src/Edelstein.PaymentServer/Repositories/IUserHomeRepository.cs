namespace Edelstein.PaymentServer.Repositories;

public interface IUserHomeRepository
{
    public Task Create(ulong xuid);
}
