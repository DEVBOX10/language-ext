using System;
using LanguageExt.Common;
using static LanguageExt.Prelude;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using LanguageExt.Effects;
using LanguageExt.Effects.Traits;
using LanguageExt.HKT;

namespace LanguageExt;

/// <summary>
/// Eff lift trait instance
/// </summary>
public class MEff<RT> : KLift2<MEff<RT>, RT, Error>
    where RT : HasIO<RT, Error>
{
    public static KArrow2<MEff<RT>, RT, Error, A> Lift<A>(Transducer<RT, Sum<Error, A>> f) => 
        Eff<RT, A>.Lift(f);
}

/// <summary>
/// Transducer based effect/`Eff` monad
/// </summary>
/// <typeparam name="RT">Runtime struct</typeparam>
/// <typeparam name="A">Bound value type</typeparam>
public readonly struct Eff<RT, A> : KArrow2<MEff<RT>, RT, Error, A>
    where RT : HasIO<RT, Error>
{
    /// <summary>
    /// Underlying transducer that captures all of the IO behaviour 
    /// </summary>
    readonly IO<RT, Error, A> effect;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Constructors
    //
    
    /// <summary>
    /// Constructor
    /// </summary>
    [MethodImpl(Opt.Default)]
    internal Eff(in IO<RT, Error, A> effect) =>
        this.effect = effect;

    /// <summary>
    /// Constructor
    /// </summary>
    [MethodImpl(Opt.Default)]
    Eff(Func<RT, Sum<Error, A>> thunk) =>
        effect = lift(thunk);

    /// <summary>
    /// Constructor
    /// </summary>
    [MethodImpl(Opt.Default)]
    Eff(Func<RT, A> thunk) 
        : this(rt => Sum<Error, A>.Right(thunk(rt)))
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    [MethodImpl(Opt.Default)]
    Eff(Transducer<RT, A> thunk) 
        : this(Transducer.compose(thunk, Transducer.mkRight<Error, A>()))
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    [MethodImpl(Opt.Default)]
    Eff(Func<RT, Either<Error, A>> thunk) 
        : this(lift(thunk))
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    [MethodImpl(Opt.Default)]
    Eff(Func<RT, Fin<A>> thunk) 
        : this(lift(thunk))
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    [MethodImpl(Opt.Default)]
    Eff(Transducer<RT, Either<Error, A>> thunk) 
        : this(thunk.Morphism.Map(e => e.ToSum()))
    { }

    /// <summary>
    /// Constructor
    /// </summary>
    [MethodImpl(Opt.Default)]
    Eff(Transducer<RT, Fin<A>> thunk) 
        : this(thunk.Morphism.Map(e => e.ToSum()))
    { }
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Transducer
    //

    /// <summary>
    /// Access to the underlying transducer
    /// </summary>
    [Pure]
    public Transducer<RT, Sum<Error, A>> Morphism =>
        effect.Morphism;
    
    /// <summary>
    /// Reduction of the underlying transducer
    /// </summary>
    /// <param name="reduce">Reducer </param>
    /// <typeparam name="S"></typeparam>
    /// <returns></returns>
    [Pure, MethodImpl(Opt.Default)]
    public Reducer<RT, S> Transform<S>(Reducer<Sum<Error, A>, S> reduce) => 
        Morphism.Transform(reduce);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Invoking
    //
    
    /// <summary>
    /// All `Eff` monads will catch exceptions at their point of invocation (i.e. in the `Run*` methods).
    /// But they do not catch exceptions elsewhere without explicitly using this `Try()` method.
    ///
    /// This wraps the `Eff` monad in a try/catch that converts exceptional errors to an `Error` and 
    /// therefore puts the `Eff` in a `Fail` state the expression that can then be matched upon.
    /// </summary>
    /// <remarks>
    /// Useful when you want exceptions to be dealt with through matching/bi-mapping/etc.
    /// and not merely be caught by the exception handler in `Run*`.
    /// </remarks>
    /// <remarks>
    /// This is used automatically for the first argument in the coalescing `|` operator so that any
    /// exceptional failures, in the first argument, allow the second argument to be invoked. 
    /// </remarks>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> Try() =>
        new(effect.Try());

    /// <summary>
    /// Invoke the effect
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<A> Run(RT env) =>
        effect.Run(env).ToFin();

    /// <summary>
    /// Invoke the effect (which could produce multiple values).  Run a reducer for
    /// each value yielded.
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<S> RunMany<S>(RT env, S initialState, Func<S, Either<Error, A>, TResult<S>> reducer) =>
        effect.RunMany(env, initialState, reducer);

    /// <summary>
    /// Invoke the effect (which could produce multiple values).  Collect those results in a `Seq`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Fin<Seq<A>> RunMany(RT env) =>
        effect.RunMany(env).ToFin();

    /// <summary>
    /// Invoke the effect asynchronously
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Task<Fin<A>> RunAsync(RT env) =>
        effect.RunAsync(env).Map(e => e.ToFin());

    /// <summary>
    /// Invoke the effect (which could produce multiple values).  Run a reducer for
    /// each value yielded.
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Task<Fin<S>> RunManyAsync<S>(RT env, S initialState, Func<S, Either<Error, A>, TResult<S>> reducer) =>
        effect.RunManyAsync(env, initialState, reducer);

    /// <summary>
    /// Invoke the effect (which could produce multiple values).  Collect those results in a `Seq`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Task<Either<Error, Seq<A>>> RunManyAsync(RT env) =>
        effect.RunManyAsync(env);
    
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
    public Eff<RT, A> Timeout(TimeSpan timeoutDelay) =>
        new(effect.Timeout(timeoutDelay));
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Lifting
    //
    
    /// <summary>
    /// Lift a value into the `Eff` monad 
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Pure(A value) =>
        new (_ => Sum<Error, A>.Right(value));
    
    /// <summary>
    /// Lift a failure into the `Eff` monad 
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Fail(Error error) =>
        new (_ => Sum<Error, A>.Left(error));

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Lift(Func<RT, Either<Error, A>> f) =>
        new (f);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Lift(Func<RT, Fin<A>> f) =>
        new (f);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Lift(Transducer<RT, Either<Error, A>> f) =>
        new (f);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Lift(Transducer<RT, Fin<A>> f) =>
        new (f);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Lift(Func<RT, Sum<Error, A>> f) =>
        new (f);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Lift(Transducer<RT, Sum<Error, A>> f) =>
        new (f);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Lift(Func<RT, A> f) =>
        new (f);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Lift(Transducer<RT, A> f) =>
        new (f);

    /// <summary>
    /// Lift a asynchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> LiftIO(Func<RT, Task<A>> f) =>
        new (liftAsync<RT, Sum<Error, A>>(
            async (_, rt) => Sum<Error, A>.Right(await f(rt).ConfigureAwait(false))));

    /// <summary>
    /// Lift a asynchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> LiftIO(Func<RT, Task<Sum<Error, A>>> f) =>
        new(liftAsync<RT, Sum<Error, A>>(
            async (_, rt) => await f(rt).ConfigureAwait(false)));

    /// <summary>
    /// Lift a asynchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> LiftIO(Func<RT, Task<Either<Error, A>>> f) =>
        new (liftAsync<RT, Either<Error, A>>(
            async (_, rt) => await f(rt).ConfigureAwait(false)));

    /// <summary>
    /// Lift a asynchronous effect into the `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> LiftIO(Func<RT, Task<Fin<A>>> f) =>
        new (liftAsync<RT, Fin<A>>(
            async (_, rt) => await f(rt).ConfigureAwait(false)));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Memoisation and tail calls
    //
    
    /// <summary>
    /// Memoise the result, so subsequent calls don't invoke the side-effect
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> Memo() =>
        new(effect.Memo());

    /// <summary>
    /// Wrap this around the final `from` call in a `IO` LINQ expression to void a recursion induced space-leak.
    /// </summary>
    /// <example>
    /// 
    ///     Eff<RT, A> recursive(int x) =>
    ///         from x in writeLine(x)
    ///         from r in tail(recursive(x + 1))
    ///         select r;      <--- this never runs
    /// 
    /// </example>
    /// <remarks>
    /// This means the result of the LINQ expression comes from the final `from`, _not_ the `select.  If the
    /// type of the `final` from differs from the type of the `select` then this has no effect.
    /// </remarks>
    /// <remarks>
    /// Background: When making recursive LINQ expressions, the final `select` is problematic because it means
    /// there's code to run _after_ the final `from` expression.  This means there's you're guaranteed to have a
    /// space-leak due to the need to hold thunks to the final `select` on every recursive step.
    ///
    /// This function ignores the `select` altogether and says that the final `from` is where we get our return
    /// result from and therefore there's no need to hold the thunk. 
    /// </remarks>
    /// <returns>IO operation that's marked ready for tail recursion</returns>        
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> Tail() =>
        new(effect.Tail());
    
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
    public Eff<RT, ForkEff<RT, A>> Fork(Option<TimeSpan> timeout = default) =>
        new(Transducer.fork(Morphism, timeout).Map(TFork.ToEff<RT, A>));
    
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
    public Eff<RT, B> Map<B>(Func<A, B> f) =>
        new(effect.Map(f));

    /// <summary>
    /// Maps the `Eff` monad if it's in a success state
    /// </summary>
    /// <param name="f">Function to map the success value with</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Select<B>(Func<A, B> f) =>
        new(effect.Select(f));

    /// <summary>
    /// Maps the `Eff` monad if it's in a success state
    /// </summary>
    /// <param name="f">Function to map the success value with</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Map<B>(Transducer<A, B> f) =>
        new(effect.Map(f));

    /// <summary>
    /// Maps the `Eff` monad if it's in a success state
    /// </summary>
    /// <param name="f">Function to map the success value with</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public  Eff<RT, A> MapFail(Func<Error, Error> f) =>
        new(effect.MapFail(f));

    /// <summary>
    /// Maps the `Eff` monad if it's in a success state
    /// </summary>
    /// <param name="f">Function to map the success value with</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public  Eff<RT, A> MapFail(Transducer<Error, Error> f) =>
        new(effect.MapFail(f));

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
    public Eff<RT, B> BiMap<B>(Func<A, B> Succ, Func<Error, Error> Fail) =>
        new(effect.BiMap(Succ, Fail));

    /// <summary>
    /// Mapping of either the Success state or the Failure state depending on what
    /// state this `Eff` monad is in.  
    /// </summary>
    /// <param name="Succ">Mapping to use if the `Eff` monad if in a success state</param>
    /// <param name="Fail">Mapping to use if the `Eff` monad if in a failure state</param>
    /// <returns>Mapped `Eff` monad</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> BiMap<B>(Transducer<A, B> Succ, Transducer<Error, Error> Fail) =>
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
    public Eff<RT, B> Match<B>(Func<A, B> Succ, Func<Error, B> Fail) =>
        new(effect.Match(Succ, Fail));

    /// <summary>
    /// Pattern match the success or failure values and collapse them down to a success value
    /// </summary>
    /// <param name="Succ">Success value mapping</param>
    /// <param name="Fail">Failure value mapping</param>
    /// <returns>IO in a success state</returns>
    [Pure]
    public Eff<RT, B> Match<B>(Transducer<A, B> Succ, Transducer<Error, B> Fail) =>
        new(effect.Match(Succ, Fail));

    /// <summary>
    /// Map the failure to a success value
    /// </summary>
    /// <param name="f">Function to map the fail value</param>
    /// <returns>IO in a success state</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> IfFail(Func<Error, A> Fail) =>
        new(effect.IfFail(Fail));

    /// <summary>
    /// Map the failure to a success value
    /// </summary>
    /// <param name="f">Function to map the fail value</param>
    /// <returns>IO in a success state</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> IfFail(Transducer<Error, A> Fail) =>
        new(effect.IfFail(Fail));

    /// <summary>
    /// Map the failure to a success value
    /// </summary>
    /// <param name="f">Function to map the fail value</param>
    /// <returns>IO in a success state</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> IfFail(in A Fail) =>
        new(effect.IfFail(Fail));

    /// <summary>
    /// Map the failure to a new IO effect
    /// </summary>
    /// <param name="f">Function to map the fail value</param>
    /// <returns>IO that encapsulates that IfFail</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> IfFailEff(Func<Error, Eff<RT, A>> Fail) =>
        new(Transducer.bimap(
                Morphism,
                e => Fail(e).Morphism,
                x => Transducer.constant<RT, Sum<Error, A>>(Sum<Error, A>.Right(x)))
            .Flatten());

    /// <summary>
    /// Map the failure to a new IO effect
    /// </summary>
    /// <param name="f">Function to map the fail value</param>
    /// <returns>IO that encapsulates that IfFail</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> IfFailEff(Eff<RT, A> Fail) =>
        IfFailEff(_ => Fail);
    
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
    public Eff<RT, A> Filter(Func<A, bool> predicate) =>
        new(effect.Filter(predicate));

    /// <summary>
    /// Only allow values through the effect if the predicate returns `true` for the bound value
    /// </summary>
    /// <param name="predicate">Predicate to apply to the bound value></param>
    /// <returns>Filtered IO</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> Filter(Transducer<A, bool> predicate) =>
        new(effect.Filter(predicate));

    /// <summary>
    /// Only allow values through the effect if the predicate returns `true` for the bound value
    /// </summary>
    /// <param name="predicate">Predicate to apply to the bound value></param>
    /// <returns>Filtered IO</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> Where(Func<A, bool> predicate) =>
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
    public Eff<RT, B> Bind<B>(Func<A, Eff<RT, B>> f) =>
        new(Transducer.bind(Morphism, x => f(x).Morphism));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// transducer provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the transducer provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Bind<B>(Transducer<A, Eff<RT, B>> f) =>
        Map(f).Flatten();

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Bind<B>(Func<A, Eff<B>> f) =>
        Bind(a => f(a).WithRuntime<RT>());

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// transducer provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the transducer provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public IO<RT, Error, B> Bind<B>(Transducer<A, IO<RT, Error, B>> f) =>
        effect.Bind(f);

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Bind<B>(Func<A, Pure<B>> f) =>
        new(effect.Bind(f));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, A> Bind(Func<A, Fail<Error>> f) =>
        new(effect.Bind(f));
    
    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Bind<B>(Func<A, Transducer<Unit, B>> f) =>
        new(effect.Bind(f));
    
    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="f">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, B> Bind<B>(Func<A, Transducer<RT, B>> f) =>
        new(effect.Bind(f));
    
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
    public Eff<RT, C> SelectMany<B, C>(Func<A, Eff<RT, B>> bind, Func<A, B, C> project) =>
        new(Transducer.selectMany(Morphism, x => bind(x).Morphism, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public IO<RT, Error, C> SelectMany<B, C>(Func<A, IO<RT, Error, B>> bind, Func<A, B, C> project) =>
        effect.SelectMany(bind, project);

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, C> SelectMany<B, C>(Func<A, Pure<B>> bind, Func<A, B, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, C> SelectMany<B, C>(Func<A, Fail<Error>> bind, Func<A, B, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, C> SelectMany<C>(Func<A, Guard<Error, Unit>> bind, Func<A, Unit, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, C> SelectMany<C>(Func<A, Guard<Fail<Error>, Unit>> bind, Func<A, Unit, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, C> SelectMany<B, C>(Func<A, Transducer<Unit, B>> bind, Func<A, B, C> project) =>
        new(effect.SelectMany(bind, project));

    /// <summary>
    /// Monadic bind operation.  This runs the current `Eff` monad and feeds its result to the
    /// function provided; which in turn returns a new `Eff` monad.  This can be thought of as
    /// chaining IO operations sequentially.
    /// </summary>
    /// <param name="bind">Bind operation</param>
    /// <returns>Composition of this monad and the result of the function provided</returns>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, C> SelectMany<B, C>(Func<A, Transducer<RT, B>> bind, Func<A, B, C> project) =>
        new(effect.SelectMany(bind, project));
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Folding
    //

    /// <summary>
    /// Fold the effect forever or until the schedule expires
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> Fold<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder) =>
        new(effect.Fold(schedule, initialState, folder));
    
    /// <summary>
    /// Fold the effect forever
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> Fold<S>(
        S initialState,
        Func<S, A, S> folder) =>
        new(effect.Fold(initialState, folder));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldUntil<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, S> folder, 
        Func<(S State, A Value), bool> predicate) =>
        new(effect.FoldUntil(schedule, initialState, folder, predicate));
    
    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldUntil<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, S> folder, 
        Func<S, bool> stateIs) =>
        new(effect.FoldUntil(schedule, initialState, folder, stateIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldUntil<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, S> folder, 
        Func<A, bool> valueIs) =>
        new(effect.FoldUntil(schedule, initialState, folder, valueIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder, 
        Func<(S State, A Value), bool> predicate) =>
        new(effect.FoldUntil(initialState, folder, predicate));
    
    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder, 
        Func<S, bool> stateIs) =>
        new(effect.FoldUntil(initialState, folder, stateIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder, 
        Func<A, bool> valueIs) =>
        new(effect.FoldUntil(initialState, folder, valueIs));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldWhile<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, S> folder, 
        Func<(S State, A Value), bool> predicate) =>
        new(effect.FoldWhile(schedule, initialState, folder, predicate));
    
    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldWhile<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, S> folder, 
        Func<S, bool> stateIs) =>
        new(effect.FoldWhile(schedule, initialState, folder, stateIs));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldWhile<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, S> folder, 
        Func<A, bool> valueIs) =>
        new(effect.FoldWhile(schedule, initialState, folder, valueIs));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder, 
        Func<(S State, A Value), bool> predicate) =>
        new(effect.FoldWhile(initialState, folder, predicate));
    
    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder, 
        Func<S, bool> stateIs) =>
        new(effect.FoldWhile(initialState, folder, stateIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder, 
        Func<A, bool> valueIs) =>
        new(effect.FoldWhile(initialState, folder, valueIs));
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Folding, where each fold is monadic
    //

    /// <summary>
    /// Fold the effect forever or until the schedule expires
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldM<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, Eff<RT, S>> folder) =>
        new(effect.FoldM(schedule, initialState, (s, x) => folder(s, x).Morphism));

    /// <summary>
    /// Fold the effect forever
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldM<S>(
        S initialState,
        Func<S, A, Eff<RT, S>> folder) =>
        new(effect.FoldM(initialState, (s, x) => folder(s, x).Morphism));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldUntilM<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, Eff<RT, S>> folder, 
        Func<A, bool> valueIs) =>
        new(effect.FoldUntilM(schedule, initialState, (s, x) => folder(s, x).Morphism, valueIs));

    /// <summary>
    /// Fold the effect until the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldUntilM<S>(
        S initialState,
        Func<S, A, Eff<RT, S>> folder, 
        Func<A, bool> valueIs) =>
        new(effect.FoldUntilM(initialState, (s, x) => folder(s, x).Morphism, valueIs));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldWhileM<S>(
        Schedule schedule, 
        S initialState,
        Func<S, A, Eff<RT, S>> folder, 
        Func<A, bool> valueIs) =>
        new(effect.FoldWhileM(schedule, initialState, (s, x) => folder(s, x).Morphism, valueIs));

    /// <summary>
    /// Fold the effect while the predicate returns `true`
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public Eff<RT, S> FoldWhileM<S>(
        S initialState,
        Func<S, A, Eff<RT, S>> folder, 
        Func<A, bool> valueIs) =>
        new(effect.FoldWhileM(initialState, (s, x) => folder(s, x).Morphism, valueIs));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Synchronisation between contexts
    //
    
    /// <summary>
    /// Make the effect run on the `SynchronizationContext` that was captured at the start
    /// of an `Invoke` call.
    /// </summary>
    /// <remarks>
    /// The effect receives its input value from the currently running sync-context and
    /// then proceeds to run its operation in the captured `SynchronizationContext`:
    /// typically a UI context, but could be any captured context.  The result of the
    /// effect is the received back on the currently running sync-context. 
    /// </remarks>
    public Eff<RT, A> Post() =>
        new(effect.Post());        
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Conversion
    //
    
    /// <summary>
    /// Natural transformation to an `IO` monad
    /// </summary>
    [Pure]
    public IO<RT, Error, A> As =>
        effect;
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Operators
    //

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(in Pure<A> ma) =>
        ma.ToEff<RT>();

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(in Fail<Error> ma) =>
        ma.ToEff<RT, A>();

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(in Either<Error, A> ma) =>
        ma.Match(Right: Pure, Left: Fail, Bottom: () => Fail(Errors.Bottom));

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(in Fin<A> ma) =>
        ma.Match(Succ: Pure, Fail: Fail);

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Transducer<RT, Sum<Error, A>> ma) =>
        Lift(ma);

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Transducer<RT, Either<Error, A>> ma) =>
        ma.Map(x => x.ToSum());

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Transducer<RT, Fin<A>> ma) =>
        ma.Map(x => x.ToSum());

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Transducer<Unit, Sum<Error, A>> ma) =>
        Lift(Transducer.compose(Transducer.constant<RT, Unit>(default), ma));

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Transducer<Unit, Either<Error, A>> ma) =>
        ma.Map(x => x.ToSum());

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Transducer<Unit, Fin<A>> ma) =>
        ma.Map(x => x.ToSum());

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Transducer<RT, A> ma) =>
        Lift(ma);

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Transducer<Unit, A> ma) =>
        Lift(Transducer.compose(Transducer.constant<RT, Unit>(default), ma));

    /// <summary>
    /// Convert to an `Eff` monad
    /// </summary>
    [Pure, MethodImpl(Opt.Default)]
    public static implicit operator Eff<RT, A>(Eff<A> ma) =>
        Lift(Transducer.compose(MinRT.convert<RT>(), ma.Morphism));
    
    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(in Eff<RT, A> ma, in Eff<RT, A> mb) =>
        new(ma.effect | mb.effect);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(in Eff<RT, A> ma, in CatchError<Error> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(in Eff<RT, A> ma, in CatchError mb) =>
        new(ma.effect | mb.As());

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(in Eff<RT, A> ma, in CatchValue<Error, A> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(in Eff<RT, A> ma, in CatchValue<A> mb) =>
        new(ma.effect | mb.As());
    
    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(in Eff<RT, A> ma, in IOCatch<RT, Error, A> mb) =>
        new(ma.effect | mb);
    
    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(in Eff<RT, A> ma, in EffCatch<RT, A> mb) =>
        new(ma.effect | mb.As());
    
    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(in Eff<RT, A> ma, in EffCatch<A> mb) =>
        new(ma.effect | mb.As<RT>());

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Eff<RT, A> ma, Transducer<RT, A> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Eff<RT, A> ma, Transducer<RT, Error> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Eff<RT, A> ma, Transducer<Unit, A> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Eff<RT, A> ma, Transducer<Unit, Error> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Eff<RT, A> ma, Transducer<RT, Sum<Error, A>> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Eff<RT, A> ma, Transducer<Unit, Sum<Error, A>> mb) =>
        new(ma.effect | mb);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Transducer<RT, A> ma, Eff<RT, A> mb) =>
        new(ma | mb.effect);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Transducer<RT, Error> ma, Eff<RT, A> mb) =>
        new(ma | mb.effect);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Transducer<Unit, A> ma, Eff<RT, A> mb) =>
        new(ma | mb.effect);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Transducer<Unit, Error> ma, Eff<RT, A> mb) =>
        new(ma | mb.effect);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Transducer<RT, Sum<Error, A>> ma, Eff<RT, A> mb) =>
        new(ma | mb.effect);

    /// <summary>
    /// Run the first IO operation; if it fails, run the second.  Otherwise return the
    /// result of the first without running the second.
    /// </summary>
    /// <param name="ma">First IO operation</param>
    /// <param name="mb">Alternative IO operation</param>
    /// <returns>Result of either the first or second operation</returns>
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> operator |(Transducer<Unit, Sum<Error, A>> ma, Eff<RT, A> mb) =>
        new(ma | mb.effect);
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Obsolete
    //
    
    /// <summary>
    /// Lift a value into the `Eff` monad 
    /// </summary>
    [Obsolete("Use either: `Eff<RT, A>.Lift`, `Prelude.liftEff`, or `lift`")]
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Success(A value) =>
        Pure(value);
    
    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Obsolete("Use either: `Eff<RT, A>.Lift`, `Prelude.liftEff`, or `lift`")]
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> Effect(Func<RT, A> f) =>
        Lift(f);

    /// <summary>
    /// Lift a synchronous effect into the `Eff` monad
    /// </summary>
    [Obsolete("Use either: `Eff<RT, A>.Lift`, `Prelude.liftEff`, or `lift`")]
    [Pure, MethodImpl(Opt.Default)]
    public static Eff<RT, A> EffectMaybe(Func<RT, Fin<A>> f) =>
        Lift(f);
}
