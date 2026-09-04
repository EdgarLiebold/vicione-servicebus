using System;
using Microsoft.Extensions.DependencyInjection;

namespace ViciOne.ServiceBus;

public interface ISetScopedConsumeContext
{
    IDisposable PushContext(IServiceScope serviceProvider, ConsumeContext context);
}
