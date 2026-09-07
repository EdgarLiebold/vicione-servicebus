using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Transports;

public sealed class PublishEndpointTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-ENDPOINT", "every-overload-validates-required-inputs")]
    public async Task PublishOperations_RejectEveryNullRequiredInputBeforeProviderUseAsync()
    {
        var endpoint = new PublishEndpoint(new RecordingProvider());
        var message = new PublishedMessage();

        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() => new PublishEndpoint(null!)).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync<PublishedMessage>(null!, CancellationToken.None))).ParamName);
        Assert.Equal("publishPipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync(message, (IPipe<PublishContext<PublishedMessage>>)null!, CancellationToken.None))).ParamName);
        Assert.Equal("publishPipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync(message, (IPipe<PublishContext>)null!, CancellationToken.None))).ParamName);
        Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync((object)null!, CancellationToken.None))).ParamName);
        Assert.Equal("publishPipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync((object)message, (IPipe<PublishContext>)null!, CancellationToken.None))).ParamName);
        Assert.Equal("messageType", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync(message, (Type)null!, CancellationToken.None))).ParamName);
        Assert.Equal("publishPipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync(message, typeof(PublishedMessage), (IPipe<PublishContext>)null!, CancellationToken.None))).ParamName);
        Assert.Equal("values", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync<PublishedMessage>((object)null!, CancellationToken.None))).ParamName);
        Assert.Equal("publishPipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync<PublishedMessage>(new { }, (IPipe<PublishContext<PublishedMessage>>)null!, CancellationToken.None))).ParamName);
        Assert.Equal("publishPipe", (await Assert.ThrowsAsync<ArgumentNullException>(() => endpoint.PublishAsync<PublishedMessage>(new { }, (IPipe<PublishContext>)null!, CancellationToken.None))).ParamName);
        Assert.Equal("observer", Assert.Throws<ArgumentNullException>(() => endpoint.ConnectPublishObserver(null!)).ParamName);

        var replaceableEndpoint = new ReplaceablePublishEndpoint(new RecordingProvider());
        Assert.Equal("provider", Assert.Throws<ArgumentNullException>(() => replaceableEndpoint.ReplaceProvider(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-ENDPOINT", "derived-contexts-can-replace-the-provider")]
    public void DerivedContext_CanReplaceThePublishProvider()
    {
        var initialProvider = new RecordingProvider();
        var replacementProvider = new RecordingProvider();
        var endpoint = new ReplaceablePublishEndpoint(initialProvider);

        endpoint.ReplaceProvider(replacementProvider);

        Assert.Same(replacementProvider, endpoint.CurrentProvider);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-ENDPOINT", "endpoint-resolution-receives-caller-cancellation")]
    public async Task Publish_ForwardsTheExactCancellationTokenToEndpointResolutionAsync()
    {
        var provider = new RecordingProvider();
        var endpoint = new PublishEndpoint(provider);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        OperationCanceledException typed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            endpoint.PublishAsync(new PublishedMessage(), cancellation.Token));
        OperationCanceledException initialized = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            endpoint.PublishAsync<PublishedMessage>(new { }, cancellation.Token));

        Assert.Equal(cancellation.Token, typed.CancellationToken);
        Assert.Equal(cancellation.Token, initialized.CancellationToken);
        Assert.Equal([cancellation.Token, cancellation.Token], provider.CancellationTokens);
    }

    private sealed record PublishedMessage;

    private sealed class ReplaceablePublishEndpoint(IPublishEndpointProvider provider) : PublishEndpoint(provider)
    {
        public IPublishEndpointProvider CurrentProvider => PublishEndpointProvider;

        public void ReplaceProvider(IPublishEndpointProvider provider) => SetPublishEndpointProvider(provider);
    }

    private sealed class RecordingProvider : IPublishEndpointProvider
    {
        public List<CancellationToken> CancellationTokens { get; } = [];

        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default)
            where T : class
        {
            CancellationTokens.Add(cancellationToken);
            return Task.FromCanceled<ISendEndpoint>(cancellationToken);
        }

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) =>
            throw new InvalidOperationException("Input validation must complete before provider use.");
    }
}
