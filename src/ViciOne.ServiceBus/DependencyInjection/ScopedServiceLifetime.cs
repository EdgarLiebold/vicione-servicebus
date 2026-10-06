using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Owns successful fallback activations until the enclosing scope finishes closing.</summary>
internal sealed class ScopedServiceLifetime : IAsyncDisposable
{
    readonly object _gate = new();
    readonly TaskCompletionSource _closeRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource _resolutionsDrained = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly Func<ValueTask> _releaseScope;
    readonly Task _disposal;
    OwnedService? _owned;
    int _activeResolutions;
    bool _closing;

    public ScopedServiceLifetime(Func<ValueTask> releaseScope)
    {
        _releaseScope = releaseScope;
        // The pending gate publishes one actual async completion before any user cleanup can run.
        _disposal = ReleaseWhenClosedAsync();
    }

    public T GetService<T>(Func<IServiceProvider> getProvider)
        where T : class
        => Resolve(() => GetServiceOrCreate<T>(getProvider()));

    public T Resolve<T>(Func<(T Value, bool Owned)> resolve)
        where T : class
    {
        // Allocate before provider/constructor code. Publication needs no collection allocation.
        var node = new OwnedService();
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_closing, this);
            _activeResolutions++;
        }

        try
        {
            var (value, owned) = resolve();
            if (owned)
                node.Value = value;
            return value;
        }
        finally
        {
            bool drained;
            lock (_gate)
            {
                if (node.Value is not null)
                {
                    node.Next = _owned;
                    _owned = node;
                }
                // Successful publication and leaving the admission are one atomic transition.
                drained = --_activeResolutions == 0 && _closing;
            }
            if (drained)
                _resolutionsDrained.TrySetResult();
        }
    }

    public static (T Value, bool Owned) GetServiceOrCreate<T>(IServiceProvider provider)
        where T : class
    {
        var registered = provider.GetService(typeof(T));
        // Keep invalid registered-type casts visible; only an absent registration is a fallback.
        return registered is not null
            ? ((T)registered, false)
            : (ActivatorUtilities.CreateInstance<T>(provider), true);
    }

    public ValueTask DisposeAsync()
    {
        bool first;
        bool drained;
        lock (_gate)
        {
            first = !_closing;
            _closing = true;
            drained = _activeResolutions == 0;
        }
        if (first)
        {
            if (drained)
                _resolutionsDrained.TrySetResult();
            _closeRequested.TrySetResult();
        }
        return new ValueTask(_disposal);
    }

    async Task ReleaseWhenClosedAsync()
    {
        await _closeRequested.Task.ConfigureAwait(false);
        await _resolutionsDrained.Task.ConfigureAwait(false);

        OwnedService? owned;
        lock (_gate)
        {
            owned = _owned;
            _owned = null;
        }

        List<Exception>? failures = null;
        for (var node = owned; node is not null; node = node.Next)
        {
            try
            {
                if (node.Value is IAsyncDisposable asyncDisposable)
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                else if (node.Value is IDisposable disposable)
                    disposable.Dispose();
            }
            catch (Exception exception)
            {
                (failures ??= new List<Exception>()).Add(exception);
            }
        }

        try
        {
            await _releaseScope().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            (failures ??= new List<Exception>()).Add(exception);
        }

        if (failures is { Count: 1 })
            ExceptionDispatchInfo.Capture(failures[0]).Throw();
        if (failures is { Count: > 1 })
            throw new AggregateException("Scoped services and scope release encountered multiple failures.", failures);
    }

    sealed class OwnedService
    {
        public object? Value;
        public OwnedService? Next;
    }
}
