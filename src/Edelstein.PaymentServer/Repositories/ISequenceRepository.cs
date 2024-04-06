using System.Numerics;

namespace Edelstein.PaymentServer.Repositories;

public interface ISequenceRepository<T>
    where T : IBinaryInteger<T>
{
    Task<T> GetNextSequenceValue(string sequenceId, ulong initialValue = 0);
}
