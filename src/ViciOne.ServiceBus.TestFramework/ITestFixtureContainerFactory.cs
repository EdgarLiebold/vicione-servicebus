// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework
{
    using System;
    using Microsoft.Extensions.DependencyInjection;


    public interface ITestFixtureContainerFactory
    {
        IServiceCollection CreateServiceCollection();
        IServiceProvider BuildServiceProvider(IServiceCollection collection);
    }
}
