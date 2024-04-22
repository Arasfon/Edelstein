namespace Edelstein.Server.Random;

public class BucketRandom<TBucketItem>
{
    private readonly List<int> _precalculatedBuckets;
    private readonly List<List<TBucketItem>> _bucketCardIds;
    private readonly System.Random _random;
    private readonly int _totalSum;

    public BucketRandom(IEnumerable<int> buckets, List<List<TBucketItem>> bucketCardIds)
    {
        _precalculatedBuckets = new List<int>();
        _totalSum = 0;
        foreach (int bucket in buckets)
        {
            _totalSum += bucket;
            _precalculatedBuckets.Add(_totalSum);
        }

        _bucketCardIds = bucketCardIds;
        _random = new System.Random();
    }

    public TBucketItem GetNext()
    {
        int randomNumber = _random.Next(1, _totalSum + 1);

        int bucketIndex = _precalculatedBuckets.BinarySearch(randomNumber);

        if (bucketIndex < 0)
            bucketIndex = ~bucketIndex;

        List<TBucketItem> selectedBucketCardIds = _bucketCardIds[bucketIndex];

        int cardIndex = _random.Next(selectedBucketCardIds.Count);
        return selectedBucketCardIds[cardIndex];
    }

    public IEnumerable<TBucketItem> GetNextRange(int count)
    {
        for (int i = 0; i < count; i++)
            yield return GetNext();
    }
}
