// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using NUnit.Framework;
    using Testing;


    public abstract class FutureTestFixture
    {
        readonly IFutureTestFixtureConfigurator _testFixtureConfigurator;

        protected ServiceProvider Provider;
        protected ITestHarness TestHarness;

        protected FutureTestFixture(IFutureTestFixtureConfigurator testFixtureConfigurator)
        {
            _testFixtureConfigurator = testFixtureConfigurator;
        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            var collection = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(cfg =>
                {
                    _testFixtureConfigurator.ConfigureFutureSagaRepository(cfg);

                    cfg.SetKebabCaseEndpointNameFormatter();

                    ConfigureViciOneServiceBus(cfg);
                });

            _testFixtureConfigurator.ConfigureServices(collection);

            ConfigureServices(collection);

            Provider = collection.BuildServiceProvider(true);

            ConfigureLogging();

            await _testFixtureConfigurator.OneTimeSetup(Provider);

            TestHarness = Provider.GetTestHarness();

            await TestHarness.Start();
        }

        protected virtual void ConfigureViciOneServiceBus(IBusRegistrationConfigurator configurator)
        {
        }

        protected virtual void ConfigureServices(IServiceCollection collection)
        {
        }

        [OneTimeTearDown]
        public async Task Teardown()
        {
            try
            {
                await _testFixtureConfigurator.OneTimeTearDown(Provider);
            }
            finally
            {
                await Provider.DisposeAsync();
            }
        }

        void ConfigureLogging()
        {
            var loggerFactory = Provider.GetRequiredService<ILoggerFactory>();

            LogContext.ConfigureCurrentLogContext(loggerFactory);
        }
    }
}
