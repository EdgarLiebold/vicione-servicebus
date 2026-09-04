using ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMqTransport.LocalIntegration.Tests;

public sealed class RabbitMqRequestResponseTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RABBITMQ-NATIVE-REQUEST", "direct-reply-to-preserves-request-correlation")]
    public async Task DirectReplyTo_PreservesRequestCorrelationExactlyOnce()
    {
        using RabbitMqBroker fixture = RabbitMqBroker.Create("replyto");
        string queue = fixture.Name("service");
        Guid expected = NewId.NextGuid();
        var consumed = new TaskCompletionSource<ConsumeContext<RequestMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int entries = 0;
        IBusControl bus = Bus.Factory.CreateUsingRabbitMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queue, endpoint =>
            {
                endpoint.Durable = true;
                endpoint.AutoDelete = false;
                endpoint.Handler<RequestMessage>(async context =>
                {
                    if (Interlocked.Increment(ref entries) != 1)
                        consumed.TrySetException(new InvalidDataException("The request was delivered more than once."));
                    else
                        consumed.TrySetResult(context);
                    await context.RespondAsync(new ResponseMessage(context.Message.CorrelationId, "exact-response"));
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IClientFactory? clientFactory = null;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            clientFactory = bus.CreateReplyToClientFactory(
                RequestTimeout.After(ms: checked((int)fixture.OperationTimeout.TotalMilliseconds)));
            IRequestClient<RequestMessage> client = clientFactory.CreateRequestClient<RequestMessage>(
                new Uri($"queue:{queue}"),
                RequestTimeout.After(ms: checked((int)fixture.OperationTimeout.TotalMilliseconds)));
            Response<ResponseMessage> response = await client.GetResponse<ResponseMessage>(
                    new RequestMessage(expected),
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            ConsumeContext<RequestMessage> request = await consumed.Task.WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(expected, request.Message.CorrelationId);
            Assert.Equal(expected, request.CorrelationId);
            Assert.NotNull(request.RequestId);
            Assert.NotNull(request.ResponseAddress);
            Assert.Contains("amq.rabbitmq.reply-to", request.ResponseAddress.AbsolutePath, StringComparison.Ordinal);
            Assert.Equal(request.RequestId, response.RequestId);
            Assert.Equal(expected, response.CorrelationId);
            Assert.Equal(expected, response.Message.CorrelationId);
            Assert.Equal("exact-response", response.Message.Value);

            if (clientFactory is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            clientFactory = null;
            await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            started = false;
            RabbitMqBroker.QueueState terminal = await fixture.Queue(queue, cancellationToken);
            Assert.Equal(0, terminal.Messages);
            Assert.Equal(1, entries);
        }
        finally
        {
            if (clientFactory is IAsyncDisposable asyncDisposable)
                await asyncDisposable.DisposeAsync();
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
            await fixture.CleanupAsync();
        }
    }

    private sealed record RequestMessage(Guid CorrelationId);

    private sealed record ResponseMessage(Guid CorrelationId, string Value);
}
