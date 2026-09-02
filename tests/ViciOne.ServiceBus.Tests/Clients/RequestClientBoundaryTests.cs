using ViciOne.ServiceBus.Clients;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestClientBoundaryTests
{
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
                _ = client.Create((BoundaryRequest)null!);
                break;
            case EntryPoint.CreateValues:
                _ = client.Create((object)null!);
                break;
            case EntryPoint.SingleTyped:
                _ = client.GetResponse<BoundaryResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.SingleValues:
                _ = client.GetResponse<BoundaryResponse>((object)null!);
                break;
            case EntryPoint.DoubleTyped:
                _ = client.GetResponse<BoundaryResponse, AlternateResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.DoubleValues:
                _ = client.GetResponse<BoundaryResponse, AlternateResponse>((object)null!);
                break;
            case EntryPoint.TripleTyped:
                _ = client.GetResponse<BoundaryResponse, AlternateResponse, ThirdResponse>((BoundaryRequest)null!);
                break;
            case EntryPoint.TripleValues:
                _ = client.GetResponse<BoundaryResponse, AlternateResponse, ThirdResponse>((object)null!);
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

        public Task<BoundaryRequest> Send(
            Guid requestId,
            object values,
            IPipe<SendContext<BoundaryRequest>> pipe,
            CancellationToken cancellationToken)
        {
            SendCount++;
            throw new InvalidOperationException("A rejected input reached the send endpoint.");
        }

        public Task Send(
            Guid requestId,
            BoundaryRequest message,
            IPipe<SendContext<BoundaryRequest>> pipe,
            CancellationToken cancellationToken)
        {
            SendCount++;
            throw new InvalidOperationException("A rejected input reached the send endpoint.");
        }
    }

    private sealed class BoundaryClientFactoryContext : ClientFactoryContext
    {
        public RequestTimeout DefaultTimeout => RequestTimeout.Default;

        public TimeProvider TimeProvider => TimeProvider.System;

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
