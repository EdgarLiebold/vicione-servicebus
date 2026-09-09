using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureRequestConsumerIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REQUEST-CONSUMER", "registration-helper-completes-through-private-consumer-endpoint")]
    public async Task RegistrationHelper_CompletesTheFutureWithTheExactConsumerResponseAsync()
    {
        await using Fixture fixture = await Fixture.StartAsync();
        Guid orderLineId = NewId.NextGuid();

        Response<PriceCalculation> response = await fixture.Client.GetResponseAsync<PriceCalculation>(
            new CalculatePriceMessage(orderLineId, "90210"),
            fixture.CancellationToken).WaitAsync(fixture.Timeout, fixture.CancellationToken);

        Assert.Equal(orderLineId, response.Message.CorrelationId);
        Assert.Equal(1234.55m, response.Message.Amount);
        Assert.Equal(2, fixture.Harness.Consumed.Select<CalculatePrice>(SnapshotOnlyToken()).Count());
        Assert.Empty(fixture.Harness.Published.Select<Fault<CalculatePrice>>(SnapshotOnlyToken()));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REQUEST-CONSUMER", "registration-helper-propagates-consumer-fault")]
    public async Task RegistrationHelper_PropagatesTheExactConsumerFailureAsARequestFaultAsync()
    {
        await using Fixture fixture = await Fixture.StartAsync();
        Guid orderLineId = NewId.NextGuid();

        RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
            fixture.Client.GetResponseAsync<PriceCalculation>(
                new CalculatePriceMessage(orderLineId, "missing"),
                fixture.CancellationToken));

        Assert.Equal(typeof(CalculatePrice), exception.RequestType);
        Fault<CalculatePrice> typedFault = Assert.IsAssignableFrom<Fault<CalculatePrice>>(exception.Fault);
        Assert.Equal(orderLineId, typedFault.Message.CorrelationId);
        Assert.Equal("missing", typedFault.Message.Sku);
        ExceptionInfo fault = Assert.Single(typedFault.Exceptions);
        Assert.Equal(typeof(ExpectedPriceException).FullName, fault.ExceptionType);
        Assert.Contains("missing", fault.Message, StringComparison.Ordinal);
        Assert.Equal(2, fixture.Harness.Consumed.Select<CalculatePrice>(SnapshotOnlyToken()).Count());
    }

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    public interface CalculatePrice : CorrelatedBy<Guid>
    {
        string Sku { get; }
    }

    public sealed record CalculatePriceMessage(Guid CorrelationId, string Sku) : CalculatePrice;

    public interface PriceCalculation
    {
        Guid CorrelationId { get; }

        decimal Amount { get; }
    }

    public sealed class CalculatePriceConsumer : IConsumer<CalculatePrice>
    {
        public Task ConsumeAsync(ConsumeContext<CalculatePrice> context)
        {
            if (context.Message.Sku == "missing")
                throw new ExpectedPriceException(context.Message.Sku);

            return context.Advanced().RespondAsync<PriceCalculation>(new
            {
                context.Message.CorrelationId,
                Amount = 1234.55m,
            });
        }
    }

    public sealed class PriceFuture : RequestConsumerFuture<CalculatePrice, PriceCalculation>
    {
        public PriceFuture(IFutureDefinition<PriceFuture> definition)
            : base(definition)
        {
            ConfigureCommand(configuration =>
                configuration.CorrelateById(context => context.Message.CorrelationId));
        }
    }

    public sealed class ExpectedPriceException(string sku) : Exception($"The sku '{sku}' is missing");

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly ServiceProvider _provider;

        private Fixture(ServiceProvider provider, ITestHarness harness, TimeSpan timeout)
        {
            _provider = provider;
            Harness = harness;
            Timeout = timeout;
            Client = harness.GetRequestClient<CalculatePrice>();
        }

        public CancellationToken CancellationToken => TestContext.Current.CancellationToken;

        public IRequestClient<CalculatePrice> Client { get; }

        public ITestHarness Harness { get; }

        public TimeSpan Timeout { get; }

        public static async Task<Fixture> StartAsync()
        {
            TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
                .GetValidatedOptions().OperationTimeout!.Value;
            ServiceProvider provider = new ServiceCollection()
                .AddViciOneServiceBusTestHarness(configuration =>
                {
                    configuration.SetTestTimeouts(timeout, timeout);
                    configuration.AddFutureRequestConsumer<
                        PriceFuture,
                        CalculatePriceConsumer,
                        CalculatePrice,
                        PriceCalculation>();
                })
                .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
            try
            {
                ITestHarness harness = await provider.StartTestHarnessAsync()
                    .WaitAsync(timeout, TestContext.Current.CancellationToken);
                return new Fixture(provider, harness, timeout);
            }
            catch
            {
                await provider.DisposeAsync();
                throw;
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await Harness.StopAsync(CancellationToken.None).WaitAsync(Timeout, CancellationToken.None);
            }
            finally
            {
                await _provider.DisposeAsync();
            }
        }
    }
}
