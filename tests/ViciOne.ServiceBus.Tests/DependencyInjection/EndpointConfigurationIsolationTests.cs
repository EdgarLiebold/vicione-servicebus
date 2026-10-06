using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection.Registration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class EndpointConfigurationIsolationTests
{
    [Theory]
    [InlineData("Consumer", "normal")]
    [InlineData("Saga", "normal")]
    [InlineData("StateMachine", "normal")]
    [InlineData("Consumer", "throw")]
    [InlineData("Saga", "throw")]
    [InlineData("StateMachine", "throw")]
    [InlineData("Consumer", "nested")]
    [InlineData("Saga", "nested")]
    [InlineData("StateMachine", "nested")]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "endpoint-callback-order-nesting-and-failure")]
    public async Task LocalCallbacks_HaveInvocationLifetimeAndPreserveOrderAsync(string family, string mode)
    {
        ServiceCollection services = Configure(family, (context, bus, marker) =>
        {
            void Endpoint(string name, Action<IOptionsSet>? callback) => bus.ReceiveEndpoint(name,
                endpoint => ConfigureFamily(family, context, endpoint, callback));
            void Local(string name, IOptionsSet configurator)
            {
                Assert.False(configurator.TryGetOptions<EndpointOptions>(out _));
                marker.Configurators.Add(configurator);
                marker.Events.Add("local:" + name);
                if (family == "Consumer")
                    configurator.Options(new EndpointOptions(marker, name));
            }

            if (mode == "throw")
            {
                var failure = new InvalidOperationException("local callback failure");
                Assert.Same(failure, Assert.Throws<InvalidOperationException>(() =>
                    Endpoint("first", configurator => { Local("first", configurator); throw failure; })));
            }
            else if (mode == "nested")
                Endpoint("first", configurator =>
                {
                    Local("first", configurator);
                    Endpoint("inner", inner => Local("inner", inner));
                });
            else
                Endpoint("first", configurator => Local("first", configurator));

            Endpoint("second", configurator => Local("second", configurator));
            Endpoint("third", null);
            bus.ConfigureEndpoints(context);
        });
        await using ServiceProvider provider = Build(services);
        _ = provider.GetRequiredService<IBusInstance<ITestBus>>();
        Marker marker = provider.GetRequiredService<Marker>();
        var expected = new List<string> { "definition", "global", "local:first" };
        if (mode == "nested")
        {
            expected.AddRange(["definition", "global", "local:inner"]);
            if (family == "Consumer") expected.Add("options:inner");
        }
        if (family == "Consumer" && mode != "throw") expected.Add("options:first");
        expected.AddRange(["definition", "global", "local:second"]);
        if (family == "Consumer") expected.Add("options:second");
        expected.AddRange(["definition", "global"]);
        Assert.Equal(expected, marker.Events);
        Assert.Equal(mode == "nested" ? 3 : 2, marker.Configurators.Count);
        Assert.Equal(marker.Configurators.Count, marker.Configurators.Distinct(ReferenceEqualityComparer.Instance).Count());
    }

    [Theory]
    [InlineData("Saga", true, true)]
    [InlineData("Saga", true, false)]
    [InlineData("Saga", false, true)]
    [InlineData("Saga", false, false)]
    [InlineData("StateMachine", true, true)]
    [InlineData("StateMachine", true, false)]
    [InlineData("StateMachine", false, true)]
    [InlineData("StateMachine", false, false)]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "saga-callbacks-stay-with-own-provider")]
    public async Task SagaLocalCallbacks_DoNotCrossLivingOrDisposedProvidersAsync(string family, bool shared, bool disposeFirst)
    {
        ServiceCollection Create() => Configure(family, (context, bus, marker) =>
        {
            bus.ReceiveEndpoint("manual", endpoint => ConfigureFamily(family, context, endpoint,
                marker.Manual ? _ => marker.LocalCalls++ : null));
            bus.ConfigureEndpoints(context);
        });
        ServiceCollection services = Create();
        await using ServiceProvider first = Build(services);
        await using ServiceProvider second = Build(shared ? services : Create());
        Marker firstMarker = first.GetRequiredService<Marker>();
        firstMarker.Manual = true;
        _ = first.GetRequiredService<IBusInstance<ITestBus>>();
        Assert.Equal(["definition", "global"], firstMarker.Events);
        Assert.Equal(1, firstMarker.LocalCalls);
        if (disposeFirst) await first.DisposeAsync();
        _ = second.GetRequiredService<IBusInstance<ITestBus>>();
        Marker secondMarker = second.GetRequiredService<Marker>();
        Assert.NotSame(firstMarker, secondMarker);
        Assert.Equal(["definition", "global"], secondMarker.Events);
        Assert.Equal(0, secondMarker.LocalCalls);
        Assert.Equal(1, firstMarker.LocalCalls);
        Assert.Equal(["definition", "global"], firstMarker.Events);
    }

    [Theory]
    [InlineData("Consumer")]
    [InlineData("Saga")]
    [InlineData("StateMachine")]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "concurrent-endpoint-callbacks-have-own-context")]
    public async Task ConcurrentProviderConfiguration_KeepsBothLocalCallbacksIsolatedAsync(string family)
    {
        using var gate = new Barrier(2);
        CancellationToken token = TestContext.Current.CancellationToken;
        ServiceCollection services = Configure(family, (context, bus, marker) =>
        {
            bus.ReceiveEndpoint("first", endpoint => ConfigureFamily(family, context, endpoint, _ =>
            {
                marker.LocalCalls++;
                Assert.True(gate.SignalAndWait(TimeSpan.FromSeconds(10), token));
                Assert.Equal(1, marker.LocalCalls);
            }));
            bus.ReceiveEndpoint("second", endpoint => ConfigureFamily(family, context, endpoint, null));
            bus.ConfigureEndpoints(context);
        });
        await using ServiceProvider first = Build(services);
        await using ServiceProvider second = Build(services);
        await Task.WhenAll(Task.Run(() => first.GetRequiredService<IBusInstance<ITestBus>>(), token),
            Task.Run(() => second.GetRequiredService<IBusInstance<ITestBus>>(), token))
            .WaitAsync(TimeSpan.FromSeconds(15), token);
        Assert.NotSame(first.GetRequiredService<Marker>(), second.GetRequiredService<Marker>());
        foreach (ServiceProvider provider in new[] { first, second })
        {
            Assert.Equal(1, provider.GetRequiredService<Marker>().LocalCalls);
            Assert.Equal(["definition", "global", "definition", "global"], provider.GetRequiredService<Marker>().Events);
        }
    }

    [Theory]
    [InlineData("Consumer", false)]
    [InlineData("Consumer", true)]
    [InlineData("Saga", false)]
    [InlineData("Saga", true)]
    [InlineData("StateMachine", false)]
    [InlineData("StateMachine", true)]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "custom-registration-dispatch-is-preserved")]
    public void CustomReimplementations_RejectLocalCallbackBeforeMutationAndPreserveNullDispatch(string family, bool standalone)
    {
        var selector = new SpySelector();
        IRegistration registration = family switch
        {
            "Consumer" => standalone ? new CustomConsumer() : new ReimplementedConsumer(selector),
            "Saga" => standalone ? new CustomSaga() : new ReimplementedSaga(selector),
            "StateMachine" => standalone ? new CustomStateSaga() : new ReimplementedStateSaga(selector),
            _ => throw new ArgumentOutOfRangeException(nameof(family)),
        };
        selector.Registration = registration;
        using ServiceProvider provider = Build(Configure(family, (_, _, _) => { }));
        var context = new RegistrationContext(provider, selector, new UnusedContextSetter());
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, ForbiddenEndpoint>();
        ConfigurationException failure = Assert.Throws<ConfigurationException>(() =>
            ConfigureFamily(family, context, endpoint, _ => throw new InvalidOperationException("callback must not run")));
        Assert.Contains("endpoint-local callback", failure.Message, StringComparison.Ordinal);
        Assert.Contains("for bus 'unknown'", failure.Message, StringComparison.Ordinal);
        Assert.Equal(0, selector.DefinitionCalls);
        Assert.Equal(0, ((ICustomRegistration)registration).Calls);
        ConfigureFamily(family, context, endpoint, null);
        Assert.Equal(1, ((ICustomRegistration)registration).Calls);
        Assert.Equal(0, selector.DefinitionCalls);
    }

    [Theory]
    [InlineData("Consumer")]
    [InlineData("Saga")]
    [InlineData("StateMachine")]
    [RequirementCoverage("REQ-VSB-DI-DEFINITION-OWNERSHIP", "unchanged-subclasses-retain-local-callback-support")]
    public void InheritedBuiltinDispatch_StillSupportsLocalCallbacks(string family)
    {
        var selector = new SpySelector();
        selector.Registration = family switch
        {
            "Consumer" => new InheritedConsumer(selector),
            "Saga" => new InheritedSaga(selector),
            "StateMachine" => new InheritedStateSaga(selector),
            _ => throw new ArgumentOutOfRangeException(nameof(family)),
        };
        using ServiceProvider provider = Build(Configure(family, (_, _, _) => { }));
        var context = new RegistrationContext(provider, selector, new UnusedContextSetter());
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, RecordingEndpoint>();
        int calls = 0;
        var recorder = (RecordingEndpoint)(object)endpoint;
        ConfigureFamily(family, context, endpoint, configurator =>
        {
            Assert.Equal(0, recorder.Specifications);
            if (family == "Consumer")
                configurator.Options(new EndpointOptions(new Marker(), "inherited", recorder));
            calls++;
        });
        ConfigureFamily(family, context, endpoint, null);
        Assert.Equal(1, calls);
        Assert.Equal(1, selector.DefinitionCalls);
        Assert.Equal(2, ((RecordingEndpoint)(object)endpoint).Specifications);
    }

    static ServiceCollection Configure(string family, Action<IBusRegistrationContext, IInMemoryBusFactoryConfigurator, Marker> configure)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<Marker>(_ => new Marker());
        services.AddViciOneServiceBus<ITestBus>(registration =>
        {
            registration.Limits(MessageLimits.Conservative);
            switch (family)
            {
                case "Consumer": registration.AddConsumer<TestConsumer>(typeof(RecordingConsumerDefinition), (context, _) => Global(context)); break;
                case "Saga": registration.AddSaga<TestSaga, RecordingSagaDefinition>((context, _) => Global(context)).InMemoryRepository(); break;
                case "StateMachine": registration.AddSagaStateMachine<TestMachine, TestState, RecordingStateDefinition>((context, _) => Global(context)).InMemoryRepository(); break;
                default: throw new ArgumentOutOfRangeException(nameof(family));
            }
            registration.UsingInMemory(new Uri("loopback://localhost/endpoint-isolation/"),
                (context, bus) => configure(context, bus, context.GetRequiredService<Marker>()));
        });
        return services;
    }

    static void Global(IRegistrationContext context) => context.GetRequiredService<Marker>().Events.Add("global");
    static ServiceProvider Build(ServiceCollection services) => services.BuildServiceProvider(
        new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
    static void ConfigureFamily(string family, IRegistrationContext context, IReceiveEndpointConfigurator endpoint,
        Action<IOptionsSet>? callback)
    {
        switch (family)
        {
            case "Consumer": context.ConfigureConsumer<TestConsumer>(endpoint, callback == null ? null : configurator => callback(configurator)); break;
            case "Saga": context.ConfigureSaga<TestSaga>(endpoint, callback == null ? null : configurator => callback(configurator)); break;
            case "StateMachine": context.ConfigureSaga<TestState>(endpoint, callback == null ? null : configurator => callback(configurator)); break;
            default: throw new ArgumentOutOfRangeException(nameof(family));
        }
    }

    public interface ITestBus : IBus;
    public sealed record Message(Guid CorrelationId) : ICorrelatedBy<Guid>;
    public sealed class TestConsumer : IConsumer<Message>
    { public Task ConsumeAsync(ConsumeContext<Message> context) => Task.CompletedTask; }
    public sealed class TestSaga : ISaga, IInitiatedBy<Message>
    {
        public Guid CorrelationId { get; set; }
        public Task ConsumeAsync(ConsumeContext<Message> context) => Task.CompletedTask;
    }
    public sealed class TestState : ISagaStateMachineInstance
    { public Guid CorrelationId { get; set; } public IState CurrentState { get; set; } = null!; }
    public sealed class TestMachine : ViciOneServiceBusStateMachine<TestState>
    {
        public TestMachine() { InstanceState(instance => instance.CurrentState); Initially(When(Started).Finalize()); SetCompletedWhenFinalized(); }
        public IEvent<Message> Started { get; private set; } = null!;
    }
    public sealed class Marker
    {
        public bool Manual { get; set; }
        public int LocalCalls { get; set; }
        public List<string> Events { get; } = [];
        public List<IOptionsSet> Configurators { get; } = [];
    }
    sealed class EndpointOptions(Marker marker, string endpoint, RecordingEndpoint? recorder = null) : IOptions, IConfigureReceiveEndpoint
    {
        public void Configure(string? name, IReceiveEndpointConfigurator configurator)
        {
            if (recorder != null) Assert.Equal(0, recorder.Specifications);
            marker.Events.Add("options:" + endpoint);
        }
    }
    public sealed class RecordingConsumerDefinition : ConsumerDefinition<TestConsumer>
    {
        protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpoint, IConsumerConfigurator<TestConsumer> consumer,
            IRegistrationContext context) => context.GetRequiredService<Marker>().Events.Add("definition");
    }
    public sealed class RecordingSagaDefinition : SagaDefinition<TestSaga>
    {
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpoint, ISagaConfigurator<TestSaga> saga,
            IRegistrationContext context) => context.GetRequiredService<Marker>().Events.Add("definition");
    }
    public sealed class RecordingStateDefinition : SagaDefinition<TestState>
    {
        protected override void ConfigureSaga(IReceiveEndpointConfigurator endpoint, ISagaConfigurator<TestState> saga,
            IRegistrationContext context) => context.GetRequiredService<Marker>().Events.Add("definition");
    }

    interface ICustomRegistration { int Calls { get; } }
    class CustomConsumer : IConsumerRegistration, ICustomRegistration
    {
        public int Calls { get; private set; }
        public Type Type => typeof(TestConsumer);
        public bool IncludeInConfigureEndpoints { get; set; } = true;
        public bool RequiresServiceInstance { get; set; }
        public void Configure(IReceiveEndpointConfigurator endpoint, IRegistrationContext context) => Calls++;
        public void AddConfigureAction<T>(Action<IRegistrationContext, IConsumerConfigurator<T>>? action) where T : class, IConsumer => throw new InvalidOperationException("Must not store a local callback");
        public IConsumerDefinition GetDefinition(IRegistrationContext context) => throw new InvalidOperationException("Must not resolve definition");
        public IConsumerRegistrationConfigurator GetConsumerRegistrationConfigurator(IRegistrationConfigurator registration) => throw new NotSupportedException();
    }
    class CustomSaga : ISagaRegistration, ICustomRegistration
    {
        public int Calls { get; private set; }
        public virtual Type Type => typeof(TestSaga);
        public Type? StateMachineType => null;
        public bool IncludeInConfigureEndpoints { get; set; } = true;
        public void Configure(IReceiveEndpointConfigurator endpoint, IRegistrationContext context) => Calls++;
        public void AddConfigureAction<T>(Action<IRegistrationContext, ISagaConfigurator<T>>? action) where T : class => throw new InvalidOperationException("Must not store a local callback");
        public ISagaDefinition GetDefinition(IRegistrationContext context) => throw new InvalidOperationException("Must not resolve definition");
    }
    sealed class CustomStateSaga : CustomSaga { public override Type Type => typeof(TestState); }
    sealed class ReimplementedConsumer(IContainerSelector selector) : ConsumerRegistration<TestConsumer>(selector), IConsumerRegistration, ICustomRegistration
    { public int Calls { get; private set; } void IConsumerRegistration.Configure(IReceiveEndpointConfigurator endpoint, IRegistrationContext context) => Calls++; }
    sealed class ReimplementedSaga(IContainerSelector selector) : SagaRegistration<TestSaga>(selector), ISagaRegistration, ICustomRegistration
    { public int Calls { get; private set; } void ISagaRegistration.Configure(IReceiveEndpointConfigurator endpoint, IRegistrationContext context) => Calls++; }
    sealed class ReimplementedStateSaga(IContainerSelector selector) : SagaStateMachineRegistration<TestMachine, TestState>(selector), ISagaRegistration, ICustomRegistration
    { public int Calls { get; private set; } void ISagaRegistration.Configure(IReceiveEndpointConfigurator endpoint, IRegistrationContext context) => Calls++; }
    sealed class InheritedConsumer(IContainerSelector selector) : ConsumerRegistration<TestConsumer>(selector);
    sealed class InheritedSaga(IContainerSelector selector) : SagaRegistration<TestSaga>(selector);
    sealed class InheritedStateSaga(IContainerSelector selector) : SagaStateMachineRegistration<TestMachine, TestState>(selector);
    sealed class UnusedContextSetter : ISetScopedConsumeContext
    { public IDisposable PushContext(IServiceScope scope, ConsumeContext context) => throw new NotSupportedException(); }
    sealed class SpySelector : IContainerSelector
    {
        public IRegistration Registration { get; set; } = null!;
        public int DefinitionCalls { get; private set; }
        public bool TryGetRegistration<T>(IServiceProvider provider, Type type, [NotNullWhen(true)] out T? value) where T : class, IRegistration
        { value = type == Registration.Type ? Registration as T : null; return value != null; }
        public IEnumerable<T> GetRegistrations<T>(IServiceProvider provider) where T : class, IRegistration => Registration is T value ? [value] : [];
        public T? GetDefinition<T>(IServiceProvider provider) where T : class, IDefinition { DefinitionCalls++; return null; }
        public IEndpointDefinition<T>? GetEndpointDefinition<T>(IServiceProvider provider) where T : class => null;
        public IConfigureReceiveEndpoint GetConfigureReceiveEndpoints(IServiceProvider provider) => throw new NotSupportedException();
        public IEndpointNameFormatter GetEndpointNameFormatter(IServiceProvider provider) => DefaultEndpointNameFormatter.Instance;
    }
    public class ForbiddenEndpoint : DispatchProxy
    { protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => throw new InvalidOperationException("Endpoint must not be touched: " + targetMethod?.Name); }
    public class RecordingEndpoint : DispatchProxy
    {
        public int Specifications { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_InputAddress") return new Uri("loopback://localhost/inherited");
            if (targetMethod?.Name == "AddEndpointSpecification") Specifications++;
            return targetMethod?.ReturnType.IsValueType == true && targetMethod.ReturnType != typeof(void)
                ? Activator.CreateInstance(targetMethod.ReturnType) : null;
        }
    }
}
