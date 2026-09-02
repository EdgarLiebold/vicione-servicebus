using Azure.Messaging.ServiceBus;
using ViciOne.ServiceBus.AzureServiceBusTransport;
using ViciOne.ServiceBus.AzureServiceBusTransport.Configuration;
using ViciOne.ServiceBus.AzureServiceBusTransport.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Azure.ServiceBus.Core.Tests;

public sealed class ServiceBusConnectionContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-CONFIGURATION", "processor-options-project-the-complete-client-settings")]
    public void ProcessorFactories_ProjectTheCompleteClientSettingsIntoTheSdkBoundary()
    {
        var client = new RecordingServiceBusClient();
        var context = new ServiceBusConnectionContext(client, null!, CancellationToken.None);
        var busConfiguration = new ServiceBusBusConfiguration(
            new ServiceBusTopologyConfiguration(AzureBusFactory.CreateMessageTopology()))
        {
            PrefetchCount = 427,
            ConcurrentMessageLimit = 13,
        };
        var endpointConfiguration = (ServiceBusEndpointConfiguration)busConfiguration.CreateEndpointConfiguration(false);
        var queueConfigurator = new ServiceBusQueueConfigurator("processor-input")
        {
            MaxConcurrentSessions = 7,
            MaxConcurrentCallsPerSession = 3,
        };
        var settings = new ReceiveEndpointSettings(endpointConfiguration, "processor-input", queueConfigurator)
        {
            MaxAutoRenewDuration = TimeSpan.FromMinutes(17),
            SessionIdleTimeout = TimeSpan.FromSeconds(31),
        };

        _ = context.CreateQueueProcessor(settings);
        _ = context.CreateQueueSessionProcessor(settings);

        ServiceBusProcessorOptions processor = Assert.IsType<ServiceBusProcessorOptions>(client.ProcessorOptions);
        Assert.Equal(427, processor.PrefetchCount);
        Assert.Equal(13, processor.MaxConcurrentCalls);
        Assert.Equal(TimeSpan.FromMinutes(17), processor.MaxAutoLockRenewalDuration);
        Assert.Equal(ServiceBusReceiveMode.PeekLock, processor.ReceiveMode);
        Assert.False(processor.AutoCompleteMessages);

        ServiceBusSessionProcessorOptions session = Assert.IsType<ServiceBusSessionProcessorOptions>(client.SessionProcessorOptions);
        Assert.Equal(427, session.PrefetchCount);
        Assert.Equal(7, session.MaxConcurrentSessions);
        Assert.Equal(3, session.MaxConcurrentCallsPerSession);
        Assert.Equal(TimeSpan.FromMinutes(17), session.MaxAutoLockRenewalDuration);
        Assert.Equal(TimeSpan.FromSeconds(31), session.SessionIdleTimeout);
        Assert.Equal(ServiceBusReceiveMode.PeekLock, session.ReceiveMode);
        Assert.False(session.AutoCompleteMessages);
    }

    sealed class RecordingServiceBusClient : ServiceBusClient
    {
        public override string FullyQualifiedNamespace => "unit.servicebus.invalid";

        public ServiceBusProcessorOptions? ProcessorOptions { get; private set; }
        public ServiceBusSessionProcessorOptions? SessionProcessorOptions { get; private set; }

        public override ServiceBusProcessor CreateProcessor(string queueName, ServiceBusProcessorOptions options)
        {
            Assert.Equal("processor-input", queueName);
            ProcessorOptions = options;
            return null!;
        }

        public override ServiceBusSessionProcessor CreateSessionProcessor(
            string queueName,
            ServiceBusSessionProcessorOptions? options = null)
        {
            Assert.Equal("processor-input", queueName);
            SessionProcessorOptions = options;
            return null!;
        }
    }

}
