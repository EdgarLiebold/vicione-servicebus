using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.DependencyInjection;

public sealed class HandlerRegistrationTests
{
    [Theory]
    [InlineData(MessageHandlerShape.Message)]
    [InlineData(MessageHandlerShape.Context)]
    [InlineData(MessageHandlerShape.MessageAndOneDependency)]
    [InlineData(MessageHandlerShape.ContextAndOneDependency)]
    [InlineData(MessageHandlerShape.MessageAndTwoDependencies)]
    [InlineData(MessageHandlerShape.ContextAndTwoDependencies)]
    [InlineData(MessageHandlerShape.MessageAndThreeDependencies)]
    [InlineData(MessageHandlerShape.ContextAndThreeDependencies)]
    [RequirementCoverage("REQ-VSB-DI-HANDLER", "message-and-context-overload-matrix")]
    public async Task MessageHandlers_InvokeTheExactOverloadWithAllScopedDependenciesAsync(
        MessageHandlerShape shape)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid messageId = NewId.NextGuid();
        var probe = new HandlerProbe(messageId);
        await using ServiceProvider provider = CreateServices(probe, configuration =>
            RegisterMessageHandler(configuration, shape, probe));
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            await harness.Bus.PublishAsync(new HandlerMessage(messageId), cancellationToken);
            HandlerInvocation invocation = await probe.Completed.WaitAsync(timeout, cancellationToken);

            Assert.Equal(messageId, invocation.MessageId);
            Assert.Equal(shape.ToString(), invocation.Shape);
            AssertDependencies(shape, invocation.DependencyIds);
            Assert.Equal(ShapeUsesContext(shape), invocation.TransportMessageId.HasValue);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        IReceivedMessage<HandlerMessage> consumed = Assert.Single(
            harness.Consumed.Select<HandlerMessage>(SnapshotOnlyToken()));
        Assert.Equal(messageId, consumed.Context.Message.MessageId);
        Assert.Null(consumed.Exception);
        Assert.Equal(1, probe.Count);
    }

    [Theory]
    [InlineData(RequestHandlerShape.Message)]
    [InlineData(RequestHandlerShape.Context)]
    [InlineData(RequestHandlerShape.MessageAndOneDependency)]
    [InlineData(RequestHandlerShape.ContextAndOneDependency)]
    [InlineData(RequestHandlerShape.MessageAndTwoDependencies)]
    [InlineData(RequestHandlerShape.ContextAndTwoDependencies)]
    [InlineData(RequestHandlerShape.MessageAndThreeDependencies)]
    [InlineData(RequestHandlerShape.ContextAndThreeDependencies)]
    [InlineData(RequestHandlerShape.MessageWithCustomEndpoint)]
    [RequirementCoverage("REQ-VSB-DI-HANDLER", "request-response-and-custom-endpoint-overload-matrix")]
    public async Task RequestHandlers_ReturnTheExactResponseFromTheConfiguredEndpointAsync(
        RequestHandlerShape shape)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        Guid messageId = NewId.NextGuid();
        var probe = new HandlerProbe(messageId);
        await using ServiceProvider provider = CreateServices(probe, configuration =>
            RegisterRequestHandler(configuration, shape, probe));
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        HandlerResponse response;
        Uri? sourceAddress;
        Guid? requestId;
        try
        {
            IRequestClient<HandlerRequest> client = harness.GetRequestClient<HandlerRequest>();
            Response<HandlerResponse> result = await client.GetResponseAsync<HandlerResponse>(
                new HandlerRequest(messageId),
                cancellationToken);
            HandlerInvocation invocation = await probe.Completed.WaitAsync(timeout, cancellationToken);
            response = result.Message;
            sourceAddress = result.SourceAddress;
            requestId = result.RequestId;

            Assert.Equal(messageId, invocation.MessageId);
            Assert.Equal(shape.ToString(), invocation.Shape);
            AssertDependencies(shape, invocation.DependencyIds);
            Assert.Equal(ShapeUsesContext(shape), invocation.TransportMessageId.HasValue);
            Assert.Equal(ShapeUsesContext(shape) ? result.RequestId : null, invocation.RequestId);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(new HandlerResponse(messageId, shape.ToString()), response);
        Assert.Equal(
            shape == RequestHandlerShape.MessageWithCustomEndpoint
                ? "native-handler-custom"
                : nameof(HandlerRequest),
            sourceAddress!.GetEndpointName());
        IReceivedMessage<HandlerRequest> consumed = Assert.Single(
            harness.Consumed.Select<HandlerRequest>(SnapshotOnlyToken()));
        ISentMessage<HandlerResponse> sent = Assert.Single(
            harness.Sent.Select<HandlerResponse>(SnapshotOnlyToken()));
        Assert.Equal(response, sent.Context.Message);
        Assert.Equal(requestId, consumed.Context.RequestId);
        Assert.Equal(requestId, sent.Context.RequestId);
        Assert.Equal(1, probe.Count);
    }

    private static ServiceProvider CreateServices(
        HandlerProbe probe,
        Action<IBusRegistrationConfigurator> configure) =>
        new ServiceCollection()
            .AddSingleton(probe)
            .AddScoped<ScopedDependency1>()
            .AddScoped<ScopedDependency2>()
            .AddScoped<ScopedDependency3>()
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(OperationTimeout(), OperationTimeout());
                configure(configuration);
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

    private static void RegisterMessageHandler(
        IBusRegistrationConfigurator configuration,
        MessageHandlerShape shape,
        HandlerProbe probe)
    {
        switch (shape)
        {
            case MessageHandlerShape.Message:
                configuration.AddHandler<HandlerMessage>(message =>
                    probe.RecordAsync(shape, message.MessageId));
                break;
            case MessageHandlerShape.Context:
                configuration.AddHandler<HandlerMessage>((ConsumeContext<HandlerMessage> context) =>
                    probe.RecordAsync(shape, context.Message.MessageId, context.Advanced()));
                break;
            case MessageHandlerShape.MessageAndOneDependency:
                configuration.AddHandler<HandlerMessage, ScopedDependency1>((message, first) =>
                    probe.RecordAsync(shape, message.MessageId, dependencies: [first.Id]));
                break;
            case MessageHandlerShape.ContextAndOneDependency:
                configuration.AddHandler<HandlerMessage, ScopedDependency1>((ConsumeContext<HandlerMessage> context, ScopedDependency1 first) =>
                    probe.RecordAsync(shape, context.Message.MessageId, context.Advanced(), first.Id));
                break;
            case MessageHandlerShape.MessageAndTwoDependencies:
                configuration.AddHandler<HandlerMessage, ScopedDependency1, ScopedDependency2>((message, first, second) =>
                    probe.RecordAsync(shape, message.MessageId, dependencies: [first.Id, second.Id]));
                break;
            case MessageHandlerShape.ContextAndTwoDependencies:
                configuration.AddHandler<HandlerMessage, ScopedDependency1, ScopedDependency2>(
                    (ConsumeContext<HandlerMessage> context, ScopedDependency1 first, ScopedDependency2 second) =>
                        probe.RecordAsync(shape, context.Message.MessageId, context.Advanced(), first.Id, second.Id));
                break;
            case MessageHandlerShape.MessageAndThreeDependencies:
                configuration.AddHandler<HandlerMessage, ScopedDependency1, ScopedDependency2, ScopedDependency3>((message, first, second, third) =>
                    probe.RecordAsync(shape, message.MessageId, dependencies: [first.Id, second.Id, third.Id]));
                break;
            case MessageHandlerShape.ContextAndThreeDependencies:
                configuration.AddHandler<HandlerMessage, ScopedDependency1, ScopedDependency2, ScopedDependency3>(
                    (ConsumeContext<HandlerMessage> context, ScopedDependency1 first, ScopedDependency2 second, ScopedDependency3 third) =>
                        probe.RecordAsync(shape, context.Message.MessageId, context.Advanced(), first.Id, second.Id, third.Id));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    private static void RegisterRequestHandler(
        IBusRegistrationConfigurator configuration,
        RequestHandlerShape shape,
        HandlerProbe probe)
    {
        switch (shape)
        {
            case RequestHandlerShape.Message:
                configuration.AddHandler<HandlerRequest, HandlerResponse>(request =>
                    probe.RespondAsync(shape, request.MessageId));
                break;
            case RequestHandlerShape.Context:
                configuration.AddHandler<HandlerRequest, HandlerResponse>((ConsumeContext<HandlerRequest> context) =>
                    probe.RespondAsync(shape, context.Message.MessageId, context.Advanced()));
                break;
            case RequestHandlerShape.MessageAndOneDependency:
                configuration.AddHandler<HandlerRequest, ScopedDependency1, HandlerResponse>((request, first) =>
                    probe.RespondAsync(shape, request.MessageId, dependencies: [first.Id]));
                break;
            case RequestHandlerShape.ContextAndOneDependency:
                configuration.AddHandler<HandlerRequest, ScopedDependency1, HandlerResponse>(
                    (ConsumeContext<HandlerRequest> context, ScopedDependency1 first) =>
                        probe.RespondAsync(shape, context.Message.MessageId, context.Advanced(), first.Id));
                break;
            case RequestHandlerShape.MessageAndTwoDependencies:
                configuration.AddHandler<HandlerRequest, ScopedDependency1, ScopedDependency2, HandlerResponse>((request, first, second) =>
                    probe.RespondAsync(shape, request.MessageId, dependencies: [first.Id, second.Id]));
                break;
            case RequestHandlerShape.ContextAndTwoDependencies:
                configuration.AddHandler<HandlerRequest, ScopedDependency1, ScopedDependency2, HandlerResponse>(
                    (ConsumeContext<HandlerRequest> context, ScopedDependency1 first, ScopedDependency2 second) =>
                        probe.RespondAsync(shape, context.Message.MessageId, context.Advanced(), first.Id, second.Id));
                break;
            case RequestHandlerShape.MessageAndThreeDependencies:
                configuration.AddHandler<HandlerRequest, ScopedDependency1, ScopedDependency2, ScopedDependency3, HandlerResponse>((request, first, second, third) =>
                    probe.RespondAsync(shape, request.MessageId, dependencies: [first.Id, second.Id, third.Id]));
                break;
            case RequestHandlerShape.ContextAndThreeDependencies:
                configuration.AddHandler<HandlerRequest, ScopedDependency1, ScopedDependency2, ScopedDependency3, HandlerResponse>(
                    (ConsumeContext<HandlerRequest> context, ScopedDependency1 first, ScopedDependency2 second, ScopedDependency3 third) =>
                        probe.RespondAsync(shape, context.Message.MessageId, context.Advanced(), first.Id, second.Id, third.Id));
                break;
            case RequestHandlerShape.MessageWithCustomEndpoint:
                configuration.AddHandler<HandlerRequest, HandlerResponse>(request =>
                        probe.RespondAsync(shape, request.MessageId))
                    .Endpoint(endpoint => endpoint.Name = "native-handler-custom");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(shape), shape, null);
        }
    }

    private static void AssertDependencies<TShape>(TShape shape, IReadOnlyList<Guid> dependencyIds)
        where TShape : struct, Enum
    {
        int expectedCount = shape.ToString() switch
        {
            string value when value.Contains("ThreeDependencies", StringComparison.Ordinal) => 3,
            string value when value.Contains("TwoDependencies", StringComparison.Ordinal) => 2,
            string value when value.Contains("OneDependency", StringComparison.Ordinal) => 1,
            _ => 0,
        };

        Assert.Equal(expectedCount, dependencyIds.Count);
        Assert.Equal(expectedCount, dependencyIds.Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, dependencyIds);
    }

    private static bool ShapeUsesContext<TShape>(TShape shape)
        where TShape : struct, Enum =>
        shape.ToString().StartsWith("Context", StringComparison.Ordinal);

    private static CancellationToken SnapshotOnlyToken() => new(canceled: true);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum MessageHandlerShape
    {
        Message,
        Context,
        MessageAndOneDependency,
        ContextAndOneDependency,
        MessageAndTwoDependencies,
        ContextAndTwoDependencies,
        MessageAndThreeDependencies,
        ContextAndThreeDependencies,
    }

    public enum RequestHandlerShape
    {
        Message,
        Context,
        MessageAndOneDependency,
        ContextAndOneDependency,
        MessageAndTwoDependencies,
        ContextAndTwoDependencies,
        MessageAndThreeDependencies,
        ContextAndThreeDependencies,
        MessageWithCustomEndpoint,
    }

    public sealed record HandlerMessage(Guid MessageId);

    public sealed record HandlerRequest(Guid MessageId);

    public sealed record HandlerResponse(Guid MessageId, string Shape);

    public sealed class ScopedDependency1
    {
        public Guid Id { get; } = NewId.NextGuid();
    }

    public sealed class ScopedDependency2
    {
        public Guid Id { get; } = NewId.NextGuid();
    }

    public sealed class ScopedDependency3
    {
        public Guid Id { get; } = NewId.NextGuid();
    }

    private sealed class HandlerProbe(Guid expectedMessageId)
    {
        private readonly ConcurrentQueue<HandlerInvocation> _invocations = new();
        private readonly TaskCompletionSource<HandlerInvocation> _completed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Count => _invocations.Count;

        public Task<HandlerInvocation> Completed => _completed.Task;

        public Task RecordAsync<TShape>(
            TShape shape,
            Guid messageId,
            ConsumeContext? context = null,
            params Guid[] dependencies)
            where TShape : struct, Enum
        {
            RecordInvocation(shape, messageId, context, dependencies);
            return Task.CompletedTask;
        }

        public Task<HandlerResponse> RespondAsync<TShape>(
            TShape shape,
            Guid messageId,
            ConsumeContext? context = null,
            params Guid[] dependencies)
            where TShape : struct, Enum
        {
            RecordInvocation(shape, messageId, context, dependencies);
            return Task.FromResult(new HandlerResponse(messageId, shape.ToString()));
        }

        private void RecordInvocation<TShape>(
            TShape shape,
            Guid messageId,
            ConsumeContext? context,
            IReadOnlyList<Guid> dependencies)
            where TShape : struct, Enum
        {
            Assert.Equal(expectedMessageId, messageId);
            var invocation = new HandlerInvocation(
                shape.ToString(),
                messageId,
                context?.MessageId,
                context?.RequestId,
                dependencies.ToArray());
            _invocations.Enqueue(invocation);
            _completed.TrySetResult(invocation);
        }
    }

    private sealed record HandlerInvocation(
        string Shape,
        Guid MessageId,
        Guid? TransportMessageId,
        Guid? RequestId,
        IReadOnlyList<Guid> DependencyIds);
}
