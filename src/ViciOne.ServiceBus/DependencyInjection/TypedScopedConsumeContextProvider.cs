using System;

namespace ViciOne.ServiceBus.DependencyInjection;

/// <summary>
/// Provides a typed scoped consume context provider implementation.
/// </summary>
public class TypedScopedConsumeContextProvider :
    ScopedConsumeContextProvider
{
    readonly IScopedConsumeContextProvider _global;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="global">The global value.</param>
    public TypedScopedConsumeContextProvider(IScopedConsumeContextProvider global)
    {
        _global = global;
    }

    /// <summary>
    /// Performs the push context operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    /// <returns>The result of the operation.</returns>
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
