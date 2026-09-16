using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ScopedConsumeEndpointProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-ENDPOINTS", "scoped-consume-provider-constructor-boundaries")]
    public void Constructors_RejectEveryMissingDependency()
    {
        var endpoint = new RecordingSendEndpoint();
        var publishProvider = new RecordingPublishEndpointProvider(endpoint);
        var sendProvider = new RecordingSendEndpointProvider(endpoint);
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, PassiveProxy>();
        var serviceProvider = new NullServiceProvider();

        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumePublishEndpointProvider(null!, consumeContext, serviceProvider)).ParamName);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumePublishEndpointProvider(publishProvider, null!, serviceProvider)).ParamName);
        Assert.Equal("serviceProvider", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumePublishEndpointProvider(publishProvider, consumeContext, null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumeSendEndpointProvider(null!, consumeContext, serviceProvider)).ParamName);
        Assert.Equal("consumeContext", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumeSendEndpointProvider(sendProvider, null!, serviceProvider)).ParamName);
        Assert.Equal("scope", Assert.Throws<ArgumentNullException>(() =>
            new ScopedConsumeSendEndpointProvider(sendProvider, consumeContext, null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-ENDPOINTS", "scoped-consume-provider-forwards-cancellation-scope-observers-and-route-diagnostic")]
    public async Task Providers_ForwardCancellationScopeObserversAndRouteOwnershipAsync()
    {
        var endpoint = new RecordingSendEndpoint();
        var publishProvider = new RecordingPublishEndpointProvider(endpoint);
        var sendProvider = new RecordingSendEndpointProvider(endpoint);
        ConsumeContext consumeContext = DispatchProxy.Create<ConsumeContext, PassiveProxy>();
        var serviceProvider = new NullServiceProvider();
        var scopedPublish = new ScopedConsumePublishEndpointProvider(publishProvider, consumeContext, serviceProvider);
        var scopedSend = new ScopedConsumeSendEndpointProvider(sendProvider, consumeContext, serviceProvider);
        IPublishObserver publishObserver = DispatchProxy.Create<IPublishObserver, PassiveProxy>();
        ISendObserver sendObserver = DispatchProxy.Create<ISendObserver, PassiveProxy>();
        using var cancellation = new CancellationTokenSource();
        Uri address = new("loopback://localhost/scoped-consume-provider");

        ISendEndpoint resolvedPublish = await ((IPublishEndpointProvider)scopedPublish)
            .GetPublishSendEndpointAsync<Probe>(cancellation.Token);
        ISendEndpoint resolvedSend = await ((ISendEndpointProvider)scopedSend)
            .GetSendEndpointAsync(address, cancellation.Token);

        ScopedSendEndpoint scopedPublishEndpoint = Assert.IsType<ScopedSendEndpoint>(resolvedPublish);
        ScopedSendEndpoint scopedSendEndpoint = Assert.IsType<ScopedSendEndpoint>(resolvedSend);
        SendEndpointProxy consumePublishEndpoint = Assert.IsAssignableFrom<SendEndpointProxy>(scopedPublishEndpoint.Endpoint);
        SendEndpointProxy consumeSendEndpoint = Assert.IsAssignableFrom<SendEndpointProxy>(scopedSendEndpoint.Endpoint);
        Assert.Same(endpoint, consumePublishEndpoint.Endpoint);
        Assert.Same(endpoint, consumeSendEndpoint.Endpoint);
        FieldInfo? contextField = consumeSendEndpoint.GetType().GetField(
            "_context",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(contextField);
        Assert.Same(consumeContext, contextField.GetValue(consumePublishEndpoint));
        Assert.Same(consumeContext, contextField.GetValue(consumeSendEndpoint));
        FieldInfo? scopeField = typeof(ScopedSendEndpoint).GetField(
            "_scope",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(scopeField);
        Assert.Same(serviceProvider, scopeField.GetValue(scopedPublishEndpoint));
        Assert.Same(serviceProvider, scopeField.GetValue(scopedSendEndpoint));
        Assert.Equal(cancellation.Token, Assert.Single(publishProvider.Resolutions));
        Assert.Equal((address, cancellation.Token), Assert.Single(sendProvider.Resolutions));
        Assert.Same(publishProvider.ObserverHandle,
            ((IPublishObserverConnector)scopedPublish).ConnectPublishObserver(publishObserver));
        Assert.Same(publishObserver, publishProvider.Observer);
        Assert.Same(sendProvider.ObserverHandle,
            ((ISendObserverConnector)scopedSend).ConnectSendObserver(sendObserver));
        Assert.Same(sendObserver, sendProvider.Observer);
        Type routeProviderType = Assert.Single(
            typeof(ScopedConsumeSendEndpointProvider).GetInterfaces(),
            type => type.Name == "IMessageRouteProvider");
        TargetInvocationException routeFailure = Assert.Throws<TargetInvocationException>(() =>
            routeProviderType.GetProperty("MessageRoutes")!.GetValue(scopedSend));
        ConfigurationException configurationFailure = Assert.IsType<ConfigurationException>(routeFailure.InnerException);
        Assert.Contains("does not expose its owning bus message routes", configurationFailure.Message, StringComparison.Ordinal);
    }

    private sealed record Probe;

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class RecordingPublishEndpointProvider(ISendEndpoint endpoint) : IPublishEndpointProvider
    {
        public List<CancellationToken> Resolutions { get; } = [];
        public ConnectHandle ObserverHandle { get; } = new EmptyConnectHandle();
        public IPublishObserver? Observer { get; private set; }

        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            Resolutions.Add(cancellationToken);
            return Task.FromResult(endpoint);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer)
        {
            Observer = observer;
            return ObserverHandle;
        }
    }

    private sealed class RecordingSendEndpointProvider(ISendEndpoint endpoint) : ISendEndpointProvider
    {
        public List<(Uri Address, CancellationToken CancellationToken)> Resolutions { get; } = [];
        public ConnectHandle ObserverHandle { get; } = new EmptyConnectHandle();
        public ISendObserver? Observer { get; private set; }
        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default)
        {
            Resolutions.Add((address, cancellationToken));
            return Task.FromResult(endpoint);
        }

        public ConnectHandle ConnectSendObserver(ISendObserver observer)
        {
            Observer = observer;
            return ObserverHandle;
        }
    }

    private sealed class RecordingSendEndpoint : ISendEndpoint
    {
        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();
    }

    private class PassiveProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            targetMethod?.ReturnType.IsValueType == true
                ? Activator.CreateInstance(targetMethod.ReturnType)
                : null;
    }
}
