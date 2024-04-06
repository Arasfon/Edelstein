using System.Collections.Concurrent;

namespace Edelstein.PaymentServer.Repositories;

public class InMemoryUnsignedLongSequenceRepository : ISequenceRepository<ulong>
{
    private static readonly ConcurrentDictionary<string, ulong> Sequences = new();

    public Task<ulong> GetNextSequenceValue(string sequenceId, ulong initialValue = 0) =>
        Task.FromResult(Sequences.AddOrUpdate(sequenceId, initialValue, (_, currentValue) => currentValue + 1));
}
