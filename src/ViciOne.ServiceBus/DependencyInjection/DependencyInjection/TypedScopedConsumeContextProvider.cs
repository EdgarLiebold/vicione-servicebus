// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;


    public class TypedScopedConsumeContextProvider :
        ScopedConsumeContextProvider
    {
        readonly IScopedConsumeContextProvider _global;

        public TypedScopedConsumeContextProvider(IScopedConsumeContextProvider global)
        {
            _global = global;
        }

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
}
