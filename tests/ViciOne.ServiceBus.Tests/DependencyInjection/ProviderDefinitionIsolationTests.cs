using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Futures;
using ViciOne.ServiceBus.Futures.DependencyInjection;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ProviderDefinitionIsolationTests
{
    [Theory]
    [InlineData("Consumer", true)]
    [InlineData("Consumer", false)]
    [InlineData("Saga", true)]
    [InlineData("Saga", false)]
    [InlineData("StateMachine", true)]
    [InlineData("StateMachine", false)]
    [InlineData("Activity", true)]
    [InlineData("Activity", false)]
    [InlineData("Execute", true)]
    [InlineData("Execute", false)]
    [InlineData("Future", true)]
    [InlineData("Future", false)]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "six-definition-caches-isolate-context-and-endpoints")]
    public void DefinitionCache_IsolatesContextsAndFullyConfiguredEndpoints(string family, bool explicitDefinition)
    {
        var selector = new ProviderSelector();
        IRegistrationContext first = CreateContext(), second = CreateContext();
        Setup(first, family, explicitDefinition);
        Setup(second, family, explicitDefinition);
        Func<IRegistrationContext, IDefinition> resolve = CreateResolver(family, selector);

        IDefinition firstDefinition = resolve(first);
        IDefinition secondDefinition = resolve(second);
        Assert.NotSame(firstDefinition, secondDefinition);
        Assert.Same(firstDefinition, resolve(first));
        Assert.Same(secondDefinition, resolve(second));
        AssertEndpoints(firstDefinition, first);
        AssertEndpoints(secondDefinition, second);
        Assert.Equal(1, selector.MainDefinitionCalls[first]);
        Assert.Equal(1, selector.MainDefinitionCalls[second]);
        if (explicitDefinition)
        {
            Assert.Same(((ContextProxy)(object)first).Definition, firstDefinition);
            Assert.Same(((ContextProxy)(object)second).Definition, secondDefinition);
        }
    }

    [Theory]
    [InlineData("Consumer")]
    [InlineData("Saga")]
    [InlineData("StateMachine")]
    [InlineData("Activity")]
    [InlineData("Execute")]
    [InlineData("Future")]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "six-definition-caches-publish-one-complete-instance")]
    public async Task DefinitionCache_ConcurrentResolutionPublishesOneCompleteInstanceAsync(string family)
    {
        var selector = new ProviderSelector();
        IRegistrationContext context = CreateContext();
        Setup(context, family, explicitDefinition: false);
        Func<IRegistrationContext, IDefinition> resolve = CreateResolver(family, selector);
        CancellationToken token = TestContext.Current.CancellationToken;
        IDefinition[] results = await Task.WhenAll(Enumerable.Range(0, 16)
            .Select(_ => Task.Run(() =>
            {
                IDefinition definition = resolve(context);
                AssertEndpoints(definition, context);
                return definition;
            }, token))).WaitAsync(TimeSpan.FromSeconds(10), token);

        Assert.Equal(16, results.Length);
        Assert.All(results, definition => Assert.Same(results[0], definition));
        Assert.Same(results[0], resolve(context));
        Assert.Equal(1, selector.MainDefinitionCalls[context]);
    }

    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, false, true)]
    [InlineData(false, false, false)]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "public-container-builds-have-own-live-consumer-definition")]
    public async Task PublicContainerBuilds_UseOwnLivingConsumerDefinitionAsync(bool typed, bool shared, bool disposeFirst)
    {
        ServiceCollection services = Configure(typed);
        var options = new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true };
        await using ServiceProvider first = services.BuildServiceProvider(options);
        await using ServiceProvider second = (shared ? services : Configure(typed)).BuildServiceProvider(options);
        ResolveBus(first, typed);
        Marker firstMarker = first.GetRequiredService<Marker>();
        OwnedConsumerDefinition firstDefinition = Assert.Single(firstMarker.Definitions);
        Assert.Same(firstMarker, firstDefinition.Marker);
        Assert.Equal(0, firstDefinition.DisposeCount);
        if (disposeFirst)
        {
            await first.DisposeAsync();
            Assert.Equal(1, firstDefinition.DisposeCount);
        }

        ResolveBus(second, typed);
        Marker secondMarker = second.GetRequiredService<Marker>();
        OwnedConsumerDefinition secondDefinition = Assert.Single(secondMarker.Definitions);
        Assert.NotSame(firstMarker, secondMarker);
        Assert.NotSame(firstDefinition, secondDefinition);
        Assert.Same(secondMarker, secondDefinition.Marker);
        Assert.Equal(0, secondDefinition.DisposeCount);
        Assert.Equal(disposeFirst ? 1 : 0, firstDefinition.DisposeCount);
        await second.DisposeAsync();
        Assert.Equal(1, secondDefinition.DisposeCount);
    }

    static ServiceCollection Configure(bool typed)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Marker>(_ => new Marker());
        if (typed)
            services.AddViciOneServiceBus<ITestBus>(registration =>
            {
                registration.Limits(MessageLimits.Conservative);
                registration.AddConsumer<TestConsumer, OwnedConsumerDefinition>();
                registration.UsingInMemory(new Uri("loopback://localhost/provider-definition-isolation/"),
                    (context, bus) => bus.ConfigureEndpoints(context));
            });
        else
            services.AddViciOneServiceBus(ConfigureBus);
        return services;
    }

    static void ConfigureBus(IBusRegistrationConfigurator registration)
    {
        registration.Limits(MessageLimits.Conservative);
        registration.AddConsumer<TestConsumer, OwnedConsumerDefinition>();
        registration.UsingInMemory(new Uri("loopback://localhost/provider-definition-isolation/"),
            (context, bus) => bus.ConfigureEndpoints(context));
    }

    static void ResolveBus(IServiceProvider provider, bool typed)
    {
        if (typed)
            _ = provider.GetRequiredService<IBusInstance<ITestBus>>();
        else
            _ = provider.GetRequiredService<IBus>();
    }

    static IRegistrationContext CreateContext() => DispatchProxy.Create<IRegistrationContext, ContextProxy>();

    static void Setup(IRegistrationContext context, string family, bool explicitDefinition)
    {
        var proxy = (ContextProxy)(object)context;
        proxy.Definition = explicitDefinition ? family switch
        {
            "Consumer" => new TestConsumerDefinition(),
            "Saga" => new TestSagaDefinition(),
            "StateMachine" => new TestSagaDefinition(),
            "Activity" => new TestActivityDefinition(),
            "Execute" => new TestExecuteDefinition(),
            "Future" => new TestFutureDefinition(),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        } : null;
        proxy.Endpoint = family switch
        {
            "Consumer" => DispatchProxy.Create<IEndpointDefinition<TestConsumer>, EndpointProxy>(),
            "Saga" or "StateMachine" => DispatchProxy.Create<IEndpointDefinition<TestSaga>, EndpointProxy>(),
            "Activity" or "Execute" => DispatchProxy.Create<IEndpointDefinition<IExecuteActivity<Arguments>>, EndpointProxy>(),
            "Future" => DispatchProxy.Create<IEndpointDefinition<TestFuture>, EndpointProxy>(),
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };
        if (family == "Activity")
            proxy.CompensateEndpoint = DispatchProxy.Create<IEndpointDefinition<ICompensateActivity<Log>>, EndpointProxy>();
    }

    static Func<IRegistrationContext, IDefinition> CreateResolver(string family, IContainerSelector selector)
        => family switch
        {
            "Consumer" => ((IConsumerRegistration)new ConsumerRegistration<TestConsumer>(selector)).GetDefinition,
            "Saga" => ((ISagaRegistration)new SagaRegistration<TestSaga>(selector)).GetDefinition,
            "StateMachine" => ((ISagaRegistration)new SagaStateMachineRegistration<TestMachine, TestSaga>(selector)).GetDefinition,
            "Activity" => ((IActivityRegistration)new ActivityRegistration<TestActivity, Arguments, Log>(selector)).GetDefinition,
            "Execute" => ((IExecuteActivityRegistration)new ExecuteActivityRegistration<TestActivity, Arguments>(selector)).GetDefinition,
            "Future" => new FutureRegistration<TestFuture>(selector).GetDefinition,
            _ => throw new ArgumentOutOfRangeException(nameof(family))
        };

    static void AssertEndpoints(IDefinition definition, IRegistrationContext context)
    {
        var owner = (ContextProxy)(object)context;
        IEndpointDefinition? endpoint = definition switch
        {
            IConsumerDefinition consumer => consumer.EndpointDefinition,
            ISagaDefinition saga => saga.EndpointDefinition,
            IExecuteActivityDefinition execute => execute.ExecuteEndpointDefinition,
            IFutureDefinition future => future.EndpointDefinition,
            _ => throw new InvalidOperationException("Unexpected definition kind.")
        };
        Assert.Same(owner.Endpoint, endpoint);
        if (definition is IActivityDefinition activity)
            Assert.Same(owner.CompensateEndpoint, activity.CompensateEndpointDefinition);
    }

    sealed class ProviderSelector : IContainerSelector
    {
        public ConcurrentDictionary<IServiceProvider, int> MainDefinitionCalls { get; } = new(ReferenceEqualityComparer.Instance);
        public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value)
            where T : class, IRegistration { value = null; return false; }
        public IEnumerable<T> GetRegistrations<T>(IServiceProvider provider) where T : class, IRegistration => [];
        public T? GetDefinition<T>(IServiceProvider provider) where T : class, IDefinition
        {
            bool main = typeof(T) == typeof(IConsumerDefinition<TestConsumer>) || typeof(T) == typeof(ISagaDefinition<TestSaga>)
                || typeof(T) == typeof(IActivityDefinition<TestActivity, Arguments, Log>)
                || typeof(T) == typeof(IExecuteActivityDefinition<TestActivity, Arguments>)
                || typeof(T) == typeof(IFutureDefinition<TestFuture>);
            if (main)
                MainDefinitionCalls.AddOrUpdate(provider, 1, (_, count) => count + 1);
            return main ? ((ContextProxy)(object)provider).Definition as T : null;
        }
        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider) where T : class
        {
            var owner = (ContextProxy)(object)provider;
            return (typeof(T) == typeof(ICompensateActivity<Log>) ? owner.CompensateEndpoint : owner.Endpoint) as IEndpointDefinition<T>;
        }
        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) => throw new NotSupportedException();
        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) => throw new NotSupportedException();
    }

    public class ContextProxy : DispatchProxy
    {
        public IDefinition? Definition { get; set; }
        public IEndpointDefinition Endpoint { get; set; } = null!;
        public IEndpointDefinition? CompensateEndpoint { get; set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new NotSupportedException(targetMethod?.Name);
    }
    public class EndpointProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new NotSupportedException(targetMethod?.Name);
    }
    public interface ITestBus : IBus;
    public sealed record Message;
    public sealed record Arguments;
    public sealed record Log;
    public sealed class TestConsumer : IConsumer<Message>
    {
        public Task ConsumeAsync(ConsumeContext<Message> context) => Task.CompletedTask;
    }
    public sealed class TestSaga : ISaga, ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
    }
    public sealed class TestMachine : ViciOneServiceBusStateMachine<TestSaga>;
    public sealed class TestFuture : ViciOneServiceBusStateMachine<FutureState>;
    public sealed class TestActivity : IActivity<Arguments, Log>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) => throw new NotSupportedException();
        public Task<CompensationResult> CompensateAsync(CompensateContext<Log> context) => throw new NotSupportedException();
    }
    sealed class TestConsumerDefinition : ConsumerDefinition<TestConsumer>;
    sealed class TestSagaDefinition : SagaDefinition<TestSaga>;
    sealed class TestActivityDefinition : ActivityDefinition<TestActivity, Arguments, Log>;
    sealed class TestExecuteDefinition : ExecuteActivityDefinition<TestActivity, Arguments>;
    sealed class TestFutureDefinition : FutureDefinition<TestFuture>;
    public sealed class Marker
    {
        public List<OwnedConsumerDefinition> Definitions { get; } = [];
    }
    public sealed class OwnedConsumerDefinition(Marker marker) : ConsumerDefinition<TestConsumer>, IDisposable
    {
        public Marker Marker { get; } = marker;
        public int DisposeCount { get; private set; }
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpoint, IConsumerConfigurator<TestConsumer> consumer,
            IRegistrationContext context) => context.GetRequiredService<Marker>().Definitions.Add(this);
        public void Dispose()
        {
            // A definition can have multiple DI aliases; observe its terminal lifetime transition.
            DisposeCount = 1;
        }
    }
}
