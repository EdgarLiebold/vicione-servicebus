using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Internal;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides timeout, cancellation, inactivity, and observation-retention services for asynchronous test harnesses.</summary>
public abstract class AsyncTestHarness :
    IDisposable
{
    readonly Lazy<AsyncInactivityObserver> _inactivityObserver;
    readonly CancellationTokenSource _harnessLifetime;
    readonly object _scopeLock;
    CancellationToken _cancellationToken;
    CancellationTokenSource? _cancellationTokenSource;
    TestContextSaveMode _contextSaveMode;
    int _maximumSavedContexts;
    TimeSpan _testInactivityTimeout;
    TimeSpan _testTimeout;
    bool _disposed;

    /// <summary>Initializes a harness that uses the system time provider.</summary>
    protected AsyncTestHarness()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a harness that uses the specified time provider.</summary>
    /// <param name="timeProvider">The time provider used for timeout and inactivity timers.</param>
    protected AsyncTestHarness(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        TestTimeout = Debugger.IsAttached ? TimeSpan.FromMinutes(50) : TimeSpan.FromSeconds(30);
        TestInactivityTimeout = Debugger.IsAttached ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(6);
        ContextSaveMode = TestContextSaveMode.All;
        MaximumSavedContexts = 4096;

        _scopeLock = new object();
        _harnessLifetime = new CancellationTokenSource();

        // Inactivity is observed for the harness lifetime, independently of individual test scopes.
        _inactivityObserver = new Lazy<AsyncInactivityObserver>(
            () => new AsyncInactivityObserver(TestInactivityTimeout, _harnessLifetime.Token, TimeProvider));
    }

    /// <summary>
    /// Starts a test scope with the configured <see cref="TestTimeout" />. An active scope keeps its
    /// cancellation token and receives a new deadline; a completed scope is replaced on next access.
    /// </summary>
    public void BeginTestScope()
    {
        lock (_scopeLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.CancelAfter(TestTimeout);
                return;
            }

            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            _cancellationToken = CancellationToken.None;
        }
    }

    /// <summary>Gets the token that is canceled when the current test scope expires or is explicitly canceled.</summary>
    public CancellationToken TestCancellationToken
    {
        get
        {
            lock (_scopeLock)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);

                if (_cancellationToken == CancellationToken.None)
                {
                    _cancellationTokenSource = new CancellationTokenSource(TestTimeout, TimeProvider);
                    _cancellationToken = _cancellationTokenSource.Token;
                }

                return _cancellationToken;
            }
        }
    }

    /// <summary>Gets the task that completes after the configured interval contains no observed bus activity.</summary>
    public Task InactivityTask => GetInactivityObserver().InactivityTask;

    /// <summary>Gets the token that is canceled after the configured interval contains no observed bus activity.</summary>
    public CancellationToken InactivityToken => GetInactivityObserver().InactivityToken;

    /// <summary>Gets the observer that resets the inactivity interval when bus activity is reported.</summary>
    private protected IInactivityObserver InactivityObserver => GetInactivityObserver();

    /// <summary>Gets or sets the maximum duration of a test scope.</summary>
    public TimeSpan TestTimeout
    {
        get => _testTimeout;
        set => _testTimeout = ValidatePositiveTimeout(value);
    }

    /// <summary>Gets or sets the period of bus inactivity after which inactivity observers complete.</summary>
    public TimeSpan TestInactivityTimeout
    {
        get => _testInactivityTimeout;
        set => _testInactivityTimeout = ValidatePositiveTimeout(value);
    }

    /// <summary>Gets the time provider used for harness timers.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Gets or sets the policy that controls which observed message contexts are retained.</summary>
    public TestContextSaveMode ContextSaveMode
    {
        get => _contextSaveMode;
        set => _contextSaveMode = Enum.IsDefined(value)
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The context save mode is not defined.");
    }

    /// <summary>Gets or sets the maximum number of contexts retained when bounded retention is enabled.</summary>
    public int MaximumSavedContexts
    {
        get => _maximumSavedContexts;
        set => _maximumSavedContexts = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public virtual void Dispose()
    {
        CancellationTokenSource? testScope;
        lock (_scopeLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            testScope = _cancellationTokenSource;
            _cancellationTokenSource = null;
            _cancellationToken = CancellationToken.None;
        }

        List<Exception>? failures = null;
        void Release(Action release)
        {
            try { release(); }
            catch (Exception exception) { (failures ??= new List<Exception>()).Add(exception); }
        }

        // User cancellation callbacks run outside the scope lock. Each owned resource is attempted.
        if (testScope is not null)
        {
            Release(testScope.Cancel);
            Release(testScope.Dispose);
        }
        Release(_harnessLifetime.Cancel);
        if (_inactivityObserver.IsValueCreated)
            Release(_inactivityObserver.Value.Dispose);
        Release(_harnessLifetime.Dispose);

        if (failures?.Count == 1)
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures?.Count > 1)
            throw new AggregateException("One or more asynchronous test-harness resources could not be released.", failures);
    }

    /// <summary>Cancels the current test scope and any harness tasks bound to it.</summary>
    public void Cancel()
    {
        CancellationTokenSource? source;
        lock (_scopeLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_cancellationTokenSource == null)
            {
                _cancellationTokenSource = new CancellationTokenSource(TestTimeout, TimeProvider);
                _cancellationToken = _cancellationTokenSource.Token;
            }

            source = _cancellationTokenSource;
        }

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException) when (_disposed)
        {
        }
    }

    /// <summary>Completes the inactivity observer without waiting for its timeout.</summary>
    public void ForceInactive()
    {
        GetInactivityObserver().ForceInactive();
    }

    /// <summary>Creates a completion source canceled by either the current test scope or the supplied token.</summary>
    /// <typeparam name="T">The result type.</typeparam>
    /// <param name="cancellationToken">An additional token that can cancel the completion source.</param>
    /// <returns>A completion source whose continuations run asynchronously.</returns>
    public TaskCompletionSource<T> CreateTaskCompletionSource<T>(CancellationToken cancellationToken = default)
    {
        TaskCompletionSource<T> source = TaskCompletionSources.Create<T>();
        RegisterCancellation(source, TestCancellationToken);
        if (cancellationToken.CanBeCanceled && cancellationToken != TestCancellationToken)
            RegisterCancellation(source, cancellationToken);

        return source;
    }

    /// <summary>Creates an observer that exposes completion tasks for one consumed, skipped, or faulted message.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <returns>A message observer bound to the current test scope.</returns>
    public TestConsumeMessageObserver<T> CreateConsumeObserver<T>()
        where T : class
    {
        return new TestConsumeMessageObserver<T>(
            CreateTaskCompletionSource<T>(),
            CreateTaskCompletionSource<T>(),
            CreateTaskCompletionSource<T>());
    }

    /// <summary>Creates an observer that records consumed message contexts until the harness becomes inactive.</summary>
    /// <returns>A consume observer configured with this harness's timeouts.</returns>
    public TestConsumeObserver CreateConsumeObserver()
    {
        return new TestConsumeObserver(TestTimeout, InactivityToken, TimeProvider);
    }

    static TimeSpan ValidatePositiveTimeout(TimeSpan value)
    {
        return value > TimeSpan.Zero && value != Timeout.InfiniteTimeSpan
            ? value
            : throw new ArgumentOutOfRangeException(nameof(value), value, "The timeout must be greater than zero.");
    }

    static void RegisterCancellation<T>(TaskCompletionSource<T> source, CancellationToken cancellationToken)
    {
        CancellationTokenRegistration registration = cancellationToken.Register(
            static state =>
            {
                var registrationState = ((TaskCompletionSource<T> Source, CancellationToken Token))state!;
                registrationState.Source.TrySetCanceled(registrationState.Token);
            },
            (source, cancellationToken));

        _ = source.Task.ContinueWith(
            static (_, state) => ((CancellationTokenRegistration)state!).Dispose(),
            registration,
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    AsyncInactivityObserver GetInactivityObserver()
    {
        lock (_scopeLock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _inactivityObserver.Value;
        }
    }
}
