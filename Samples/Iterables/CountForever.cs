using LanguageExt;
using static LanguageExt.Prelude;

namespace Iterables;

public static class CountForever
{
    public static IO<Unit> run =>
        from f in forkIO(example.Iter()).As()
        from k in Console.readKey
        from r in f.Cancel 
        select unit;

    static StreamT<IO, long> naturals =>
        StreamT<IO>.lift(Range(0, long.MaxValue));
    
    static StreamT<IO, Unit> example =>
        from v in naturals
        where v % 10000 == 0
        from _ in Console.writeLine($"{v:N0}")
        where false
        select unit;
}
