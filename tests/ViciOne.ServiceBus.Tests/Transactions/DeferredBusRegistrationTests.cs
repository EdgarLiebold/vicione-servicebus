using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Internals;
using ViciOne.ServiceBus.Tests.InternalAccess.Transactions;
using ViciOne.ServiceBus.Transactions;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transactions;

public sealed class DeferredBusRegistrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "null-registration-configurator-is-rejected")]
    public void RegistrationExtensions_RejectNullConfiguratorsForDefaultAndTypedBuses()
    {
        IBusRegistrationConfigurator defaultConfigurator = null!;
        IBusRegistrationConfigurator<ISecondaryBus> typedConfigurator = null!;

        ArgumentNullException[] actual =
        [
            Assert.Throws<ArgumentNullException>(() => defaultConfigurator.AddAmbientTransactionBus()),
            Assert.Throws<ArgumentNullException>(() => defaultConfigurator.AddBufferedBus()),
            Assert.Throws<ArgumentNullException>(() => typedConfigurator.AddAmbientTransactionBus()),
            Assert.Throws<ArgumentNullException>(() => typedConfigurator.AddBufferedBus()),
        ];

        Assert.All(actual, exception => Assert.Equal("busConfigurator", exception.ParamName));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "null-bus-collaborators-are-rejected")]
    public void InternalAdapters_RejectNullBusCollaboratorsAtConstruction()
    {
        ArgumentNullException ambient = Assert.Throws<ArgumentNullException>(() =>
            new AmbientTransactionBusTestDriver(null!));
        ArgumentNullException buffered = Assert.Throws<ArgumentNullException>(() =>
            new BufferedBusTestDriver(null!));

        Assert.Equal("bus", ambient.ParamName);
        Assert.Equal("bus", buffered.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-API", "truthful-capabilities-and-internal-implementations")]
    public void PublicSurface_SeparatesAmbientAndBufferedCapabilitiesAndRetiresTheAmbiguousApi()
    {
        Assembly assembly = typeof(IAmbientTransactionBus).Assembly;
        MethodInfo flush = Assert.IsAssignableFrom<MethodInfo>(typeof(IBufferedBus).GetMethod(nameof(IBufferedBus.FlushAsync)));
        Dictionary<string, bool> expectedImplementationShapes = new(StringComparer.Ordinal)
        {
            ["ViciOne.ServiceBus.DependencyInjection.AmbientTransactionScopedBusContextProvider`1"] = true,
            ["ViciOne.ServiceBus.DependencyInjection.BufferedBusScopedBusContextProvider`1"] = true,
            ["ViciOne.ServiceBus.DependencyInjection.DeferredBusScopedContextProvider`1"] = false,
            ["ViciOne.ServiceBus.Transactions.AmbientTransactionBus"] = true,
            ["ViciOne.ServiceBus.Transactions.AmbientTransactionNotification"] = true,
            ["ViciOne.ServiceBus.Transactions.BufferedBus"] = true,
            ["ViciOne.ServiceBus.Transactions.DeferredBus"] = false,
            ["ViciOne.ServiceBus.Transactions.DeferredBusPublishEndpointProvider"] = true,
            ["ViciOne.ServiceBus.Transactions.DeferredBusSendEndpoint"] = true,
        };
        Type[] implementationTypes = assembly.GetTypes()
            .Where(type => !type.IsNested
                && ((type.Namespace == "ViciOne.ServiceBus.Transactions" && !type.IsInterface)
                    || expectedImplementationShapes.ContainsKey(type.FullName!)))
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        Assert.True(typeof(IAmbientTransactionBus).IsPublic);
        Assert.True(typeof(IBufferedBus).IsPublic);
        Assert.Null(typeof(IAmbientTransactionBus).GetMethod(nameof(IBufferedBus.FlushAsync)));
        Assert.Equal(typeof(Task), flush.ReturnType);
        Assert.Equal(typeof(CancellationToken), Assert.Single(flush.GetParameters()).ParameterType);
        Assert.Equal(expectedImplementationShapes.Keys.Order(StringComparer.Ordinal),
            implementationTypes.Select(type => type.FullName));
        Assert.All(implementationTypes, type =>
        {
            Assert.True(type.IsNotPublic);
            Assert.Equal(expectedImplementationShapes[type.FullName!], type.IsSealed);
            Assert.Equal(!expectedImplementationShapes[type.FullName!], type.IsAbstract);
        });
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Transactions.ITransactionalBus"));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Transactions.TransactionalBus"));
        Assert.Null(assembly.GetType("ViciOne.ServiceBus.Transactions.TransactionalEnlistmentBus"));

        string[] registrationMethods = typeof(DependencyInjectionTransactionExtensions)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Select(method => method.Name)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.Equal([nameof(DependencyInjectionTransactionExtensions.AddAmbientTransactionBus),
            nameof(DependencyInjectionTransactionExtensions.AddBufferedBus)], registrationMethods);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "default-lifetimes-and-idempotent-owner-bindings")]
    public void DefaultRegistrations_UseTruthfulLifetimesAndRemainIdempotent()
    {
        var ambientServices = new ServiceCollection();
        ambientServices.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.AddAmbientTransactionBus();
            configuration.AddAmbientTransactionBus();
        });
        var bufferedServices = new ServiceCollection();
        bufferedServices.AddViciOneServiceBusTestHarness(configuration =>
        {
            configuration.AddBufferedBus();
            configuration.AddBufferedBus();
        });

        ServiceDescriptor ambient = Assert.Single(ambientServices, descriptor =>
            descriptor.ServiceType == typeof(IAmbientTransactionBus));
        ServiceDescriptor ambientBinding = Assert.Single(ambientServices, descriptor =>
            descriptor.ServiceType == typeof(Bind<IBus, IAmbientTransactionBus>));
        ServiceDescriptor buffered = Assert.Single(bufferedServices, descriptor =>
            descriptor.ServiceType == typeof(IBufferedBus));
        ServiceDescriptor bufferedBinding = Assert.Single(bufferedServices, descriptor =>
            descriptor.ServiceType == typeof(Bind<IBus, IBufferedBus>));

        Assert.Equal(ServiceLifetime.Singleton, ambient.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, ambientBinding.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, buffered.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, bufferedBinding.Lifetime);
        AssertScopedProvider(ambientServices, "AmbientTransactionScopedBusContextProvider`1");
        AssertScopedProvider(bufferedServices, "BufferedBusScopedBusContextProvider`1");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "cross-bus-consumer-scope-retains-capability")]
    public async Task SecondaryBufferedBus_RetainsItsBoundaryInsideADefaultBusConsumerScopeAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var coordinator = new CrossBusBufferedCoordinator(timeout);
        var services = new ServiceCollection();
        services.AddSingleton(coordinator);
        services
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<CrossBusBufferedConsumer>();
            })
            .AddViciOneServiceBus<ISecondaryBus>(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddBufferedBus();
                configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://localhost/secondary-buffered")));
            });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);
        Bind<ISecondaryBus, IBufferedBus> secondary = harness.Scope.ServiceProvider
            .GetRequiredService<Bind<ISecondaryBus, IBufferedBus>>();
        var observer = new RecordingPublishObserver();
        using ConnectHandle observerHandle = secondary.Value.ConnectPublishObserver(observer);
        var trigger = new CrossBusBufferedTrigger(NewId.NextGuid());

        try
        {
            await harness.Bus.PublishAsync(trigger, cancellationToken);
            await coordinator.Buffered.Task.WaitAsync(timeout, cancellationToken);

            Assert.Empty(observer.Events);

            coordinator.Release.TrySetResult();
            await coordinator.Flushed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(["Pre", "Post"], observer.Events.Select(item => item.Stage));
            Assert.All(observer.Events, item =>
                Assert.Equal(new CrossBusBufferedResult(trigger.CorrelationId), Assert.IsType<CrossBusBufferedResult>(item.Message)));
        }
        finally
        {
            coordinator.Release.TrySetResult();
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "multibus-capabilities-are-owner-bound")]
    public async Task MultiBusRegistration_BindsEachCapabilityOnlyToItsOwningBusAsync()
    {
        var services = new ServiceCollection();
        services
            .AddViciOneServiceBusTestHarness(configuration => configuration.AddBufferedBus())
            .AddViciOneServiceBus<ISecondaryBus>(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddAmbientTransactionBus();
                configuration.AddAmbientTransactionBus();
                configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://localhost/secondary")));
            })
            .AddViciOneServiceBus<ITertiaryBus>(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddBufferedBus();
                configuration.AddBufferedBus();
                configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://localhost/tertiary")));
            });

        ServiceDescriptor defaultCapability = Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(Bind<IBus, IBufferedBus>));
        ServiceDescriptor secondaryCapability = Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(Bind<ISecondaryBus, IAmbientTransactionBus>));
        ServiceDescriptor tertiaryCapability = Assert.Single(services, descriptor =>
            descriptor.ServiceType == typeof(Bind<ITertiaryBus, IBufferedBus>));

        Assert.Equal(ServiceLifetime.Scoped, defaultCapability.Lifetime);
        Assert.Equal(ServiceLifetime.Singleton, secondaryCapability.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, tertiaryCapability.Lifetime);
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(Bind<IBus, IAmbientTransactionBus>));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(Bind<ISecondaryBus, IBufferedBus>));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(Bind<ITertiaryBus, IAmbientTransactionBus>));
        Assert.DoesNotContain(services, descriptor => descriptor.ServiceType == typeof(IAmbientTransactionBus));
        AssertScopedProvider(services, "BufferedBusScopedBusContextProvider`1", typeof(IBus));
        AssertScopedProvider(services, "AmbientTransactionScopedBusContextProvider`1", typeof(ISecondaryBus));
        AssertScopedProvider(services, "BufferedBusScopedBusContextProvider`1", typeof(ITertiaryBus));

        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        using IServiceScope scope = provider.CreateScope();
        Bind<IBus, IBufferedBus> resolvedDefault = scope.ServiceProvider.GetRequiredService<Bind<IBus, IBufferedBus>>();
        Bind<ISecondaryBus, IAmbientTransactionBus> resolvedSecondary =
            scope.ServiceProvider.GetRequiredService<Bind<ISecondaryBus, IAmbientTransactionBus>>();
        Bind<ITertiaryBus, IBufferedBus> resolvedTertiary =
            scope.ServiceProvider.GetRequiredService<Bind<ITertiaryBus, IBufferedBus>>();

        Assert.IsAssignableFrom<IBufferedBus>(resolvedDefault.Value);
        Assert.IsAssignableFrom<IAmbientTransactionBus>(resolvedSecondary.Value);
        Assert.IsAssignableFrom<IBufferedBus>(resolvedTertiary.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "mixed-capabilities-fail-fast")]
    public void SameBus_RejectsMixedAmbientAndBufferedOwnershipInEitherOrder()
    {
        ConfigurationException ambientFirst = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.AddAmbientTransactionBus();
                configuration.AddBufferedBus();
            }));
        ConfigurationException bufferedFirst = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.AddBufferedBus();
                configuration.AddAmbientTransactionBus();
            }));

        Assert.Contains(nameof(DependencyInjectionTransactionExtensions.AddBufferedBus), ambientFirst.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IAmbientTransactionBus), ambientFirst.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(DependencyInjectionTransactionExtensions.AddAmbientTransactionBus), bufferedFirst.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IBufferedBus), bufferedFirst.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "typed-bus-mixed-capabilities-fail-fast")]
    public void SameTypedBus_RejectsMixedAmbientAndBufferedOwnershipInEitherOrder(bool ambientFirst)
    {
        ConfigurationException actual = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBus<ISecondaryBus>(configuration =>
            {
                if (ambientFirst)
                {
                    configuration.AddAmbientTransactionBus();
                    configuration.AddBufferedBus();
                }
                else
                {
                    configuration.AddBufferedBus();
                    configuration.AddAmbientTransactionBus();
                }
            }));

        Assert.Contains(
            ambientFirst
                ? nameof(DependencyInjectionTransactionExtensions.AddBufferedBus)
                : nameof(DependencyInjectionTransactionExtensions.AddAmbientTransactionBus),
            actual.Message,
            StringComparison.Ordinal);
        Assert.Contains(ambientFirst ? nameof(IAmbientTransactionBus) : nameof(IBufferedBus),
            actual.Message,
            StringComparison.Ordinal);
        Assert.Contains(nameof(ISecondaryBus), actual.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "buffer-capacity-validation")]
    public void BufferedRegistrations_RejectNonPositiveCapacityForDefaultAndTypedBuses()
    {
        ArgumentOutOfRangeException defaultFailure = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ServiceCollection().AddViciOneServiceBusTestHarness(configuration => configuration.AddBufferedBus(0)));
        ArgumentOutOfRangeException typedFailure = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ServiceCollection().AddViciOneServiceBus<ISecondaryBus>(configuration => configuration.AddBufferedBus(-1)));

        Assert.Equal("capacity", defaultFailure.ParamName);
        Assert.Equal(0, defaultFailure.ActualValue);
        Assert.Equal("capacity", typedFailure.ParamName);
        Assert.Equal(-1, typedFailure.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "conflicting-buffer-capacity-rejected")]
    public void RepeatedBufferedRegistration_RejectsAConflictingCapacityInsteadOfSilentlyKeepingTheFirst()
    {
        ConfigurationException defaultFailure = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.AddBufferedBus(2);
                configuration.AddBufferedBus(3);
            }));
        ConfigurationException typedFailure = Assert.Throws<ConfigurationException>(() =>
            new ServiceCollection().AddViciOneServiceBus<ISecondaryBus>(configuration =>
            {
                configuration.AddBufferedBus(5);
                configuration.AddBufferedBus(7);
            }));

        Assert.Contains("2", defaultFailure.Message, StringComparison.Ordinal);
        Assert.Contains("3", defaultFailure.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(IBus), defaultFailure.Message, StringComparison.Ordinal);
        Assert.Contains("5", typedFailure.Message, StringComparison.Ordinal);
        Assert.Contains("7", typedFailure.Message, StringComparison.Ordinal);
        Assert.Contains(nameof(ISecondaryBus), typedFailure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "typed-bus-reflection-preserves-failure-identity")]
    public void TypedBusReflectionBoundary_PreservesTheOriginalConfigurationFailure()
    {
        var expected = new ConfigurationException("expected typed-bus configuration failure");
        var callback = new ThrowingBusInstanceCallback(expected);

        ConfigurationException actual = Assert.Throws<ConfigurationException>(() =>
            BusInstanceBuilderTestDriver.GetBusInstanceType<ISecondaryBus, object>(callback));

        Assert.Same(expected, actual);
        Assert.Contains(
            $"{nameof(ThrowingBusInstanceCallback)}.{nameof(ThrowingBusInstanceCallback.GetResult)}",
            actual.StackTrace,
            StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-DEFERRED-BUS-DI", "resolved-lifetimes-share-owner-bound-instance")]
    public async Task ResolvedCapabilities_MatchTheirOwnerBindingAndConfiguredLifetimeAsync()
    {
        await using ServiceProvider ambientProvider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration => configuration.AddAmbientTransactionBus())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        await using ServiceProvider bufferedProvider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(configuration => configuration.AddBufferedBus())
            .BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using IServiceScope ambientScopeOne = ambientProvider.CreateScope();
        using IServiceScope ambientScopeTwo = ambientProvider.CreateScope();
        using IServiceScope bufferedScopeOne = bufferedProvider.CreateScope();
        using IServiceScope bufferedScopeTwo = bufferedProvider.CreateScope();

        IAmbientTransactionBus ambientOne = ambientScopeOne.ServiceProvider.GetRequiredService<IAmbientTransactionBus>();
        IAmbientTransactionBus ambientTwo = ambientScopeTwo.ServiceProvider.GetRequiredService<IAmbientTransactionBus>();
        IBufferedBus bufferedOne = bufferedScopeOne.ServiceProvider.GetRequiredService<IBufferedBus>();
        IBufferedBus bufferedOneAgain = bufferedScopeOne.ServiceProvider.GetRequiredService<IBufferedBus>();
        IBufferedBus bufferedTwo = bufferedScopeTwo.ServiceProvider.GetRequiredService<IBufferedBus>();

        Assert.Same(ambientOne, ambientTwo);
        Assert.Same(
            ambientOne,
            ambientScopeOne.ServiceProvider.GetRequiredService<Bind<IBus, IAmbientTransactionBus>>().Value);
        Assert.Same(bufferedOne, bufferedOneAgain);
        Assert.Same(
            bufferedOne,
            bufferedScopeOne.ServiceProvider.GetRequiredService<Bind<IBus, IBufferedBus>>().Value);
        Assert.NotSame(bufferedOne, bufferedTwo);
    }

    public sealed record CrossBusBufferedTrigger(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed record CrossBusBufferedResult(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class CrossBusBufferedConsumer(
        Bind<ISecondaryBus, IPublishEndpoint> publishEndpoint,
        Bind<ISecondaryBus, IBufferedBus> bufferedBus,
        CrossBusBufferedCoordinator coordinator) : IConsumer<CrossBusBufferedTrigger>
    {
        public async Task ConsumeAsync(ConsumeContext<CrossBusBufferedTrigger> context)
        {
            try
            {
                await publishEndpoint.Value.PublishAsync(
                    new CrossBusBufferedResult(context.Message.CorrelationId),
                    context.CancellationToken);
                coordinator.Buffered.TrySetResult();
                await coordinator.Release.Task.WaitAsync(coordinator.Timeout, context.CancellationToken);
                await bufferedBus.Value.FlushAsync(context.CancellationToken);
                coordinator.Flushed.TrySetResult();
            }
            catch (Exception exception)
            {
                coordinator.Buffered.TrySetException(exception);
                coordinator.Flushed.TrySetException(exception);
                throw;
            }
        }
    }

    private sealed class CrossBusBufferedCoordinator(TimeSpan timeout)
    {
        public TaskCompletionSource Buffered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Flushed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TimeSpan Timeout { get; } = timeout;
    }

    private sealed class RecordingPublishObserver : IPublishObserver
    {
        private readonly ConcurrentQueue<PublishObservation> _events = new();

        public PublishObservation[] Events => _events.ToArray();

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Pre", context.Message));
            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Post", context.Message));
            return Task.CompletedTask;
        }

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class
        {
            _events.Enqueue(new PublishObservation("Fault", context.Message));
            return Task.CompletedTask;
        }
    }

    private sealed record PublishObservation(string Stage, object Message);

    private sealed class ThrowingBusInstanceCallback(ConfigurationException exception) :
        IBusInstanceBuilderCallback<ISecondaryBus, object>
    {
        public object GetResult<TBusInstance>()
            where TBusInstance : BusInstance<ISecondaryBus>, ISecondaryBus
        {
            throw exception;
        }
    }

    private static void AssertScopedProvider(
        IEnumerable<ServiceDescriptor> services,
        string genericTypeName,
        Type? busType = null)
    {
        ServiceDescriptor descriptor = Assert.Single(services, candidate =>
            candidate.ServiceType.IsGenericType
            && candidate.ServiceType.GetGenericTypeDefinition() == typeof(IScopedBusContextProvider<>)
            && (busType == null || candidate.ServiceType.GenericTypeArguments[0] == busType));

        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
        Assert.NotNull(descriptor.ImplementationType);
        Assert.Equal(genericTypeName, descriptor.ImplementationType.Name);
        Assert.True(descriptor.ImplementationType.IsNotPublic);
        Assert.True(descriptor.ImplementationType.IsSealed);
    }

    public interface ISecondaryBus : IBus;

    public interface ITertiaryBus : IBus;
}
