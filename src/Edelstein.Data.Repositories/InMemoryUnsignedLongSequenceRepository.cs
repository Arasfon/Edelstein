using Edelstein.Data.Extensions;

using System.Collections.Concurrent;

namespace Edelstein.Data.Repositories;

public class InMemoryUnsignedLongSequenceRepository : ISequenceRepository<ulong>
{
    private static readonly ConcurrentDictionary<string, ulong> Sequences = new();

    public Task<ulong> GetNextValueById(string sequenceId, ulong initialValue = 0) =>
        Task.FromResult(Sequences.AddOrUpdate(sequenceId, initialValue, (_, currentValue) => currentValue + 1));

    public Task<IEnumerable<ulong>> GetNextRangeById(string sequenceId, ulong count, ulong initialValue = 0)
    {
        ulong lastRangeValue = Sequences.AddOrUpdate(sequenceId, initialValue, (_, currentValue) => currentValue + count);

        return Task.FromResult(EnumerableExtensions.Range(lastRangeValue - count + 1, lastRangeValue));
    }
}
