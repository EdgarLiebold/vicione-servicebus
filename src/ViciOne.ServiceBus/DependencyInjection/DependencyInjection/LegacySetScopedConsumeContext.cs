// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;
    using Microsoft.Extensions.DependencyInjection;


    public class LegacySetScopedConsumeContext :
        ISetScopedConsumeContext
    {
        public static readonly ISetScopedConsumeContext Instance = new LegacySetScopedConsumeContext();

        LegacySetScopedConsumeContext()
        {
        }

        public IDisposable PushContext(IServiceScope scope, ConsumeContext context)
        {
            return scope.ServiceProvider.GetRequiredService<IScopedConsumeContextProvider>().PushContext(context);
        }
    }
}
