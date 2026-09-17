using System.Reflection;
using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierRegistrationBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "courier-services-are-idempotent-and-null-safe")]
    public void AddCourier_IsIdempotentAndRejectsAMissingConfigurator()
    {
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CourierRegistrationConfiguratorExtensions.AddCourier(null!)).ParamName);
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        IRegistrationConfigurator first = configurator.AddCourier();
        IRegistrationConfigurator second = configurator.AddCourier();

        Assert.Same(configurator, first);
        Assert.Same(configurator, second);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRoutingSlipExecutor));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "service-registration-is-bus-specific-and-null-safe")]
    public void CourierServiceRegistration_RegistersDefaultAndNamedBusExecutorsExactlyOnce()
    {
        Assert.Equal("services", Assert.Throws<ArgumentNullException>(() =>
            CourierServiceRegistration.Register(null!, typeof(IBus))).ParamName);
        Assert.Equal("busType", Assert.Throws<ArgumentNullException>(() =>
            CourierServiceRegistration.Register(new ServiceCollection(), null!)).ParamName);
        var services = new ServiceCollection();

        CourierServiceRegistration.Register(services, typeof(IBus));
        CourierServiceRegistration.Register(services, typeof(IBus));
        CourierServiceRegistration.Register(services, typeof(ISecondaryBus));
        CourierServiceRegistration.Register(services, typeof(ISecondaryBus));

        ServiceDescriptor defaultBus = Assert.Single(services,
            descriptor => descriptor.ServiceType == typeof(IRoutingSlipExecutor));
        ServiceDescriptor secondaryBus = Assert.Single(services,
            descriptor => descriptor.ServiceType == typeof(Bind<ISecondaryBus, IRoutingSlipExecutor>));
        Assert.Equal(ServiceLifetime.Scoped, defaultBus.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, secondaryBus.Lifetime);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "runtime-types-register-their-exact-courier-shape")]
    public void RuntimeRegistration_RegistersCompensatableAndExecuteOnlyActivities()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        IActivityRegistrationConfigurator activity = configurator.AddActivity(typeof(RegisteredActivity));
        IExecuteActivityRegistrationConfigurator executeOnly = configurator.AddExecuteActivity(typeof(RegisteredExecuteActivity));

        Assert.NotNull(activity);
        Assert.NotNull(executeOnly);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(RegisteredActivity)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(RegisteredExecuteActivity)
            && descriptor.Lifetime == ServiceLifetime.Scoped);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IRoutingSlipExecutor));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "runtime-registration-associates-matching-definition-types")]
    public void RuntimeRegistration_AssociatesMatchingActivityDefinitions()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        IActivityRegistrationConfigurator activity = configurator.AddActivity(
            typeof(RegisteredActivity),
            typeof(RegisteredActivityDefinition));
        IExecuteActivityRegistrationConfigurator executeOnly = configurator.AddExecuteActivity(
            typeof(RegisteredExecuteActivity),
            typeof(RegisteredExecuteActivityDefinition));

        Assert.NotNull(activity);
        Assert.NotNull(executeOnly);
        Assert.Contains(services, descriptor => descriptor.ImplementationType == typeof(RegisteredActivityDefinition));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == typeof(RegisteredExecuteActivityDefinition));

        var genericServices = new ServiceCollection();
        var genericConfigurator = new ServiceCollectionBusConfigurator(genericServices);

        IActivityRegistrationConfigurator<RegisteredActivity, RegisteredArguments, RegisteredLog> genericActivity =
            genericConfigurator.AddActivity<RegisteredActivity, RegisteredArguments, RegisteredLog, RegisteredActivityDefinition>();

        Assert.NotNull(genericActivity);
        Assert.Contains(genericServices, descriptor => descriptor.ImplementationType == typeof(RegisteredActivityDefinition));

        var defaultServices = new ServiceCollection();
        var defaultConfigurator = new ServiceCollectionBusConfigurator(defaultServices);

        IActivityRegistrationConfigurator<RegisteredActivity, RegisteredArguments, RegisteredLog> defaultActivity =
            defaultConfigurator.AddActivity<RegisteredActivity, RegisteredArguments, RegisteredLog>();

        Assert.NotNull(defaultActivity);
        Assert.Contains(defaultServices, descriptor => descriptor.ServiceType == typeof(RegisteredActivity));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "runtime-registration-rejects-missing-inputs-and-mismatched-definitions")]
    public void RuntimeRegistration_RejectsMissingInputsAndMismatchedDefinitions()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        int baseline = services.Count;

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CourierRegistrationConfiguratorRuntimeExtensions.AddActivity(null!, typeof(RegisteredActivity))).ParamName);
        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CourierRegistrationConfiguratorRuntimeExtensions.AddExecuteActivity(null!, typeof(RegisteredExecuteActivity))).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() => configurator.AddActivity(null!)).ParamName);
        Assert.Equal("activityType", Assert.Throws<ArgumentNullException>(() => configurator.AddExecuteActivity(null!)).ParamName);

        ArgumentException activityDefinition = Assert.Throws<ArgumentException>(() => configurator.AddActivity(
            typeof(RegisteredActivity),
            typeof(RegisteredExecuteActivityDefinition)));
        ArgumentException executeDefinition = Assert.Throws<ArgumentException>(() => configurator.AddExecuteActivity(
            typeof(RegisteredExecuteActivity),
            typeof(RegisteredActivityDefinition)));
        ArgumentException ambiguousDefinition = Assert.Throws<ArgumentException>(() => configurator.AddExecuteActivity(
            typeof(RegisteredExecuteActivity),
            typeof(AmbiguousExecuteActivityDefinition)));

        Assert.Equal("activityDefinitionType", activityDefinition.ParamName);
        Assert.Equal("activityDefinitionType", executeDefinition.ParamName);
        Assert.Equal("activityDefinitionType", ambiguousDefinition.ParamName);
        Assert.Equal(baseline, services.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "runtime-registration-rejects-non-concrete-types-before-service-effects")]
    public void RuntimeRegistration_RejectsNonConcreteTypesBeforeChangingServices()
    {
        var activityServices = new ServiceCollection();
        var activityConfigurator = new ServiceCollectionBusConfigurator(activityServices);
        int activityBaseline = activityServices.Count;

        ArgumentException activityFailure = Assert.Throws<ArgumentException>(() =>
            activityConfigurator.AddExecuteActivity(typeof(AbstractExecuteActivity)));

        Assert.Equal("activityType", activityFailure.ParamName);
        Assert.Equal(activityBaseline, activityServices.Count);

        var definitionServices = new ServiceCollection();
        var definitionConfigurator = new ServiceCollectionBusConfigurator(definitionServices);
        int definitionBaseline = definitionServices.Count;

        ArgumentException definitionFailure = Assert.Throws<ArgumentException>(() =>
            definitionConfigurator.AddExecuteActivity(
                typeof(RegisteredExecuteActivity),
                typeof(AbstractExecuteActivityDefinition)));

        Assert.Equal("activityDefinitionType", definitionFailure.ParamName);
        Assert.Equal(definitionBaseline, definitionServices.Count);

        var genericActivityServices = new ServiceCollection();
        var genericActivityConfigurator = new ServiceCollectionBusConfigurator(genericActivityServices);
        int genericActivityBaseline = genericActivityServices.Count;

        ArgumentException genericActivityFailure = Assert.Throws<ArgumentException>(() =>
            genericActivityConfigurator.AddExecuteActivity<AbstractExecuteActivity, RegisteredArguments>());

        Assert.Equal("TActivity", genericActivityFailure.ParamName);
        Assert.Equal(genericActivityBaseline, genericActivityServices.Count);

        var genericDefinitionServices = new ServiceCollection();
        var genericDefinitionConfigurator = new ServiceCollectionBusConfigurator(genericDefinitionServices);
        int genericDefinitionBaseline = genericDefinitionServices.Count;

        ArgumentException genericDefinitionFailure = Assert.Throws<ArgumentException>(() =>
            genericDefinitionConfigurator.AddExecuteActivity<
                RegisteredExecuteActivity,
                RegisteredArguments,
                AbstractExecuteActivityDefinition>());

        Assert.Equal("executeActivityDefinitionType", genericDefinitionFailure.ParamName);
        Assert.Equal(genericDefinitionBaseline, genericDefinitionServices.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "generic-execute-only-registration-rejects-compensatable-activities")]
    public void GenericExecuteOnlyRegistration_RejectsACompensatableActivity()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            configurator.AddExecuteActivity<RegisteredActivity, RegisteredArguments>());

        Assert.Contains("AddActivity", exception.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(RegisteredActivity), exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(ExecuteOnlyActivityWithTwoContracts), false)]
    [InlineData(typeof(RegisteredActivity), false)]
    [InlineData(typeof(RegisteredExecuteActivity), true)]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "runtime-registration-rejects-wrong-or-ambiguous-shapes")]
    public void RuntimeRegistration_RejectsTypesWithoutOneExactGenericContract(Type type, bool compensatableApi)
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        ArgumentException exception = compensatableApi
            ? Assert.Throws<ArgumentException>(() => configurator.AddActivity(type))
            : Assert.Throws<ArgumentException>(() => configurator.AddExecuteActivity(type));

        Assert.Equal("activityType", exception.ParamName);
        Assert.Contains("Courier", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "explicit-type-scan-honors-definitions-and-filter")]
    public void AddActivities_WithExplicitTypesAssociatesDefinitionsAndHonorsTheFilter()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        Type[] candidates =
        [
            typeof(RegisteredActivity),
            typeof(RegisteredActivityDefinition),
            typeof(RegisteredExecuteActivity),
            typeof(RegisteredExecuteActivityDefinition),
        ];

        configurator.AddActivities(type => type == typeof(RegisteredActivity), candidates);

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(RegisteredActivity));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(RegisteredExecuteActivity));
        Assert.Contains(services, descriptor => descriptor.ImplementationType == typeof(RegisteredActivityDefinition));
        Assert.DoesNotContain(services, descriptor => descriptor.ImplementationType == typeof(RegisteredExecuteActivityDefinition));

        var executeServices = new ServiceCollection();
        var executeConfigurator = new ServiceCollectionBusConfigurator(executeServices);

        executeConfigurator.AddActivities(typeof(RegisteredExecuteActivity), typeof(RegisteredExecuteActivityDefinition));

        Assert.Contains(executeServices, descriptor => descriptor.ServiceType == typeof(RegisteredExecuteActivity));
        Assert.Contains(executeServices, descriptor => descriptor.ImplementationType == typeof(RegisteredExecuteActivityDefinition));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "explicit-scan-preflights-failures-before-service-effects")]
    public void AddActivities_PreflightsFilterFailuresAndAmbiguousDefinitionsBeforeChangingServices()
    {
        var filterServices = new ServiceCollection();
        var filterConfigurator = new ServiceCollectionBusConfigurator(filterServices);
        int filterBaseline = filterServices.Count;
        var filterCalls = 0;

        Assert.Throws<InvalidOperationException>(() => filterConfigurator.AddActivities(
            _ => ++filterCalls == 2 ? throw new InvalidOperationException("filter failure") : true,
            typeof(RegisteredActivity),
            typeof(RegisteredExecuteActivity)));

        Assert.Equal(filterBaseline, filterServices.Count);

        var definitionServices = new ServiceCollection();
        var definitionConfigurator = new ServiceCollectionBusConfigurator(definitionServices);
        int definitionBaseline = definitionServices.Count;

        ArgumentException duplicateDefinition = Assert.Throws<ArgumentException>(() => definitionConfigurator.AddActivities(
            typeof(RegisteredActivity),
            typeof(RegisteredActivityDefinition),
            typeof(AlternativeRegisteredActivityDefinition)));

        Assert.Equal("types", duplicateDefinition.ParamName);
        Assert.Equal(definitionBaseline, definitionServices.Count);

        var ambiguousServices = new ServiceCollection();
        var ambiguousConfigurator = new ServiceCollectionBusConfigurator(ambiguousServices);
        int ambiguousBaseline = ambiguousServices.Count;

        ArgumentException ambiguousDefinition = Assert.Throws<ArgumentException>(() => ambiguousConfigurator.AddActivities(
            typeof(RegisteredExecuteActivity),
            typeof(SecondRegisteredExecuteActivity),
            typeof(AmbiguousExecuteActivityDefinition)));

        Assert.Equal("types", ambiguousDefinition.ParamName);
        Assert.Equal(ambiguousBaseline, ambiguousServices.Count);

        var abstractServices = new ServiceCollection();
        var abstractConfigurator = new ServiceCollectionBusConfigurator(abstractServices);
        int abstractBaseline = abstractServices.Count;

        ArgumentException abstractDefinition = Assert.Throws<ArgumentException>(() => abstractConfigurator.AddActivities(
            typeof(RegisteredExecuteActivity),
            typeof(AbstractExecuteActivityDefinition)));

        Assert.Equal("types", abstractDefinition.ParamName);
        Assert.Equal(abstractBaseline, abstractServices.Count);

        var ignoredServices = new ServiceCollection();
        var ignoredConfigurator = new ServiceCollectionBusConfigurator(ignoredServices);
        int ignoredBaseline = ignoredServices.Count;

        ignoredConfigurator.AddActivities(_ => false,
            typeof(AbstractExecuteActivity),
            typeof(AbstractExecuteActivityDefinition));

        Assert.Equal(ignoredBaseline, ignoredServices.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "public-scanning-boundaries-reject-null-elements")]
    public void AddActivities_RejectsMissingConfiguratorCollectionsAndElements()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CourierRegistrationExtensions.AddActivities(null!, Array.Empty<Type>())).ParamName);
        Assert.Equal("types", Assert.Throws<ArgumentNullException>(() => configurator.AddActivities((Type[])null!)).ParamName);
        Assert.Equal("types", Assert.Throws<ArgumentException>(() =>
            configurator.AddActivities(new Type[] { typeof(RegisteredActivity), null! })).ParamName);
        Assert.Equal("assemblies", Assert.Throws<ArgumentNullException>(() => configurator.AddActivities((Assembly[])null!)).ParamName);
        Assert.Equal("assemblies", Assert.Throws<ArgumentException>(() =>
            configurator.AddActivities(new Assembly[] { typeof(RegisteredActivity).Assembly, null! })).ParamName);

        configurator.AddActivities(typeof(RequirementCoverageAttribute).Assembly);

        Exception? domainScanFailure = Record.Exception(() => configurator.AddActivities(Array.Empty<Assembly>()));

        Assert.Null(domainScanFailure);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "namespace-scan-requires-a-named-namespace")]
    public void AddActivitiesFromNamespaceContaining_RejectsMissingOrNamespaceLessTypes()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);
        Type namespaceLessType = new DynamicNamespaceLessType().Create();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CourierRegistrationExtensions.AddActivitiesFromNamespaceContaining(null!, typeof(RegisteredActivity))).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            configurator.AddActivitiesFromNamespaceContaining(null!)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() =>
            configurator.AddActivitiesFromNamespaceContaining(namespaceLessType)).ParamName);

        int baseline = services.Count;
        configurator.AddActivitiesFromNamespaceContaining<RequirementCoverageAttribute>(_ => false);
        configurator.AddActivitiesFromNamespaceContaining<RequirementCoverageAttribute>();

        Assert.Equal(baseline, services.Count);
    }

    private sealed record RegisteredArguments(string Value);

    private sealed record AlternativeArguments(string Value);

    private sealed record RegisteredLog(string Value);

    private sealed class RegisteredActivity : IActivity<RegisteredArguments, RegisteredLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RegisteredArguments> context) => throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<RegisteredLog> context) => throw new NotSupportedException();
    }

    private sealed class RegisteredExecuteActivity : IExecuteActivity<RegisteredArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RegisteredArguments> context) => throw new NotSupportedException();
    }

    private sealed class SecondRegisteredExecuteActivity : IExecuteActivity<AlternativeArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<AlternativeArguments> context) => throw new NotSupportedException();
    }

    private abstract class AbstractExecuteActivity : IExecuteActivity<RegisteredArguments>
    {
        public abstract Task<ExecutionResult> ExecuteAsync(ExecuteContext<RegisteredArguments> context);
    }

    private sealed class ExecuteOnlyActivityWithTwoContracts :
        IExecuteActivity<RegisteredArguments>,
        IExecuteActivity<AlternativeArguments>
    {
        Task<ExecutionResult> IExecuteActivity<RegisteredArguments>.ExecuteAsync(ExecuteContext<RegisteredArguments> context) =>
            throw new NotSupportedException();

        Task<ExecutionResult> IExecuteActivity<AlternativeArguments>.ExecuteAsync(ExecuteContext<AlternativeArguments> context) =>
            throw new NotSupportedException();
    }

    private sealed class RegisteredActivityDefinition :
        ActivityDefinition<RegisteredActivity, RegisteredArguments, RegisteredLog>;

    private sealed class AlternativeRegisteredActivityDefinition :
        ActivityDefinition<RegisteredActivity, RegisteredArguments, RegisteredLog>;

    private sealed class RegisteredExecuteActivityDefinition :
        ExecuteActivityDefinition<RegisteredExecuteActivity, RegisteredArguments>;

    private abstract class AbstractExecuteActivityDefinition :
        ExecuteActivityDefinition<RegisteredExecuteActivity, RegisteredArguments>;

    private sealed class AmbiguousExecuteActivityDefinition :
        ExecuteActivityDefinition<RegisteredExecuteActivity, RegisteredArguments>,
        IExecuteActivityDefinition<SecondRegisteredExecuteActivity, AlternativeArguments>
    {
        IEndpointDefinition<IExecuteActivity<AlternativeArguments>>?
            IExecuteActivityDefinition<SecondRegisteredExecuteActivity, AlternativeArguments>.ExecuteEndpointDefinition
        {
            set { }
        }

        void IExecuteActivityDefinition<SecondRegisteredExecuteActivity, AlternativeArguments>.Configure(
            IReceiveEndpointConfigurator endpointConfigurator,
            IExecuteActivityConfigurator<SecondRegisteredExecuteActivity, AlternativeArguments> executeActivityConfigurator,
            IRegistrationContext context)
        {
        }
    }

    private sealed class DynamicNamespaceLessType
    {
        public Type Create()
        {
            var assembly = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName($"CourierNamespaceLess_{Guid.NewGuid():N}"),
                AssemblyBuilderAccess.Run);
            return assembly.DefineDynamicModule("main").DefineType("NamespaceLessType").CreateType()!;
        }
    }

    private interface ISecondaryBus : IBus;
}
