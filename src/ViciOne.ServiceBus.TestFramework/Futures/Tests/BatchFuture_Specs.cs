// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Futures.Tests;

using NUnit.Framework;


[TestFixture]
public class BatchFuture_Specs :
    FutureTestFixture
{
    public BatchFuture_Specs(IFutureTestFixtureConfigurator testFixtureConfigurator)
        : base(testFixtureConfigurator)
    {
    }

    protected override void ConfigureViciOneServiceBus(IBusRegistrationConfigurator configurator)
    {
        configurator.AddConsumer<ProcessBatchItemConsumer>();
        configurator.AddFuture<BatchFuture>();
    }
}
