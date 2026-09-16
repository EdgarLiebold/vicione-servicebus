using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Captures the <see cref="ConsumeContext" /> for the current message as a scoped provider, so that it can be resolved
/// by components at runtime (since MS DI doesn't support runtime configuration of scopes).
/// </summary>
public class ScopedConsumeContextProvider :
    IScopedConsumeContextProvider
{
    ConsumeContext? _context;
    readonly object _syncRoot = new();

    /// <summary>Gets a value indicating whether this instance has context.</summary>
    public bool HasContext => TryGetContext(out _);

    /// <summary>Tries to get the current available context as one atomic snapshot.</summary>
    /// <param name="context">The current context when one is available; otherwise, <see langword="null" />.</param>
    /// <returns><see langword="true" /> when an available context was captured.</returns>
    public bool TryGetContext([NotNullWhen(true)] out ConsumeContext? context)
    {
        context = Volatile.Read(ref _context);
        if (context != null && context is not UnavailableConsumeContext)
            return true;

        context = null;
        return false;
    }

    /// <summary>Pushes context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public virtual IDisposable PushContext(ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        lock (_syncRoot)
        {
            var originalContext = Volatile.Read(ref _context);

            Volatile.Write(ref _context, context);

            return new PushedContext(this, context, originalContext);
        }
    }

    /// <summary>Gets context.</summary>
    /// <returns>The current context, or <see langword="null" /> when no context is active.</returns>
    public ConsumeContext? GetContext()
    {
        return Volatile.Read(ref _context);
    }

    void PopContext(ConsumeContext context, ConsumeContext? originalContext)
    {
        Interlocked.CompareExchange(ref _context, originalContext, context);
    }


    sealed class PushedContext :
        IDisposable
    {
        readonly ConsumeContext _context;
        readonly ConsumeContext? _originalContext;
        readonly ScopedConsumeContextProvider _provider;

        public PushedContext(ScopedConsumeContextProvider provider, ConsumeContext context, ConsumeContext? originalContext)
        {
            _provider = provider;
            _context = context;
            _originalContext = originalContext;
        }

        public void Dispose()
        {
            _provider.PopContext(_context, _originalContext);
        }
    }
}
