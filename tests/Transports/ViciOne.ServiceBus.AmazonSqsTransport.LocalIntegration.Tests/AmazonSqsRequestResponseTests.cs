namespace ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests;

using ViciOne.ServiceBus.AmazonSqsTransport.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class AmazonSqsRequestResponseTests
{
    private const int RequestCount = 100;

    [Fact]
    [RequirementCoverage("OBL-R0-CLOUD-0183", "one-hundred-overlapping-requests-exactly-correlate")]
    public async Task ConcurrentRequests_ReturnTheirExactResponses()
    {
        await using AmazonSqsLocalStack fixture = AmazonSqsLocalStack.Create("requests");
        string queueName = fixture.Name("service");
        IBusControl bus = Bus.Factory.CreateUsingAmazonSqs(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Durable = false;
                endpoint.AutoDelete = true;
                endpoint.PrefetchCount = RequestCount;
                endpoint.ConcurrentMessageLimit = 32;
                endpoint.ConcurrentDeliveryLimit = 10;
                endpoint.Handler<RequestMessage>(context => context.RespondAsync(
                    new ResponseMessage(context.Message.Sequence, context.Message.CorrelationId)));
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IRequestClient<RequestMessage> client = bus.CreateRequestClient<RequestMessage>(
                new Uri($"queue:{queueName}"),
                RequestTimeout.After(ms: checked((int)fixture.OperationTimeout.TotalMilliseconds)));
            RequestMessage[] requests = Enumerable.Range(0, RequestCount)
                .Select(sequence => new RequestMessage(sequence, Guid.NewGuid()))
                .ToArray();

            Task<Response<ResponseMessage>>[] pending = requests
                .Select(request => client.GetResponse<ResponseMessage>(request, cancellationToken))
                .ToArray();
            Response<ResponseMessage>[] responses = await Task.WhenAll(pending)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);

            Assert.Equal(RequestCount, responses.Length);
            Assert.Equal(
                requests.Select(request => (request.Sequence, request.CorrelationId)).OrderBy(item => item.Sequence),
                responses.Select(response => (response.Message.Sequence, response.Message.CorrelationId)).OrderBy(item => item.Sequence));
            Assert.Equal(RequestCount, responses.Select(response => response.Message.Sequence).Distinct().Count());
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private sealed record RequestMessage(int Sequence, Guid CorrelationId);
    private sealed record ResponseMessage(int Sequence, Guid CorrelationId);
}
