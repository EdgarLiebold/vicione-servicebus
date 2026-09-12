using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Mediator;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Mediator;

public sealed class ScopedMediatorContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-MEDIATOR", "all-publish-forms-preserve-di-scope")]
    public async Task PublishApi_DispatchesEveryFormInsideTheCallingDependencyInjectionScopeAsync()
    {
        var observations = new ConcurrentQueue<ScopedObservation>();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observations)
            .AddScoped<ScopeMarker>()
            .AddMediator(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddConsumer<ScopedMessageConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        ScopeMarker marker = scope.ServiceProvider.GetRequiredService<ScopeMarker>();
        IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
        IAdvancedPublishEndpoint publish = ((IPublishEndpoint)mediator).Advanced();
        CancellationToken token = TestContext.Current.CancellationToken;
        var pipes = 0;
        var messages = Enumerable.Range(0, 8)
            .Select(index => new ScopedMessage { Value = $"message-{index}" })
            .ToArray();

        await mediator.PublishAsync(messages[0], new PublishOptions { CorrelationId = NewId.NextGuid() }, token);
        await ((IPublishEndpoint)publish).PublishAsync(messages[1], token);
        await publish.PublishAsync(messages[2], new RecordingPipe<PublishContext<ScopedMessage>>(_ =>
            Interlocked.Increment(ref pipes)), token);
        await publish.PublishAsync(messages[3], new RecordingPipe<PublishContext>(_ =>
            Interlocked.Increment(ref pipes)), token);
        await publish.PublishAsync((object)messages[4], token);
        await publish.PublishAsync(messages[5], new RecordingPipe<PublishContext>(_ =>
            Interlocked.Increment(ref pipes)), token);
        await publish.PublishAsync(messages[6], typeof(ScopedMessage), token);
        await publish.PublishAsync(messages[7], typeof(ScopedMessage), new RecordingPipe<PublishContext>(_ =>
            Interlocked.Increment(ref pipes)), token);
        await publish.PublishAsync<ScopedMessage>(new { Value = "values" }, token);
        await publish.PublishAsync<ScopedMessage>(new { Value = "values-typed-pipe" },
            new RecordingPipe<PublishContext<ScopedMessage>>(_ => Interlocked.Increment(ref pipes)), token);
        await publish.PublishAsync<ScopedMessage>(new { Value = "values-untyped-pipe" },
            new RecordingPipe<PublishContext>(_ => Interlocked.Increment(ref pipes)), token);

        ScopedObservation[] recorded = observations.ToArray();
        Assert.Equal(11, recorded.Length);
        Assert.Equal(
            [
                "message-0", "message-1", "message-2", "message-3", "message-4", "message-5", "message-6", "message-7",
                "values", "values-typed-pipe", "values-untyped-pipe",
            ],
            recorded.Select(x => x.Value));
        Assert.All(recorded, observation => Assert.Same(marker, observation.Marker));
        Assert.Equal(6, Volatile.Read(ref pipes));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-MEDIATOR", "thread-safe-client-factory-and-runtime-connectors")]
    public async Task ContextAndConnectors_AreStableUnderConcurrentScopedAccessAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddMediator(configuration => configuration.Limits(MessageLimits.Conservative))
            .BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();

        ClientFactoryContext[] contexts = await Task.WhenAll(Enumerable.Range(0, 64)
            .Select(_ => Task.Run(() => mediator.Context, TestContext.Current.CancellationToken)));
        ISendEndpoint publishEndpoint = await mediator.GetPublishSendEndpointAsync<ScopedMessage>(
            TestContext.Current.CancellationToken);
        using ConnectHandle publishHandle = mediator.ConnectPublishObserver(new EmptyPublishObserver());
        using ConnectHandle consumeHandle = mediator.ConnectConsumeObserver(new EmptyConsumeObserver());
        using ConnectHandle messageHandle = mediator.ConnectConsumeMessageObserver(new EmptyConsumeMessageObserver<ScopedMessage>());
        using ConnectHandle pipeHandle = mediator.ConnectConsumePipe(Pipe.Empty<ConsumeContext<ScopedMessage>>());

        Assert.All(contexts, context => Assert.Same(contexts[0], context));
        Assert.Equal(new Uri("loopback://localhost/response"), contexts[0].ResponseAddress);
        Assert.NotNull(publishEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-MEDIATOR", "request-message-initializer-and-client-forms")]
    public async Task RequestApi_CompletesEveryNonContextualFormInsideTheScopeAsync()
    {
        await using ServiceProvider provider = new ServiceCollection()
            .AddMediator(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddHandler<ScopedRequest, ScopedResponse>(context => Task.FromResult(
                    new ScopedResponse(context.Message.CorrelationId, $"response:{context.Message.Value}", context.DestinationAddress)));
            })
            .BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();
        TimeSpan timeoutValue = TimeSpan.FromSeconds(10);
        var timeout = new RequestTimeout(timeoutValue);
        CancellationToken token = TestContext.Current.CancellationToken;
        var address = new Uri("loopback://localhost/scoped-request");
        var responses = new List<ScopedResponse>
        {
            await GetResponseAsync(mediator.CreateRequest(NewRequest("message"), timeout, token), timeoutValue, token),
            await GetResponseAsync(mediator.CreateRequest(address, NewRequest("message-address"), timeout, token), timeoutValue, token),
            await GetResponseAsync(mediator.CreateRequest<ScopedRequest>(NewValues("values"), timeout, token), timeoutValue, token),
            await GetResponseAsync(mediator.CreateRequest<ScopedRequest>(address, NewValues("values-address"), timeout, token),
                timeoutValue, token),
        };
        IRequestClient<ScopedRequest> routedClient = mediator.CreateRequestClient<ScopedRequest>(timeout);
        IRequestClient<ScopedRequest> addressedClient = mediator.CreateRequestClient<ScopedRequest>(address, timeout);
        responses.Add((await routedClient.GetResponseAsync<ScopedResponse>(NewRequest("client"), token)
            .WaitAsync(timeoutValue, token)).Message);
        responses.Add((await addressedClient.GetResponseAsync<ScopedResponse>(NewRequest("client-address"), token)
            .WaitAsync(timeoutValue, token)).Message);

        Assert.Equal(
            ["response:message", "response:message-address", "response:values", "response:values-address", "response:client", "response:client-address"],
            responses.Select(x => x.Value));
        Assert.Equal(address, responses[1].DestinationAddress);
        Assert.Equal(address, responses[3].DestinationAddress);
        Assert.Equal(address, responses[5].DestinationAddress);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCOPED-MEDIATOR", "all-consume-context-request-forms-preserve-di-scope")]
    public async Task ContextualRequestApi_CompletesEveryFormInsideTheConsumingScopeAsync()
    {
        var observation = new TaskCompletionSource<ScopedContextResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddScoped<ScopeMarker>()
            .AddMediator(configuration =>
            {
                configuration.Limits(MessageLimits.Conservative);
                configuration.AddConsumer<ScopedContextCoordinator>();
                configuration.AddConsumer<ScopedContextRequestConsumer>();
            })
            .BuildServiceProvider(validateScopes: true);
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        ScopeMarker marker = scope.ServiceProvider.GetRequiredService<ScopeMarker>();
        IScopedMediator mediator = scope.ServiceProvider.GetRequiredService<IScopedMediator>();

        await mediator.SendAsync(new ScopedContextTrigger(), TestContext.Current.CancellationToken);
        ScopedContextResult result = await observation.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            ["message", "message-address", "values", "values-address", "client", "client-address"],
            result.Responses.Select(response => response.Value));
        Assert.All(result.Responses, response => Assert.Equal(marker.Id, response.ScopeId));
        var routedAddress = new Uri("loopback://localhost/mediator");
        Assert.Equal(routedAddress, result.Responses[0].DestinationAddress);
        Assert.Equal(result.Address, result.Responses[1].DestinationAddress);
        Assert.Equal(routedAddress, result.Responses[2].DestinationAddress);
        Assert.Equal(result.Address, result.Responses[3].DestinationAddress);
        Assert.Equal(routedAddress, result.Responses[4].DestinationAddress);
        Assert.Equal(result.Address, result.Responses[5].DestinationAddress);
    }

    private static ScopedRequest NewRequest(string value) => new() { CorrelationId = NewId.NextGuid(), Value = value };

    private static object NewValues(string value) => new { CorrelationId = NewId.NextGuid(), Value = value };

    private static async Task<ScopedResponse> GetResponseAsync(
        RequestHandle<ScopedRequest> request,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        using (request)
        {
            Response<ScopedResponse> response = await request.GetResponseAsync<ScopedResponse>(cancellationToken: cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            return response.Message;
        }
    }

    private sealed class ScopeMarker
    {
        public Guid Id { get; } = NewId.NextGuid();
    }

    private sealed class ScopedContextCoordinator(
        IScopedMediator mediator,
        TaskCompletionSource<ScopedContextResult> observation) : IConsumer<ScopedContextTrigger>
    {
        public async Task ConsumeAsync(ConsumeContext<ScopedContextTrigger> context)
        {
            TimeSpan timeoutValue = TimeSpan.FromSeconds(10);
            var timeout = new RequestTimeout(timeoutValue);
            var address = new Uri("loopback://localhost/scoped-context-request");
            ConsumeContext infrastructureContext = context.Advanced();
            CancellationToken token = context.CancellationToken;
            var responses = new List<ScopedContextResponse>
            {
                await GetResponseAsync(
                    mediator.CreateRequest(infrastructureContext, NewContextRequest("message"), timeout, token),
                    timeoutValue,
                    token),
                await GetResponseAsync(
                    mediator.CreateRequest(infrastructureContext, address, NewContextRequest("message-address"), timeout, token),
                    timeoutValue,
                    token),
                await GetResponseAsync(
                    mediator.CreateRequest<ScopedContextRequest>(infrastructureContext, NewContextValues("values"), timeout, token),
                    timeoutValue,
                    token),
                await GetResponseAsync(
                    mediator.CreateRequest<ScopedContextRequest>(infrastructureContext, address, NewContextValues("values-address"), timeout, token),
                    timeoutValue,
                    token),
            };
            IRequestClient<ScopedContextRequest> routedClient = mediator.CreateRequestClient<ScopedContextRequest>(infrastructureContext, timeout);
            IRequestClient<ScopedContextRequest> addressedClient = mediator.CreateRequestClient<ScopedContextRequest>(
                infrastructureContext,
                address,
                timeout);
            responses.Add((await routedClient.GetResponseAsync<ScopedContextResponse>(NewContextRequest("client"), token)
                .WaitAsync(timeoutValue, token)).Message);
            responses.Add((await addressedClient.GetResponseAsync<ScopedContextResponse>(NewContextRequest("client-address"), token)
                .WaitAsync(timeoutValue, token)).Message);

            observation.TrySetResult(new ScopedContextResult(address, responses));
        }

        static ScopedContextRequest NewContextRequest(string value) => new() { Value = value };

        static object NewContextValues(string value) => new { Value = value };

        static async Task<ScopedContextResponse> GetResponseAsync(
            RequestHandle<ScopedContextRequest> request,
            TimeSpan timeout,
            CancellationToken cancellationToken)
        {
            using (request)
            {
                Response<ScopedContextResponse> response = await request.GetResponseAsync<ScopedContextResponse>(
                        cancellationToken: cancellationToken)
                    .WaitAsync(timeout, cancellationToken);
                return response.Message;
            }
        }
    }

    private sealed class ScopedContextRequestConsumer(ScopeMarker marker) : IConsumer<ScopedContextRequest>
    {
        public Task ConsumeAsync(ConsumeContext<ScopedContextRequest> context) =>
            context.RespondAsync(new ScopedContextResponse(
                context.Message.Value,
                marker.Id,
                context.DestinationAddress));
    }

    private sealed class ScopedMessageConsumer(
        ScopeMarker marker,
        ConcurrentQueue<ScopedObservation> observations) : IConsumer<ScopedMessage>
    {
        public Task ConsumeAsync(ConsumeContext<ScopedMessage> context)
        {
            observations.Enqueue(new ScopedObservation(context.Message.Value, marker));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPipe<TContext>(Action<TContext> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context)
        {
            callback(context);
            return Task.CompletedTask;
        }

        public void Probe(ProbeContext context) => context.CreateScope("recordingPipe");
    }

    private sealed class EmptyPublishObserver : IPublishObserver
    {
        public Task PrePublishAsync<T>(PublishContext<T> context) where T : class => Task.CompletedTask;
        public Task PostPublishAsync<T>(PublishContext<T> context) where T : class => Task.CompletedTask;
        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    private sealed class EmptyConsumeObserver : IConsumeObserver
    {
        public Task PreConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;
        public Task PostConsumeAsync<T>(ConsumeContext<T> context) where T : class => Task.CompletedTask;
        public Task ConsumeFaultAsync<T>(ConsumeContext<T> context, Exception exception) where T : class => Task.CompletedTask;
    }

    private sealed class EmptyConsumeMessageObserver<TMessage> : IConsumeMessageObserver<TMessage>
        where TMessage : class
    {
        public Task PreConsumeAsync(ConsumeContext<TMessage> context) => Task.CompletedTask;
        public Task PostConsumeAsync(ConsumeContext<TMessage> context) => Task.CompletedTask;
        public Task ConsumeFaultAsync(ConsumeContext<TMessage> context, Exception exception) => Task.CompletedTask;
    }

    private sealed class ScopedMessage
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed class ScopedRequest : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
        public string Value { get; set; } = string.Empty;
    }

    private sealed record ScopedResponse(Guid CorrelationId, string Value, Uri? DestinationAddress) : CorrelatedBy<Guid>;
    private sealed record ScopedObservation(string Value, ScopeMarker Marker);
    private sealed record ScopedContextTrigger;
    private sealed class ScopedContextRequest
    {
        public string Value { get; set; } = string.Empty;
    }

    private sealed record ScopedContextResponse(string Value, Guid ScopeId, Uri? DestinationAddress);
    private sealed record ScopedContextResult(Uri Address, IReadOnlyList<ScopedContextResponse> Responses);
}
