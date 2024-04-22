using System.Numerics;

namespace Edelstein.Server.Repositories;

public interface ISequenceRepository<T>
    where T : IBinaryInteger<T>
{
    public Task<T> GetNextValueById(string sequenceId, ulong initialValue = 0);
    public Task<IEnumerable<T>> GetNextRangeById(string sequenceId, T count, ulong initialValue = 0);
}
