using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Futures.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Futures;

public sealed class FutureRegistrationBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "built-in-companion-futures-activate-through-normal-public-composition")]
    public async Task BuiltInCompanionFutures_ActivateThroughNormalPublicCompositionAsync()
    {
        var services = new ServiceCollection();
        services.AddViciOneServiceBus(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.SetInMemorySagaRepositoryProvider();
            configuration.AddFutureRequestConsumer<
                RequestConsumerFuture<BuiltinCommandA, BuiltinResultA>, BuiltinConsumerA,
                BuiltinCommandA, BuiltinResultA>();
            configuration.AddFutureRequestConsumer<
                RequestConsumerFuture<BuiltinCommandB, BuiltinResultB>, BuiltinConsumerB,
                BuiltinCommandB, BuiltinResultB>();
            configuration.UsingInMemory((context, bus) => bus.ConfigureEndpoints(context));
        });
        await using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);

        // Actual bus construction configures companion endpoints and activates the
        // registered machines. No invented untyped definition alias is supplied.
        IBus bus = provider.GetRequiredService<IBus>();
        var first = provider.GetRequiredService<RequestConsumerFuture<BuiltinCommandA, BuiltinResultA>>();
        var second = provider.GetRequiredService<RequestConsumerFuture<BuiltinCommandB, BuiltinResultB>>();

        Assert.NotNull(bus.Address);
        Assert.NotNull(first.CommandReceived);
        Assert.NotNull(second.CommandReceived);
        Assert.NotSame(first, second);
        Assert.Same(first, provider.GetRequiredService<RequestConsumerFuture<BuiltinCommandA, BuiltinResultA>>());
        Assert.Same(second, provider.GetRequiredService<RequestConsumerFuture<BuiltinCommandB, BuiltinResultB>>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "typed-and-runtime-registration-preserve-definition")]
    public void TypedAndRuntimeRegistration_PreserveDefaultAndExplicitDefinitions()
    {
        var typedServices = new ServiceCollection();
        var typedConfigurator = new ServiceCollectionBusConfigurator(typedServices);
        var runtimeDefaultServices = new ServiceCollection();
        var runtimeDefaultConfigurator = new ServiceCollectionBusConfigurator(runtimeDefaultServices);
        var runtimeExplicitServices = new ServiceCollection();
        var runtimeExplicitConfigurator = new ServiceCollectionBusConfigurator(runtimeExplicitServices);

        IFutureRegistrationConfigurator<RegisteredFuture> typed =
            typedConfigurator.AddFuture<RegisteredFuture, RegisteredFutureDefinition>();
        IFutureRegistrationConfigurator runtimeDefault = runtimeDefaultConfigurator.AddFuture(typeof(RegisteredFuture));
        IFutureRegistrationConfigurator runtimeExplicit =
            runtimeExplicitConfigurator.AddFuture(typeof(RegisteredFuture), typeof(RegisteredFutureDefinition));

        Assert.NotNull(typed);
        Assert.NotNull(runtimeDefault);
        Assert.NotNull(runtimeExplicit);
        AssertRegisteredExactlyOnce(typedServices);
        Assert.Single(runtimeDefaultServices, descriptor => descriptor.ServiceType == typeof(RegisteredFuture));
        Assert.Contains(runtimeDefaultServices, descriptor => descriptor.ImplementationType is { IsGenericType: true } type
            && type.GetGenericTypeDefinition() == typeof(DefaultFutureDefinition<>));
        AssertRegisteredExactlyOnce(runtimeExplicitServices);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "explicit-type-scan-honors-definitions-and-filter")]
    public void AddFutures_WithExplicitTypesAssociatesDefinitionsAndHonorsTheFilter()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        Type[] candidates =
        [
            typeof(RegisteredFuture),
            typeof(RegisteredFutureDefinition),
            typeof(AlternativeFuture),
            typeof(AlternativeFutureDefinition),
        ];

        configurator.AddFutures(type => type == typeof(RegisteredFuture), candidates);

        AssertRegisteredExactlyOnce(services);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(AlternativeFuture));
        Assert.DoesNotContain(services, descriptor => descriptor.ImplementationType == typeof(AlternativeFutureDefinition));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "assembly-and-namespace-scans-discover-the-selected-future")]
    public void AddFutures_AssemblyAndNamespaceOverloadsDiscoverOnlyTheSelectedFuture()
    {
        static bool SelectRegisteredFuture(Type type) => type == typeof(RegisteredFuture);

        ServiceCollection assemblyServices = Register(configurator =>
            configurator.AddFuturesFromAssemblies(SelectRegisteredFuture, typeof(RegisteredFuture).Assembly));
        ServiceCollection genericNamespaceServices = Register(configurator =>
            configurator.AddFuturesFromNamespaceContaining<RegisteredFuture>(SelectRegisteredFuture));
        ServiceCollection runtimeNamespaceServices = Register(configurator =>
            configurator.AddFuturesFromNamespaceContaining(typeof(RegisteredFuture), SelectRegisteredFuture));
        ServiceCollection explicitServices = Register(configurator =>
            configurator.AddFutures(typeof(RegisteredFuture), typeof(RegisteredFutureDefinition)));
        ServiceCollection loadedAssemblyServices = Register(configurator =>
            configurator.AddFuturesFromLoadedAssemblies(static _ => false));

        AssertRegisteredExactlyOnce(assemblyServices);
        AssertRegisteredExactlyOnce(genericNamespaceServices);
        AssertRegisteredExactlyOnce(runtimeNamespaceServices);
        AssertRegisteredExactlyOnce(explicitServices);
        Assert.DoesNotContain(loadedAssemblyServices, descriptor => descriptor.ServiceType == typeof(IFutureRegistration));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "request-consumer-registration-includes-future-consumer-and-companion-definition")]
    public void AddFutureRequestConsumer_RegistersTheFutureAndItsCompanionConsumer()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        IFutureRegistrationConfigurator<RequestFuture> registration = configurator
            .AddFutureRequestConsumer<RequestFuture, RequestConsumer, RequestMessage, ResponseMessage>();

        Assert.NotNull(registration);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(RequestFuture));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(RequestConsumer));
        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        var definition = provider.GetRequiredService<
            RequestConsumerFutureDefinition<RequestFuture, RequestConsumer, RequestMessage, ResponseMessage>>();
        Assert.IsType<RequestConsumerFutureDefinition<RequestFuture, RequestConsumer, RequestMessage, ResponseMessage>>(definition);
        Assert.Same(definition, provider.GetRequiredService<IFutureDefinition<RequestFuture>>());
        Assert.NotNull(definition.EndpointDefinition);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "service-collection-overloads-preserve-default-explicit-and-runtime-definitions")]
    public void ServiceCollectionRegistrationOverloads_RegisterTheirExactFutureDefinitions()
    {
        var defaultServices = new ServiceCollection();
        var explicitServices = new ServiceCollection();
        var runtimeServices = new ServiceCollection();

        IFutureRegistration defaultRegistration = defaultServices.RegisterFuture<RegisteredFuture>();
        IFutureRegistration explicitRegistration = explicitServices.RegisterFuture<RegisteredFuture, RegisteredFutureDefinition>();
        IFutureRegistration runtimeRegistration = runtimeServices.RegisterFuture<RegisteredFuture>(typeof(RegisteredFutureDefinition));

        Assert.Equal(typeof(RegisteredFuture), defaultRegistration.Type);
        Assert.Equal(typeof(RegisteredFuture), explicitRegistration.Type);
        Assert.Equal(typeof(RegisteredFuture), runtimeRegistration.Type);
        Assert.Contains(defaultServices, descriptor => descriptor.ImplementationType is { IsGenericType: true } type
            && type.GetGenericTypeDefinition() == typeof(DefaultFutureDefinition<>));
        Assert.Contains(explicitServices, descriptor => descriptor.ImplementationType == typeof(RegisteredFutureDefinition));
        Assert.Contains(runtimeServices, descriptor => descriptor.ImplementationType == typeof(RegisteredFutureDefinition));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "public-scanning-boundaries-reject-null-elements")]
    public void AddFutures_RejectsMissingConfiguratorCollectionsAndElements()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            FutureRegistrationExtensions.AddFutures(null!, Array.Empty<Type>())).ParamName);
        Assert.Equal("types", Assert.Throws<ArgumentNullException>(() => configurator.AddFutures((Type[])null!)).ParamName);
        Assert.Equal("types", Assert.Throws<ArgumentException>(() =>
            configurator.AddFutures(new Type[] { typeof(RegisteredFuture), null! })).ParamName);
        Assert.Equal("assemblies", Assert.Throws<ArgumentNullException>(() => configurator.AddFuturesFromAssemblies((Assembly[])null!)).ParamName);
        Assert.Equal("assemblies", Assert.Throws<ArgumentException>(() => configurator.AddFuturesFromAssemblies()).ParamName);
        Assert.Equal("assemblies", Assert.Throws<ArgumentException>(() =>
            configurator.AddFuturesFromAssemblies(new Assembly[] { typeof(RegisteredFuture).Assembly, null! })).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            FutureRegistrationExtensions.AddFuturesFromLoadedAssemblies(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "namespace-scan-requires-a-named-namespace")]
    public void AddFuturesFromNamespaceContaining_RejectsMissingOrNamespaceLessTypes()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());
        Type namespaceLessType = CreateNamespaceLessType();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            FutureRegistrationExtensions.AddFuturesFromNamespaceContaining(null!, typeof(RegisteredFuture))).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            configurator.AddFuturesFromNamespaceContaining(null!)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() =>
            configurator.AddFuturesFromNamespaceContaining(namespaceLessType)).ParamName);
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(WrongStateMachine))]
    [InlineData(typeof(AbstractFuture))]
    [InlineData(typeof(OpenFuture<>))]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "runtime-future-type-is-concrete-closed-and-exact")]
    public void RuntimeRegistration_RejectsInvalidFutureTypes(Type futureType)
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        ArgumentException exception = Assert.Throws<ArgumentException>(() => configurator.AddFuture(futureType));

        Assert.Equal("futureType", exception.ParamName);
    }

    [Theory]
    [InlineData(typeof(object))]
    [InlineData(typeof(AlternativeFutureDefinition))]
    [InlineData(typeof(AbstractFutureDefinition))]
    [InlineData(typeof(OpenFutureDefinition<>))]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "runtime-definition-is-concrete-closed-and-associated")]
    public void RuntimeRegistration_RejectsInvalidDefinitionTypes(Type definitionType)
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            configurator.AddFuture(typeof(RegisteredFuture), definitionType));

        Assert.Equal("futureDefinitionType", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "runtime-registration-null-boundaries")]
    public void RuntimeRegistration_RejectsMissingConfiguratorAndFutureType()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            FutureRegistrationConfiguratorRuntimeExtensions.AddFuture(null!, typeof(RegisteredFuture))).ParamName);
        Assert.Equal("futureType", Assert.Throws<ArgumentNullException>(() => configurator.AddFuture(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "fluent-endpoint-and-repository-settings-are-applied")]
    public void FluentRegistration_AppliesEndpointAndRepositoryConfiguration()
    {
        var services = new ServiceCollection();
        var busConfigurator = new ServiceCollectionBusConfigurator(services);
        IFutureRegistrationConfigurator<RegisteredFuture> future = busConfigurator.AddFuture<RegisteredFuture>();

        IFutureRegistrationConfigurator endpointResult = ((IFutureRegistrationConfigurator)future).Endpoint(endpoint =>
        {
            endpoint.Name = "registered-future-endpoint";
            endpoint.Temporary = true;
            endpoint.PrefetchCount = 11;
            endpoint.ConcurrentMessageLimit = 7;
            endpoint.ConfigureConsumeTopology = false;
            endpoint.InstanceId = "blue";
        });
        IFutureRegistrationConfigurator<RegisteredFuture> repositoryResult = future.Repository(repository =>
            repository.AddSingleton<RepositoryMarker>());

        Assert.Same(future, endpointResult);
        Assert.Same(future, repositoryResult);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(FutureEndpointDefinition<RegisteredFuture>));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IEndpointDefinition<RegisteredFuture>));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(RepositoryMarker));

        using ServiceProvider provider = services.BuildServiceProvider();
        IEndpointDefinition<RegisteredFuture> definition = provider.GetRequiredService<IEndpointDefinition<RegisteredFuture>>();
        Assert.Equal("registered-future-endpoint-blue", definition.GetEndpointName(KebabCaseEndpointNameFormatter.Instance));
        Assert.True(definition.IsTemporary);
        Assert.Equal(11, definition.PrefetchCount);
        Assert.Equal(7, definition.ConcurrentMessageLimit);
        Assert.False(definition.ConfigureConsumeTopology);
        Assert.NotNull(provider.GetService<RepositoryMarker>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-FUTURE-REGISTRATION", "endpoint-exclusion-is-terminal-and-callbacks-are-required")]
    public void FluentRegistration_RejectsMissingCallbacksAndEndpointConfigurationAfterExclusion()
    {
        var services = new ServiceCollection();
        var busConfigurator = new ServiceCollectionBusConfigurator(services);
        IFutureRegistrationConfigurator<RegisteredFuture> future = busConfigurator.AddFuture<RegisteredFuture>();
        IFutureRegistration registration = Assert.IsAssignableFrom<IFutureRegistration>(
            Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IFutureRegistration)).ImplementationInstance);

        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => future.Endpoint(null!)).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() => future.Repository(null!)).ParamName);

        future.ExcludeFromConfigureEndpoints();

        Assert.False(registration.IncludeInConfigureEndpoints);
        ConfigurationException exception = Assert.Throws<ConfigurationException>(() => future.Endpoint(_ => { }));
        Assert.Contains("excluded from ConfigureEndpoints", exception.Message, StringComparison.Ordinal);
    }

    private static void AssertRegisteredExactlyOnce(IServiceCollection services)
    {
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(RegisteredFuture));
        Assert.Single(services, descriptor => descriptor.ImplementationType == typeof(RegisteredFutureDefinition));
    }

    private static ServiceCollection Register(Action<ServiceCollectionBusConfigurator> configure)
    {
        var services = new ServiceCollection();
        configure(new ServiceCollectionBusConfigurator(services));
        return services;
    }

    private static Type CreateNamespaceLessType()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(
            new AssemblyName($"FutureNamespaceLess_{Guid.NewGuid():N}"),
            AssemblyBuilderAccess.Run);
        return assembly.DefineDynamicModule("main").DefineType("NamespaceLessType").CreateType()!;
    }

    public sealed class RegisteredFuture : Future<RegistrationCommand, RegistrationResult>;

    private sealed class AlternativeFuture : Future<RegistrationCommand, RegistrationResult>;

    private abstract class AbstractFuture : Future<RegistrationCommand, RegistrationResult>;

    private sealed class OpenFuture<T> : Future<RegistrationCommand, RegistrationResult>
        where T : class;

    private sealed class WrongStateMachine : ViciOneServiceBusStateMachine<WrongState>
    {
        public WrongStateMachine()
        {
            InstanceState(instance => instance.CurrentState);
        }
    }

    public sealed class RegisteredFutureDefinition : FutureDefinition<RegisteredFuture>;

    private sealed class AlternativeFutureDefinition : FutureDefinition<AlternativeFuture>;

    private abstract class AbstractFutureDefinition : FutureDefinition<RegisteredFuture>;

    private sealed class OpenFutureDefinition<T> : FutureDefinition<RegisteredFuture>
        where T : class;

    private sealed class WrongState : ISagaStateMachineInstance
    {
        public int CurrentState { get; set; }

        public Guid CorrelationId { get; set; }
    }

    public sealed class RegistrationCommand;

    public sealed class RegistrationResult;

    private sealed class RequestFuture : Future<RequestMessage, ResponseMessage>;

    private sealed record RequestMessage;

    private sealed record ResponseMessage;

    private sealed class RequestConsumer : IConsumer<RequestMessage>
    {
        public Task ConsumeAsync(ConsumeContext<RequestMessage> context) => Task.CompletedTask;
    }

    public sealed record BuiltinCommandA(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record BuiltinCommandB(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed record BuiltinResultA(string Value);
    public sealed record BuiltinResultB(string Value);
    public sealed class BuiltinConsumerA : IConsumer<BuiltinCommandA>
    {
        public Task ConsumeAsync(ConsumeContext<BuiltinCommandA> context) => Task.CompletedTask;
    }
    public sealed class BuiltinConsumerB : IConsumer<BuiltinCommandB>
    {
        public Task ConsumeAsync(ConsumeContext<BuiltinCommandB> context) => Task.CompletedTask;
    }

    private sealed class RepositoryMarker;
}
