using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Observers;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class BusObserverOwnershipTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-OBSERVER-OWNERSHIP", "global-and-owner-bound-observers-are-distinct")]
    public async Task MultiBusLifecycle_ConnectsGlobalObserversToEveryBusAndBoundObserversOnlyToTheirOwnerAsync()
    {
        TimeSpan timeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observations = new LifecycleObservations();
        var services = new ServiceCollection();
        services.AddViciOneServiceBusTextWriterLogger(TextWriter.Null);
        services.AddSingleton(observations);
        services.AddBusObserver<GlobalObserver>();
        services.AddBusObserver<IAlphaBus, AlphaObserver>();
        services.AddBusObserver<IBetaBus, BetaObserver>(provider =>
            new BetaObserver(provider.GetRequiredService<LifecycleObservations>()));
        services.AddViciOneServiceBus<IAlphaBus>(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://observer-alpha/")));
        });
        services.AddViciOneServiceBus<IBetaBus>(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            configuration.UsingInMemory((_, bus) => bus.Host(new Uri("loopback://observer-beta/")));
        });
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
        IBusControl alpha = (IBusControl)provider.GetRequiredService<IAlphaBus>();
        IBusControl beta = (IBusControl)provider.GetRequiredService<IBetaBus>();
        bool alphaStarted = false;
        bool betaStarted = false;

        try
        {
            await alpha.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            alphaStarted = true;
            await beta.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            betaStarted = true;

            Assert.Equal(2, observations.Count("global", LifecyclePhase.Created));
            Assert.Equal(2, observations.Count("global", LifecyclePhase.Started));
            Assert.Equal(1, observations.Count("alpha", LifecyclePhase.Created));
            Assert.Equal(1, observations.Count("alpha", LifecyclePhase.Started));
            Assert.Equal(1, observations.Count("beta", LifecyclePhase.Created));
            Assert.Equal(1, observations.Count("beta", LifecyclePhase.Started));
            Assert.All(observations.Addresses("alpha"), address =>
                Assert.Equal("observer-alpha", address.Host));
            Assert.All(observations.Addresses("beta"), address =>
                Assert.Equal("observer-beta", address.Host));

            await beta.StopAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            betaStarted = false;
            Assert.Equal(1, observations.Count("beta", LifecyclePhase.Stopping));
            Assert.Equal(0, observations.Count("alpha", LifecyclePhase.Stopping));

            await alpha.StopAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            alphaStarted = false;
            Assert.Equal(2, observations.Count("global", LifecyclePhase.Stopping));
            Assert.Equal(1, observations.Count("alpha", LifecyclePhase.Stopping));
        }
        finally
        {
            if (betaStarted)
                await beta.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            if (alphaStarted)
                await alpha.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-OBSERVER-OWNERSHIP", "registration-null-guards")]
    public void Registration_RejectsMissingCollectionsAndFactories()
    {
        ArgumentNullException missingServices = Assert.Throws<ArgumentNullException>(() =>
            ObserverRegistrationExtensions.AddBusObserver<IAlphaBus, AlphaObserver>(null!));
        ArgumentNullException missingGlobalFactory = Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddBusObserver<GlobalObserver>(null!));
        ArgumentNullException missingBoundFactory = Assert.Throws<ArgumentNullException>(() =>
            new ServiceCollection().AddBusObserver<IAlphaBus, AlphaObserver>(null!));

        AssertMissingObserverArguments<IReceiveEndpointObserver>(
            ObserverRegistrationExtensions.AddReceiveEndpointObserver<IReceiveEndpointObserver>,
            ObserverRegistrationExtensions.AddReceiveEndpointObserver<IReceiveEndpointObserver>);
        AssertMissingObserverArguments<IReceiveObserver>(
            ObserverRegistrationExtensions.AddReceiveObserver<IReceiveObserver>,
            ObserverRegistrationExtensions.AddReceiveObserver<IReceiveObserver>);
        AssertMissingObserverArguments<IConsumeObserver>(
            ObserverRegistrationExtensions.AddConsumeObserver<IConsumeObserver>,
            ObserverRegistrationExtensions.AddConsumeObserver<IConsumeObserver>);
        AssertMissingObserverArguments<ISendObserver>(
            ObserverRegistrationExtensions.AddSendObserver<ISendObserver>,
            ObserverRegistrationExtensions.AddSendObserver<ISendObserver>);
        AssertMissingObserverArguments<IPublishObserver>(
            ObserverRegistrationExtensions.AddPublishObserver<IPublishObserver>,
            ObserverRegistrationExtensions.AddPublishObserver<IPublishObserver>);

        Assert.Equal("services", missingServices.ParamName);
        Assert.Equal("factory", missingGlobalFactory.ParamName);
        Assert.Equal("factory", missingBoundFactory.ParamName);
    }

    private static void AssertMissingObserverArguments<TObserver>(
        Func<IServiceCollection, IServiceCollection> register,
        Func<IServiceCollection, Func<IServiceProvider, TObserver>, IServiceCollection> registerFactory)
        where TObserver : class
    {
        ArgumentNullException missingServices = Assert.Throws<ArgumentNullException>(() => register(null!));
        ArgumentNullException missingFactory = Assert.Throws<ArgumentNullException>(() =>
            registerFactory(new ServiceCollection(), null!));

        Assert.Equal("services", missingServices.ParamName);
        Assert.Equal("factory", missingFactory.ParamName);
    }

    public interface IAlphaBus : IBus;

    public interface IBetaBus : IBus;

    public sealed class GlobalObserver(LifecycleObservations observations) : LifecycleObserver("global", observations);

    public sealed class AlphaObserver(LifecycleObservations observations) : LifecycleObserver("alpha", observations);

    public sealed class BetaObserver(LifecycleObservations observations) : LifecycleObserver("beta", observations);

    public abstract class LifecycleObserver(string owner, LifecycleObservations observations) : IBusObserver
    {
        public void PostCreate(IBus bus) => observations.Add(owner, LifecyclePhase.Created, bus.Address);

        public void CreateFaulted(Exception exception)
        {
        }

        public Task PreStartAsync(IBus bus) => Task.CompletedTask;

        public Task PostStartAsync(IBus bus, Task<BusReady> busReady)
        {
            observations.Add(owner, LifecyclePhase.Started, bus.Address);
            return Task.CompletedTask;
        }

        public Task StartFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;

        public Task PreStopAsync(IBus bus)
        {
            observations.Add(owner, LifecyclePhase.Stopping, bus.Address);
            return Task.CompletedTask;
        }

        public Task PostStopAsync(IBus bus) => Task.CompletedTask;

        public Task StopFaultedAsync(IBus bus, Exception exception) => Task.CompletedTask;
    }

    public sealed class LifecycleObservations
    {
        private readonly ConcurrentQueue<LifecycleObservation> _observations = [];

        public void Add(string owner, LifecyclePhase phase, Uri address) =>
            _observations.Enqueue(new LifecycleObservation(owner, phase, address));

        public int Count(string owner, LifecyclePhase phase) =>
            _observations.Count(value => value.Owner == owner && value.Phase == phase);

        public Uri[] Addresses(string owner) =>
            [.. _observations.Where(value => value.Owner == owner).Select(value => value.Address).Distinct()];
    }

    public enum LifecyclePhase
    {
        Created,
        Started,
        Stopping,
    }

    private sealed record LifecycleObservation(string Owner, LifecyclePhase Phase, Uri Address);
}
