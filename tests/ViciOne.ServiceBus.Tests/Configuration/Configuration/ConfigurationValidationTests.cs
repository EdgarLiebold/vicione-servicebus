using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Configuration.Configuration;

public sealed class ConfigurationValidationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-MESSAGE-CONTRACT-VALIDATION", "unconsumed-message-type")]
    public void ConsumerMessageConfiguration_RejectsAContractTheConsumerDoesNotImplement()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => Bus.Factory.CreateUsingInMemory(configurator =>
            configurator.ReceiveEndpoint($"invalid-consumer-message-{NewId.NextGuid():N}", endpoint =>
                endpoint.Consumer<SingleMessageConsumer>(consumer => consumer.Message<UnconsumedMessage>(_ => { })))));

        Assert.Contains(nameof(UnconsumedMessage), exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(SingleMessageConsumer), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(EmptyRetryScope.Endpoint)]
    [InlineData(EmptyRetryScope.Consumer)]
    [InlineData(EmptyRetryScope.ConsumerMessage)]
    [InlineData(EmptyRetryScope.Saga)]
    [InlineData(EmptyRetryScope.SagaMessage)]
    [InlineData(EmptyRetryScope.Handler)]
    [InlineData(EmptyRetryScope.ExecuteActivity)]
    [InlineData(EmptyRetryScope.CompensateActivity)]
    [RequirementCoverage("REQ-VSB-RETRY-CONFIGURATION-VALIDATION", "empty-policy-at-every-observed-scope")]
    public void EmptyRetryPolicy_IsRejectedAtEverySupportedConfigurationScope(EmptyRetryScope scope)
    {
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => Bus.Factory.CreateUsingInMemory(configurator =>
            configurator.ReceiveEndpoint($"empty-retry-{scope}-{NewId.NextGuid():N}", endpoint => ConfigureEmptyRetry(endpoint, scope))));

        Assert.Null(exception.InnerException);
        Assert.Contains("retry", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TRANSPORT-REGISTRATION-VALIDATION", "host-start-without-transport")]
    public async Task HostedServiceStart_RejectsARegistrationWithoutATransportAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddViciOneServiceBusTextWriterLogger(TextWriter.Null)
            .AddViciOneServiceBus(_ => { })
            .BuildServiceProvider();

        ConfigurationException exception = await Assert.ThrowsAsync<ConfigurationException>(async () =>
        {
            IHostedService hostedService = provider.GetServices<IHostedService>()
                .Single(service => service is ViciOneServiceBusHostedService);
            await hostedService.StartAsync(TestContext.Current.CancellationToken);
        });

        Assert.Null(exception.InnerException);
        Assert.Contains("transport", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static void ConfigureEmptyRetry(IInMemoryReceiveEndpointConfigurator endpoint, EmptyRetryScope scope)
    {
        Action<IRetryConfigurator> emptyPolicy = _ => { };

        switch (scope)
        {
            case EmptyRetryScope.Endpoint:
                endpoint.UseMessageRetry(emptyPolicy);
                endpoint.Consumer<SingleMessageConsumer>();
                break;
            case EmptyRetryScope.Consumer:
                endpoint.Consumer<SingleMessageConsumer>(consumer => consumer.UseMessageRetry(emptyPolicy));
                break;
            case EmptyRetryScope.ConsumerMessage:
                endpoint.Consumer<SingleMessageConsumer>(consumer =>
                    consumer.Message<ConsumedMessage>(message => message.UseMessageRetry(emptyPolicy)));
                break;
            case EmptyRetryScope.Saga:
                endpoint.Saga(new InMemorySagaRepository<RetrySaga>(), saga => saga.UseMessageRetry(emptyPolicy));
                break;
            case EmptyRetryScope.SagaMessage:
                endpoint.Saga(new InMemorySagaRepository<RetrySaga>(), saga =>
                    saga.Message<StartSaga>(message => message.UseMessageRetry(emptyPolicy)));
                break;
            case EmptyRetryScope.Handler:
                endpoint.Handler<HandlerMessage>(_ => Task.CompletedTask, handler => handler.UseMessageRetry(emptyPolicy));
                break;
            case EmptyRetryScope.ExecuteActivity:
                endpoint.UseMessageRetry(emptyPolicy);
                endpoint.ExecuteActivityHost<RetryActivity, ActivityArguments>();
                break;
            case EmptyRetryScope.CompensateActivity:
                endpoint.UseMessageRetry(emptyPolicy);
                endpoint.CompensateActivityHost<RetryActivity, ActivityLog>();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unknown retry configuration scope.");
        }
    }

    public sealed record ConsumedMessage;

    public sealed record UnconsumedMessage;

    public sealed class SingleMessageConsumer : IConsumer<ConsumedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ConsumedMessage> context) => Task.CompletedTask;
    }

    public sealed record StartSaga(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class RetrySaga : ISaga, InitiatedBy<StartSaga>
    {
        public RetrySaga(Guid correlationId)
        {
            CorrelationId = correlationId;
        }

        public Guid CorrelationId { get; set; }

        public Task ConsumeAsync(ConsumeContext<StartSaga> context) => Task.CompletedTask;
    }

    public sealed record HandlerMessage;

    public sealed record ActivityArguments;

    public sealed record ActivityLog;

    public sealed class RetryActivity : IActivity<ActivityArguments, ActivityLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ActivityArguments> context) =>
            Task.FromResult(context.Completed(new ActivityLog()));

        public Task<CompensationResult> CompensateAsync(CompensateContext<ActivityLog> context) =>
            Task.FromResult(context.Compensated());
    }

    public enum EmptyRetryScope
    {
        Endpoint,
        Consumer,
        ConsumerMessage,
        Saga,
        SagaMessage,
        Handler,
        ExecuteActivity,
        CompensateActivity,
    }
}
