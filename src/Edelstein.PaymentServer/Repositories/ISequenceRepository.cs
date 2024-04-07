using System.Numerics;

namespace Edelstein.PaymentServer.Repositories;

public interface ISequenceRepository<T>
    where T : IBinaryInteger<T>
{
    Task<T> GetNextValueById(string sequenceId, ulong initialValue = 0);
}
