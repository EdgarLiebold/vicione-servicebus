using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipRequestIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUEST", "successful-routing-slip-response")]
    public async Task SuccessfulRoutingSlip_ReturnsTheResponseForTheOriginalRequestAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-request-success");
        ExecuteActivityTestHarness<RequestSuccessActivity, RequestActivityArguments> activity = harness.ExecuteActivity<
            RequestSuccessActivity,
            RequestActivityArguments>();
        var requestProxy = new SuccessfulRequestProxy(() => activity.ExecuteAddress);
        var responseProxy = new SuccessfulResponseProxy();
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.Instance(requestProxy);
            endpoint.Instance(responseProxy);
        };
        await harness.StartAsync(cancellationToken);

        try
        {
            var request = new CourierRequest(NewId.NextGuid(), "request-value");
            IRequestClient<CourierRequest> client = harness.Bus.CreateRequestClient<CourierRequest>(
                harness.InputQueueAddress,
                new RequestTimeout(timeout));

            Response<CourierResponse> response = await client.Advanced().GetResponseAsync<CourierResponse>(
                request,
                cancellationToken: cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(request.DomainRequestId, response.Message.DomainRequestId);
            Assert.Equal("request-value-completed", response.Message.ActivityValue);
            Assert.NotEqual(Guid.Empty, response.Message.TrackingNumber);
            Assert.Equal(response.RequestId, response.Message.TransportRequestId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUEST", "ordinary-request-fault")]
    public async Task FaultedRoutingSlip_ProducesTheStandardFaultForTheOriginalRequestAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-request-fault");
        ExecuteActivityTestHarness<RequestFaultActivity, RequestActivityArguments> activity = harness.ExecuteActivity<
            RequestFaultActivity,
            RequestActivityArguments>();
        var requestProxy = new FaultingRequestProxy(() => activity.ExecuteAddress);
        var responseProxy = new StandardFaultResponseProxy();
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.Instance(requestProxy);
            endpoint.Instance(responseProxy);
        };
        await harness.StartAsync(cancellationToken);

        try
        {
            var request = new CourierRequest(NewId.NextGuid(), "ordinary-fault");
            IRequestClient<CourierRequest> client = harness.Bus.CreateRequestClient<CourierRequest>(
                harness.InputQueueAddress,
                new RequestTimeout(timeout));

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.Advanced().GetResponseAsync<CourierResponse>(request, cancellationToken: cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Fault<CourierRequest> fault = Assert.IsAssignableFrom<Fault<CourierRequest>>(exception.Fault);
            Assert.Equal(request, fault.Message);
            ExceptionInfo faultException = Assert.Single(fault.Exceptions);
            Assert.Equal(TypeCache<CourierRequestFailure>.ShortName, faultException.ExceptionType);
            Assert.Equal("ordinary-fault", faultException.Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUEST", "response-proxy-retry-count-is-forwarded-to-request-fault")]
    public async Task RetriedResponseProxy_ForwardsItsExactRetryCountToTheRequestFaultAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-request-retry-fault");
        ExecuteActivityTestHarness<RequestFaultActivity, RequestActivityArguments> activity = harness.ExecuteActivity<
            RequestFaultActivity,
            RequestActivityArguments>();
        var requestProxy = new FaultingRequestProxy(() => activity.ExecuteAddress);
        var responseProxy = new RetryingFaultResponseProxy();
        harness.OnConfigureInMemoryBus += bus => bus.ConfigureDelayedMessageScheduler();
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.Instance(requestProxy);
            endpoint.Instance(responseProxy);
        };
        await harness.StartAsync(cancellationToken);

        try
        {
            var request = new CourierRequest(NewId.NextGuid(), "retry-fault");
            IRequestClient<CourierRequest> client = harness.Bus.CreateRequestClient<CourierRequest>(
                harness.InputQueueAddress,
                new RequestTimeout(timeout));

            RequestFaultException exception = await Assert.ThrowsAsync<RequestFaultException>(() =>
                client.Advanced().GetResponseAsync<CourierResponse>(request, cancellationToken: cancellationToken));
            ISentMessage<Fault<CourierRequest>> sentFault = await harness.Sent
                .SelectAsync<Fault<CourierRequest>>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.True(sentFault.Context.TryGetHeader(MessageHeaders.FaultRetryCount, out int? retryCount));
            Assert.Equal(1, retryCount);
            Assert.Equal(request, Assert.IsAssignableFrom<Fault<CourierRequest>>(exception.Fault).Message);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-REQUEST", "declared-fault-response")]
    public async Task FaultedRoutingSlip_ReturnsTheDeclaredFaultResponseInsteadOfAStandardFaultAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-request-declared-fault");
        ExecuteActivityTestHarness<RequestFaultActivity, RequestActivityArguments> activity = harness.ExecuteActivity<
            RequestFaultActivity,
            RequestActivityArguments>();
        var requestProxy = new FaultingRequestProxy(() => activity.ExecuteAddress);
        var responseProxy = new DeclaredFaultResponseProxy();
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.Instance(requestProxy);
            endpoint.Instance(responseProxy);
        };
        await harness.StartAsync(cancellationToken);

        try
        {
            var request = new CourierRequest(NewId.NextGuid(), "declared-fault");
            IRequestClient<CourierRequest> client = harness.Bus.CreateRequestClient<CourierRequest>(
                harness.InputQueueAddress,
                new RequestTimeout(timeout));

            (Task<Response<CourierResponse>> responseTask, Task<Response<CourierFaultResponse>> faultTask) =
                await client.Advanced().GetResponseAsync<CourierResponse, CourierFaultResponse>(
                    request,
                    cancellationToken: cancellationToken);
            Response<CourierFaultResponse> response = await faultTask.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.False(responseTask.IsCompletedSuccessfully);
            Assert.Equal(request.DomainRequestId, response.Message.DomainRequestId);
            Assert.NotEqual(Guid.Empty, response.Message.TrackingNumber);
            Assert.Equal("declared-fault", response.Message.ExceptionMessage);
            Assert.Equal(response.RequestId, response.Message.TransportRequestId);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    public sealed record CourierRequest(Guid DomainRequestId, string Value);

    public sealed record CourierResponse(
        Guid DomainRequestId,
        Guid TransportRequestId,
        Guid TrackingNumber,
        string ActivityValue);

    public sealed record CourierFaultResponse(
        Guid DomainRequestId,
        Guid TransportRequestId,
        Guid TrackingNumber,
        string ExceptionMessage);

    public sealed record RequestActivityArguments(Guid RequestId, string Value);

    public sealed class RequestSuccessActivity : IExecuteActivity<RequestActivityArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RequestActivityArguments> context) =>
            Task.FromResult(context.CompletedWithVariables(new
            {
                ActivityValue = $"{context.Arguments.Value}-completed",
            }));
    }

    public sealed class RequestFaultActivity : IExecuteActivity<RequestActivityArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<RequestActivityArguments> context) =>
            Task.FromResult(context.Faulted(new CourierRequestFailure(context.Arguments.Value)));
    }

    public sealed class CourierRequestFailure(string message) : Exception(message);

    private sealed class SuccessfulRequestProxy(Func<Uri> activityAddress) : RoutingSlipRequestProxy<CourierRequest>
    {
        protected override Task BuildRoutingSlipAsync(RoutingSlipBuilder builder, ConsumeContext<CourierRequest> request)
        {
            builder.AddActivity(
                nameof(RequestSuccessActivity),
                activityAddress(),
                new RequestActivityArguments(request.Message.DomainRequestId, request.Message.Value));
            return Task.CompletedTask;
        }
    }

    private sealed class FaultingRequestProxy(Func<Uri> activityAddress) : RoutingSlipRequestProxy<CourierRequest>
    {
        protected override Task BuildRoutingSlipAsync(RoutingSlipBuilder builder, ConsumeContext<CourierRequest> request)
        {
            builder.AddActivity(
                nameof(RequestFaultActivity),
                activityAddress(),
                new RequestActivityArguments(request.Message.DomainRequestId, request.Message.Value));
            return Task.CompletedTask;
        }
    }

    private sealed class SuccessfulResponseProxy : RoutingSlipResponseProxy<CourierRequest, CourierResponse>
    {
        protected override Task<CourierResponse> CreateResponseMessageAsync(
            ConsumeContext<RoutingSlipCompleted> context,
            CourierRequest request) =>
            Task.FromResult(new CourierResponse(
                request.DomainRequestId,
                context.GetVariable<Guid>("RequestId")
                    ?? throw new InvalidDataException("The transport request identifier is missing."),
                context.Message.TrackingNumber,
                context.GetVariable<string>("ActivityValue")
                    ?? throw new InvalidDataException("The activity result variable is missing.")));
    }

    private sealed class StandardFaultResponseProxy : RoutingSlipResponseProxy<CourierRequest, CourierResponse>
    {
        protected override Task<CourierResponse> CreateResponseMessageAsync(
            ConsumeContext<RoutingSlipCompleted> context,
            CourierRequest request) =>
            throw new InvalidOperationException("The fault scenario must not create a success response.");
    }

    private sealed class RetryingFaultResponseProxy : RoutingSlipResponseProxy<CourierRequest, CourierResponse>
    {
        protected override IRetryPolicy RetryPolicy { get; } = Retry.Immediate(1);

        protected override Task<CourierResponse> CreateResponseMessageAsync(
            ConsumeContext<RoutingSlipCompleted> context,
            CourierRequest request) =>
            throw new InvalidOperationException("The fault scenario must not create a success response.");
    }

    private sealed class DeclaredFaultResponseProxy :
        RoutingSlipResponseProxy<CourierRequest, CourierResponse, CourierFaultResponse>
    {
        protected override Task<CourierResponse> CreateResponseMessageAsync(
            ConsumeContext<RoutingSlipCompleted> context,
            CourierRequest request) =>
            throw new InvalidOperationException("The declared fault scenario must not create a success response.");

        protected override Task<CourierFaultResponse> CreateFaultedResponseMessageAsync(
            ConsumeContext<RoutingSlipFaulted> context,
            CourierRequest request,
            Guid requestId)
        {
            ExceptionInfo exception = Assert.Single(context.Message.ActivityExceptions).ExceptionInfo;
            return Task.FromResult(new CourierFaultResponse(
                request.DomainRequestId,
                requestId,
                context.Message.TrackingNumber,
                exception.Message));
        }
    }
}
