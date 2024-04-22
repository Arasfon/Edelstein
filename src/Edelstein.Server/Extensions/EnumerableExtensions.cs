namespace Edelstein.Server.Extensions;

public static class EnumerableExtensions
{
    public static IEnumerable<TSource> ExceptBy<TSource, TKey>(this IEnumerable<TSource> first, IEnumerable<TSource> second,
        Func<TSource, TKey> keySelector) =>
        first.ExceptBy(second.Select(keySelector), keySelector);
}
