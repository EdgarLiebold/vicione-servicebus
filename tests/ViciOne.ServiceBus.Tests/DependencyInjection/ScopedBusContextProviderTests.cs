using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Registration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transactions;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ScopedBusContextProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-CONTEXT", "normal-and-deferred-selector-constructor-boundaries")]
    public void Constructors_RejectMissingBindingsProvidersAndBoundValues()
    {
        IBus bus = Proxy<IBus>();
        IAmbientTransactionBus ambientBus = Proxy<IAmbientTransactionBus>();
        IBufferedBus bufferedBus = Proxy<IBufferedBus>();
        IClientFactory clientFactory = Proxy<IClientFactory>();
        var provider = new NullServiceProvider();
        var absentContext = new RecordingConsumeContextProvider(context: null);
        var presentContext = new RecordingConsumeContextProvider(Proxy<ConsumeContext>());
        Bind<IBus, IClientFactory> clientBinding = Bind<IBus>.Create(clientFactory);
        Bind<IBus, IScopedConsumeContextProvider> contextBinding = Bind<IBus>.Create<IScopedConsumeContextProvider>(absentContext);
        Bind<IBus, IScopedConsumeContextProvider> presentContextBinding =
            Bind<IBus>.Create<IScopedConsumeContextProvider>(presentContext);
        Bind<IBus, IScopedConsumeContextProvider> throwingContextBinding =
            Bind<IBus>.Create<IScopedConsumeContextProvider>(new ThrowingConsumeContextProvider());
        Bind<IBus, IAmbientTransactionBus> ambientBinding = Bind<IBus>.Create(ambientBus);
        Bind<IBus, IBufferedBus> bufferedBinding = Bind<IBus>.Create(bufferedBus);

        AssertParameter("bus", () =>
            new ScopedBusContextProvider<IBus>(null!, clientBinding, presentContextBinding, absentContext, provider));
        AssertParameter("clientFactory", () =>
            new ScopedBusContextProvider<IBus>(bus, null!, contextBinding, absentContext, provider));
        AssertParameter("busConsumeContextProvider", () =>
            new ScopedBusContextProvider<IBus>(bus, clientBinding, null!, absentContext, provider));
        AssertParameter("globalConsumeContextProvider", () =>
            new ScopedBusContextProvider<IBus>(bus, clientBinding, contextBinding, null!, provider));
        AssertParameter("provider", () =>
            new ScopedBusContextProvider<IBus>(bus, clientBinding, contextBinding, absentContext, null!));
        AssertParameter("clientFactory", () => new ScopedBusContextProvider<IBus>(
            bus, new Bind<IBus, IClientFactory>(null!), throwingContextBinding, absentContext, provider));
        AssertParameter("busConsumeContextProvider", () => new ScopedBusContextProvider<IBus>(
            bus, clientBinding, new Bind<IBus, IScopedConsumeContextProvider>(null!), absentContext, provider));

        AssertParameter("bus", () =>
            new AmbientTransactionScopedBusContextProvider<IBus>(null!, clientBinding, contextBinding, absentContext, provider));
        AssertParameter("bus", () => new AmbientTransactionScopedBusContextProvider<IBus>(
            new Bind<IBus, IAmbientTransactionBus>(null!), clientBinding, contextBinding, absentContext, provider));
        AssertParameter("clientFactory", () =>
            new AmbientTransactionScopedBusContextProvider<IBus>(ambientBinding, null!, contextBinding, absentContext, provider));
        AssertParameter("clientFactory", () => new AmbientTransactionScopedBusContextProvider<IBus>(
            ambientBinding, new Bind<IBus, IClientFactory>(null!), throwingContextBinding, absentContext, provider));
        AssertParameter("consumeContextProvider", () =>
            new AmbientTransactionScopedBusContextProvider<IBus>(ambientBinding, clientBinding, null!, absentContext, provider));
        AssertParameter("consumeContextProvider", () => new AmbientTransactionScopedBusContextProvider<IBus>(
            ambientBinding, clientBinding, new Bind<IBus, IScopedConsumeContextProvider>(null!), absentContext, provider));
        AssertParameter("globalConsumeContextProvider", () =>
            new AmbientTransactionScopedBusContextProvider<IBus>(ambientBinding, clientBinding, contextBinding, null!, provider));
        AssertParameter("provider", () =>
            new AmbientTransactionScopedBusContextProvider<IBus>(ambientBinding, clientBinding, contextBinding, absentContext, null!));
        AssertParameter("bus", () =>
            new BufferedBusScopedBusContextProvider<IBus>(null!, clientBinding, contextBinding, absentContext, provider));
        Assert.NotNull(new BufferedBusScopedBusContextProvider<IBus>(
            bufferedBinding, clientBinding, contextBinding, absentContext, provider).Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-CONTEXT", "normal-selector-specific-global-root-precedence")]
    public void NormalSelector_PrefersSpecificThenGlobalThenRootContext()
    {
        IBus bus = Proxy<IBus>();
        IClientFactory clientFactory = Proxy<IClientFactory>();
        var provider = new NullServiceProvider();
        ConsumeContext specificContext = Proxy<ConsumeContext>();
        ConsumeContext globalContext = Proxy<ConsumeContext>();
        Bind<IBus, IClientFactory> clientBinding = Bind<IBus>.Create(clientFactory);

        var specific = new ScopedBusContextProvider<IBus>(
            bus,
            clientBinding,
            Bind<IBus>.Create<IScopedConsumeContextProvider>(new RecordingConsumeContextProvider(specificContext)),
            new RecordingConsumeContextProvider(globalContext),
            provider);
        var global = new ScopedBusContextProvider<IBus>(
            bus,
            clientBinding,
            Bind<IBus>.Create<IScopedConsumeContextProvider>(new RecordingConsumeContextProvider(null)),
            new RecordingConsumeContextProvider(globalContext),
            provider);
        var root = new ScopedBusContextProvider<IBus>(
            bus,
            clientBinding,
            Bind<IBus>.Create<IScopedConsumeContextProvider>(new RecordingConsumeContextProvider(null)),
            new RecordingConsumeContextProvider(null),
            provider);

        var specificResult = Assert.IsType<ConsumeContextScopedBusContext>(specific.Context);
        Assert.Same(specificContext, specificResult.SendEndpointProvider);
        Assert.IsType<ConsumeContextScopedBusContext<IBus>>(global.Context);
        Assert.Same(globalContext, GetField(global.Context, "_context"));
        Assert.IsType<BusScopedBusContext<IBus>>(root.Context);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-CONTEXT", "deferred-selectors-specific-global-root-precedence")]
    public void DeferredSelectors_PreferSpecificThenGlobalThenRootContext()
    {
        IAmbientTransactionBus ambientBus = Proxy<IAmbientTransactionBus>();
        IBufferedBus bufferedBus = Proxy<IBufferedBus>();
        IClientFactory clientFactory = Proxy<IClientFactory>();
        var provider = new NullServiceProvider();
        ConsumeContext specificContext = Proxy<ConsumeContext>();
        ConsumeContext globalContext = Proxy<ConsumeContext>();
        Bind<IBus, IClientFactory> clientBinding = Bind<IBus>.Create(clientFactory);
        Bind<IBus, IAmbientTransactionBus> ambientBinding = Bind<IBus>.Create(ambientBus);
        Bind<IBus, IBufferedBus> bufferedBinding = Bind<IBus>.Create(bufferedBus);
        Bind<IBus, IScopedConsumeContextProvider> specificBinding = Bind<IBus>.Create<IScopedConsumeContextProvider>(
            new RecordingConsumeContextProvider(specificContext));
        Bind<IBus, IScopedConsumeContextProvider> absentBinding = Bind<IBus>.Create<IScopedConsumeContextProvider>(
            new RecordingConsumeContextProvider(null));

        var specific = new AmbientTransactionScopedBusContextProvider<IBus>(
            ambientBinding, clientBinding, specificBinding, new RecordingConsumeContextProvider(globalContext), provider);
        var global = new BufferedBusScopedBusContextProvider<IBus>(
            bufferedBinding, clientBinding, absentBinding, new RecordingConsumeContextProvider(globalContext), provider);
        var root = new AmbientTransactionScopedBusContextProvider<IBus>(
            ambientBinding, clientBinding, absentBinding, new RecordingConsumeContextProvider(null), provider);

        Assert.IsType<ConsumeContextScopedBusContext<IBus>>(specific.Context);
        Assert.Same(specificContext, GetField(specific.Context, "_context"));
        Assert.IsType<ConsumeContextScopedBusContext<IBus>>(global.Context);
        Assert.Same(globalContext, GetField(global.Context, "_context"));
        Assert.IsType<BusScopedBusContext<IBus>>(root.Context);
    }

    private static T Proxy<T>() where T : class => DispatchProxy.Create<T, PassiveProxy>();

    private static object GetField(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private sealed class RecordingConsumeContextProvider(ConsumeContext? context) : IScopedConsumeContextProvider
    {
        public bool HasContext => context is not null;

        public ConsumeContext GetContext() => context ?? throw new InvalidOperationException("No context is present.");

        public IDisposable PushContext(ConsumeContext pushedContext) => throw new NotSupportedException();
    }

    private sealed class ThrowingConsumeContextProvider : IScopedConsumeContextProvider
    {
        public bool HasContext => throw new InvalidOperationException("The selector inspected context before validating its bindings.");

        public ConsumeContext GetContext() => throw new NotSupportedException();

        public IDisposable PushContext(ConsumeContext context) => throw new NotSupportedException();
    }

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }
}
