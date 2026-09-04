using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.RabbitMq.Operations;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.RabbitMqTransport.Configuration;

public sealed class RabbitMqReliabilityRegistrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-RELIABILITY-DI", "classifier-and-queue-operations-registration")]
    public void UsingRabbitMq_RegistersClassifierAndBothQueueOperationOwnersAsSingletons()
    {
        IServiceCollection services = new TestServiceCollection();

        services.AddViciOneServiceBus(configurator => configurator.UsingRabbitMq());

        ServiceDescriptor classifier = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(ITransportSendFailureClassifier));
        ServiceDescriptor defaultOperations = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IRabbitMqQueueOperations));
        ServiceDescriptor typedOperations = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IRabbitMqQueueOperations<>));
        Assert.Equal(typeof(RabbitMqSendFailureClassifier), classifier.ImplementationType);
        Assert.Equal(typeof(RabbitMqQueueOperations), defaultOperations.ImplementationType);
        Assert.Equal(typeof(RabbitMqQueueOperations<>), typedOperations.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, classifier.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, defaultOperations.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, typedOperations.Lifetime);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-SEND", "single-provider-owned-dispatcher-registration")]
    public void UsingRabbitMq_RegistersExactlyOneProviderOwnedDurableDispatcher(bool transportFirst)
    {
        IServiceCollection services = new TestServiceCollection();

        services.AddViciOneServiceBus(configurator =>
        {
            if (transportFirst)
                configurator.UsingRabbitMq();
            configurator.UseDurableSender(durable => durable
                .UseInMemoryStore());
            if (!transportFirst)
                configurator.UsingRabbitMq();
        });

        ServiceDescriptor dispatcher = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableSendDispatcher<IBus>));
        Assert.Equal(typeof(RabbitMqDurableSendDispatcher<IBus>), dispatcher.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, dispatcher.Lifetime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-DURABLE-SEND", "typed-bus-provider-owned-dispatcher-registration")]
    public void UsingRabbitMq_TypedBusOwnsItsMatchingDurableDispatcher()
    {
        IServiceCollection services = new TestServiceCollection();

        services.AddViciOneServiceBus<ITestBus>(configurator =>
        {
            configurator.UseDurableSender(durable => durable.UseInMemoryStore());
            configurator.UsingRabbitMq();
        });

        ServiceDescriptor dispatcher = Assert.Single(
            services,
            descriptor => descriptor.ServiceType == typeof(IDurableSendDispatcher<ITestBus>));
        Assert.Equal(typeof(RabbitMqDurableSendDispatcher<ITestBus>), dispatcher.ImplementationType);
        Assert.Equal(ServiceLifetime.Singleton, dispatcher.Lifetime);
        Assert.DoesNotContain(services, descriptor =>
            descriptor.ServiceType == typeof(IDurableSendDispatcher<IBus>));
    }

    private sealed class TestServiceCollection : List<ServiceDescriptor>, IServiceCollection;

    public interface ITestBus : IBus;
}
