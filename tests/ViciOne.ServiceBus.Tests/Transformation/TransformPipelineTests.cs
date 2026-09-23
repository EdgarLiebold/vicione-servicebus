using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transformation;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transformation;

public sealed class TransformPipelineTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-NESTED-TRANSFORM-METADATA", "parent-sent-time-and-host-are-preserved")]
    public void NestedPropertyTransform_PreservesTheParentMessageMetadata()
    {
        DateTimeOffset sentTime = new(2026, 9, 7, 10, 30, 0, TimeSpan.Zero);
        HostInfo host = HostMetadataCache.Host;
        var parent = new ParentTransformContext(sentTime, host);

        var nested = new PropertyTransformContext<TransformMessage, NestedValue>(
            parent,
            new NestedValue("value"));

        Assert.Equal(sentTime, nested.SentTime);
        Assert.Same(host, nested.Host);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TRANSFORM", "publish-isolation")]
    public async Task SendTransform_DoesNotChangeAPublishedMessageAsync()
    {
        TransformMessage consumed = await RunBusTransformAsync(
            configureSend: true,
            dispatchSend: false);

        Assert.Equal("Hello", consumed.First);
        Assert.Null(consumed.Second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-TRANSFORM", "sent-message")]
    public async Task SendTransform_ChangesTheSentMessageAsync()
    {
        TransformMessage consumed = await RunBusTransformAsync(
            configureSend: true,
            dispatchSend: true);

        Assert.Equal("Hello", consumed.First);
        Assert.Equal("World", consumed.Second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-TRANSFORM", "published-message")]
    public async Task PublishTransform_ChangesThePublishedMessageAsync()
    {
        TransformMessage consumed = await RunBusTransformAsync(
            configureSend: false,
            dispatchSend: false);

        Assert.Equal("Hello", consumed.First);
        Assert.Equal("World", consumed.Second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-TRANSFORM", "send-isolation")]
    public async Task PublishTransform_DoesNotChangeASentMessageAsync()
    {
        TransformMessage consumed = await RunBusTransformAsync(
            configureSend: false,
            dispatchSend: true);

        Assert.Equal("Hello", consumed.First);
        Assert.Null(consumed.Second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TRANSFORM", "replace-original-message")]
    public Task ConsumeTransform_WithReplace_ChangesTheOriginalMessageInstanceAsync() =>
        AssertEndpointTransformAsync(replace: true);

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUME-TRANSFORM", "create-new-message")]
    public Task ConsumeTransform_WithoutReplace_CreatesANewMessageInstanceAsync() =>
        AssertEndpointTransformAsync(replace: false);

    [Fact]
    [RequirementCoverage("REQ-VSB-HANDLER-TRANSFORM", "replace-interface-message")]
    public Task HandlerTransform_WithReplace_ChangesOnlyTheConfiguredInterfaceHandlerAsync() =>
        AssertHandlerTransformAsync(replace: true);

    [Fact]
    [RequirementCoverage("REQ-VSB-HANDLER-TRANSFORM", "copy-interface-message")]
    public Task HandlerTransform_WithoutReplace_ChangesOnlyTheConfiguredInterfaceHandlerAsync() =>
        AssertHandlerTransformAsync(replace: false);

    [Theory]
    [InlineData("Previous", "Hello:Previous")]
    [InlineData(null, "Hello:<null>")]
    [RequirementCoverage("REQ-VSB-CONSUME-TRANSFORM-SPECIFICATION", "class-based-specification")]
    public async Task ClassBasedTransform_ChangesEveryConfiguredPropertyAsync(string? previous, string expectedSecond)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var transformed = new TaskCompletionSource<TransformMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CreateHarness(timeout, "class");
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.UseTransform<TransformMessage>(specification => specification.Get<FullTransform>());
            endpoint.Handler<TransformMessage>(context =>
            {
                transformed.TrySetResult(context.Message);
                return Task.CompletedTask;
            });
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                    new TransformMessage { First = "Hello", Second = previous },
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            TransformMessage consumed = await transformed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal("First", consumed.First);
            Assert.Equal(expectedSecond, consumed.Second);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static async Task<TransformMessage> RunBusTransformAsync(bool configureSend, bool dispatchSend)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CreateHarness(timeout, $"bus-{configureSend}-{dispatchSend}");
        HandlerTestHarness<TransformMessage> handler = harness.AddHandler<TransformMessage>();
        harness.InMemoryBusConfiguring += bus =>
        {
            void Configure(ITransformConfigurator<TransformMessage> transform)
            {
                transform.Replace = true;
                transform.Set(message => message.Second, "World");
            }

            if (configureSend)
                bus.ConfigureSend(send => send.UseTransform<TransformMessage>(Configure));
            else
                bus.ConfigurePublish(publish => publish.UseTransform<TransformMessage>(Configure));
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var message = new TransformMessage { First = "Hello" };
            if (dispatchSend)
            {
                await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken)
                    .WaitAsync(timeout, cancellationToken);
            }
            else
            {
                await harness.Bus.PublishAsync(message, cancellationToken)
                    .WaitAsync(timeout, cancellationToken);
            }

            return (await handler.Consumed.SelectAsync(cancellationToken).FirstObservedAsync()).Context.Message;
        }
        finally
        {
            await harness.StopAsync().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static async Task AssertEndpointTransformAsync(bool replace)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var beforeTransform = new TaskCompletionSource<TransformMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var afterTransform = new TaskCompletionSource<TransformMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CreateHarness(timeout, $"endpoint-{replace}");
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.UseExecute(context =>
            {
                if (context.TryGetMessage(out ConsumeContext<TransformMessage>? messageContext))
                    beforeTransform.TrySetResult(messageContext.Message);
            });
            endpoint.UseTransform<TransformMessage>(transform =>
            {
                transform.Replace = replace;
                transform.Set(message => message.Second, "World");
            });
            endpoint.Handler<TransformMessage>(context =>
            {
                afterTransform.TrySetResult(context.Message);
                return Task.CompletedTask;
            });
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                    new TransformMessage { First = "Hello" },
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            TransformMessage original = await beforeTransform.Task.WaitAsync(timeout, cancellationToken);
            TransformMessage transformed = await afterTransform.Task.WaitAsync(timeout, cancellationToken);

            if (replace)
                Assert.Same(original, transformed);
            else
                Assert.NotSame(original, transformed);
            Assert.Equal("Hello", transformed.First);
            Assert.Equal("World", transformed.Second);
        }
        finally
        {
            await harness.StopAsync().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static async Task AssertHandlerTransformAsync(bool replace)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var unmodified = new TaskCompletionSource<ITransformContract>(TaskCreationOptions.RunContinuationsAsynchronously);
        var transformed = new TaskCompletionSource<ITransformContract>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CreateHarness(timeout, $"handler-{replace}");
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.Handler<ITransformContract>(
                context =>
                {
                    transformed.TrySetResult(context.Message);
                    return Task.CompletedTask;
                },
                handler => handler.UseTransform(transform =>
                {
                    transform.Replace = replace;
                    transform.Set(message => message.Second, "World");
                }));
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        IHostReceiveEndpointHandle? controlEndpoint = null;
        try
        {
            controlEndpoint = harness.Bus.ConnectReceiveEndpoint(
                $"transform-control-{NewId.NextGuid():N}",
                endpoint => endpoint.Handler<ITransformContract>(context =>
                {
                    unmodified.TrySetResult(context.Message);
                    return Task.CompletedTask;
                }));
            await controlEndpoint.Ready.WaitAsync(timeout, cancellationToken);
            await harness.Bus.PublishAsync<ITransformContract>(
                    new TransformMessage { First = "Hello" },
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            ITransformContract original = await unmodified.Task.WaitAsync(timeout, cancellationToken);
            ITransformContract changed = await transformed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal("Hello", original.First);
            Assert.Null(original.Second);
            Assert.Equal("Hello", changed.First);
            Assert.Equal("World", changed.Second);
        }
        finally
        {
            if (controlEndpoint is not null)
            {
                await controlEndpoint.StopAsync(CancellationToken.None)
                    .WaitAsync(timeout, CancellationToken.None);
            }
            await harness.StopAsync().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static InMemoryTestHarness CreateHarness(TimeSpan timeout, string name) =>
        new($"transform-{name}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public interface ITransformContract
    {
        string First { get; }

        string? Second { get; }
    }

    public sealed class TransformMessage : ITransformContract
    {
        public string First { get; set; } = string.Empty;

        public string? Second { get; set; }
    }

    private sealed class FullTransform : ConsumeTransformSpecification<TransformMessage>
    {
        public FullTransform()
        {
            Set(message => message.First, "First");
            Set(message => message.Second, context =>
                context.HasValue ? $"{context.Input.First}:{context.Value ?? "<null>"}" : "missing");
        }
    }

    private sealed record NestedValue(string Value);

    private sealed class ParentTransformContext(DateTimeOffset sentTime, HostInfo host) :
        BasePipeContext,
        TransformContext<TransformMessage>
    {
        public Guid? MessageId => null;
        public Guid? RequestId => null;
        public Guid? CorrelationId => null;
        public Guid? ConversationId => null;
        public Guid? InitiatorId => null;
        public DateTimeOffset? ExpirationTime => null;
        public Uri? SourceAddress => null;
        public Uri? DestinationAddress => null;
        public Uri? ResponseAddress => null;
        public Uri? FaultAddress => null;
        public DateTimeOffset? SentTime => sentTime;
        public Headers Headers => EmptyHeaders.Instance;
        public HostInfo Host => host;
        public bool HasInput => true;
        public TransformMessage Input { get; } = new();
    }
}
