using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RetryPolicies;

/// <summary>Separates invocation-scoped retry ownership from retained terminal diagnostics on a pipeline context.</summary>
internal sealed class RetryOperationState
{
    static readonly AsyncLocal<Operation?> Current = new();
    readonly Dictionary<Operation, State> _operations = new(ReferenceEqualityComparer.Instance);
    readonly object _sync = new();
    RetryContext? _publishedDiagnostic;

    /// <summary>Creates independent policy ownership while retaining its active caller association.</summary>
    /// <param name="context">The input context governed by this policy invocation.</param>
    /// <returns>A lease that releases this invocation's ownership.</returns>
    public static IDisposable BeginPolicy(PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Operation? previous = Current.Value;
        var operation = new Operation(previous != null && Volatile.Read(ref previous.ActiveScopes) > 0 ? previous : null);
        return Enter(context, operation, previous);
    }

    /// <summary>Retains lifecycle ownership for the active use of a pipeline context.</summary>
    /// <param name="context">The context that carries ownership for the current asynchronous operation.</param>
    /// <returns>A lease that releases ownership when its operation ends.</returns>
    public static IDisposable Enter(PipeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        Operation? previous = Current.Value;
        Operation operation = previous != null && Volatile.Read(ref previous.ActiveScopes) > 0 ? previous : new Operation(null);
        return Enter(context, operation, previous);
    }

    static IDisposable Enter(PipeContext context, Operation operation, Operation? previous)
    {
        Current.Value = operation;
        var state = context.GetOrAddPayload(static () => new RetryOperationState());
        state.Begin(operation);
        return new Lease(state, operation, previous);
    }

    /// <summary>Determines whether the exact failure belongs to retry lifecycle work.</summary>
    /// <param name="context">The context that carries failure ownership.</param>
    /// <param name="exception">The escaped failure to identify.</param>
    /// <returns>Whether the same exception instance is owned by active lifecycle work.</returns>
    public static bool IsOwned(PipeContext context, Exception exception)
    {
        Operation? operation = Current.Value;
        if (operation == null || !context.TryGetPayload(out RetryOperationState? state))
            return false;
        lock (state._sync)
            return state._operations.TryGetValue(operation, out State? current) && current.Failures.Contains(exception);
    }

    /// <summary>Transfers exact lifecycle-failure ownership to a pipeline context.</summary>
    /// <param name="context">The context that must carry ownership to its caller.</param>
    /// <param name="exception">The exact lifecycle failure to preserve.</param>
    public static void Mark(PipeContext context, Exception exception)
    {
        var state = context.GetOrAddPayload(static () => new RetryOperationState());
        lock (state._sync)
            state.GetCurrent().Failures.Add(exception);
    }

    /// <summary>Publishes a terminal decision without transferring another caller's diagnostic ownership.</summary>
    /// <param name="context">The context that carries the active decision and public diagnostic.</param>
    /// <param name="retryContext">The exact terminal decision owned by the current operation.</param>
    public static void PublishTerminal(PipeContext context, RetryContext retryContext)
    {
        try
        {
            Exception terminalException = retryContext.Exception;
            var state = context.GetOrAddPayload(static () => new RetryOperationState());
            lock (state._sync)
            {
                State current = state.GetCurrent();
                current.TerminalContext = retryContext;
                current.TerminalException = terminalException;
            }

            // Payload-cache updates serialize diagnostic replacement. Only the previously published
            // diagnostic belongs to this owner; unrelated caller-owned payloads remain unchanged.
            context.AddOrUpdatePayload<RetryContext>(() =>
            {
                state._publishedDiagnostic = retryContext;
                return retryContext;
            }, existing =>
            {
                if (!ReferenceEquals(existing, state._publishedDiagnostic))
                    return existing;
                state._publishedDiagnostic = retryContext;
                return retryContext;
            });
        }
        catch (Exception exception)
        {
            Mark(context, exception);
            throw;
        }
    }

    /// <summary>Transfers an actually escaping decision or lifecycle failure to its active policy caller.</summary>
    /// <param name="context">The child input context already associated with the calling policy.</param>
    /// <param name="exception">The exact failure that escapes after child processing and cleanup.</param>
    public static void Propagate(PipeContext context, Exception exception)
    {
        Operation? operation = Current.Value;
        if (operation?.Parent == null || !context.TryGetPayload(out RetryOperationState? state))
            return;

        lock (state._sync)
        {
            if (!state._operations.TryGetValue(operation, out State? child)
                || !state._operations.TryGetValue(operation.Parent, out State? parent))
                return;

            if (child.Failures.Contains(exception))
                parent.Failures.Add(exception);
            if (ReferenceEquals(child.TerminalException, exception))
            {
                parent.TerminalContext = child.TerminalContext;
                parent.TerminalException = child.TerminalException;
            }
        }
    }

    /// <summary>Looks up terminal business ownership for the exact failure in the current operation.</summary>
    /// <param name="context">The context carried by downstream retry processing.</param>
    /// <param name="exception">The business failure whose terminal owner is required.</param>
    /// <param name="retryContext">The exact current terminal decision, when present.</param>
    /// <returns>Whether this active operation owns a terminal decision for the same failure.</returns>
    public static bool TryGetTerminal(PipeContext context, Exception exception, [NotNullWhen(true)] out RetryContext? retryContext)
    {
        Operation? operation = Current.Value;
        if (operation != null && context.TryGetPayload(out RetryOperationState? state))
        {
            lock (state._sync)
            {
                if (state._operations.TryGetValue(operation, out State? current)
                    && ReferenceEquals(current.TerminalException, exception) && current.TerminalContext != null)
                {
                    retryContext = current.TerminalContext;
                    return true;
                }
            }
        }
        retryContext = null;
        return false;
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

    void Begin(Operation operation)
    {
        lock (_sync)
        {
            if (!_operations.TryGetValue(operation, out State? state))
            {
                state = new State();
                _operations.Add(operation, state);
            }
            state.ActiveScopes++;
            Interlocked.Increment(ref operation.ActiveScopes);
        }
    }

    State GetCurrent()
    {
        Operation? operation = Current.Value;
        return operation != null && _operations.TryGetValue(operation, out State? state)
            ? state : throw new InvalidOperationException("Retry ownership requires an active operation scope.");
    }

    void End(Operation operation)
    {
        lock (_sync)
        {
            if (--_operations[operation].ActiveScopes == 0)
                _operations.Remove(operation);
            Interlocked.Decrement(ref operation.ActiveScopes);
        }
    }

    sealed class Operation(Operation? parent)
    {
        public Operation? Parent { get; } = parent;
        public int ActiveScopes;
    }

    sealed class State
    {
        public readonly HashSet<Exception> Failures = new(ReferenceEqualityComparer.Instance);
        public int ActiveScopes;
        public RetryContext? TerminalContext;
        public Exception? TerminalException;
    }

    sealed class Lease(RetryOperationState state, Operation operation, Operation? previous) : IDisposable
    {
        RetryOperationState? _state = state;

        public void Dispose()
        {
            RetryOperationState? owner = Interlocked.Exchange(ref _state, null);
            if (owner == null)
                return;
            owner.End(operation);
            Current.Value = previous;
        }
    }
}
