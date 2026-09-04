using ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AmazonSqs.LocalIntegration.Tests;

public sealed class AmazonSqsLifecycleTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0245", "start-stop-start-delivers-one-response-per-lifecycle")]
    public async Task Bus_StartStopStart_ReacquiresCleanResourcesAsync()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("restart");
        string queueName = fixture.Name("service");
        int consumed = 0;
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.Handler<LifecycleRequest>(context =>
                {
                    int attempt = Interlocked.Increment(ref consumed);
                    return context.RespondAsync(new LifecycleResponse(context.Message.Id, attempt));
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            LifecycleResponse first = await StartAndRequestAsync(1);
            await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = false;
            LifecycleResponse second = await StartAndRequestAsync(2);

            Assert.Equal((1, 1), (first.Id, first.ConsumerAttempt));
            Assert.Equal((2, 2), (second.Id, second.ConsumerAttempt));
            Assert.Equal(2, Volatile.Read(ref consumed));
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }

        async Task<LifecycleResponse> StartAndRequestAsync(int id)
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IRequestClient<LifecycleRequest> client = bus.CreateRequestClient<LifecycleRequest>(
                new Uri($"queue:{queueName}"),
                RequestTimeout.After(ms: checked((int)fixture.OperationTimeout.TotalMilliseconds)));
            Response<LifecycleResponse> response = await client.GetResponseAsync<LifecycleResponse>(
                    new LifecycleRequest(id), cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            return response.Message;
        }
    }

    private sealed record LifecycleRequest(int Id);
    private sealed record LifecycleResponse(int Id, int ConsumerAttempt);
}
