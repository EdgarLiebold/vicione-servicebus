using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Util;

namespace ViciOne.ServiceBus.Testing;

/// <summary>Provides a test harness for async test.</summary>
public abstract class AsyncTestHarness :
    IDisposable
{
    readonly Lazy<AsyncInactivityObserver> _inactivityObserver;
    readonly CancellationTokenSource _harnessLifetime;
    readonly object _scopeLock;
    CancellationToken _cancellationToken;
    CancellationTokenSource? _cancellationTokenSource;
    int _maximumSavedContexts;
    bool _disposed;

    /// <summary>Initializes a new instance.</summary>
    protected AsyncTestHarness()
        : this(TimeProvider.System)
    {
    }

    /// <summary>Initializes a new instance.</summary>
    /// <param name="timeProvider">The time source used by the operation.</param>
    protected AsyncTestHarness(TimeProvider timeProvider)
    {
        TimeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        TestTimeout = Debugger.IsAttached ? TimeSpan.FromMinutes(50) : TimeSpan.FromSeconds(30);
        TestInactivityTimeout = Debugger.IsAttached ? TimeSpan.FromMinutes(30) : TimeSpan.FromSeconds(6);
        ContextSaveMode = TestContextSaveMode.All;
        MaximumSavedContexts = 4096;

        _scopeLock = new object();
        _harnessLifetime = new CancellationTokenSource();

        // The fixture-lifetime token keeps inactivity observation active across every test scope.
        _inactivityObserver = new Lazy<AsyncInactivityObserver>(
            () => new AsyncInactivityObserver(TestInactivityTimeout, _harnessLifetime.Token, TimeProvider));
    }

    /// <summary>
    /// Begins the scope of one test and grants it the whole configured <see cref="TestTimeout" />.
    /// <para>
    /// Long-lived harness instances may execute multiple test operations. Without resetting the budget,
    /// the timeout created on first use would be shared across those operations and later assertions could
    /// inherit an expired cancellation source.
    /// </para>
    /// <para>
    /// A live source keeps its identity and only has its deadline moved. That matters, because a
    /// fixture typically creates its expected tasks through <see cref="GetTask{T}" /> while it is
    /// being set up, and those tasks are bound to the token that existed then. An expired or
    /// explicitly cancelled source is never revived; the next test starts from a fresh one.
    /// </para>
    /// <para>Call this at the start of each logical test operation when a harness instance is reused.</para>
    /// </summary>
    public void BeginTestScope()
    {
        lock (_scopeLock)
        {
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

    /// <summary>CancellationToken that is canceled when the test is being aborted.</summary>
    public CancellationToken TestCancellationToken
    {
        get
        {
            lock (_scopeLock)
            {
                if (_cancellationToken == CancellationToken.None)
                {
                    _cancellationTokenSource = new CancellationTokenSource(TestTimeout, TimeProvider);
                    _cancellationToken = _cancellationTokenSource.Token;

                }

                return _cancellationToken;
            }
        }
    }

    /// <summary>Task that is completed when the bus inactivity timeout has elapsed with no bus activity.</summary>
    public Task InactivityTask => _inactivityObserver.Value.InactivityTask;

    /// <summary>CancellationToken that is cancelled when the test inactivity timeout has elapsed with no bus activity.</summary>
    public CancellationToken InactivityToken => _inactivityObserver.Value.InactivityToken;

    /// <summary>Gets the inactivity observer.</summary>
    public IInactivityObserver InactivityObserver => _inactivityObserver.Value;

    /// <summary>Timeout for the test, used for any delay timers.</summary>
    public TimeSpan TestTimeout { get; set; }

    /// <summary>Timeout specifying the elapsed time with no bus activity after which the test could be completed.</summary>
    public TimeSpan TestInactivityTimeout { get; set; }

    /// <summary>Gets the time provider.</summary>
    public TimeProvider TimeProvider { get; }

    /// <summary>Gets or sets the context save mode.</summary>
    public TestContextSaveMode ContextSaveMode { get; set; }

    /// <summary>Gets or sets the maximum saved contexts.</summary>
    public int MaximumSavedContexts
    {
        get => _maximumSavedContexts;
        set => _maximumSavedContexts = value > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    }

    /// <summary>Releases the resources owned by this instance.</summary>
    public virtual void Dispose()
    {
        // Both a container fixture and its service provider may dispose this shared harness.
        lock (_scopeLock)
        {
            if (_disposed)
                return;

            _disposed = true;
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
        }

        // Disposal terminates the fixture-lifetime inactivity loop.
        _harnessLifetime.Cancel();
        if (_inactivityObserver.IsValueCreated)
            _inactivityObserver.Value.Dispose();
        _harnessLifetime.Dispose();
    }

    /// <summary>Forces the test to be cancelled, aborting any awaiting tasks.</summary>
    public void Cancel()
    {
        CancellationTokenSource? source;
        lock (_scopeLock)
            source = _cancellationTokenSource;

        // Timeout cancellation is scoped to the currently running test.
        source?.Cancel();
    }

    /// <summary>Forces inactive.</summary>
    public void ForceInactive()
    {
        _inactivityObserver.Value.ForceInactive();
    }

    /// <summary>Returns a task completion that is automatically canceled when the test is canceled.</summary>
    /// <typeparam name="T">The task type.</typeparam>
    /// <returns>The task.</returns>
    public TaskCompletionSource<T> GetTask<T>()
    {
        TaskCompletionSource<T> source = TaskCompletionSources.Create<T>();
        CancellationToken cancellationToken = TestCancellationToken;

        if (cancellationToken.IsCancellationRequested)
            source.TrySetCanceled(cancellationToken);
        else
            cancellationToken.Register(static state => ((TaskCompletionSource<T>)state!).TrySetCanceled(), source);

        return source;
    }

    /// <summary>Gets consume observer.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <returns>The consume observer.</returns>
    public TestConsumeMessageObserver<T> GetConsumeObserver<T>()
        where T : class
    {
        return new TestConsumeMessageObserver<T>(GetTask<T>(), GetTask<T>(), GetTask<T>());
    }

    /// <summary>Gets consume observer.</summary>
    /// <returns>The consume observer.</returns>
    public TestConsumeObserver GetConsumeObserver()
    {
        return new TestConsumeObserver(TestTimeout, InactivityToken, TimeProvider);
    }
}
