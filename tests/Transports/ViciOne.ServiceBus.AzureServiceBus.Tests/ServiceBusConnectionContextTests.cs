using System.Reflection;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.AzureServiceBus.Topology;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusConnectionContextTests
{
    [Theory]
    [InlineData("ambient")]
    [InlineData("token")]
    [InlineData("messaging-client")]
    [InlineData("administration-client")]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "custom-port-requires-credential-bearing-string-or-complete-clients")]
    public async Task CustomPortWithoutConnectionString_RejectsIncompleteClientOrCredentialRoutesAsync(string route)
    {
        var address = new Uri("sb://localhost:5672/");
        var settings = new HostSettings { ServiceUri = address };
        switch (route)
        {
            case "token":
                settings.TokenCredential = new Azure.Identity.DefaultAzureCredential();
                break;
            case "messaging-client":
                settings.ServiceBusClient = new RecordingServiceBusClient(namespaceName: "localhost");
                break;
            case "administration-client":
                settings.ServiceBusAdministrationClient = new RecordingServiceBusAdministrationClient();
                break;
        }

        IServiceBusHostConfiguration configuration = DispatchProxy.Create<IServiceBusHostConfiguration, HostConfigurationProxy>();
        var proxy = (HostConfigurationProxy)(object)configuration;
        proxy.Address = address;
        proxy.Settings = settings;
        var supervisor = new Supervisor();
        IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(configuration);

        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(supervisor);
        ServiceBusConnectionException exception = await Assert.ThrowsAsync<ServiceBusConnectionException>(() => agent.Context);

        Assert.Contains("custom port", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(address, configuration.HostAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "custom-port-accepts-complete-caller-supplied-clients")]
    public async Task CustomPortWithBothPreconfiguredClients_CreatesTheSuppliedNamespaceContextAsync()
    {
        var address = new Uri("sb://localhost:5672/scope");
        var client = new RecordingServiceBusClient(namespaceName: "localhost");
        var administration = new RecordingServiceBusAdministrationClient();
        var settings = new HostSettings
        {
            ServiceUri = address,
            ServiceBusClient = client,
            ServiceBusAdministrationClient = administration,
        };
        IServiceBusHostConfiguration configuration = DispatchProxy.Create<IServiceBusHostConfiguration, HostConfigurationProxy>();
        var proxy = (HostConfigurationProxy)(object)configuration;
        proxy.Address = address;
        proxy.Settings = settings;
        var supervisor = new Supervisor();
        IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(configuration);

        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(supervisor);
        ConnectionContext connection = await agent.Context;

        var concrete = Assert.IsType<ServiceBusConnectionContext>(connection);
        Assert.Equal(new Uri("sb://localhost:5672/"), concrete.Endpoint);
        _ = concrete.CreateQueueProcessor(CreateSettings());
        Assert.NotNull(client.ProcessorOptions);
        Uri inputAddress = CreateSettings().GetInputAddress(concrete.Endpoint, "processor-input");
        Assert.Equal(5672, inputAddress.Port);
        Assert.Equal("/processor-input", inputAddress.AbsolutePath);
        using var caller = new CancellationTokenSource();
        InvalidOperationException failure = await Assert.ThrowsAsync<InvalidOperationException>(
            () => concrete.CreateQueueAsync(new CreateQueueOptions("admin-probe"), caller.Token));
        Assert.Same(administration.Failure, failure);
        Assert.Equal("admin-probe", administration.RequestedQueue);
        Assert.Equal(caller.Token, administration.RequestedToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "custom-port-accepts-credential-bearing-emulator-string")]
    public async Task CustomPortWithEmulatorConnectionString_CreatesSdkClientsAsync()
    {
        var address = new Uri("sb://localhost:5672/");
        var settings = new HostSettings
        {
            ServiceUri = address,
            ConnectionString = "Endpoint=sb://localhost:5672/;SharedAccessKeyName=unit;SharedAccessKey=dGVzdA==;UseDevelopmentEmulator=true",
        };
        IServiceBusHostConfiguration configuration = DispatchProxy.Create<IServiceBusHostConfiguration, HostConfigurationProxy>();
        var proxy = (HostConfigurationProxy)(object)configuration;
        proxy.Address = address;
        proxy.Settings = settings;
        IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(configuration);

        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new Supervisor());
        await using var connection = Assert.IsType<ServiceBusConnectionContext>(await agent.Context);

        Assert.Equal(address, connection.Endpoint);
        Assert.Equal(5672, CreateSettings().GetInputAddress(connection.Endpoint, "processor-input").Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "preconfigured-client-namespace-must-match-host")]
    public async Task PreconfiguredMessagingClientFromAnotherNamespace_IsRejectedAtConfigurationAndFactoryBoundariesAsync()
    {
        var address = new Uri("sb://localhost:5672/");
        var client = new RecordingServiceBusClient();
        var administration = new RecordingServiceBusAdministrationClient();

        ArgumentException configurationFailure = Assert.Throws<ArgumentException>(
            () => new ViciOne.ServiceBus.Configuration.ServiceBusHostConfigurator(address, client, administration));
        Assert.Equal("serviceBusClient", configurationFailure.ParamName);

        var settings = new HostSettings
        {
            ServiceUri = address,
            ServiceBusClient = client,
            ServiceBusAdministrationClient = administration,
        };
        IServiceBusHostConfiguration configuration = DispatchProxy.Create<IServiceBusHostConfiguration, HostConfigurationProxy>();
        var proxy = (HostConfigurationProxy)(object)configuration;
        proxy.Address = address;
        proxy.Settings = settings;
        IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(configuration);
        IPipeContextAgent<ConnectionContext> agent = factory.CreateContext(new Supervisor());

        ServiceBusConnectionException factoryFailure = await Assert.ThrowsAsync<ServiceBusConnectionException>(() => agent.Context);
        Assert.Contains("namespace", factoryFailure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "queue-entity-path-follows-initialized-processor-kind")]
    public async Task QueueClientEntityPath_UsesTheInitializedMessageOrSessionProcessorAsync()
    {
        const string connectionString = "Endpoint=sb://unit.servicebus.invalid/;SharedAccessKeyName=unit;SharedAccessKey=dGVzdA==";
        await using var client = new ServiceBusClient(connectionString);
        ReceiveEndpointSettings settings = CreateSettings();
        ConnectionContext connection = DispatchProxy.Create<ConnectionContext, ProcessorConnectionProxy>();
        ((ProcessorConnectionProxy)(object)connection).Handler = (method, args) =>
        {
            Assert.Same(settings, Assert.Single(args));
            return method.Name switch
            {
                nameof(ConnectionContext.CreateQueueProcessor) => client.CreateProcessor("sdk-message-queue"),
                nameof(ConnectionContext.CreateQueueSessionProcessor) => client.CreateSessionProcessor("sdk-session-queue"),
                _ => throw new NotSupportedException(method.Name),
            };
        };
        var inputAddress = new Uri("sb://unit.servicebus.invalid/different-input");

        await using var messageContext = new QueueClientContext(connection, inputAddress, settings, null!);
        InvalidOperationException beforeMessage = Assert.Throws<InvalidOperationException>(() => messageContext.EntityPath);
        Assert.Contains("not been initialized", beforeMessage.Message);
        messageContext.ConfigureMessageProcessor(
            static (_, _, _) => Task.CompletedTask,
            static _ => Task.CompletedTask);
        Assert.Equal("sdk-message-queue", messageContext.EntityPath);

        await using var sessionContext = new QueueClientContext(connection, inputAddress, settings, null!);
        InvalidOperationException beforeSession = Assert.Throws<InvalidOperationException>(() => sessionContext.EntityPath);
        Assert.Contains("not been initialized", beforeSession.Message);
        sessionContext.ConfigureSessionProcessor(
            static (_, _, _) => Task.CompletedTask,
            static _ => Task.CompletedTask);
        Assert.Equal("sdk-session-queue", sessionContext.EntityPath);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-PROCESSOR-LIFECYCLE", "queue-start-forwards-caller-cancellation-token")]
    public async Task QueueClientStart_ForwardsTheCallerCancellationTokenAsync()
    {
        var processor = new RecordingServiceBusProcessor();
        var client = new RecordingServiceBusClient(processor);
        var connection = new ServiceBusConnectionContext(client, null!, CancellationToken.None);
        ReceiveEndpointSettings settings = CreateSettings();
        var context = new QueueClientContext(connection, new Uri("sb://unit.servicebus.invalid/input"), settings, null!);
        context.ConfigureMessageProcessor(
            static (_, _, _) => Task.CompletedTask,
            static _ => Task.CompletedTask);
        using var caller = new CancellationTokenSource();

        await context.StartAsync(caller.Token);

        Assert.Equal(caller.Token, processor.StartToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-ENDPOINT-CONFIGURATION", "processor-options-project-the-complete-client-settings")]
    public void ProcessorFactories_ProjectTheCompleteClientSettingsIntoTheSdkBoundary()
    {
        var client = new RecordingServiceBusClient();
        var context = new ServiceBusConnectionContext(client, null!, CancellationToken.None);
        ReceiveEndpointSettings settings = CreateSettings();

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

    static ReceiveEndpointSettings CreateSettings()
    {
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
        return new ReceiveEndpointSettings(endpointConfiguration, "processor-input", queueConfigurator)
        {
            MaxAutoRenewDuration = TimeSpan.FromMinutes(17),
            SessionIdleTimeout = TimeSpan.FromSeconds(31),
        };
    }

    sealed class RecordingServiceBusClient(RecordingServiceBusProcessor? processor = null, string namespaceName = "unit.servicebus.invalid") : ServiceBusClient
    {
        public override string FullyQualifiedNamespace => namespaceName;

        public ServiceBusProcessorOptions? ProcessorOptions { get; private set; }
        public ServiceBusSessionProcessorOptions? SessionProcessorOptions { get; private set; }

        public override ServiceBusProcessor CreateProcessor(string queueName, ServiceBusProcessorOptions options)
        {
            Assert.Equal("processor-input", queueName);
            ProcessorOptions = options;
            return processor!;
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

    sealed class RecordingServiceBusProcessor : ServiceBusProcessor
    {
        public CancellationToken StartToken { get; private set; }

        public override Task StartProcessingAsync(CancellationToken cancellationToken = default)
        {
            StartToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    sealed class RecordingServiceBusAdministrationClient : ServiceBusAdministrationClient
    {
        public InvalidOperationException Failure { get; } = new("Administration client probe");
        public string? RequestedQueue { get; private set; }
        public CancellationToken RequestedToken { get; private set; }

        public override Task<global::Azure.Response<global::Azure.Messaging.ServiceBus.Administration.QueueProperties>> GetQueueAsync(
            string queueName, CancellationToken cancellationToken = default)
        {
            RequestedQueue = queueName;
            RequestedToken = cancellationToken;
            throw Failure;
        }
    }

    public class ProcessorConnectionProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[], object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(Assert.IsAssignableFrom<MethodInfo>(targetMethod), args ?? []);
    }

    public class HostConfigurationProxy : DispatchProxy
    {
        public Uri Address { get; set; } = null!;
        public ServiceBusHostSettings Settings { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_HostAddress" => Address,
            "get_Settings" => Settings,
            _ => throw new NotSupportedException(targetMethod?.Name),
        };
    }
}
