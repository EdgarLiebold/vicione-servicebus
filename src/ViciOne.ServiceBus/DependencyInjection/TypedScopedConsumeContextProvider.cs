using System;
using System.Threading;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>Provides typed scoped consume context services.</summary>
public class TypedScopedConsumeContextProvider :
    ScopedConsumeContextProvider
{
    readonly IScopedConsumeContextProvider _global;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="global">The global.</param>
    public TypedScopedConsumeContextProvider(IScopedConsumeContextProvider global)
    {
        ArgumentNullException.ThrowIfNull(global);

        _global = global;
    }

    /// <summary>Pushes context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public override IDisposable PushContext(ConsumeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var globalContext = _global.PushContext(context);
        return new CombinedDisposable(base.PushContext(context), globalContext);
    }


    sealed class CombinedDisposable :
        IDisposable
    {
        IDisposable[]? _disposables;

        public CombinedDisposable(params IDisposable[] disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            var disposables = Interlocked.Exchange(ref _disposables, null);
            if (disposables == null)
                return;

            for (var i = 0; i < disposables.Length; i++)
                disposables[i].Dispose();
        }
    }
}
