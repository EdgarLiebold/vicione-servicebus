using System.Reflection;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Clients;
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
    [RequirementCoverage("REQ-VSB-APPLICATION-REQUEST-OPTIONS", "all-envelope-fields-and-request-identity-round-trip")]
    public async Task ApplicationRequestOptions_ReachTheRequestEnvelopeAndPreserveResponseCorrelationAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        var requestSeen = new TaskCompletionSource<ConsumeContext<MetadataRequest>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Handler<MetadataRequest>(async context =>
        {
            requestSeen.TrySetResult(context);
            await context.RespondAsync(new MetadataResponse(context.Message.CorrelationId, "options"));
        });

        await harness.StartAsync(cancellationToken);
        try
        {
            Guid requestId = Guid.Parse("11000000-0000-0000-0000-000000000011");
            Guid messageId = Guid.Parse("22000000-0000-0000-0000-000000000022");
            Guid correlationId = Guid.Parse("33000000-0000-0000-0000-000000000033");
            Guid conversationId = Guid.Parse("44000000-0000-0000-0000-000000000044");
            TimeSpan lifetime = TimeSpan.FromMinutes(19);
            IRequestClient<MetadataRequest> client =
                harness.Bus.CreateRequestClient<MetadataRequest>(harness.InputQueueAddress, timeout);
            var options = new RequestOptions
            {
                Headers = new Dictionary<string, object?>
                {
                    ["application"] = "orders",
                    ["attempt"] = 7,
                },
                TimeToLive = lifetime,
                CorrelationId = correlationId,
                ConversationId = conversationId,
                MessageId = messageId,
                RequestId = requestId,
                Deadline = TimeProvider.System.GetUtcNow().Add(timeout),
            };

            Response<MetadataResponse> response = await client.GetResponseAsync<MetadataResponse>(
                new MetadataRequest(Guid.NewGuid(), false),
                options,
                cancellationToken);
            ConsumeContext<MetadataRequest> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal("orders", request.Headers.Get<string>("application"));
            Assert.Equal(7, request.Headers.Get<int>("attempt"));
            TimeSpan observedLifetime = Assert.IsType<DateTimeOffset>(request.ExpirationTime)
                - Assert.IsType<DateTimeOffset>(request.SentTime);
            Assert.InRange(observedLifetime, lifetime, lifetime.Add(TimeSpan.FromSeconds(1)));
            Assert.Equal(correlationId, request.CorrelationId);
            Assert.Equal(conversationId, request.ConversationId);
            Assert.Equal(messageId, request.MessageId);
            Assert.Equal(requestId, request.RequestId);
            Assert.Equal(requestId, response.RequestId);
            Assert.Equal(new MetadataResponse(request.Message.CorrelationId, "options"), response.Message);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-REQUEST-OPTIONS", "deadline-uses-injected-clock-and-rejects-current-instant")]
    public async Task RequestDeadline_AtTheInjectedCurrentInstantFailsBeforeEndpointUseAsync()
    {
        DateTimeOffset now = new(2041, 2, 3, 4, 5, 6, TimeSpan.Zero);
        var clock = new FakeTimeProvider(now);
        ClientFactoryContext context = DispatchProxy.Create<ClientFactoryContext, RequestClientContextProxy>();
        ((RequestClientContextProxy)(object)context).Clock = clock;
        IRequestSendEndpoint<MetadataRequest> endpoint =
            DispatchProxy.Create<IRequestSendEndpoint<MetadataRequest>, UnusedRequestEndpointProxy>();
        IRequestClient<MetadataRequest> client = new RequestClient<MetadataRequest>(
            context,
            endpoint,
            RequestTimeout.After(m: 1));

        ArgumentOutOfRangeException exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            client.GetResponseAsync<MetadataResponse>(
                new MetadataRequest(Guid.NewGuid(), false),
                new RequestOptions { Deadline = now },
                TestContext.Current.CancellationToken));

        Assert.Equal("options", exception.ParamName);
        Assert.Equal(now, exception.ActualValue);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-REQUEST-OPTIONS", "partition-capability-failure-is-not-silent")]
    public async Task RequestPartitionKey_UnsupportedByTheTransportFailsExplicitlyAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout);
        await harness.StartAsync(cancellationToken);
        try
        {
            IRequestClient<MetadataRequest> client =
                harness.Bus.CreateRequestClient<MetadataRequest>(harness.InputQueueAddress, timeout);

            RequestException exception = await Assert.ThrowsAsync<RequestException>(() =>
                client.GetResponseAsync<MetadataResponse>(
                    new MetadataRequest(Guid.NewGuid(), false),
                    new RequestOptions { PartitionKey = "tenant-42" },
                    cancellationToken));

            NotSupportedException inner = Assert.IsType<NotSupportedException>(exception.InnerException);
            Assert.Contains("partition key", inner.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-METADATA", "request-pipe-header-round-trip")]
    public async Task RequestPipeHeader_IsPresentOnTheRequestAndReturnedResponseAsync()
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

        await harness.StartAsync(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("f3b95cb8-b0be-4468-8b18-35d58000aa23");
            IRequestClient<MetadataRequest> client =
                harness.Bus.CreateRequestClient<MetadataRequest>(harness.InputQueueAddress, timeout);

            Response<MetadataResponse> response = await client.Advanced().GetResponseAsync<MetadataResponse>(
                new MetadataRequest(correlationId, false),
                callback: configurator => configurator.UseExecute(context =>
                    context.Headers.Set(TraceHeader, TraceValue)),
                cancellationToken: cancellationToken);
            ConsumeContext<MetadataRequest> request = await requestSeen.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(TraceValue, request.Headers.Get<string>(TraceHeader));
            Assert.Equal(TraceValue, response.Headers.Get<string>(TraceHeader));
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Equal(new MetadataResponse(correlationId, "accepted"), response.Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-METADATA", "deadline-applies-only-to-request-outcomes")]
    public async Task RequestDeadline_IsInheritedByResponseButNotByIndependentConsumerWorkAsync()
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
            await context.Advanced().PublishAsync(new PublishedSideEffect(context.Message.CorrelationId), context.CancellationToken);
            ISendEndpoint auditEndpoint = await context.Advanced().GetSendEndpointAsync(auditAddress);
            await auditEndpoint.SendAsync(
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

        await harness.StartAsync(cancellationToken);
        try
        {
            Guid correlationId = Guid.Parse("b7bdf9b1-2df2-43ce-a121-c58945e4fb36");
            IRequestClient<MetadataRequest> client =
                harness.Bus.CreateRequestClient<MetadataRequest>(harness.InputQueueAddress, RequestTimeout.After(m: 5));

            Response<MetadataResponse> response = await client.Advanced().GetResponseAsync<MetadataResponse>(
                new MetadataRequest(correlationId, false),
                cancellationToken: cancellationToken);
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
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-FAULT", "multi-response-complete-fault-envelope")]
    public async Task ConsumerFailure_FaultsAMultiResponseRequestWithTheCompleteTypedEnvelopeAsync()
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

        await harness.StartAsync(cancellationToken);
        try
        {
            Task<ConsumeContext<Fault<MetadataRequest>>> faultSeen =
                harness.SubscribeHandlerAsync<Fault<MetadataRequest>>(TestContext.Current.CancellationToken);
            var requestMessage = new MetadataRequest(
                Guid.Parse("6f56f140-cd81-4d10-b224-fd3bbb70d4df"),
                true);
            IRequestClient<MetadataRequest> client =
                harness.Bus.CreateRequestClient<MetadataRequest>(harness.InputQueueAddress, RequestTimeout.After(m: 5));

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.Advanced().GetResponseAsync<MetadataResponse, AlternateResponse>(
                    requestMessage,
                    cancellationToken: cancellationToken));
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
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
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

    private class RequestClientContextProxy : DispatchProxy
    {
        public TimeProvider Clock { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            return targetMethod.Name == "get_TimeProvider"
                ? Clock
                : throw new NotSupportedException(targetMethod.Name);
        }
    }

    private class UnusedRequestEndpointProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The request endpoint must not be used for an expired deadline ({targetMethod?.Name}).");
    }
}
