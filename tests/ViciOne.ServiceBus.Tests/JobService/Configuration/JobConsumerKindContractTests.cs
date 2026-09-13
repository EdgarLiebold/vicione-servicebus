using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.JobService.Configuration;

public sealed class JobConsumerKindContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONSUMER-KIND", "planning-claims-only-job-consumers-with-complete-metadata")]
    public void Planning_ClaimsOnlyJobConsumersAndExposesCompleteEndpointMetadata()
    {
        var kind = new JobConsumerKind();
        IRegistrationContext registrationContext = CreateRegistrationContext(new RecordingContainerSelector());
        var definition = new RecordingConsumerDefinition(typeof(FirstJobConsumer));
        var jobRegistration = new RecordingConsumerRegistration(typeof(FirstJobConsumer), definition);
        var ordinaryRegistration = new RecordingConsumerRegistration(
            typeof(OrdinaryConsumer),
            new RecordingConsumerDefinition(typeof(OrdinaryConsumer)));
        var formatter = new KebabCaseEndpointNameFormatter(false);
        var context = new RecordingConsumerKindContext(
            registrationContext,
            formatter,
            [jobRegistration, ordinaryRegistration]);

        IConsumerKindRegistration planned = Assert.Single(kind.GetRegistrations(context));

        Assert.Equal("Job", kind.Name);
        Assert.Equal(50, kind.Order);
        Assert.False(kind.IsFallback);
        Assert.Equal(typeof(FirstJobConsumer), planned.RegistrationType);
        Assert.Same(definition, planned.Definition);
        Assert.Equal("first-job-consumer", planned.EndpointName);
        Assert.Same(definition.EndpointDefinition, planned.EndpointDefinition);
        Assert.True(planned.RequiresServiceInstance);
        Assert.Empty(planned.CompanionEndpointNames);
        Assert.Same(registrationContext, jobRegistration.DefinitionContext);
        Assert.Same(formatter, definition.Formatter);

        IReceiveEndpointConfigurator endpoint = CreateEndpointConfigurator();
        planned.Configure(new RecordingEndpointContext(registrationContext, endpoint));

        Assert.Equal(1, jobRegistration.ConfigureCount);
        Assert.Same(endpoint, jobRegistration.EndpointConfigurator);
        Assert.Same(registrationContext, jobRegistration.ConfigurationContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONSUMER-KIND", "runtime-and-bulk-configuration-honor-ownership-and-exclusions")]
    public void RuntimeAndBulkConfiguration_HonorOwnershipRegistrationAndExclusions()
    {
        var kind = new JobConsumerKind();
        var first = Registration<FirstJobConsumer>();
        var second = Registration<SecondJobConsumer>();
        var ordinary = Registration<OrdinaryConsumer>();
        var selector = new RecordingContainerSelector(first, second, ordinary);
        IRegistrationContext context = CreateRegistrationContext(selector);
        IReceiveEndpointConfigurator endpoint = CreateEndpointConfigurator();

        Assert.False(kind.TryConfigure(typeof(OrdinaryConsumer), endpoint, context));
        Assert.False(kind.TryConfigure(typeof(UnregisteredJobConsumer), endpoint, context));
        Assert.True(kind.TryConfigure(typeof(FirstJobConsumer), endpoint, context));
        Assert.True(kind.RequiresServiceInstance(typeof(FirstJobConsumer)));
        Assert.False(kind.RequiresServiceInstance(typeof(OrdinaryConsumer)));

        IReadOnlyCollection<Type> configured = kind.ConfigureAll(
            endpoint,
            context,
            new HashSet<Type> { typeof(FirstJobConsumer) });

        Assert.Equal([typeof(SecondJobConsumer)], configured);
        Assert.Equal(1, first.ConfigureCount);
        Assert.Equal(1, second.ConfigureCount);
        Assert.Equal(0, ordinary.ConfigureCount);
        Assert.Equal("registrationType", Assert.Throws<ArgumentNullException>(() =>
            kind.TryConfigure(null!, endpoint, context)).ParamName);
        Assert.Equal("endpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            kind.TryConfigure(typeof(FirstJobConsumer), null!, context)).ParamName);
        Assert.Equal("registrationContext", Assert.Throws<ArgumentNullException>(() =>
            kind.TryConfigure(typeof(FirstJobConsumer), endpoint, null!)).ParamName);
        Assert.Equal("registrationType", Assert.Throws<ArgumentNullException>(() =>
            kind.RequiresServiceInstance(null!)).ParamName);
        Assert.Equal("endpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            kind.ConfigureAll(null!, context, new HashSet<Type>())).ParamName);
        Assert.Equal("registrationContext", Assert.Throws<ArgumentNullException>(() =>
            kind.ConfigureAll(endpoint, null!, new HashSet<Type>())).ParamName);
        Assert.Equal("excludedRegistrationTypes", Assert.Throws<ArgumentNullException>(() =>
            kind.ConfigureAll(endpoint, context, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-JOB-CONSUMER-KIND", "dispatcher-and-harness-use-the-job-consumer-identity")]
    public void DispatcherAndHarness_UseOnlyTheJobConsumerIdentity()
    {
        var kind = new JobConsumerKind();
        var first = Registration<FirstJobConsumer>();
        var ordinary = Registration<OrdinaryConsumer>();
        var harness = new RecordingTestHarnessContext(first, ordinary);
        var factory = new RecordingDispatcherFactory();
        var formatter = new KebabCaseEndpointNameFormatter(false);

        kind.ConfigureTestHarness(harness);
        Assert.Equal([typeof(FirstJobConsumer)], harness.ObservedTypes);

        Assert.False(kind.TryCreateDispatcher(typeof(OrdinaryConsumer), factory, formatter, out IReceiveEndpointDispatcher? missing));
        Assert.Null(missing);
        Assert.True(kind.TryCreateDispatcher(typeof(FirstJobConsumer), factory, formatter, out IReceiveEndpointDispatcher? dispatcher));
        Assert.Same(factory.Dispatcher, dispatcher);
        Assert.Equal("first-job", factory.QueueName);
        Assert.NotNull(factory.Configure);
        Assert.Equal("registrationType", Assert.Throws<ArgumentNullException>(() =>
            kind.TryCreateDispatcher(null!, factory, formatter, out _)).ParamName);
        Assert.Equal("factory", Assert.Throws<ArgumentNullException>(() =>
            kind.TryCreateDispatcher(typeof(FirstJobConsumer), null!, formatter, out _)).ParamName);
        Assert.Equal("formatter", Assert.Throws<ArgumentNullException>(() =>
            kind.TryCreateDispatcher(typeof(FirstJobConsumer), factory, null!, out _)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => kind.ConfigureTestHarness(null!)).ParamName);
    }

    private static RecordingConsumerRegistration Registration<TConsumer>()
        where TConsumer : class, IConsumer =>
        new(typeof(TConsumer), new RecordingConsumerDefinition(typeof(TConsumer)));

    private static IRegistrationContext CreateRegistrationContext(IContainerSelector selector)
    {
        IRegistrationContext context = DispatchProxy.Create<IRegistrationContext, RegistrationContextProxy>();
        ((RegistrationContextProxy)(object)context).Selector = selector;
        return context;
    }

    private static IReceiveEndpointConfigurator CreateEndpointConfigurator() =>
        DispatchProxy.Create<IReceiveEndpointConfigurator, PassiveProxy>();

    private sealed class RecordingConsumerKindContext(
        IRegistrationContext registrationContext,
        IEndpointNameFormatter endpointNameFormatter,
        IReadOnlyCollection<IConsumerRegistration> registrations) : IConsumerKindContext
    {
        public IRegistrationContext RegistrationContext { get; } = registrationContext;

        public IEndpointNameFormatter EndpointNameFormatter { get; } = endpointNameFormatter;

        public IEnumerable<TRegistration> GetRegistrations<TRegistration>()
            where TRegistration : class, IRegistration => registrations.OfType<TRegistration>();
    }

    private sealed class RecordingEndpointContext(
        IRegistrationContext registrationContext,
        IReceiveEndpointConfigurator endpointConfigurator) : IConsumerKindEndpointContext
    {
        public IRegistrationContext RegistrationContext { get; } = registrationContext;

        public IReceiveEndpointConfigurator EndpointConfigurator { get; } = endpointConfigurator;

        public void ConfigureCompanionEndpoint(
            string endpointName,
            IEndpointDefinition? endpointDefinition,
            Action<IReceiveEndpointConfigurator> configure) =>
            throw new InvalidOperationException("Job consumers do not own companion endpoints.");
    }

    private sealed class RecordingTestHarnessContext(params IConsumerRegistration[] registrations) : IConsumerKindTestHarnessContext
    {
        public string KindName => "Job";

        public List<Type> ObservedTypes { get; } = [];

        public IEnumerable<TRegistration> GetRegistrations<TRegistration>()
            where TRegistration : class, IRegistration => registrations.OfType<TRegistration>();

        public void Observe(Type registrationType, params Type[] supportingTypes)
        {
            Assert.Empty(supportingTypes);
            ObservedTypes.Add(registrationType);
        }
    }

    private sealed class RecordingContainerSelector(params IConsumerRegistration[] registrations) : IContainerSelector
    {
        public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
            where T : class, IRegistration
        {
            value = registrations.FirstOrDefault(registration => registration.Type == type) as T;
            return value is not null;
        }

        public IEnumerable<T> GetRegistrations<T>(IServiceProvider provider)
            where T : class, IRegistration => registrations.OfType<T>();

        public T? GetDefinition<T>(IServiceProvider provider)
            where T : class, IDefinition => null;

        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider)
            where T : class => null;

        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) =>
            throw new NotSupportedException();

        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingConsumerRegistration(Type type, IConsumerDefinition definition) : IConsumerRegistration
    {
        public Type Type { get; } = type;

        public bool IncludeInConfigureEndpoints { get; set; } = true;

        public bool RequiresServiceInstance { get; set; }

        public int ConfigureCount { get; private set; }

        public IReceiveEndpointConfigurator? EndpointConfigurator { get; private set; }

        public IRegistrationContext? ConfigurationContext { get; private set; }

        public IRegistrationContext? DefinitionContext { get; private set; }

        public void AddConfigureAction<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? configure)
            where T : class, IConsumer => throw new NotSupportedException();

        public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
        {
            ConfigureCount++;
            EndpointConfigurator = configurator;
            ConfigurationContext = context;
        }

        public IConsumerDefinition GetDefinition(IRegistrationContext context)
        {
            DefinitionContext = context;
            return definition;
        }

        public IConsumerRegistrationConfigurator GetConsumerRegistrationConfigurator(IRegistrationConfigurator registrationConfigurator) =>
            throw new NotSupportedException();
    }

    private sealed class RecordingConsumerDefinition(Type consumerType) : IConsumerDefinition
    {
        public Type ConsumerType { get; } = consumerType;

        public int? ConcurrentMessageLimit => 3;

        public IEndpointDefinition? EndpointDefinition { get; } =
            DispatchProxy.Create<IEndpointDefinition, PassiveProxy>();

        public IEndpointNameFormatter? Formatter { get; private set; }

        public string GetEndpointName(IEndpointNameFormatter formatter)
        {
            Formatter = formatter;
            return formatter.SanitizeName(ConsumerType.Name);
        }
    }

    private sealed class RecordingDispatcherFactory : IReceiveEndpointDispatcherFactory
    {
        public IReceiveEndpointDispatcher Dispatcher { get; } =
            DispatchProxy.Create<IReceiveEndpointDispatcher, PassiveProxy>();

        public string? QueueName { get; private set; }

        public Action<IReceiveEndpointConfigurator, IRegistrationContext>? Configure { get; private set; }

        public IReceiveEndpointDispatcher CreateReceiver(string queueName)
        {
            QueueName = queueName;
            return Dispatcher;
        }

        public IReceiveEndpointDispatcher CreateReceiver(
            string queueName,
            Action<IReceiveEndpointConfigurator, IRegistrationContext> configure)
        {
            QueueName = queueName;
            Configure = configure;
            return Dispatcher;
        }

        public IReceiveEndpointDispatcher CreateRegistrationReceiver(
            Type registrationType,
            string fallbackQueueName,
            IEndpointNameFormatter formatter) => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class RegistrationContextProxy : DispatchProxy
    {
        public IContainerSelector Selector { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(IServiceProvider.GetService)
                && args is [Type serviceType]
                && serviceType == typeof(IContainerSelector))
                return Selector;

            throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class FirstJobConsumer : IJobConsumer<JobMessage>
    {
        public Task RunAsync(JobContext<JobMessage> context) => throw new NotSupportedException();
    }

    private sealed class SecondJobConsumer : IJobConsumer<JobMessage>
    {
        public Task RunAsync(JobContext<JobMessage> context) => throw new NotSupportedException();
    }

    private sealed class UnregisteredJobConsumer : IJobConsumer<JobMessage>
    {
        public Task RunAsync(JobContext<JobMessage> context) => throw new NotSupportedException();
    }

    private sealed class OrdinaryConsumer : IConsumer;

    private sealed record JobMessage;
}
