using System.Collections.Concurrent;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class MediatorRequestApiContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-API", "direct-message-initializer-and-client-forms")]
    public async Task RequestApi_CompletesEveryDirectFormAsync()
    {
        TimeSpan timeoutValue = OperationTimeout();
        var timeout = new RequestTimeout(timeoutValue);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var consumed = new ConcurrentQueue<RequestSnapshot>();
        await using IMediator mediator = CreateMediator(consumed);
        var logicalAddress = new Uri("loopback://localhost/direct-request");
        var directResponses = new List<ApiResponse>
        {
            await GetResponseAsync(mediator.CreateRequest(NewRequest("message"), timeout, cancellationToken), timeoutValue, cancellationToken),
            await GetResponseAsync(mediator.CreateRequest(logicalAddress, NewRequest("message-address"), timeout, cancellationToken),
                timeoutValue, cancellationToken),
            await GetResponseAsync(mediator.CreateRequest<ApiRequest>(NewValues("values"), timeout, cancellationToken), timeoutValue,
                cancellationToken),
            await GetResponseAsync(mediator.CreateRequest<ApiRequest>(logicalAddress, NewValues("values-address"), timeout,
                cancellationToken), timeoutValue, cancellationToken),
        };
        IRequestClient<ApiRequest> routedClient = mediator.CreateRequestClient<ApiRequest>(timeout);
        IRequestClient<ApiRequest> addressedClient = mediator.CreateRequestClient<ApiRequest>(logicalAddress, timeout);
        directResponses.Add((await routedClient.GetResponseAsync<ApiResponse>(NewRequest("client"), cancellationToken)
            .WaitAsync(timeoutValue, cancellationToken)).Message);
        directResponses.Add((await addressedClient.GetResponseAsync<ApiResponse>(NewRequest("client-address"), cancellationToken)
            .WaitAsync(timeoutValue, cancellationToken)).Message);

        Assert.Equal(6, directResponses.Count);
        Assert.Equal(
            ["response:message", "response:message-address", "response:values", "response:values-address", "response:client", "response:client-address"],
            directResponses.Select(x => x.Value));
        Assert.Equal(logicalAddress, directResponses[1].DestinationAddress);
        Assert.Equal(logicalAddress, directResponses[3].DestinationAddress);
        Assert.Equal(logicalAddress, directResponses[5].DestinationAddress);
        Assert.Equal(6, consumed.Count);
        Assert.All(consumed, snapshot => Assert.Null(snapshot.InitiatorId));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-API", "contextual-message-initializer-and-client-forms")]
    public async Task RequestApi_CompletesEveryContextualFormAsync()
    {
        TimeSpan timeoutValue = OperationTimeout();
        var timeout = new RequestTimeout(timeoutValue);
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var consumed = new ConcurrentQueue<RequestSnapshot>();
        var nestedResponses = new ConcurrentQueue<ApiResponse>();
        IMediator mediator = null!;
        mediator = MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            ConfigureRequestHandler(configuration, consumed);
            configuration.Handler<ContextTrigger>(async context =>
            {
                ConsumeContext advancedContext = context.Advanced();
                var logicalAddress = new Uri("loopback://localhost/contextual-request");
                nestedResponses.Enqueue(await GetResponseAsync(
                    mediator.CreateRequest(advancedContext, NewRequest("context-message"), timeout, context.CancellationToken),
                    timeoutValue, context.CancellationToken));
                nestedResponses.Enqueue(await GetResponseAsync(
                    mediator.CreateRequest(advancedContext, logicalAddress, NewRequest("context-message-address"), timeout,
                        context.CancellationToken), timeoutValue, context.CancellationToken));
                nestedResponses.Enqueue(await GetResponseAsync(
                    mediator.CreateRequest<ApiRequest>(advancedContext, NewValues("context-values"), timeout, context.CancellationToken),
                    timeoutValue, context.CancellationToken));
                nestedResponses.Enqueue(await GetResponseAsync(
                    mediator.CreateRequest<ApiRequest>(advancedContext, logicalAddress, NewValues("context-values-address"), timeout,
                        context.CancellationToken), timeoutValue, context.CancellationToken));

                IRequestClient<ApiRequest> routedClient = mediator.CreateRequestClient<ApiRequest>(advancedContext, timeout);
                IRequestClient<ApiRequest> addressedClient = mediator.CreateRequestClient<ApiRequest>(advancedContext, logicalAddress, timeout);
                nestedResponses.Enqueue((await routedClient.GetResponseAsync<ApiResponse>(
                    NewRequest("context-client"), context.CancellationToken).WaitAsync(timeoutValue, context.CancellationToken)).Message);
                nestedResponses.Enqueue((await addressedClient.GetResponseAsync<ApiResponse>(
                    NewRequest("context-client-address"), context.CancellationToken).WaitAsync(timeoutValue, context.CancellationToken)).Message);

                await context.RespondAsync(new ContextResponse(context.Message.CorrelationId, nestedResponses.Count));
            });
        });
        await using IAsyncDisposable lifetime = mediator;
        var trigger = new ContextTrigger(NewId.NextGuid());

        Response<ContextResponse> contextResponse = await mediator.CreateRequestClient<ContextTrigger>(timeout)
            .GetResponseAsync<ContextResponse>(trigger, cancellationToken)
            .WaitAsync(timeoutValue, cancellationToken);

        Assert.Equal(6, contextResponse.Message.NestedResponseCount);
        Assert.Equal(
            [
                "response:context-message", "response:context-message-address", "response:context-values",
                "response:context-values-address", "response:context-client", "response:context-client-address",
            ],
            nestedResponses.Select(x => x.Value));
        Assert.Equal(6, consumed.Count);
        Assert.All(consumed, snapshot =>
        {
            Assert.Equal(trigger.CorrelationId, snapshot.InitiatorId);
            Assert.NotNull(snapshot.ConversationId);
        });
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MEDIATOR-REQUEST-API", "required-argument-boundaries")]
    public async Task RequestApi_RejectsEveryNullRequiredArgumentAtItsBoundaryAsync()
    {
        await using IMediator mediator = MediatorFactory.Create(configuration =>
            configuration.Limits(MessageLimits.Conservative));
        var message = NewRequest("valid");
        var address = new Uri("loopback://localhost/request");
        var timeout = new RequestTimeout(OperationTimeout());
        CancellationToken token = TestContext.Current.CancellationToken;

        AssertParameter("message", () => mediator.CreateRequest<ApiRequest>((ApiRequest)null!, timeout, token));
        AssertParameter("destinationAddress", () => mediator.CreateRequest((Uri)null!, message, timeout, token));
        AssertParameter("message", () => mediator.CreateRequest<ApiRequest>(address, (ApiRequest)null!, timeout, token));
        AssertParameter("values", () => mediator.CreateRequest<ApiRequest>((object)null!, timeout, token));
        AssertParameter("destinationAddress", () => mediator.CreateRequest<ApiRequest>((Uri)null!, NewValues("valid"), timeout, token));
        AssertParameter("values", () => mediator.CreateRequest<ApiRequest>(address, (object)null!, timeout, token));
        AssertParameter("destinationAddress", () => mediator.CreateRequestClient<ApiRequest>((Uri)null!, timeout));

        using RequestHandle<ApiRequest> request = mediator.CreateRequest(message, timeout, token);
        Task<Response<ApiResponse>> response = request.GetResponseAsync<ApiResponse>(cancellationToken: token);

        MessageNotConsumedException sendFailure = await Assert.ThrowsAsync<MessageNotConsumedException>(() =>
            request.Message.WaitAsync(OperationTimeout(), token));
        RequestException responseFailure = await Assert.ThrowsAsync<RequestException>(() =>
            response.WaitAsync(OperationTimeout(), token));

        Assert.Same(sendFailure, responseFailure.InnerException);
    }

    private static ApiRequest NewRequest(string value) => new() { CorrelationId = NewId.NextGuid(), Value = value };

    private static object NewValues(string value) => new { CorrelationId = NewId.NextGuid(), Value = value };

    private static IMediator CreateMediator(ConcurrentQueue<RequestSnapshot> consumed) =>
        MediatorFactory.Create(configuration =>
        {
            configuration.Limits(MessageLimits.Conservative);
            ConfigureRequestHandler(configuration, consumed);
        });

    private static void ConfigureRequestHandler(
        IReceiveEndpointConfigurator configuration,
        ConcurrentQueue<RequestSnapshot> consumed)
    {
        configuration.Handler<ApiRequest>(context =>
        {
            consumed.Enqueue(new RequestSnapshot(
                context.Message,
                context.DestinationAddress,
                context.InitiatorId,
                context.ConversationId));
            return context.RespondAsync(new ApiResponse(
                context.Message.CorrelationId,
                $"response:{context.Message.Value}",
                context.DestinationAddress));
        });
    }

    private static async Task<ApiResponse> GetResponseAsync(
        RequestHandle<ApiRequest> request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using (request)
        {
            Response<ApiResponse> response = await request.GetResponseAsync<ApiResponse>(cancellationToken: cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            return response.Message;
        }
    }

    private static void AssertParameter(string expected, Action action)
    {
        ArgumentNullException failure = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(expected, failure.ParamName);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions().OperationTimeout!.Value;

    private sealed class ApiRequest : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    private sealed record ApiResponse(Guid CorrelationId, string Value, Uri? DestinationAddress) : CorrelatedBy<Guid>;
    private sealed record ContextTrigger(Guid CorrelationId) : CorrelatedBy<Guid>;
    private sealed record ContextResponse(Guid CorrelationId, int NestedResponseCount) : CorrelatedBy<Guid>;
    private sealed record RequestSnapshot(ApiRequest Message, Uri? DestinationAddress, Guid? InitiatorId, Guid? ConversationId);
}
