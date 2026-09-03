using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.RabbitMqTransport.Operations;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.Tests.RabbitMqTransport.Configuration;

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

    private sealed class TestServiceCollection : List<ServiceDescriptor>, IServiceCollection;
}
