using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Middleware;

public sealed class ConsumeContextPayloadPropagationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-CONTEXT-PAYLOAD", "root-and-message-send-filters")]
    public async Task SendFilters_DistinguishExternalSendFromConsumeOwnedSendAndSeeItsPayloadAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"send-context-payload-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var inbound = NewSignal<ConsumeContext<InboundSend>>();
        var outbound = NewSignal<ConsumeContext<OutboundSend>>();

        harness.OnConfigureInMemoryBus += bus =>
        {
            bus.Route<OutboundSend>(harness.InputQueueAddress);
            bus.ConfigureSend(send =>
            {
                send.UseExecute(context => context.Headers.Set("root-payload", Describe(context)));
                send.ConnectSendPipeSpecificationObserver(new SendPayloadSpecificationObserver());
            });
        };
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.UseExecute(context => context.GetOrAddPayload(() => new PayloadMarker("hello")));
            endpoint.Handler<InboundSend>(async context =>
            {
                inbound.TrySetResult(context);
                await context.Advanced().SendAsync(new OutboundSend(context.Message.CorrelationId), context.CancellationToken);
            });
            endpoint.Handler<OutboundSend>(context => CompleteAsync(outbound, context));
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var message = new InboundSend(NewId.NextGuid());
            await harness.InputQueueSendEndpoint.SendAsync(message, cancellationToken);

            ConsumeContext<InboundSend> inboundContext = await inbound.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<OutboundSend> outboundContext = await outbound.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message.CorrelationId, inboundContext.Message.CorrelationId);
            Assert.Equal(message.CorrelationId, outboundContext.Message.CorrelationId);
            Assert.Equal("external", inboundContext.Headers.Get<string>("root-payload"));
            Assert.Equal("external", inboundContext.Headers.Get<string>("message-payload"));
            Assert.Equal("consume:hello", outboundContext.Headers.Get<string>("root-payload"));
            Assert.Equal("consume:hello", outboundContext.Headers.Get<string>("message-payload"));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-CONTEXT-PAYLOAD", "root-and-message-publish-filters")]
    public async Task PublishFilters_SeeTheConsumeContextAndItsCustomPayloadAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = new InMemoryTestHarness($"publish-context-payload-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        var inbound = NewSignal<ConsumeContext<InboundPublish>>();
        var outbound = NewSignal<ConsumeContext<OutboundPublish>>();

        harness.OnConfigureInMemoryBus += bus => bus.ConfigurePublish(publish =>
        {
            publish.UseExecute(context => context.Headers.Set("root-payload", Describe(context)));
            publish.ConnectPublishPipeSpecificationObserver(new PublishPayloadSpecificationObserver());
        });
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
        {
            endpoint.UseExecute(context => context.GetOrAddPayload(() => new PayloadMarker("hello")));
            endpoint.Handler<InboundPublish>(async context =>
            {
                inbound.TrySetResult(context);
                await context.Advanced().PublishAsync(new OutboundPublish(context.Message.CorrelationId), context.CancellationToken);
            });
            endpoint.Handler<OutboundPublish>(context => CompleteAsync(outbound, context));
        };

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            var message = new InboundPublish(NewId.NextGuid());
            await harness.Bus.PublishAsync(message, cancellationToken);

            ConsumeContext<InboundPublish> inboundContext = await inbound.Task.WaitAsync(timeout, cancellationToken);
            ConsumeContext<OutboundPublish> outboundContext = await outbound.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(message.CorrelationId, inboundContext.Message.CorrelationId);
            Assert.Equal(message.CorrelationId, outboundContext.Message.CorrelationId);
            Assert.Equal("external", inboundContext.Headers.Get<string>("root-payload"));
            Assert.Equal("external", inboundContext.Headers.Get<string>("message-payload"));
            Assert.Equal("consume:hello", outboundContext.Headers.Get<string>("root-payload"));
            Assert.Equal("consume:hello", outboundContext.Headers.Get<string>("message-payload"));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static string Describe(PipeContext context)
    {
        if (!context.TryGetPayload(out ConsumeContext? consumeContext))
            return "external";

        return consumeContext.TryGetPayload(out PayloadMarker? marker)
            ? $"consume:{marker.Value}"
            : "consume:missing";
    }

    private static Task CompleteAsync<T>(TaskCompletionSource<ConsumeContext<T>> signal, ConsumeContext<T> context)
        where T : class
    {
        signal.TrySetResult(context);
        return Task.CompletedTask;
    }

    private static TaskCompletionSource<T> NewSignal<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record PayloadMarker(string Value);

    private sealed record InboundSend(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record OutboundSend(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record InboundPublish(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed record OutboundPublish(Guid CorrelationId) : CorrelatedBy<Guid>;

    private sealed class SendPayloadSpecificationObserver : ISendPipeSpecificationObserver
    {
        public void MessageSpecificationCreated<T>(IMessageSendPipeSpecification<T> specification)
            where T : class =>
            specification.AddPipeSpecification(new SendPayloadSpecification<T>());
    }

    private sealed class SendPayloadSpecification<T> : IPipeSpecification<SendContext<T>>
        where T : class
    {
        public void Apply(IPipeBuilder<SendContext<T>> builder) =>
            builder.AddFilter(new SendPayloadFilter<T>());

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class SendPayloadFilter<T> : IFilter<SendContext<T>>
        where T : class
    {
        public async Task SendAsync(SendContext<T> context, IPipe<SendContext<T>> next)
        {
            context.Headers.Set("message-payload", Describe(context));
            await next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateScope("consume-context-payload");
    }

    private sealed class PublishPayloadSpecificationObserver : IPublishPipeSpecificationObserver
    {
        public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
            where T : class =>
            specification.AddPipeSpecification(new PublishPayloadSpecification<T>());
    }

    private sealed class PublishPayloadSpecification<T> : IPipeSpecification<PublishContext<T>>
        where T : class
    {
        public void Apply(IPipeBuilder<PublishContext<T>> builder) =>
            builder.AddFilter(new PublishPayloadFilter<T>());

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class PublishPayloadFilter<T> : IFilter<PublishContext<T>>
        where T : class
    {
        public async Task SendAsync(PublishContext<T> context, IPipe<PublishContext<T>> next)
        {
            context.Headers.Set("message-payload", Describe(context));
            await next.SendAsync(context);
        }

        public void Probe(ProbeContext context) => context.CreateScope("consume-context-payload");
    }
}
