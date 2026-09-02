using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Clients;

public sealed class RequestClientMetadataTests
{
    private const string TraceHeader = "Client-Trace";
    private const string TraceValue = "request-7f11";

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-METADATA", "request-pipe-header-round-trip")]
    public async Task RequestPipeHeader_IsPresentOnTheRequestAndReturnedResponse()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var requestSeen = new TaskCompletionSource<ConsumeContext<MetadataRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Handler<MetadataRequest>(async context =>
        {
            requestSeen.TrySetResult(context);
            await context.RespondAsync(new MetadataResponse(context.Message.CorrelationId, "accepted"));
        });

        await harness.Start(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("f3b95cb8-b0be-4468-8b18-35d58000aa23");
            IRequestClient<MetadataRequest> client =
                harness.Bus.CreateRequestClient<MetadataRequest>(harness.InputQueueAddress, timeout);

            Response<MetadataResponse> response = await client.GetResponse<MetadataResponse>(
                new MetadataRequest(correlationId, false),
                configurator => configurator.UseExecute(context =>
                    context.Headers.Set(TraceHeader, TraceValue)),
                cancellationToken);
            ConsumeContext<MetadataRequest> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(TraceValue, request.Headers.Get<string>(TraceHeader));
            Assert.Equal(TraceValue, response.Headers.Get<string>(TraceHeader));
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Equal(new MetadataResponse(correlationId, "accepted"), response.Message);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-METADATA", "deadline-applies-only-to-request-outcomes")]
    public async Task RequestDeadline_IsInheritedByResponseButNotByIndependentConsumerWork()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var requestSeen = new TaskCompletionSource<ConsumeContext<MetadataRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var publishedSeen = new TaskCompletionSource<ConsumeContext<PublishedSideEffect>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var sentSeen = new TaskCompletionSource<ConsumeContext<SentSideEffect>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        Uri auditAddress = new(harness.BaseAddress, "request-client-audit");
        harness.Handler<MetadataRequest>(async context =>
        {
            requestSeen.TrySetResult(context);
            await context.Publish(new PublishedSideEffect(context.Message.CorrelationId), context.CancellationToken);
            ISendEndpoint auditEndpoint = await context.GetSendEndpoint(auditAddress);
            await auditEndpoint.Send(
                new SentSideEffect(context.Message.CorrelationId),
                context.CancellationToken);
            await context.RespondAsync(new MetadataResponse(context.Message.CorrelationId, "completed"));
        });
        harness.OnConfigureInMemoryBus += configurator =>
            configurator.ReceiveEndpoint("request-client-audit", endpoint =>
            {
                endpoint.Handler<PublishedSideEffect>(context =>
                {
                    publishedSeen.TrySetResult(context);
                    return Task.CompletedTask;
                });
                endpoint.Handler<SentSideEffect>(context =>
                {
                    sentSeen.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });

        await harness.Start(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("b7bdf9b1-2df2-43ce-a121-c58945e4fb36");
            IRequestClient<MetadataRequest> client =
                harness.Bus.CreateRequestClient<MetadataRequest>(harness.InputQueueAddress, RequestTimeout.After(m: 5));

            Response<MetadataResponse> response = await client.GetResponse<MetadataResponse>(
                new MetadataRequest(correlationId, false),
                cancellationToken);
            ConsumeContext<MetadataRequest> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<PublishedSideEffect> published =
                await publishedSeen.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<SentSideEffect> sent = await sentSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.NotNull(request.ExpirationTime);
            Assert.NotNull(response.ExpirationTime);
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Null(published.RequestId);
            Assert.Null(published.ExpirationTime);
            Assert.Null(sent.RequestId);
            Assert.Null(sent.ExpirationTime);
            Assert.Equal(correlationId, published.Message.CorrelationId);
            Assert.Equal(correlationId, sent.Message.CorrelationId);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-FAULT", "multi-response-complete-fault-envelope")]
    public async Task ConsumerFailure_FaultsAMultiResponseRequestWithTheCompleteTypedEnvelope()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var requestSeen = new TaskCompletionSource<ConsumeContext<MetadataRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Handler<MetadataRequest>(context =>
        {
            requestSeen.TrySetResult(context);
            throw new ExpectedRequestFailure("request rejected");
        });

        await harness.Start(cancellationToken);
        try
        {
            Task<ConsumeContext<Fault<MetadataRequest>>> faultSeen =
                harness.SubscribeHandler<Fault<MetadataRequest>>();
            var requestMessage = new MetadataRequest(
                Guid.Parse("6f56f140-cd81-4d10-b224-fd3bbb70d4df"),
                true);
            IRequestClient<MetadataRequest> client =
                harness.Bus.CreateRequestClient<MetadataRequest>(harness.InputQueueAddress, RequestTimeout.After(m: 5));

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.GetResponse<MetadataResponse, AlternateResponse>(requestMessage, cancellationToken));
            ConsumeContext<MetadataRequest> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<Fault<MetadataRequest>> publishedFault =
                await faultSeen.WaitAsync(timeout, cancellationToken);
            Fault<MetadataRequest> fault = Assert.IsAssignableFrom<Fault<MetadataRequest>>(exception.Fault);
            ExceptionInfo faultException = Assert.Single(fault.Exceptions);

            Assert.Equal(requestMessage, fault.Message);
            Assert.Equal(request.MessageId, fault.FaultedMessageId);
            Assert.Equal(request.RequestId, publishedFault.RequestId);
            Assert.NotNull(publishedFault.ExpirationTime);
            Assert.Equal(TypeCache<MetadataRequest>.ShortName, exception.RequestType);
            Assert.Equal(MessageTypeCache<MetadataRequest>.MessageTypeNames, fault.FaultMessageTypes);
            Assert.Equal(TypeCache<ExpectedRequestFailure>.ShortName, faultException.ExceptionType);
            Assert.Equal("request rejected", faultException.Message);
        }
        finally
        {
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout) =>
        new($"request-metadata-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record MetadataRequest(Guid CorrelationId, bool Fail) : CorrelatedBy<Guid>;

    private sealed record MetadataResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record AlternateResponse(Guid CorrelationId, string Value) : CorrelatedBy<Guid>;

    private sealed record PublishedSideEffect(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record SentSideEffect(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class ExpectedRequestFailure(string message) : Exception(message);
}
