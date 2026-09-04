using System;

namespace ViciOne.ServiceBus.DependencyInjection;

public interface IScopedConsumeContextProvider
{
    bool HasContext { get; }
    ConsumeContext GetContext();
    IDisposable PushContext(ConsumeContext context);
}
