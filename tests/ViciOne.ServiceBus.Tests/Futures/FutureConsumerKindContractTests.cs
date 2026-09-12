using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Futures.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureConsumerKindContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "consumer-kind-plans-complete-future-registration-metadata")]
    public void Planning_ExposesTheOwnedDefinitionEndpointAndConfigurationCallback()
    {
        var kind = new FutureConsumerKind();
        IRegistrationContext registrationContext = CreateRegistrationContext(new RecordingContainerSelector());
        var definition = new RecordingFutureDefinition(typeof(FirstFuture));
        var registration = new RecordingFutureRegistration(typeof(FirstFuture), definition);
        var formatter = new KebabCaseEndpointNameFormatter(false);
        var context = new RecordingConsumerKindContext(registrationContext, formatter, [registration]);

        IConsumerKindRegistration planned = Assert.Single(kind.GetRegistrations(context));

        Assert.Equal("Future", kind.Name);
        Assert.Equal(40, kind.Order);
        Assert.False(kind.IsFallback);
        Assert.Equal(typeof(FirstFuture), planned.RegistrationType);
        Assert.Same(definition, planned.Definition);
        Assert.Equal("first-future", planned.EndpointName);
        Assert.Null(planned.EndpointDefinition);
        Assert.False(planned.RequiresServiceInstance);
        Assert.Empty(planned.CompanionEndpointNames);
        Assert.Same(registrationContext, registration.DefinitionContext);
        Assert.Same(formatter, definition.Formatter);

        IReceiveEndpointConfigurator endpoint = CreateEndpointConfigurator();
        planned.Configure(new RecordingEndpointContext(registrationContext, endpoint));

        Assert.Equal(1, registration.ConfigureCount);
        Assert.Same(endpoint, registration.EndpointConfigurator);
        Assert.Same(registrationContext, registration.ConfigurationContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "runtime-and-typed-configuration-claim-only-owned-futures")]
    public void RuntimeAndTypedConfiguration_ClaimOnlyOwnedFutureRegistrations()
    {
        var kind = new FutureConsumerKind();
        var registration = new RecordingFutureRegistration(
            typeof(FirstFuture),
            new RecordingFutureDefinition(typeof(FirstFuture)));
        var selector = new RecordingContainerSelector(registration);
        IRegistrationContext registrationContext = CreateRegistrationContext(selector);
        IReceiveEndpointConfigurator endpoint = CreateEndpointConfigurator();

        Assert.False(kind.TryConfigure(typeof(SecondFuture), endpoint, registrationContext));
        Assert.True(kind.TryConfigure(typeof(FirstFuture), endpoint, registrationContext));
        Assert.Equal(1, registration.ConfigureCount);

        IConsumerKindTypedConfigurator typed = kind;
        Assert.Throws<ArgumentException>(() => typed.TryConfigure<FirstFuture>(endpoint, registrationContext, new Action(() => { })));
        Assert.True(typed.TryConfigure<FirstFuture>(endpoint, registrationContext));
        Assert.Equal(2, registration.ConfigureCount);

        Assert.Equal("registrationType", Assert.Throws<ArgumentNullException>(() =>
            kind.TryConfigure(null!, endpoint, registrationContext)).ParamName);
        Assert.Equal("endpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            kind.TryConfigure(typeof(FirstFuture), null!, registrationContext)).ParamName);
        Assert.Equal("registrationContext", Assert.Throws<ArgumentNullException>(() =>
            kind.TryConfigure(typeof(FirstFuture), endpoint, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "bulk-configuration-honors-exclusions-and-harness-observes-state")]
    public void BulkConfigurationAndHarnessObservation_HonorTheirExactRegistrationSets()
    {
        var kind = new FutureConsumerKind();
        var first = new RecordingFutureRegistration(
            typeof(FirstFuture),
            new RecordingFutureDefinition(typeof(FirstFuture)));
        var second = new RecordingFutureRegistration(
            typeof(SecondFuture),
            new RecordingFutureDefinition(typeof(SecondFuture)));
        var selector = new RecordingContainerSelector(first, second);
        IRegistrationContext registrationContext = CreateRegistrationContext(selector);
        IReceiveEndpointConfigurator endpoint = CreateEndpointConfigurator();

        IReadOnlyCollection<Type> configured = kind.ConfigureAll(
            endpoint,
            registrationContext,
            new HashSet<Type> { typeof(SecondFuture) });

        Assert.Equal([typeof(FirstFuture)], configured);
        Assert.Equal(1, first.ConfigureCount);
        Assert.Equal(0, second.ConfigureCount);

        var harness = new RecordingTestHarnessContext(first, second);
        kind.ConfigureTestHarness(harness);

        Assert.Equal(
            [
                new Observation(typeof(FutureState), typeof(FirstFuture)),
                new Observation(typeof(FutureState), typeof(SecondFuture)),
            ],
            harness.Observations);
        Assert.Equal("endpointConfigurator", Assert.Throws<ArgumentNullException>(() =>
            kind.ConfigureAll(null!, registrationContext, new HashSet<Type>())).ParamName);
        Assert.Equal("registrationContext", Assert.Throws<ArgumentNullException>(() =>
            kind.ConfigureAll(endpoint, null!, new HashSet<Type>())).ParamName);
        Assert.Equal("excludedRegistrationTypes", Assert.Throws<ArgumentNullException>(() =>
            kind.ConfigureAll(endpoint, registrationContext, null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => kind.ConfigureTestHarness(null!)).ParamName);
    }

    private static IRegistrationContext CreateRegistrationContext(IContainerSelector selector)
    {
        IRegistrationContext context = DispatchProxy.Create<IRegistrationContext, RegistrationContextProxy>();
        ((RegistrationContextProxy)(object)context).Selector = selector;
        return context;
    }

    private static IReceiveEndpointConfigurator CreateEndpointConfigurator() =>
        DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointConfiguratorProxy>();

    private sealed class RecordingConsumerKindContext(
        IRegistrationContext registrationContext,
        IEndpointNameFormatter endpointNameFormatter,
        IReadOnlyCollection<IFutureRegistration> registrations) :
        IConsumerKindContext
    {
        public IRegistrationContext RegistrationContext { get; } = registrationContext;

        public IEndpointNameFormatter EndpointNameFormatter { get; } = endpointNameFormatter;

        public IEnumerable<TRegistration> GetRegistrations<TRegistration>()
            where TRegistration : class, IRegistration => registrations.OfType<TRegistration>();
    }

    private sealed class RecordingEndpointContext(
        IRegistrationContext registrationContext,
        IReceiveEndpointConfigurator endpointConfigurator) :
        IConsumerKindEndpointContext
    {
        public IRegistrationContext RegistrationContext { get; } = registrationContext;

        public IReceiveEndpointConfigurator EndpointConfigurator { get; } = endpointConfigurator;

        public void ConfigureCompanionEndpoint(string endpointName, IEndpointDefinition? endpointDefinition,
            Action<IReceiveEndpointConfigurator> configure) => throw new InvalidOperationException("Futures do not use companion endpoints.");
    }

    private sealed class RecordingTestHarnessContext(params IFutureRegistration[] registrations) :
        IConsumerKindTestHarnessContext
    {
        public string KindName => "Future";

        public List<Observation> Observations { get; } = [];

        public IEnumerable<TRegistration> GetRegistrations<TRegistration>()
            where TRegistration : class, IRegistration => registrations.OfType<TRegistration>();

        public void Observe(Type registrationType, params Type[] supportingTypes)
        {
            Assert.Single(supportingTypes);
            Observations.Add(new Observation(registrationType, supportingTypes[0]));
        }
    }

    private sealed class RecordingContainerSelector(params IFutureRegistration[] registrations) :
        IContainerSelector
    {
        public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
            where T : class, IRegistration
        {
            value = registrations.FirstOrDefault(registration => registration.Type == type) as T;
            return value != null;
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

    private sealed class RecordingFutureRegistration(Type type, IFutureDefinition definition) :
        IFutureRegistration
    {
        public Type Type { get; } = type;

        public bool IncludeInConfigureEndpoints { get; set; } = true;

        public int ConfigureCount { get; private set; }

        public IReceiveEndpointConfigurator? EndpointConfigurator { get; private set; }

        public IRegistrationContext? ConfigurationContext { get; private set; }

        public IRegistrationContext? DefinitionContext { get; private set; }

        public void Configure(IReceiveEndpointConfigurator configurator, IRegistrationContext context)
        {
            ConfigureCount++;
            EndpointConfigurator = configurator;
            ConfigurationContext = context;
        }

        public IFutureDefinition GetDefinition(IRegistrationContext context)
        {
            DefinitionContext = context;
            return definition;
        }
    }

    private sealed class RecordingFutureDefinition :
        IFutureDefinition
    {
        private readonly Type _futureType;

        public RecordingFutureDefinition(Type futureType)
        {
            _futureType = futureType;
        }

        public Type FutureType => _futureType;

        public IEndpointDefinition? EndpointDefinition => null;

        public int? ConcurrentMessageLimit => 3;

        public IEndpointNameFormatter? Formatter { get; private set; }

        public string GetEndpointName(IEndpointNameFormatter formatter)
        {
            Formatter = formatter;
            return formatter.SanitizeName(_futureType.Name);
        }
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

    private class EndpointConfiguratorProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException(targetMethod?.Name);
    }

    private sealed class FirstFuture : Future<Command, Result>;

    private sealed class SecondFuture : Future<Command, Result>;

    private sealed record Command;

    private sealed record Result;

    private sealed record Observation(Type RegistrationType, Type SupportingType);
}
