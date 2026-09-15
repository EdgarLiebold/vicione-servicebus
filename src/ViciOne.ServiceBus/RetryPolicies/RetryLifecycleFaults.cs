using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Tracks exact lifecycle failures while nested retry operations share a pipeline context.</summary>
internal sealed class RetryLifecycleFaults
{
    readonly HashSet<Exception> _exceptions = new(ReferenceEqualityComparer.Instance);
    readonly object _sync = new();
    int _activeScopes;

    /// <summary>Retains lifecycle ownership for the active use of a pipeline context.</summary>
    /// <param name="context">The context that carries operation-scoped failure ownership.</param>
    /// <returns>A lease that releases ownership when its operation ends.</returns>
    public static IDisposable Enter(PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return context.GetOrAddPayload(static () => new RetryLifecycleFaults()).Begin();
    }

    /// <summary>Determines whether the exact failure belongs to retry lifecycle work.</summary>
    /// <param name="context">The context that carries failure ownership.</param>
    /// <param name="exception">The escaped failure to identify.</param>
    /// <returns>Whether the same exception instance is owned by active lifecycle work.</returns>
    public static bool IsOwned(PipeContext context, Exception exception)
    {
        if (!context.TryGetPayload(out RetryLifecycleFaults? faults))
            return false;
        lock (faults._sync)
            return faults._exceptions.Contains(exception);
    }

    /// <summary>Transfers exact lifecycle-failure ownership to a pipeline context.</summary>
    /// <param name="context">The context that must carry ownership to its caller.</param>
    /// <param name="exception">The exact lifecycle failure to preserve.</param>
    public static void Mark(PipeContext context, Exception exception)
    {
        var faults = context.GetOrAddPayload(static () => new RetryLifecycleFaults());
        lock (faults._sync)
            faults._exceptions.Add(exception);
    }

    /// <summary>Awaits retry lifecycle work and identifies any escaped failure without replacing it.</summary>
    /// <param name="context">The owning pipeline context.</param>
    /// <param name="execute">The retry preparation or lifecycle notification to await.</param>
    /// <returns>A task that completes when lifecycle work succeeds or propagates its exact failure.</returns>
    public static async Task ExecuteAsync(PipeContext context, Func<Task> execute)
    {
        try
        {
            Task notification = execute()
                ?? throw new InvalidOperationException("The retry lifecycle work returned a null task.");
            await notification.ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            Mark(context, exception);
            throw;
        }
    }

    IDisposable Begin()
    {
        lock (_sync)
        {
            if (_activeScopes++ == 0)
                _exceptions.Clear();
        }
        return new Lease(this);
    }

    void End()
    {
        lock (_sync)
        {
            if (--_activeScopes == 0)
                _exceptions.Clear();
        }
    }

    sealed class Lease(RetryLifecycleFaults faults) : IDisposable
    {
        RetryLifecycleFaults? _faults = faults;

        public void Dispose() => Interlocked.Exchange(ref _faults, null)?.End();
    }
}
