using System;
using LanguageExt.Common;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LanguageExt.Effects;
using LanguageExt.Pipes;
using LanguageExt.Traits;
using static LanguageExt.Prelude;

namespace LanguageExt;

/// <summary>
/// Transducer based effect/`Eff` monad
/// </summary>
/// <typeparam name="RT">Runtime struct</typeparam>
/// <typeparam name="A">Bound value type</typeparam>
public record Eff<A>(Eff<MinRT, A> effect) :
    K<Eff, A>,
    StateM<Eff<A>, A>,
    Resource<Eff<A>>,
    Alternative<Eff<A>>,
    Monad<Eff<A>>
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Invoking
    //

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<A> Run(MinRT env) =>
        effect.Run(env);

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<A> Run(EnvIO envIO) =>
        Run(new MinRT(envIO));

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<A> Run() =>
        Run(new MinRT());

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [MethodImpl(Opt.Default)]
    public Unit RunUnit(MinRT env) =>
        ignore(effect.Run(env));

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [MethodImpl(Opt.Default)]
    public Unit RunUnit(EnvIO envIO) =>
        ignore(Run(new MinRT(envIO)));

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [MethodImpl(Opt.Default)]
    public Unit RunUnit() =>
        ignore(Run(new MinRT()));

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<A> Run(MinRT env, Resources resources) =>
        effect.Run(env, resources, EnvIO.New());

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<A> Run(Resources resources, EnvIO envIO) =>
        Run(new MinRT(envIO), resources);

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<A> Run(Resources resources) =>
        Run(new MinRT(), resources);

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [MethodImpl(Opt.Default)]
    public Unit RunUnit(MinRT env, Resources resources, EnvIO envIO) =>
        ignore(effect.Run(env, resources, envIO));

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [MethodImpl(Opt.Default)]
    public Unit RunUnit(Resources resources, EnvIO envIO) =>
        ignore(Run(new MinRT(envIO), resources));

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [MethodImpl(Opt.Default)]
    public Unit RunUnit(Resources resources) =>
        ignore(Run(new MinRT(), resources));
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Timeout
    //

    /// <summary>
    /// Cancel the operation if it takes too long
    /// </summary>
    /// <param name="timeoutDelay">Timeout period</param>
    /// <returns>An IO operation that will timeout if it takes too long</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<A> Timeout(TimeSpan timeoutDelay) =>
        new(effect.Timeout(timeoutDelay));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Lifting
    //

    /// <summary>
    /// Lift a value into the `Eff` monad 
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Pure(A value) =>
        new(Eff<MinRT, A>.Pure(value));

    /// <summary>
    /// Lift a failure into the `Eff` monad 
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Fail(Error error) =>
        new(Eff<MinRT, A>.Fail(error));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Lift(Func<MinRT, Either<Error, A>> f) =>
        new(Eff<MinRT, A>.Lift(f));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Lift(Func<MinRT, Fin<A>> f) =>
        new(Eff<MinRT, A>.Lift(f));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Lift(Func<MinRT, A> f) =>
        new(Eff<MinRT, A>.Lift(f));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> LiftIO(Func<MinRT, ValueTask<A>> f) =>
        new(Eff<MinRT, A>.LiftIO(f));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> LiftIO(Func<MinRT, ValueTask<Fin<A>>> f) =>
        new(Eff<MinRT, A>.LiftIO(rt => f(rt).Map(r => r.ThrowIfFail())));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> LiftIO(Func<MinRT, IO<A>> f) =>
        new(Eff<MinRT, A>.LiftIO(f));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> LiftIO(IO<A> ma) =>
        new(Eff<MinRT, A>.LiftIO(ma));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Lift(Func<Either<Error, A>> f) =>
        new(Eff<MinRT, A>.Lift(_ => f()));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Lift(Func<Fin<A>> f) =>
        new(Eff<MinRT, A>.Lift(_ => f()));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Lift(Func<A> f) =>
        new(Eff<MinRT, A>.Lift(_ => f()));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> LiftIO(Func<ValueTask<A>> f) =>
        new(Eff<MinRT, A>.LiftIO(_ => f()));

    /// <summary>
    /// Lift an effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> LiftIO(Func<ValueTask<Fin<A>>> f) =>
        new(Eff<MinRT, A>.LiftIO(_ => f().Map(r => r.ThrowIfFail())));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 
    // Forking
    //

    /// <summary>
    /// Queue this IO operation to run on the thread-pool. 
    /// </summary>
    /// <param name="timeout">Maximum time that the forked IO operation can run for. `None` for no timeout.</param>
    /// <returns>Returns a `ForkIO` data-structure that contains two IO effects that can be used to either cancel
    /// the forked IO operation or to await the result of it.
    /// </returns>
    [MethodImpl(Opt.Default)]
    public Eff<ForkIO<A>> Fork(Option<TimeSpan> timeout = default) =>
        new(effect.Fork(timeout));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Map and map-left
    //

    /// <summary>
    /// Maps the `Eff` monad if it's in a success state
    /// </summary>
    /// <param name="f">Function to map the success value with</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> Map<B>(Func<A, B> f) =>
        new(effect.Map(f));

    /// <summary>
    /// Maps the `Eff` monad if it's in a success state
    /// </summary>
    /// <param name="f">Function to map the success value with</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> Select<B>(Func<A, B> f) =>
        new(effect.Select(f));

    /// <summary>
    /// Maps the `Eff` monad if it's in a success state
    /// </summary>
    /// <param name="f">Function to map the success value with</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<A> MapFail(Func<Error, Error> f) =>
        new(effect.MapFail(f));

    /// <summary>
    /// Maps the inner IO monad
    /// </summary>
    /// <param name="f">Function to map with</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> MapIO<B>(Func<IO<A>, IO<B>> f) =>
        new(effect.MapIO(f));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Bi-map
    //

    /// <summary>
    /// Mapping of either the Success state or the Failure state depending on what
    /// state this `Eff` monad is in.  
    /// </summary>
    /// <param name="Succ">Mapping to use if the `Eff` monad if in a success state</param>
    /// <param name="Fail">Mapping to use if the `Eff` monad if in a failure state</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> BiMap<B>(Func<A, B> Succ, Func<Error, Error> Fail) =>
        new(effect.BiMap(Succ, Fail));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Matching
    //

    /// <summary>
    /// Pattern match the success or failure values and collapse them down to a success value
    /// </summary>
    /// <param name="Succ">Success value mapping</param>
    /// <param name="Fail">Failure value mapping</param>
    /// <returns>IO in a success state</returns>
    [Pure]
    public Eff<B> Match<B>(Func<A, B> Succ, Func<Error, B> Fail) =>
        new(effect.Match(Succ, Fail));

    /// <summary>
    /// Map the failure to a success value
    /// </summary>
    /// <param name="f">Function to map the fail value</param>
    /// <returns>IO in a success state</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<A> IfFail(Func<Error, A> Fail) =>
        new(effect.IfFail(Fail));

    /// <summary>
    /// Map the failure to a new IO effect
    /// </summary>
    /// <param name="f">Function to map the fail value</param>
    /// <returns>IO that encapsulates that IfFail</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<A> IfFailEff(Func<Error, Eff<A>> Fail) =>
        new(effect.IfFailEff(x => Fail(x).effect));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Filter
    //

    /// <summary>
    /// Only allow values through the effect if the predicate returns `true` for the bound value
    /// </summary>
    /// <param name="predicate">Predicate to apply to the bound value></param>
    /// <returns>Filtered IO</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<A> Filter(Func<A, bool> predicate) =>
        new(effect.Filter(predicate));

    /// <summary>
    /// Only allow values through the effect if the predicate returns `true` for the bound value
    /// </summary>
    /// <param name="predicate">Predicate to apply to the bound value></param>
    /// <returns>Filtered IO</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<A> Where(Func<A, bool> predicate) =>
        new(effect.Where(predicate));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Monadic binding
    //

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> Bind<B>(Func<A, Eff<B>> f) =>
        new(effect.Bind(x => f(x).effect));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> Bind<B>(Func<A, IO<B>> f) =>
        new(effect.Bind(f));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> Bind<B>(Func<A, Gets<MinRT, B>> f) =>
        new(effect.Bind(f));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<Unit> Bind(Func<A, Put<MinRT>> f) =>
        new(effect.Bind(f));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<Unit> Bind(Func<A, Modify<MinRT>> f) =>
        new(effect.Bind(f));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Bind<RT, B>(Func<A, Eff<RT, B>> f) =>
        WithRuntime<RT>().Bind(f);

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Bind<RT, B>(Func<A, K<Eff<RT>, B>> f) =>
        Bind(a => f(a).As());

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> Bind<B>(Func<A, K<Eff, B>> f) =>
        Bind(a => f(a).As());

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<B> Bind<B>(Func<A, Pure<B>> f) =>
        Map(x => f(x).Value);

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<A> Bind(Func<A, Fail<Error>> f) =>
        Bind(x => Fail(f(x).Value));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Monadic binding and projection
    //

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<B, C>(Func<A, Eff<B>> bind, Func<A, B, C> project) =>
        Bind(x => bind(x).Map(y => project(x, y)));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, C> SelectMany<RT, B, C>(Func<A, Eff<RT, B>> bind, Func<A, B, C> project) =>
        Bind(x => bind(x).Map(y => project(x, y)));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, C> SelectMany<RT, B, C>(Func<A, K<Eff<RT>, B>> bind, Func<A, B, C> project) =>
        SelectMany(x => bind(x).As(), project);

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<B, C>(Func<A, K<Eff, B>> bind, Func<A, B, C> project) =>
        SelectMany(x => bind(x).As(), project);

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<B, C>(Func<A, Pure<B>> bind, Func<A, B, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<B, C>(Func<A, IO<B>> bind, Func<A, B, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<B, C>(Func<A, Gets<MinRT, B>> bind, Func<A, B, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<C>(Func<A, Modify<MinRT>> bind, Func<A, Unit, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<C>(Func<A, Put<MinRT>> bind, Func<A, Unit, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<B, C>(Func<A, Fail<Error>> bind, Func<A, B, C> project) =>
        SelectMany(x => Eff<B>.Fail(bind(x).Value), project);

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<C>(Func<A, Guard<Error, Unit>> bind, Func<A, Unit, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<C> SelectMany<C>(Func<A, Guard<Fail<Error>, Unit>> bind, Func<A, Unit, C> project) =>
        new(effect.SelectMany(bind, project));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Folding
    //

    /// <summary>
    /// Fold the effect forever or until the schedule expires
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> Fold<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder) =>
        new(effect.Fold(schedule, initialState, folder));

    /// <summary>
    /// Fold the effect forever
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> Fold<S>(
        S initialState,
        Func<S, A, S> folder) =>
        new(effect.Fold(initialState, folder));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldUntil<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<(S State, A Value), bool> predicate) =>
        new(effect.FoldUntil(schedule, initialState, folder, predicate));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldUntil<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<S, bool> stateIs) =>
        new(effect.FoldUntil(schedule, initialState, folder, stateIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldUntil<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<A, bool> valueIs) =>
        new(effect.FoldUntil(schedule, initialState, folder, valueIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<(S State, A Value), bool> predicate) =>
        new(effect.FoldUntil(initialState, folder, predicate));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<S, bool> stateIs) =>
        new(effect.FoldUntil(initialState, folder, stateIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<A, bool> valueIs) =>
        new(effect.FoldUntil(initialState, folder, valueIs));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldWhile<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<(S State, A Value), bool> predicate) =>
        new(effect.FoldWhile(schedule, initialState, folder, predicate));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldWhile<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<S, bool> stateIs) =>
        new(effect.FoldWhile(schedule, initialState, folder, stateIs));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldWhile<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<A, bool> valueIs) =>
        new(effect.FoldWhile(schedule, initialState, folder, valueIs));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<(S State, A Value), bool> predicate) =>
        new(effect.FoldWhile(initialState, folder, predicate));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<S, bool> stateIs) =>
        new(effect.FoldWhile(initialState, folder, stateIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<A, bool> valueIs) =>
        new(effect.FoldWhile(initialState, folder, valueIs));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Synchronisation between contexts
    //

    /// <summary>
    /// Make the effect run on the `SynchronizationContext` that was captured at the start
    /// of an `Run` call.
    /// </summary>
    /// <remarks>
    /// The effect receives its input value from the currently running sync-context and
    /// then proceeds to run its operation in the captured `SynchronizationContext`:
    /// typically a UI context, but could be any captured context.  The result of the
    /// effect is the received back on the currently running sync-context.
    /// </remarks>
    public Eff<A> Post() =>
        new(effect.Post());

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Operators
    //

    /// <summary>
    /// Convert to an `Eff` monad with a runtime
    /// </summary>
    public Eff<RT, A> WithRuntime<RT>()
    {
        var e = effect;
        return (from eio in Eff<RT, EnvIO>.LiftIO(envIO)
                from res in resourcesEff<RT>()
                from fin in e.Run(new MinRT(eio), res, eio) switch
                           {
                               Fin.Succ<A> (var x) => Eff<RT, A>.Pure(x),
                               Fin.Fail<A> (var x) => Eff<RT, A>.Fail(x),
                               _ => throw new NotSupportedException()
                           }
                select fin).As();
    }

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Pure<A> ma) =>
        ma.ToEff();

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Fail<Error> ma) =>
        ma.ToEff<A>();

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Lift<A> ma) =>
        Lift(ma.Function);

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Lift<Fin<A>> ma) =>
        Lift(ma.Function);

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Lift<MinRT, A> ma) =>
        Lift(ma.Function);

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Lift<MinRT, Fin<A>> ma) =>
        Lift(ma.Function);

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Either<Error, A> ma) =>
        ma.Match(Left: Fail, Right: Pure);

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Fin<A> ma) =>
        ma.Match(Succ: Pure, Fail: Fail);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, Eff<A> mb) =>
        new(ma.effect | mb.effect);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative value if the IO operation fails</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, Pure<A> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="error">Alternative value if the IO operation fails</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, Fail<Error> error) =>
        new(ma.effect | error);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="error">Error if the IO operation fails</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, Error error) =>
        new(ma.effect | error);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="value">Alternative value if the IO operation fails</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, A value) =>
        new(ma.effect | Prelude.Pure(value));

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, CatchError<Error> mb) =>
        ma.MapIO(io => io | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, CatchError mb) =>
        ma.MapIO(io => io | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, CatchError<Exception> mb) =>
        ma.MapIO(io => io | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, CatchValue<Error, A> mb) =>
        ma.MapIO(io => io | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, CatchValue<Exception, A> mb) =>
        ma.MapIO(io => io | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, CatchValue<A> mb) =>
        ma.MapIO(io => io | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> operator |(Eff<A> ma, CatchM<Eff, A> mb) =>
        ma.IfFailEff(e => mb.Run(e, Fail(e)).As());

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<A>(in Effect<Eff, A> ma) =>
        ma.RunEffect().As();

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Obsolete
    //

    /// <summary>
    /// Lift a value into the `Eff` monad
    /// </summary>
    [Obsolete("Use either: `Prelude.Pure` or `Eff<A>.Pure`")]
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Success(A value) =>
        Pure(value);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Obsolete("Use either: `Prelude.lift` or `Eff<A>.Lift`")]
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> Effect(Func<A> f) =>
        Lift(_ => f());

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Obsolete("Use either: `Prelude.lift` or `Eff<A>.Lift`")]
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<A> EffectMaybe(Func<Fin<A>> f) =>
        Lift(_ => f());


    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Trait implementations for `Eff<RT, A>`
    //
    // It's important to remember that the code below is the trait implementations for `Eff<RT, A>`, and not
    // related to `Eff<A>` in any way at all.  `A` in this instance is the `RT` in `Eff<RT, A>`.  
    //
    // It is this way to make it easier to work with Eff traits, even if this is a bit ugly.
    //

    static K<Eff<A>, U> Monad<Eff<A>>.Bind<T, U>(K<Eff<A>, T> ma, Func<T, K<Eff<A>, U>> f) =>
        ma.As().Bind(f);

    static K<Eff<A>, U> Functor<Eff<A>>.Map<T, U>(Func<T, U> f, K<Eff<A>, T> ma) =>
        ma.As().Map(f);

    static K<Eff<A>, T> Applicative<Eff<A>>.Pure<T>(T value) =>
        Eff<A, T>.Pure(value);

    static K<Eff<A>, U> Applicative<Eff<A>>.Apply<T, U>(K<Eff<A>, Func<T, U>> mf, K<Eff<A>, T> ma) =>
        mf.As().Apply(ma.As());

    static K<Eff<A>, U> Applicative<Eff<A>>.Action<T, U>(K<Eff<A>, T> ma, K<Eff<A>, U> mb) =>
        ma.As().Action(mb.As());

    static K<Eff<A>, T> Alternative<Eff<A>>.Empty<T>() =>
        Eff<A, T>.Fail(Errors.None);

    static K<Eff<A>, T> SemiAlternative<Eff<A>>.Or<T>(K<Eff<A>, T> ma, K<Eff<A>, T> mb) =>
        ma.As() | mb.As();

    static K<Eff<A>, T> Resource<Eff<A>>.Use<T>(IO<T> ma, Func<T, IO<Unit>> release) =>
        new Eff<A, T>(StateT<A>.lift(ResourceT<IO>.use(ma, release)));

    static K<Eff<A>, Unit> Resource<Eff<A>>.Release<T>(T value) =>
        new Eff<A, Unit>(StateT<A>.lift(ResourceT<IO>.release(value)));

    static K<Eff<A>, Unit> StateM<Eff<A>, A>.Put(A value) =>
        new Eff<A, Unit>(StateT.put<ResourceT<IO>, A>(value));

    static K<Eff<A>, Unit> StateM<Eff<A>, A>.Modify(Func<A, A> modify) =>
        new Eff<A, Unit>(StateT.modify<ResourceT<IO>, A>(modify));

    static K<Eff<A>, T> StateM<Eff<A>, A>.Gets<T>(Func<A, T> f) =>
        new Eff<A, T>(StateT.gets<ResourceT<IO>, A, T>(f));

    public override string ToString() => 
        "Eff";
}
