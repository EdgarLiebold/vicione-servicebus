using System.Collections.Concurrent;
using ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.LocalIntegration.Tests;

public sealed class ActiveMqRequestResponseTests
{
    private const int RequestCount = 100;

    [Theory]
    [InlineData(ActiveMqBroker.OpenWireFlavor)]
    [InlineData(ActiveMqBroker.AmqpFlavor)]
    [InlineData(ActiveMqBroker.ArtemisFlavor)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-REQUESTS", "concurrent-default-and-explicit-paths-correlate-exactly")]
    public async Task ConcurrentRequests_ReturnExactResponsesAcrossConfigurationPathsAsync(string flavor)
    {
        using ActiveMqBroker fixture = ActiveMqBroker.Create(flavor, "requests");
        string queueName = fixture.Name("service");
        var handled = new ConcurrentDictionary<(string Path, int Sequence, Guid CorrelationId), byte>();
        IBusControl bus = Bus.Factory.CreateUsingActiveMq(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.MessageTopology.GetMessageTopology<RequestMessage>().SetEntityName(fixture.Name("request"));
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.PrefetchCount = RequestCount;
                endpoint.ConcurrentMessageLimit = 32;
                endpoint.Handler<RequestMessage>(context =>
                {
                    var identity = (context.Message.Path, context.Message.Sequence, context.Message.CorrelationId);
                    if (!handled.TryAdd(identity, 0))
                        throw new InvalidDataException($"Request {identity} was delivered more than once.");

                    return context.RespondAsync(
                        new ResponseMessage(context.Message.Path, context.Message.Sequence, context.Message.CorrelationId));
                });
            });
        });
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            RequestMessage[] defaultRequests = CreateRequests("default");
            RequestMessage[] explicitRequests = CreateRequests("explicit");
            IRequestClient<RequestMessage> explicitClient = bus.CreateRequestClient<RequestMessage>(
                new Uri($"queue:{queueName}"),
                new RequestTimeout(fixture.OperationTimeout));

            Response<ResponseMessage>[] defaultResponses = await AwaitResponsesAsync(
                defaultRequests,
                request => bus.RequestAsync<RequestMessage, ResponseMessage>(request, cancellationToken: cancellationToken),
                handled,
                fixture.OperationTimeout,
                cancellationToken);
            AssertExact(defaultRequests, defaultResponses);

            Response<ResponseMessage>[] explicitResponses = await AwaitResponsesAsync(
                explicitRequests,
                request => explicitClient.GetResponseAsync<ResponseMessage>(request, cancellationToken),
                handled,
                fixture.OperationTimeout,
                cancellationToken);
            AssertExact(explicitRequests, explicitResponses);
            Assert.Equal(RequestCount * 2, handled.Count);
        }
        finally
        {
            if (started)
                await bus.StopAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
        }
    }

    private static RequestMessage[] CreateRequests(string path) =>
        Enumerable.Range(0, RequestCount)
            .Select(sequence => new RequestMessage(path, sequence, Guid.NewGuid()))
            .ToArray();

    private static async Task<Response<ResponseMessage>[]> AwaitResponsesAsync(
        RequestMessage[] requests,
        Func<RequestMessage, Task<Response<ResponseMessage>>> request,
        ConcurrentDictionary<(string Path, int Sequence, Guid CorrelationId), byte> handled,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        Task<Response<ResponseMessage>>[] pending = requests.Select(request).ToArray();
        try
        {
            return await Task.WhenAll(pending).WaitAsync(timeout, cancellationToken);
        }
        catch (TimeoutException error)
        {
            int completed = pending.Count(task => task.IsCompletedSuccessfully);
            int faulted = pending.Count(task => task.IsFaulted);
            int handledForPath = handled.Keys.Count(identity => identity.Path == requests[0].Path);
            string faultSummary = string.Join(
                "; ",
                pending
                    .Where(task => task.IsFaulted)
                    .SelectMany(task => task.Exception!.Flatten().InnerExceptions)
                    .GroupBy(exception => $"{exception.GetType().Name}: {exception.Message}", StringComparer.Ordinal)
                    .Select(group => $"{group.Count()}x {group.Key}")
                    .OrderBy(summary => summary, StringComparer.Ordinal));
            throw new TimeoutException(
                $"Request path '{requests[0].Path}' timed out: handled={handledForPath}, completed={completed}, faulted={faulted}, pending={pending.Length - completed - faulted}, faults=[{faultSummary}].",
                error);
        }
    }

    private static void AssertExact(
        IEnumerable<RequestMessage> requests,
        IEnumerable<Response<ResponseMessage>> responses)
    {
        (string Path, int Sequence, Guid CorrelationId)[] expected = requests
            .Select(request => (request.Path, request.Sequence, request.CorrelationId))
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Sequence)
            .ToArray();
        (string Path, int Sequence, Guid CorrelationId)[] actual = responses
            .Select(response => (response.Message.Path, response.Message.Sequence, response.Message.CorrelationId))
            .OrderBy(item => item.Path, StringComparer.Ordinal)
            .ThenBy(item => item.Sequence)
            .ToArray();

        Assert.Equal(RequestCount, actual.Length);
        Assert.Equal(expected, actual);
        Assert.Equal(RequestCount, actual.Select(item => item.CorrelationId).Distinct().Count());
    }

    private sealed record RequestMessage(string Path, int Sequence, Guid CorrelationId);
    private sealed record ResponseMessage(string Path, int Sequence, Guid CorrelationId);
}
