using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class ScopedSendEndpointTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-ENDPOINTS", "scoped-send-endpoint-and-pipe-constructor-boundaries")]
    public void Constructors_RejectMissingRequiredDependenciesAndAllowAnOmittedCallerPipe()
    {
        var endpoint = new PassiveSendEndpoint();
        var serviceProvider = new NullServiceProvider();

        Assert.Equal("endpoint", Assert.Throws<ArgumentNullException>(() =>
            new ScopedSendEndpoint(null!, serviceProvider)).ParamName);
        Assert.Equal("scope", Assert.Throws<ArgumentNullException>(() =>
            new ScopedSendEndpoint(endpoint, null!)).ParamName);
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() =>
            new ScopedSendPipeAdapter<Probe>(null!, pipe: null)).ParamName);
        Assert.IsType<ScopedSendPipeAdapter<Probe>>(
            new ScopedSendPipeAdapter<Probe>(serviceProvider, pipe: null));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONTAINER-SCOPED-BUS-ENDPOINTS", "scoped-send-pipe-adds-preserves-and-forwards-scope")]
    public async Task Pipe_AddsMissingScopePreservesExistingScopeAndForwardsTheTypedCallerPipeAsync()
    {
        var endpoint = new PassiveSendEndpoint();
        var serviceProvider = new NullServiceProvider();
        var existingProvider = new NullServiceProvider();
        IServiceProvider? observedProvider = null;
        var callerPipe = Pipe.Execute<SendContext<Probe>>(context =>
            observedProvider = context.GetPayload<IServiceProvider>());
        var scopedEndpoint = new ExposedScopedSendEndpoint(endpoint, serviceProvider);
        IPipe<SendContext<Probe>> adapter = scopedEndpoint.CreatePipe(callerPipe);
        var emptyContext = new MessageSendContext<Probe>(new Probe(), CancellationToken.None);
        var populatedContext = new MessageSendContext<Probe>(new Probe(), CancellationToken.None);
        populatedContext.GetOrAddPayload<IServiceProvider>(() => existingProvider);

        await ((ISendContextPipe)adapter).SendAsync(emptyContext, CancellationToken.None);
        Assert.Same(serviceProvider, emptyContext.GetPayload<IServiceProvider>());
        Assert.Null(observedProvider);
        await adapter.SendAsync(emptyContext);
        Assert.Same(serviceProvider, observedProvider);

        observedProvider = null;
        await ((ISendContextPipe)adapter).SendAsync(populatedContext, CancellationToken.None);
        await adapter.SendAsync(populatedContext);
        Assert.Same(existingProvider, populatedContext.GetPayload<IServiceProvider>());
        Assert.Same(existingProvider, observedProvider);
    }

    private sealed record Probe;

    private sealed class NullServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class ExposedScopedSendEndpoint(ISendEndpoint endpoint, IServiceProvider scope) :
        ScopedSendEndpoint(endpoint, scope)
    {
        public IPipe<SendContext<T>> CreatePipe<T>(IPipe<SendContext<T>>? pipe = null)
            where T : class => GetPipeProxy(pipe);
    }

    private sealed class PassiveSendEndpoint : ISendEndpoint
    {
        public Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
            where T : class => Task.CompletedTask;

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();
    }
}
