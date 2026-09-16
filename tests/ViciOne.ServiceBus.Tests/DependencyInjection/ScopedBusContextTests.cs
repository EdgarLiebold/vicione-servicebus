using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ScopedBusContextTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-CONTEXT", "all-context-constructor-boundaries")]
    public void Constructors_RejectEveryMissingDependency()
    {
        IBus bus = DispatchProxy.Create<IBus, PassiveProxy>();
        IClientFactory clientFactory = DispatchProxy.Create<IClientFactory, PassiveProxy>();
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, PassiveProxy>();
        var provider = new NullServiceProvider();
        var innerContext = new RecordingScopedBusContext();

        AssertParameter("bus", () => new BusScopedBusContext<IBus>(null!, clientFactory, provider));
        AssertParameter("clientFactory", () => new BusScopedBusContext<IBus>(bus, null!, provider));
        AssertParameter("provider", () => new BusScopedBusContext<IBus>(bus, clientFactory, null!));
        AssertParameter("scopedBusContext", () => new BusScopedBusContext(null!, clientFactory, provider));
        AssertParameter("clientFactory", () => new BusScopedBusContext(innerContext, null!, provider));
        AssertParameter("provider", () => new BusScopedBusContext(innerContext, clientFactory, null!));
        AssertParameter("context", () => new ConsumeContextScopedBusContext(null!, clientFactory));
        AssertParameter("clientFactory", () => new ConsumeContextScopedBusContext(consumeContext, null!));
        AssertParameter("bus", () =>
            new ConsumeContextScopedBusContext<IBus>(null!, consumeContext, clientFactory, provider));
        AssertParameter("context", () =>
            new ConsumeContextScopedBusContext<IBus>(bus, null!, clientFactory, provider));
        AssertParameter("clientFactory", () =>
            new ConsumeContextScopedBusContext<IBus>(bus, consumeContext, null!, provider));
        AssertParameter("provider", () =>
            new ConsumeContextScopedBusContext<IBus>(bus, consumeContext, clientFactory, null!));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-CONTEXT", "root-context-thread-safe-facade-identity-and-forwarding")]
    public async Task RootContexts_CacheOneFacadePerScopeAndForwardWrappedEndpointsAsync()
    {
        IBus bus = DispatchProxy.Create<IBus, PassiveProxy>();
        IClientFactory clientFactory = DispatchProxy.Create<IClientFactory, PassiveProxy>();
        var provider = new NullServiceProvider();
        var generic = new BusScopedBusContext<IBus>(bus, clientFactory, provider);

        ScopedBusContext[] genericReads = await ReadConcurrentlyAsync(generic);

        AssertSingleFacadeSet(genericReads);
        Assert.IsType<ScopedSendEndpointProvider>(genericReads[0].SendEndpointProvider);
        Assert.IsType<PublishEndpoint>(genericReads[0].PublishEndpoint);
        Assert.IsType<ScopedClientFactory>(genericReads[0].ClientFactory);

        var inner = new RecordingScopedBusContext();
        var wrapper = new BusScopedBusContext(inner, clientFactory, provider);
        ScopedBusContext[] wrapperReads = await ReadConcurrentlyAsync(wrapper);

        AssertSingleFacadeSet(wrapperReads);
        Assert.Same(inner.SendEndpointProvider, wrapperReads[0].SendEndpointProvider);
        Assert.Same(inner.PublishEndpoint, wrapperReads[0].PublishEndpoint);
        Assert.IsType<ScopedClientFactory>(wrapperReads[0].ClientFactory);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-CONTEXT", "consume-context-thread-safe-facade-identity-and-ambient-context")]
    public async Task ConsumeContexts_PreserveAmbientContextAndCacheOneFacadePerScopeAsync()
    {
        IBus bus = DispatchProxy.Create<IBus, PassiveProxy>();
        IClientFactory clientFactory = DispatchProxy.Create<IClientFactory, PassiveProxy>();
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, PassiveProxy>();
        var provider = new NullServiceProvider();
        var direct = new ConsumeContextScopedBusContext(consumeContext, clientFactory);

        Assert.Same(consumeContext, direct.SendEndpointProvider);
        Assert.Same(consumeContext, direct.PublishEndpoint);
        AssertScopedClientContext(direct.ClientFactory, consumeContext);

        var generic = new ConsumeContextScopedBusContext<IBus>(bus, consumeContext, clientFactory, provider);
        ScopedBusContext[] reads = await ReadConcurrentlyAsync(generic);

        AssertSingleFacadeSet(reads);
        var sendProvider = Assert.IsType<ScopedConsumeSendEndpointProvider>(reads[0].SendEndpointProvider);
        var publishEndpoint = Assert.IsType<PublishEndpoint>(reads[0].PublishEndpoint);
        AssertScopedClientContext(reads[0].ClientFactory, consumeContext);
        Assert.Same(consumeContext, GetField(sendProvider, "_consumeContext"));
        object publishProvider = GetField(publishEndpoint, "_publishEndpointProvider");
        Assert.IsType<ScopedConsumePublishEndpointProvider>(publishProvider);
        Assert.Same(consumeContext, GetField(publishProvider, "_consumeContext"));
    }

    private static async Task<ScopedBusContext[]> ReadConcurrentlyAsync(ScopedBusContext context)
    {
        Task<ScopedBusContext>[] reads = Enumerable.Range(0, 64)
            .Select(_ => Task.Run<ScopedBusContext>(() => new SnapshotScopedBusContext(
                context.SendEndpointProvider,
                context.PublishEndpoint,
                context.ClientFactory)))
            .ToArray();
        return await Task.WhenAll(reads);
    }

    private static void AssertSingleFacadeSet(ScopedBusContext[] reads)
    {
        Assert.Equal(64, reads.Length);
        Assert.All(reads, value => Assert.Same(reads[0].SendEndpointProvider, value.SendEndpointProvider));
        Assert.All(reads, value => Assert.Same(reads[0].PublishEndpoint, value.PublishEndpoint));
        Assert.All(reads, value => Assert.Same(reads[0].ClientFactory, value.ClientFactory));
    }

    private static void AssertScopedClientContext(IScopedClientFactory clientFactory, ConsumeContext consumeContext)
    {
        var scoped = Assert.IsType<ScopedClientFactory>(clientFactory);
        Assert.Same(consumeContext, GetField(scoped, "_consumeContext"));
    }

    private static object GetField(object instance, string name) =>
        instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;

    private static void AssertParameter(string expected, Action action) =>
        Assert.Equal(expected, Assert.Throws<ArgumentNullException>(action).ParamName);

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class RecordingScopedBusContext : ScopedBusContext
    {
        public ISendEndpointProvider SendEndpointProvider { get; } =
            DispatchProxy.Create<ISendEndpointProvider, PassiveProxy>();
        public IPublishEndpoint PublishEndpoint { get; } = DispatchProxy.Create<IPublishEndpoint, PassiveProxy>();
        public IScopedClientFactory ClientFactory { get; } =
            DispatchProxy.Create<IScopedClientFactory, PassiveProxy>();
    }

    private sealed record SnapshotScopedBusContext(
        ISendEndpointProvider SendEndpointProvider,
        IPublishEndpoint PublishEndpoint,
        IScopedClientFactory ClientFactory) : ScopedBusContext;

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }
}
