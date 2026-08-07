// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus
{
    using System;
    using Microsoft.Extensions.DependencyInjection;


    public interface ISetScopedConsumeContext
    {
        IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context);
    }
}
