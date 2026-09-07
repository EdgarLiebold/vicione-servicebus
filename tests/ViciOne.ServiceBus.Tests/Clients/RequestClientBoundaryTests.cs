using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Introspection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestClientBoundaryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "factory-constructor-dependencies")]
    public void ClientFactories_RejectMissingConstructorDependenciesImmediately()
    {
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => new ClientFactory(null!)).ParamName);
        Assert.Equal("clientFactory", Assert.Throws<ArgumentNullException>(() => new ScopedClientFactory(null!, null)).ParamName);
        Assert.Equal("bus", Assert.Throws<ArgumentNullException>(() => new BusClientFactoryContext(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-DIAGNOSTICS", "request-identity-and-contract")]
    public void RequestHandleProbe_ReportsItsIdentityAndRequestContract()
    {
        var context = new BoundaryClientFactoryContext();
        ClientRequestHandle<BoundaryRequest>.SendRequestCallback callback = async (_, _, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new BoundaryRequest("unused");
        };
        using var handle = new ClientRequestHandle<BoundaryRequest>(context, callback);

        IProbeResult result = handle.GetProbeResult(TestContext.Current.CancellationToken);

        IReadOnlyDictionary<string, object> filter = Assert.IsAssignableFrom<IReadOnlyDictionary<string, object>>(
            Assert.Contains("filters", result.Results));
        Assert.Equal("request", Assert.Contains("filterType", filter));
        Assert.Equal(handle.RequestId, Assert.Contains("requestId", filter));
        Assert.Equal(TypeCache<BoundaryRequest>.ShortName, Assert.Contains("requestType", filter));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "response-registration-closes-before-send")]
    public void RequestHandle_RejectsResponseRegistrationAfterSendIsReleased()
    {
        var context = new BoundaryClientFactoryContext();
        ClientRequestHandle<BoundaryRequest>.SendRequestCallback callback = async (_, _, cancellationToken) =>
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return new BoundaryRequest("unused");
        };
        using var handle = new ClientRequestHandle<BoundaryRequest>(
            context,
            callback,
            TestContext.Current.CancellationToken);

        _ = handle.GetResponseAsync<BoundaryResponse>(
            readyToSend: true,
            TestContext.Current.CancellationToken);

        void RegisterAnotherResponse() => _ = handle.GetResponseAsync<AlternateResponse>(
            readyToSend: false,
            TestContext.Current.CancellationToken);

        RequestException exception = Assert.Throws<RequestException>(RegisterAnotherResponse);
        Assert.Contains("cannot be registered", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(EntryPoint.CreateTyped, "message")]
    [InlineData(EntryPoint.CreateValues, "values")]
    [InlineData(EntryPoint.SingleTyped, "message")]
    [InlineData(EntryPoint.SingleValues, "values")]
    [InlineData(EntryPoint.DoubleTyped, "message")]
    [InlineData(EntryPoint.DoubleValues, "values")]
    [InlineData(EntryPoint.TripleTyped, "message")]
    [InlineData(EntryPoint.TripleValues, "values")]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "required-message-inputs")]
    public void EveryRequestEntryPoint_RejectsItsMissingMessageBeforeSending(
        EntryPoint entryPoint,
        string expectedParameter)
    {
        var endpoint = new RejectUnexpectedSendEndpoint();
        var client = new RequestClient<BoundaryRequest>(
            new BoundaryClientFactoryContext(),
            endpoint,
            RequestTimeout.After(s: 1));

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => Invoke(entryPoint, client));

        Assert.Equal(expectedParameter, exception.ParamName);
        Assert.Equal(0, endpoint.SendCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-CLIENT-BOUNDARY", "required-constructor-dependencies")]
    public void RequestClientAndHandle_RejectMissingConstructorDependenciesImmediately()
    {
        var context = new BoundaryClientFactoryContext();
        var endpoint = new RejectUnexpectedSendEndpoint();
        ClientRequestHandle<BoundaryRequest>.SendRequestCallback callback =
            (_, _, _) => Task.FromResult(new BoundaryRequest("unused"));

        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new RequestClient<BoundaryRequest>(null!, endpoint, RequestTimeout.Default)).ParamName);
        Assert.Equal("requestSendEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new RequestClient<BoundaryRequest>(context, null!, RequestTimeout.Default)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() =>
            new ClientRequestHandle<BoundaryRequest>(null!, callback)).ParamName);
        Assert.Equal("sendRequestCallback", Assert.Throws<ArgumentNullException>(() =>
            new ClientRequestHandle<BoundaryRequest>(context, null!)).ParamName);
    }

    private static void Invoke(EntryPoint entryPoint, IRequestClient<BoundaryRequest> client)
    {
        switch (entryPoint)
        {
            case EntryPoint.CreateTyped:
                _ = client.Advanced().Create((BoundaryRequest)null!);
                break;
            case EntryPoint.CreateValues:
                _ = client.Advanced().Create((object)null!);
                break;
            case EntryPoint.SingleTyped:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.SingleValues:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse>((object)null!);
                break;
            case EntryPoint.DoubleTyped:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse, AlternateResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.DoubleValues:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse, AlternateResponse>((object)null!);
                break;
            case EntryPoint.TripleTyped:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse, AlternateResponse, ThirdResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.TripleValues:
                _ = client.Advanced().GetResponseAsync<BoundaryResponse, AlternateResponse, ThirdResponse>((object)null!);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(entryPoint), entryPoint, null);
        }
    }

    public enum EntryPoint
    {
        CreateTyped,
        CreateValues,
        SingleTyped,
        SingleValues,
        DoubleTyped,
        DoubleValues,
        TripleTyped,
        TripleValues,
    }

    private sealed record BoundaryRequest(string Value);

    private sealed record BoundaryResponse(string Value);

    private sealed record AlternateResponse(string Value);

    private sealed record ThirdResponse(string Value);

    private sealed class RejectUnexpectedSendEndpoint : IRequestSendEndpoint<BoundaryRequest>
    {
        public int SendCount { get; private set; }

        public Task<BoundaryRequest> SendAsync(
            Guid requestId,
            object values,
            IPipe<SendContext<BoundaryRequest>> pipe,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<BoundaryRequest>(cancellationToken);

            SendCount++;
            throw new InvalidOperationException("A rejected input reached the send endpoint.");
        }

        public Task SendAsync(
            Guid requestId,
            BoundaryRequest message,
            IPipe<SendContext<BoundaryRequest>> pipe,
            CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            SendCount++;
            throw new InvalidOperationException("A rejected input reached the send endpoint.");
        }
    }

    private sealed class BoundaryClientFactoryContext : ClientFactoryContext
    {
        public RequestTimeout DefaultTimeout => RequestTimeout.Default;

        public TimeProvider TimeProvider => TimeProvider.System;

        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();

        public Uri ResponseAddress { get; } = new("loopback://localhost/response");

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();

        public IRequestSendEndpoint<T> GetRequestEndpoint<T>(Uri destinationAddress, ConsumeContext? consumeContext = default)
            where T : class => throw new NotSupportedException();
    }
}
