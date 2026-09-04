using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsHandlerFlowTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HANDLER-FLOW", "send-consume-handler-send-and-handler-publish")]
    public async Task SendAndPublish_InvokeOnlyTheirTypedHandlersAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("handlerflow");
        string queueName = fixture.Name("input");
        Guid flowId = Guid.NewGuid();
        Guid sentAId = Guid.NewGuid();
        Guid publishedBId = Guid.NewGuid();
        Guid handlerSentCId = Guid.NewGuid();
        Guid handlerPublishedDId = Guid.NewGuid();
        var receivedA = NewObservation<Observed<A>>();
        var receivedB = NewObservation<Observed<B>>();
        var sentC = NewObservation<Observed<C>>();
        var receivedD = NewObservation<Observed<D>>();
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<A>(async context =>
                {
                    receivedA.TrySetResult(new Observed<A>(context.MessageId, context.Message));

                    Uri sourceAddress = context.SourceAddress
                        ?? throw new InvalidDataException("An Amazon SQS receive contract must carry its source queue address.");
                    ISendEndpoint source = await context.Advanced().GetSendEndpointAsync(sourceAddress);
                    await source.SendAsync(
                        new C(context.Message.FlowId),
                        sendContext => sendContext.MessageId = handlerSentCId,
                        context.CancellationToken);
                    await context.Advanced().PublishAsync(
                        new D(context.Message.FlowId),
                        publishContext => publishContext.MessageId = handlerPublishedDId,
                        context.CancellationToken);
                });
                endpoint.Handler<B>(context =>
                {
                    receivedB.TrySetResult(new Observed<B>(context.MessageId, context.Message));
                    return Task.CompletedTask;
                });
                endpoint.Handler<D>(context =>
                {
                    receivedD.TrySetResult(new Observed<D>(context.MessageId, context.Message));
                    return Task.CompletedTask;
                });
            });
        });
        using ConnectHandle sendObservation = bus.ConnectSendObserver(new SentMessageObserver<C>(sentC));
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await input.SendAsync(new A(flowId), context => context.MessageId = sentAId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(new B(flowId), context => context.MessageId = publishedBId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Observed<A> actualA = await receivedA.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Observed<B> actualB = await receivedB.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Observed<C> actualC = await sentC.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Observed<D> actualD = await receivedD.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal((sentAId, flowId), (actualA.MessageId, actualA.Message.FlowId));
            Assert.Equal((publishedBId, flowId), (actualB.MessageId, actualB.Message.FlowId));
            Assert.Equal((handlerSentCId, flowId), (actualC.MessageId, actualC.Message.FlowId));
            Assert.Equal((handlerPublishedDId, flowId), (actualD.MessageId, actualD.Message.FlowId));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-AWS-SQS-HANDLER-FLOW", "exact-message-id-for-send-and-publish")]
    public async Task Handlers_ReceiveExactSendAndPublishContractsAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("contracts");
        string queueName = fixture.Name("input");
        Guid sentFlowId = Guid.NewGuid();
        Guid publishedFlowId = Guid.NewGuid();
        Guid sentMessageId = Guid.NewGuid();
        Guid publishedMessageId = Guid.NewGuid();
        var sent = NewObservation<Observed<A>>();
        var published = NewObservation<Observed<A>>();
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<A>(context =>
                {
                    var observation = new Observed<A>(context.MessageId, context.Message);
                    if (context.Message.FlowId == sentFlowId)
                        sent.TrySetResult(observation);
                    else if (context.Message.FlowId == publishedFlowId)
                        published.TrySetResult(observation);
                    return Task.CompletedTask;
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            ISendEndpoint input = await bus.GetSendEndpointAsync(new Uri($"queue:{queueName}"), TestContext.Current.CancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);

            await input.SendAsync(new A(sentFlowId), context => context.MessageId = sentMessageId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.PublishAsync(new A(publishedFlowId), context => context.MessageId = publishedMessageId, cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Observed<A> actualSent = await sent.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);
            Observed<A> actualPublished = await published.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal((sentMessageId, sentFlowId), (actualSent.MessageId, actualSent.Message.FlowId));
            Assert.Equal((publishedMessageId, publishedFlowId), (actualPublished.MessageId, actualPublished.Message.FlowId));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record A(Guid FlowId);
    private sealed record B(Guid FlowId);
    private sealed record C(Guid FlowId);
    private sealed record D(Guid FlowId);
    private sealed record Observed<T>(Guid? MessageId, T Message);

    private sealed class SentMessageObserver<TMessage>(TaskCompletionSource<Observed<TMessage>> completion) : ISendObserver
        where TMessage : class
    {
        public Task PreSendAsync<T>(SendContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PostSendAsync<T>(SendContext<T> context)
            where T : class
        {
            if (context.Message is TMessage message)
                completion.TrySetResult(new Observed<TMessage>(context.MessageId, message));

            return Task.CompletedTask;
        }

        public Task SendFaultAsync<T>(SendContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }
}
