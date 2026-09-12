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

    [Theory]
    [InlineData(typeof(object), false)]
    [InlineData(typeof(ExecuteOnlyActivityWithTwoContracts), false)]
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
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "public-scanning-boundaries-reject-null-elements")]
    public void AddActivities_RejectsMissingConfiguratorCollectionsAndElements()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CourierRegistrationExtensions.AddActivities(null!, Array.Empty<Type>())).ParamName);
        Assert.Equal("types", Assert.Throws<ArgumentNullException>(() => configurator.AddActivities((Type[])null!)).ParamName);
        Assert.Equal("types", Assert.Throws<ArgumentException>(() =>
            configurator.AddActivities(new Type[] { typeof(RegisteredActivity), null! })).ParamName);
        Assert.Equal("assemblies", Assert.Throws<ArgumentNullException>(() => configurator.AddActivities((Assembly[])null!)).ParamName);
        Assert.Equal("assemblies", Assert.Throws<ArgumentException>(() =>
            configurator.AddActivities(new Assembly[] { typeof(RegisteredActivity).Assembly, null! })).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REGISTRATION", "namespace-scan-requires-a-named-namespace")]
    public void AddActivitiesFromNamespaceContaining_RejectsMissingOrNamespaceLessTypes()
    {
        var configurator = new ServiceCollectionBusConfigurator(new ServiceCollection());
        Type namespaceLessType = new DynamicNamespaceLessType().Create();

        Assert.Equal("configurator", Assert.Throws<ArgumentNullException>(() =>
            CourierRegistrationExtensions.AddActivitiesFromNamespaceContaining(null!, typeof(RegisteredActivity))).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentNullException>(() =>
            configurator.AddActivitiesFromNamespaceContaining(null!)).ParamName);
        Assert.Equal("type", Assert.Throws<ArgumentException>(() =>
            configurator.AddActivitiesFromNamespaceContaining(namespaceLessType)).ParamName);
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

    private sealed class RegisteredExecuteActivityDefinition :
        ExecuteActivityDefinition<RegisteredExecuteActivity, RegisteredArguments>;

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
