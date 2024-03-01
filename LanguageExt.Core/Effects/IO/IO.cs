using System;
using System.Threading;
using System.Threading.Tasks;
using LanguageExt.Common;
using LanguageExt.Traits;

namespace LanguageExt;

/// <summary>
/// A value of type `IO` a is a computation which, when performed, does some I/O before returning
/// a value of type `A`.
///
/// There is really only one way you should _"perform"_ an I/O action: bind it to `Main` in your
/// program:  When your program is run, the I/O will be performed. It shouldn't be possible to
/// perform I/O from an arbitrary function, unless that function is itself in the `IO` monad and
/// called at some point, directly or indirectly, from `Main`.
///
/// Obviously, as this is C#, the above restrictions are for you to enforce. It would be reasonable
/// to relax that approach and have I/O invoked from, say, web-request handlers - or any other 'edges'
/// of your application.
/// 
/// `IO` is a monad, so `IO` actions can be combined using either the LINQ-notation or the `bind` 
/// operations from the `Monad` class.
/// </summary>
/// <param name="runIO">The lifted thunk that is the IO operation</param>
/// <typeparam name="A">Bound value</typeparam>
public record IO<A>(Func<EnvIO, Fin<A>> runIO) : K<IO, A>, Monoid<IO<A>>
{
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Construction
    //
    
    public static IO<A> Pure(A value) => 
        new(_ => value);
    
    public static IO<A> Fail(Error value) => 
        new(_ => value);

    public static readonly IO<A> Empty =
        new(_ => Errors.None);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Lifting
    //
    
    public static IO<A> Lift(Func<A> f) =>
        new(_ => f());

    public static IO<A> Lift(Func<Fin<A>> f) =>
        new(_ => f());

    public static IO<A> Lift(Func<EnvIO, A> f) =>
        new(e => f(e));

    public static IO<A> Lift(Func<EnvIO, Fin<A>> f) =>
        new(f);

    public static IO<A> LiftAsync(Func<ValueTask<A>> f) =>
        new(env => Run(_ => f(), env));

    public static IO<A> LiftAsync(Func<EnvIO, ValueTask<A>> f) =>
        new(env => Run(f, env));

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Functor
    //

    public IO<B> Map<B>(Func<A, B> f) =>
        new(e =>
            {
                if (e.Token.IsCancellationRequested) throw new TaskCanceledException();
                return Run(e).Map(f);
            });

    public IO<A> MapFail(Func<Error, Error> f) => 
        new(e =>
            {
                if (e.Token.IsCancellationRequested) throw new TaskCanceledException();
                return Run(e).MapFail(f);
            });

    public IO<B> BiMap<B>(Func<A, B> Succ, Func<Error, Error> Fail) => 
        new(e =>
            {
                if (e.Token.IsCancellationRequested) throw new TaskCanceledException();
                return Run(e).BiMap(Succ, Fail);
            });

    public IO<B> Match<B>(Func<A, B> Succ, Func<Error, B> Fail) => 
        new(e =>
            {
                if (e.Token.IsCancellationRequested) throw new TaskCanceledException();
                return Run(e).Match(Succ, Fail);
            });

    public IO<A> IfFail(Func<Error, A> Fail) =>
        Match(Prelude.identity, Fail);

    public IO<A> IfFail(A Fail) =>
        Match(Prelude.identity, _ => Fail);
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // 
    //  Folding
    //

    public IO<S> Fold<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder) =>
        FoldUntil(schedule, initialState, folder, predicate: _ => false);

    public IO<S> Fold<S>(
        S initialState,
        Func<S, A, S> folder) =>
        FoldUntil(Schedule.Forever, initialState, folder, predicate: _ => false);


    public IO<S> FoldWhile<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<S, bool> stateIs) =>
        FoldUntil(schedule, initialState, folder, Prelude.not(stateIs));

    public IO<S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<S, bool> stateIs) =>
        FoldUntil(Schedule.Forever, initialState, folder, Prelude.not(stateIs));

    
    public IO<S> FoldWhile<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<A, bool> valueIs) =>
        FoldUntil(schedule, initialState, folder, Prelude.not(valueIs));

    public IO<S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<A, bool> valueIs) =>
        FoldUntil(Schedule.Forever, initialState, folder, Prelude.not(valueIs));

    
    public IO<S> FoldWhile<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<(S State, A Value), bool> predicate) =>
        FoldUntil(schedule, initialState, folder, Prelude.not(predicate));

    public IO<S> FoldWhile<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<(S State, A Value), bool> predicate) =>
        FoldUntil(Schedule.Forever, initialState, folder, Prelude.not(predicate));
    
    public IO<S> FoldUntil<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<S, bool> stateIs) =>
        FoldUntil(schedule, initialState, folder, p => stateIs(p.State));
    
    public IO<S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<S, bool> stateIs) =>
        FoldUntil(Schedule.Forever, initialState, folder, p => stateIs(p.State));

    
    public IO<S> FoldUntil<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<A, bool> valueIs) =>
        FoldUntil(schedule, initialState, folder, p => valueIs(p.Value));
    
    public IO<S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<A, bool> valueIs) =>
        FoldUntil(Schedule.Forever, initialState, folder, p => valueIs(p.Value));
    
    
    public IO<S> FoldUntil<S>(
        S initialState,
        Func<S, A, S> folder,
        Func<(S State, A Value), bool> predicate) =>
        FoldUntil(Schedule.Forever, initialState, folder, predicate);
    
    public IO<S> FoldUntil<S>(
        Schedule schedule,
        S initialState,
        Func<S, A, S> folder,
        Func<(S State, A Value), bool> predicate) =>
        new(
            envIO =>
            {
                if (envIO.Token.IsCancellationRequested) throw new TaskCanceledException();
                var r = Run(envIO);
                if (r.IsFail) return r.FailValue;
                var state = folder(initialState, r.SuccValue);
                if (predicate((state, r.SuccValue))) return state;
                
                var token = envIO.Token;
                foreach (var delay in schedule.Run())
                {
                    YieldFor(delay, token);
                    r = Run(envIO);
                    if (r.IsFail) return r.FailValue;
                    state = folder(state, r.SuccValue);
                    if (predicate((state, r.SuccValue))) return state;
                }
                return state;
            });
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    // Cross thread-context posting
    //
 
    /// <summary>
    /// Make this IO computation run on the `SynchronizationContext` that was captured at the start
    /// of the IO chain (i.e. the one embedded within the `EnvIO` environment that is passed through
    /// all IO computations)
    /// </summary>
    public IO<A> Post() =>
        new(env =>
            {
                if (env.Token.IsCancellationRequested) throw new TaskCanceledException();
                if (env.SyncContext is null) return Run(env);

                Fin<A>     value = Errors.None;
                Exception? error = default;
                using var  wait  = new AutoResetEvent(false);
                env.SyncContext.Post(_ =>
                                     {
                                         try
                                         {
                                             value = Run(env);
                                         }
                                         catch (Exception e)
                                         {
                                             error = e;
                                         }
                                         finally
                                         {
                                             wait.Set();
                                         }
                                     }, null);

                wait.WaitOne();
                if (error is not null) error.Rethrow<A>();
                return value;
            });
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Monad
    //

    public IO<B> Bind<B>(Func<A, IO<B>> f) =>
        new(e =>
            {
                if (e.Token.IsCancellationRequested) throw new TaskCanceledException();
                var fa = Run(e);
                if (fa.IsFail) return fa.FailValue;
                return f(fa.SuccValue).Run(e);
            });

    public IO<B> Bind<B>(Func<A, K<IO, B>> f) =>
        new(e =>
            {
                if (e.Token.IsCancellationRequested) throw new TaskCanceledException();
                var fa = Run(e);
                if (fa.IsFail) return fa.FailValue;
                return f(fa.SuccValue).As().Run(e);
            });

    public IO<B> Bind<B>(Func<A, Pure<B>> f) =>
        new(e =>
            {
                if (e.Token.IsCancellationRequested) throw new TaskCanceledException();
                var fa = Run(e);
                if (fa.IsFail) return fa.FailValue;
                return f(fa.SuccValue).Value;
            });

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  LINQ
    //
    
    public IO<B> Select<B>(Func<A, B> f) =>
        Map(f);

    public IO<C> SelectMany<B, C>(Func<A, IO<B>> bind, Func<A, B, C> project) =>
        Bind(x => bind(x).Map(y => project(x, y)));

    public IO<C> SelectMany<B, C>(Func<A, Pure<B>> bind, Func<A, B, C> project) =>
        Bind(x => bind(x).Map(y => project(x, y)));

    public OptionT<M, C> SelectMany<M, B, C>(Func<A, OptionT<M, B>> bind, Func<A, B, C> project)
        where M : Monad<M>, Alternative<M> =>
        OptionT<M, A>.LiftIO(this).SelectMany(bind, project);

    public TryT<M, C> SelectMany<M, B, C>(Func<A, TryT<M, B>> bind, Func<A, B, C> project)
        where M : Monad<M>, Alternative<M> =>
        TryT<M, A>.LiftIO(this).SelectMany(bind, project);

    public EitherT<L, M, C> SelectMany<L, M, B, C>(Func<A, EitherT<L, M, B>> bind, Func<A, B, C> project)
        where M : Monad<M>, Alternative<M> =>
        EitherT<L, M, A>.LiftIO(this).SelectMany(bind, project);

    public ReaderT<Env, M, C> SelectMany<Env, M, B, C>(Func<A, ReaderT<Env, M, B>> bind, Func<A, B, C> project)
        where M : Monad<M>, Alternative<M> =>
        ReaderT<Env, M, A>.LiftIO(this).SelectMany(bind, project);

    public StateT<S, M, C> SelectMany<S, M, B, C>(Func<A, StateT<S, M, B>> bind, Func<A, B, C> project)
        where M : Monad<M>, Alternative<M> =>
        StateT<S, M, A>.LiftIO(this).SelectMany(bind, project);

    public ResourceT<M, C> SelectMany<M, B, C>(Func<A, ResourceT<M, B>> bind, Func<A, B, C> project)
        where M : Monad<M>, Alternative<M> =>
        ResourceT<M, A>.LiftIO(this).SelectMany(bind, project);

    public Eff<C> SelectMany<B, C>(Func<A, Eff<B>> bind, Func<A, B, C> project) =>
        Eff<A>.LiftIO(this).SelectMany(bind, project);

    public Eff<RT, C> SelectMany<RT, B, C>(Func<A, Eff<RT, B>> bind, Func<A, B, C> project) =>
        Eff<RT, A>.LiftIO(this).SelectMany(bind, project);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Fail coalescing
    //

    public IO<A> Or(IO<A> mb) =>
        new(e =>
            {
                if (e.Token.IsCancellationRequested) throw new TaskCanceledException();
                var fa = Run(e);
                if (fa.IsSucc) return fa;
                return mb.Run(e);
            });
    
    public static IO<A> operator |(IO<A> ma, IO<A> mb) =>
        ma.Or(mb);

    public static IO<A> operator |(IO<A> ma, Pure<A> mb) =>
        ma.Or(mb);

    public static IO<A> operator |(IO<A> ma, Fail<Error> mb) =>
        ma.Or(mb);

    public static IO<A> operator |(IO<A> ma, Fail<Exception> mb) =>
        ma.Or(mb);

    public static IO<A> operator |(IO<A> ma, Error mb) =>
        ma.Or(mb);

    public static IO<A> operator |(IO<A> ma, CatchError mb) =>
        ma | mb.As();

    public static IO<A> operator |(IO<A> ma, CatchValue<A> mb) =>
        ma | mb.As();

    public static IO<A> operator |(IO<A> ma, CatchIO<A> mb) =>
        new(env =>
            {
                var fa = ma.Run(env);
                if (fa.IsFail && mb.Match(fa.FailValue)) return mb.Value(fa.FailValue).Run(env);
                return fa;
            });

    public static IO<A> operator |(IO<A> ma, CatchError<Error> mb) =>
        new(env =>
            {
                var fa = ma.Run(env);
                if (fa.IsFail && mb.Match(fa.FailValue)) return mb.Value(fa.FailValue);
                return fa;
            });

    public static IO<A> operator |(IO<A> ma, CatchError<Exception> mb) =>
        new(env =>
            {
                var fa = ma.Run(env);
                if (fa.IsFail && mb.Match(fa.FailValue)) return Error.New(mb.Value(fa.FailValue));
                return fa;
            });

    public static IO<A> operator |(IO<A> ma, CatchValue<Error, A> mb) =>
        new(env =>
            {
                var fa = ma.Run(env);
                if (fa.IsFail && mb.Match(fa.FailValue)) return mb.Value(fa.FailValue);
                return fa;
            });

    public static IO<A> operator |(IO<A> ma, CatchValue<Exception, A> mb) =>
        new(env =>
            {
                var fa = ma.Run(env);
                if (fa.IsFail && mb.Match(fa.FailValue)) return mb.Value(fa.FailValue);
                return fa;
            });

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Trait implementation
    //
    
    IO<A> Semigroup<IO<A>>.Append(IO<A> y) => 
        this | y;

    static IO<A> Monoid<IO<A>>.Empty =>
        Empty;

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Conversion
    //
    
    public static implicit operator IO<A>(Pure<A> ma) =>
        Pure(ma.Value);

    public static implicit operator IO<A>(Error error) =>
        Lift(error.Throw<A>);

    public static implicit operator IO<A>(Fail<Error> ma) =>
        Lift(() => ma.Value.Throw<A>());

    public static implicit operator IO<A>(Fail<Exception> ma) =>
        Lift(() => ma.Value.Rethrow<A>());

    public static implicit operator IO<A>(Lift<EnvIO, A> ma) =>
        Lift(ma.Function);

    public static implicit operator IO<A>(Lift<A> ma) =>
        Lift(ma.Function);

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Parallel
    //

    /// <summary>
    /// Applies a time limit to the IO computation.  If exceeded an exception is thrown.  
    /// </summary>
    /// <param name="timeout">Timeout</param>
    /// <returns>Result of the operation or throws if the time limit exceeded.</returns>
    public IO<A> Timeout(TimeSpan timeout) =>
        Fork(timeout).Await();
    
    /// <summary>
    /// Queues the specified work to run on the thread pool  
    /// </summary>
    /// <param name="timeout">Optional timeout</param>
    /// <returns>`Fork` record that contains members for cancellation and optional awaiting</returns>
    public IO<ForkIO<A>> Fork(Option<TimeSpan> timeout = default) =>
        IO<ForkIO<A>>.Lift(
            env =>
            {
                if (env.Token.IsCancellationRequested) throw new TaskCanceledException();
                
                // Create a new local token-source with its own cancellation token
                var tsrc = timeout.IsSome
                               ? new CancellationTokenSource((TimeSpan)timeout)
                               : new CancellationTokenSource();
                var token = tsrc.Token;

                // If the parent cancels, we should too
                var reg = env.Token.Register(() => tsrc.Cancel());

                // Run the transducer asynchronously
                var cleanup = new CleanUp(tsrc, reg);

                var task = Task.Run(
                    () =>
                    {
                        try
                        {
                            return runIO(new EnvIO(token, tsrc, env.SyncContext));
                        }
                        finally
                        {
                            cleanup.Dispose();
                        }
                    }, token);

                return new ForkIO<A>(
                        IO<Unit>.Lift(() => { tsrc.Cancel(); return default; }),
                        Lift(e => Awaiting(task, e.Token, tsrc)));
            });

    /// <summary>
    /// Run the `IO` monad to get its result
    /// </summary>
    /// <remarks>
    /// Any lifted asynchronous operations will yield to the thread-scheduler, allowing other queued
    /// operations to run concurrently.  So, even though this call isn't awaitable it still plays
    /// nicely and doesn't block the thread.
    /// </remarks>
    /// <remarks>
    /// NOTE: An exception will always be thrown if the IO operation fails.  Lift this monad into
    /// other error handling monads to leverage more declarative error handling. 
    /// </remarks>
    /// <returns>Result of the IO operation</returns>
    /// <exception cref="TaskCanceledException">Throws if the operation is cancelled</exception>
    /// <exception cref="BottomException">Throws if any lifted task fails without a value `Exception` value.</exception>
    public Fin<A> Run() =>
        Run(EnvIO.New());

    /// <summary>
    /// Run the `IO` monad to get its result
    /// </summary>
    /// <remarks>
    /// Any lifted asynchronous operations will yield to the thread-scheduler, allowing other queued
    /// operations to run concurrently.  So, even though this call isn't awaitable it still plays
    /// nicely and doesn't block the thread.
    /// </remarks>
    /// <remarks>
    /// NOTE: An exception will always be thrown if the IO operation fails.  Lift this monad into
    /// other error handling monads to leverage more declarative error handling. 
    /// </remarks>
    /// <param name="env">IO environment</param>
    /// <returns>Result of the IO operation</returns>
    /// <exception cref="TaskCanceledException">Throws if the operation is cancelled</exception>
    /// <exception cref="BottomException">Throws if any lifted task fails without a value `Exception` value.</exception>
    public Fin<A> Run(EnvIO env)
    {
        try
        {
            if (env.Token.IsCancellationRequested) return Errors.Cancelled;
            return runIO(env);
        }
        catch (ErrorException ex)
        {
            return ex.ToError();
        }
        catch (Exception ex)
        {
            return Error.New(ex);
        }
    }
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Repeating the effect
    //
    
    /// <summary>
    /// Keeps repeating the computation forever, or until an error occurs
    /// </summary>
    /// <returns>The result of the last invocation</returns>
    public IO<A> Repeat() =>
        RepeatUntil(Schedule.Forever, _ => true);

    /// <summary>
    /// Keeps repeating the computation, until the scheduler expires, or an error occurs  
    /// </summary>
    /// <param name="schedule">Scheduler strategy for repeating</param>
    /// <returns>The result of the last invocation</returns>
    public IO<A> Repeat(Schedule schedule) =>
        RepeatUntil(schedule, _ => true);

    /// <summary>
    /// Keeps repeating the computation until the predicate returns false, or an error occurs 
    /// </summary>
    /// <param name="predicate">Keep repeating while this predicate returns `true` for each computed value</param>
    /// <returns>The result of the last invocation</returns>
    public IO<A> RepeatWhile(Func<A, bool> predicate) => 
        RepeatUntil(Schedule.Forever, Prelude.not(predicate));

    /// <summary>
    /// Keeps repeating the computation, until the scheduler expires, or the predicate returns false, or an error occurs
    /// </summary>
    /// <param name="schedule">Scheduler strategy for repeating</param>
    /// <param name="predicate">Keep repeating while this predicate returns `true` for each computed value</param>
    /// <returns>The result of the last invocation</returns>
    public IO<A> RepeatWhile(
        Schedule schedule,
        Func<A, bool> predicate) =>
        RepeatUntil(schedule, Prelude.not(predicate));

    /// <summary>
    /// Keeps repeating the computation until the predicate returns true, or an error occurs
    /// </summary>
    /// <param name="predicate">Keep repeating until this predicate returns `true` for each computed value</param>
    /// <returns>The result of the last invocation</returns>
    public IO<A> RepeatUntil(
        Func<A, bool> predicate) =>
        RepeatUntil(Schedule.Forever, predicate);

    /// <summary>
    /// Keeps repeating the computation, until the scheduler expires, or the predicate returns true, or an error occurs
    /// </summary>
    /// <param name="schedule">Scheduler strategy for repeating</param>
    /// <param name="predicate">Keep repeating until this predicate returns `true` for each computed value</param>
    /// <returns>The result of the last invocation</returns>
    public IO<A> RepeatUntil(
        Schedule schedule,
        Func<A, bool> predicate) =>
        new(env =>
            {
                if (env.Token.IsCancellationRequested) throw new TaskCanceledException();
                var token  = env.Token;
                var result = Run(env);
                if (result.IsFail) return result;
                if (predicate(result.SuccValue)) return result;
                
                foreach(var delay in  schedule.Run())
                {
                    YieldFor(delay, token);
                    result = Run(env);
                    if (predicate(result.SuccValue)) return result;
                }
                return result;
            });
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Retrying the effect when it fails
    //

    /// <summary>
    /// Retry if the IO computation fails 
    /// </summary>
    /// <remarks>
    /// This variant will retry forever
    /// </remarks>
    public IO<A> Retry() =>
        RetryUntil(Schedule.Forever, _ => true);

    /// <summary>
    /// Retry if the IO computation fails 
    /// </summary>
    /// <remarks>
    /// This variant will retry until the schedule expires
    /// </remarks>
    public IO<A> Retry(Schedule schedule) =>
        RetryUntil(schedule, _ => true);

    /// <summary>
    /// Retry if the IO computation fails 
    /// </summary>
    /// <remarks>
    /// This variant will keep retrying whilst the predicate returns `true` for the error generated at each iteration;
    /// at which point the last raised error will be thrown.
    /// </remarks>
    public IO<A> RetryWhile(Func<Error, bool> predicate) => 
        RetryUntil(Schedule.Forever, Prelude.not(predicate));

    /// <summary>
    /// Retry if the IO computation fails 
    /// </summary>
    /// <remarks>
    /// This variant will keep retrying whilst the predicate returns `true` for the error generated at each iteration;
    /// or, until the schedule expires; at which point the last raised error will be thrown.
    /// </remarks>
    public IO<A> RetryWhile(
        Schedule schedule,
        Func<Error, bool> predicate) =>
        RetryUntil(schedule, Prelude.not(predicate));

    /// <summary>
    /// Retry if the IO computation fails 
    /// </summary>
    /// <remarks>
    /// This variant will keep retrying until the predicate returns `true` for the error generated at each iteration;
    /// at which point the last raised error will be thrown.
    /// </remarks>
    public IO<A> RetryUntil(
        Func<Error, bool> predicate) =>
        RetryUntil(Schedule.Forever, predicate);

    /// <summary>
    /// Retry if the IO computation fails 
    /// </summary>
    /// <remarks>
    /// This variant will keep retrying until the predicate returns `true` for the error generated at each iteration;
    /// or, until the schedule expires; at which point the last raised error will be thrown.
    /// </remarks>
    public IO<A> RetryUntil(
        Schedule schedule,
        Func<Error, bool> predicate) =>
        new(env =>
            {
                if (env.Token.IsCancellationRequested) throw new TaskCanceledException();
                var token  = env.Token;
                
                var result = Run(env);
                if (result.IsSucc) return result;
                if (predicate(result.FailValue)) return result;
                var lastError = result.FailValue;
                
                foreach(var delay in  schedule.Run())
                {
                    YieldFor(delay, token);
                    
                    result = Run(env);
                    if (result.IsSucc) return result;
                    if (predicate(result.FailValue)) return result;
                    lastError = result.FailValue;
                }
                return lastError;
            });
    
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    //
    //  Internal
    //

    /// <summary>
    /// Internal running of tasks without using the async/await machinery but still
    /// yielding the thread for concurrency.
    /// </summary>
    /// <exception cref="TaskCanceledException"></exception>
    /// <exception cref="BottomException"></exception>
    static A Run(Func<EnvIO, ValueTask<A>> runIO, EnvIO env)
    {
        var token = env.Token;
        if (token.IsCancellationRequested) throw new TaskCanceledException();

        // Launch the task
        #pragma warning disable CA2012
        var t = runIO(env);
        #pragma warning restore CA2012

        // Spin waiting for the task to complete or be cancelled
        SpinWait sw = default;
        while (!t.IsCompleted && !token.IsCancellationRequested)
        {
            sw.SpinOnce();
        }

        if (t.IsCanceled || token.IsCancellationRequested)
        {
            throw new TaskCanceledException();
        }

        if (t.IsFaulted)
        {
            return t.AsTask().Exception is Exception e
                       ? e.Rethrow<A>()
                       : throw new BottomException();
        }

        return t.Result;
    }

    /// <summary>
    /// Yields the thread for the `Duration` specified allowing for concurrency
    /// on the current thread without having to use async/await
    /// </summary>
    static void YieldFor(Duration d, CancellationToken token)
    {
        if (d == Duration.Zero) return;
        var      start = TimeProvider.System.GetTimestamp();
        var      span  = (TimeSpan)d;
        SpinWait sw    = default;
        do
        {
            if (token.IsCancellationRequested) throw new TaskCanceledException();
            if (TimeProvider.System.GetElapsedTime(start) >= span) return;
            sw.SpinOnce();
        } while (true);
    }

    /// <summary>
    /// Waits for the task to complete and returns the result
    /// </summary>
    static Fin<A> Awaiting(Task<Fin<A>> t, CancellationToken envToken, CancellationTokenSource taskSrc)
    {
        if (taskSrc.IsCancellationRequested) return Errors.Cancelled;
        if (envToken.IsCancellationRequested)
        {
            taskSrc.Cancel();
            return Errors.Cancelled;
        }

        // Spin waiting for the task to complete or be cancelled
        SpinWait sw = default;
        while (!t.IsCompleted && !taskSrc.IsCancellationRequested && !envToken.IsCancellationRequested)
        {
            sw.SpinOnce();
        }

        if (envToken.IsCancellationRequested)
        {
            taskSrc.Cancel();
            return Errors.Cancelled;
        }

        if (t.IsCanceled || taskSrc.IsCancellationRequested)
        {
            return Errors.Cancelled;
        }

        if (t.IsFaulted)
        {
            return t.AsTask().Exception is Exception e
                       ? Error.New(e)
                       : Errors.None;
        }
        return t.Result;        
    }
    
    record CleanUp(CancellationTokenSource Src, CancellationTokenRegistration Reg) : IDisposable
    {
        volatile int disposed;
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0)
            {
                try{Src.Dispose();} catch { /* not important */ } 
                try{Reg.Dispose();} catch { /* not important */ }
            }
        }
    }

    public override string ToString() => 
        "IO";
}
