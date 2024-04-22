namespace Edelstein.Server.Random;

public static class BinaryRandom
{
    public static bool Next(long numerator, long denumerator) =>
        numerator > System.Random.Shared.NextInt64(denumerator);

    public static int NextMultipleCount(int amount, long numerator, long denumerator) =>
        Enumerable.Range(0, amount).Sum(_ => Convert.ToInt32(Next(numerator, denumerator)));
}
