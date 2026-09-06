using System;

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
        _global = global;
    }

    /// <summary>Pushes context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <returns>The disposable produced by the operation.</returns>
    public override IDisposable PushContext(ConsumeContext context)
    {
        return new CombinedDisposable(_global.PushContext(context), base.PushContext(context));
    }


    class CombinedDisposable :
        IDisposable
    {
        readonly IDisposable[] _disposables;

        public CombinedDisposable(params IDisposable[] disposables)
        {
            _disposables = disposables;
        }

        public void Dispose()
        {
            for (var i = 0; i < _disposables.Length; i++)
                _disposables[i].Dispose();
        }
    }
}
