using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ConsumerKindExtensionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CAPABILITY-PACKAGES", "dispatcher-success-guarantees-non-null-result")]
    public void DispatcherProvider_DeclaresThatSuccessProducesANonNullDispatcher()
    {
        MethodInfo method = Assert.Single(
            typeof(IConsumerKindDispatcherProvider).GetMethods(),
            candidate => candidate.Name == nameof(IConsumerKindDispatcherProvider.TryCreateDispatcher));
        ParameterInfo dispatcher = Assert.Single(method.GetParameters(), parameter => parameter.Name == "dispatcher");
        NotNullWhenAttribute annotation = Assert.Single(dispatcher.GetCustomAttributes<NotNullWhenAttribute>());

        Assert.True(annotation.ReturnValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CAPABILITY-PACKAGES", "consumer-kind-segregates-optional-capabilities")]
    public void ConsumerKindContract_HasNoSilentOptionalOperations()
    {
        string[] commonOperations = typeof(IConsumerKind)
            .GetMethods()
            .Select(method => method.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            ["ConfigureTestHarness", "GetRegistrations", "get_IsFallback", "get_Name", "get_Order"],
            commonOperations);
        Assert.All(typeof(IConsumerKind).GetMethods(), method => Assert.True(method.IsAbstract));
        Assert.False(typeof(IConsumerKindRuntimeConfigurator).IsAssignableFrom(typeof(IConsumerKind)));
        Assert.False(typeof(IConsumerKindTypedConfigurator).IsAssignableFrom(typeof(IConsumerKind)));
        Assert.False(typeof(IConsumerKindBulkConfigurator).IsAssignableFrom(typeof(IConsumerKind)));
        Assert.False(typeof(IConsumerKindCompanionConfigurator).IsAssignableFrom(typeof(IConsumerKind)));
        Assert.False(typeof(IConsumerKindDispatcherProvider).IsAssignableFrom(typeof(IConsumerKind)));
        Assert.False(typeof(IConsumerKindServiceRequirement).IsAssignableFrom(typeof(IConsumerKind)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CAPABILITY-PACKAGES", "custom-consumer-kind-rejects-unsupported-harness-shape")]
    public void CustomConsumerKind_RejectsAnUnsupportedHarnessObservationShape()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IConsumerKind>(new UnsupportedConsumerKind());

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            services.AddViciOneServiceBusTestHarness());

        Assert.Contains(nameof(UnsupportedRegistration), exception.Message, StringComparison.Ordinal);
        Assert.Contains("UnsupportedProbe", exception.Message, StringComparison.Ordinal);
        Assert.Contains("Observe a consumer, saga, state machine, or activity registration", exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CAPABILITY-PACKAGES", "custom-consumer-kind-complete-endpoint-and-harness-contract")]
    public async Task CustomConsumerKind_MaterializesItsCompleteEndpointAndHarnessContractAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions().OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var probe = new ConsumerKindProbe();
        var kind = new ProbeConsumerKind(probe);
        var services = new ServiceCollection();
        services.AddSingleton<IConsumerKind>(kind);

        await using ServiceProvider provider = services
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.SetKebabCaseEndpointNameFormatter();
                configuration.AddConsumer<ProbeConsumer>();
            })
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });

        Assert.Equal(kind.Name, kind.HarnessKindName);
        Assert.NotNull(provider.GetService<IConsumerTestHarness<ProbeConsumer>>());
        Assert.Contains(kind, provider.GetServices<IConsumerKind>());

        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: cancellationToken)
            .WaitAsync(timeout, cancellationToken);

        try
        {
            ISendEndpoint primary = await harness.Bus
                .GetSendEndpointAsync(new Uri($"queue:{ProbeConsumerKind.PrimaryEndpointName}"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            ISendEndpoint companion = await harness.Bus
                .GetSendEndpointAsync(new Uri($"queue:{ProbeConsumerKind.CompanionEndpointName}"), cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            await primary.SendAsync(new ProbeMessage("primary"), cancellationToken);
            await companion.SendAsync(new ProbeCompanionMessage("companion"), cancellationToken);

            ProbeDelivery primaryDelivery = await probe.Primary.Task.WaitAsync(timeout, cancellationToken);
            ProbeDelivery companionDelivery = await probe.Companion.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(new ProbeDelivery("primary", ProbeConsumerKind.PrimaryEndpointName), primaryDelivery);
            Assert.Equal(new ProbeDelivery("companion", ProbeConsumerKind.CompanionEndpointName), companionDelivery);
            Assert.Equal(1, kind.GetRegistrationsCallCount);
            Assert.Equal(1, kind.ConfigureCallCount);
            Assert.NotNull(kind.PlanningContext);
            Assert.NotNull(kind.EndpointContext);
            Assert.Same(kind.Definition, kind.RegistrationDefinition);
            Assert.Same(kind.EndpointDefinition, kind.RegistrationEndpointDefinition);
            Assert.Same(kind.PlanningContext!.RegistrationContext, kind.EndpointContext!.RegistrationContext);
            Assert.Equal(1, kind.EndpointDefinition.ConfigureCount);
            Assert.Same(kind.EndpointContext.RegistrationContext, kind.EndpointDefinition.LastRegistrationContext);
            Assert.Equal(
                ProbeConsumerKind.PrimaryEndpointName,
                kind.EndpointContext.EndpointConfigurator.InputAddress.AbsolutePath.Trim('/'));
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed record ProbeMessage(string Value);

    public sealed record ProbeCompanionMessage(string Value);

    public sealed record ProbeDelivery(string Value, string EndpointName);

    public sealed class ProbeConsumer :
        IConsumer<ProbeMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ProbeMessage> context) => Task.CompletedTask;
    }

    private sealed class ConsumerKindProbe
    {
        public TaskCompletionSource<ProbeDelivery> Primary { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource<ProbeDelivery> Companion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    private sealed class ProbeConsumerKind :
        IConsumerKind
    {
        public const string PrimaryEndpointName = "custom-kind-primary";
        public const string CompanionEndpointName = "custom-kind-companion";
        private readonly ConsumerKindProbe _probe;
        private int _configureCallCount;
        private int _getRegistrationsCallCount;

        public ProbeConsumerKind(ConsumerKindProbe probe)
        {
            _probe = probe;
            Definition = new ProbeDefinition();
            EndpointDefinition = new ProbeEndpointDefinition(PrimaryEndpointName);
        }

        public string Name => "Probe";

        public bool IsFallback => false;

        public int Order => -100;

        public int ConfigureCallCount => Volatile.Read(ref _configureCallCount);

        public int GetRegistrationsCallCount => Volatile.Read(ref _getRegistrationsCallCount);

        public string? HarnessKindName { get; private set; }

        public IConsumerKindContext? PlanningContext { get; private set; }

        public IConsumerKindEndpointContext? EndpointContext { get; private set; }

        public ProbeDefinition Definition { get; }

        public ProbeEndpointDefinition EndpointDefinition { get; }

        public IDefinition? RegistrationDefinition { get; private set; }

        public IEndpointDefinition? RegistrationEndpointDefinition { get; private set; }

        public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            Interlocked.Increment(ref _getRegistrationsCallCount);
            PlanningContext = context;
            var registration = new ProbeRegistration(this, _probe);
            RegistrationDefinition = registration.Definition;
            RegistrationEndpointDefinition = registration.EndpointDefinition;
            return [registration];
        }

        public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
        {
            ArgumentNullException.ThrowIfNull(context);
            HarnessKindName = context.KindName;
            context.Observe(typeof(ProbeConsumer));
        }

        private sealed class ProbeRegistration :
            IConsumerKindRegistration
        {
            private static readonly IReadOnlyCollection<string> CompanionEndpoints = [CompanionEndpointName];
            private readonly ProbeConsumerKind _kind;
            private readonly ConsumerKindProbe _probe;

            public ProbeRegistration(ProbeConsumerKind kind, ConsumerKindProbe probe)
            {
                _kind = kind;
                _probe = probe;
            }

            public Type RegistrationType => typeof(ProbeConsumer);

            public IDefinition Definition => _kind.Definition;

            public string EndpointName => PrimaryEndpointName;

            public IEndpointDefinition EndpointDefinition => _kind.EndpointDefinition;

            public bool RequiresServiceInstance => false;

            public IReadOnlyCollection<string> CompanionEndpointNames => CompanionEndpoints;

            public void Configure(IConsumerKindEndpointContext context)
            {
                ArgumentNullException.ThrowIfNull(context);
                Interlocked.Increment(ref _kind._configureCallCount);
                _kind.EndpointContext = context;
                context.EndpointConfigurator.Handler<ProbeMessage>(message =>
                {
                    _probe.Primary.TrySetResult(new ProbeDelivery(
                        message.Message.Value,
                        message.Advanced().ReceiveContext.InputAddress.AbsolutePath.Trim('/')));
                    return Task.CompletedTask;
                });
                context.ConfigureCompanionEndpoint(CompanionEndpointName, null, endpoint =>
                    endpoint.Handler<ProbeCompanionMessage>(message =>
                    {
                        _probe.Companion.TrySetResult(new ProbeDelivery(
                            message.Message.Value,
                            message.Advanced().ReceiveContext.InputAddress.AbsolutePath.Trim('/')));
                        return Task.CompletedTask;
                    }));
            }
        }
    }

    private sealed class ProbeDefinition :
        IDefinition
    {
        public int? ConcurrentMessageLimit => 1;
    }

    private sealed class ProbeEndpointDefinition :
        IEndpointDefinition
    {
        private readonly string _endpointName;
        private int _configureCount;

        public ProbeEndpointDefinition(string endpointName)
        {
            _endpointName = endpointName;
        }

        public bool ConfigureConsumeTopology => true;

        public bool IsTemporary => false;

        public int? PrefetchCount => 2;

        public int? ConcurrentMessageLimit => 1;

        public int ConfigureCount => Volatile.Read(ref _configureCount);

        public IRegistrationContext? LastRegistrationContext { get; private set; }

        public string GetEndpointName(IEndpointNameFormatter formatter) => _endpointName;

        public void Configure<T>(T configurator, IRegistrationContext? context)
            where T : IReceiveEndpointConfigurator
        {
            Interlocked.Increment(ref _configureCount);
            LastRegistrationContext = context;
        }
    }

    private sealed class UnsupportedConsumerKind :
        IConsumerKind
    {
        public string Name => "UnsupportedProbe";

        public bool IsFallback => false;

        public int Order => 0;

        public IEnumerable<IConsumerKindRegistration> GetRegistrations(IConsumerKindContext context) =>
            Array.Empty<IConsumerKindRegistration>();

        public void ConfigureTestHarness(IConsumerKindTestHarnessContext context)
        {
            context.Observe(typeof(UnsupportedRegistration));
        }
    }

    private sealed class UnsupportedRegistration;
}
