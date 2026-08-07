// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DependencyInjection
{
    using System;


    public interface IScopedConsumeContextProvider
    {
        bool HasContext { get; }
        ConsumeContext GetContext();
        IDisposable PushContext(ConsumeContext context);
    }
}
