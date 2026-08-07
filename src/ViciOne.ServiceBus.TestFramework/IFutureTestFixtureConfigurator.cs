// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;


    public interface IFutureTestFixtureConfigurator
    {
        void ConfigureFutureSagaRepository(IBusRegistrationConfigurator configurator);
        void ConfigureServices(IServiceCollection collection);
        Task OneTimeSetup(IServiceProvider provider);
        Task OneTimeTearDown(IServiceProvider provider);
    }
}
