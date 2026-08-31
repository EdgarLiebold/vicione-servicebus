using System.Collections.Concurrent;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

[Collection(MessageDataDefaultsCollection.Name)]
public sealed class MessageDataEndpointIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-SERIALIZATION", "system-text-json-large-payload-size-matrix")]
    public async Task SystemTextJson_RoundTripsEveryLargePayloadSizeWithoutLoss()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var defaults = StoredDefaults();
        var repository = new InMemoryMessageDataRepository();
        var observations = new ConcurrentDictionary<Guid, TaskCompletionSource<LargePayloadSnapshot>>();
        var deliveryCounts = new ConcurrentDictionary<Guid, int>();
        using var harness = CreateHarness("message-data-large-json", timeout, repository);
        harness.OnConfigureInMemoryBus += configurator => configurator.UseJsonSerializer();
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<LargePayloadEvent>(async context =>
        {
            if (!observations.TryGetValue(context.Message.CorrelationId, out TaskCompletionSource<LargePayloadSnapshot>? completion))
                throw new InvalidOperationException($"Unexpected payload correlation: {context.Message.CorrelationId}");

            try
            {
                LargePayload body = await context.Message.Body.Value;
                deliveryCounts.AddOrUpdate(context.Message.CorrelationId, 1, (_, count) => count + 1);
                completion.TrySetResult(new LargePayloadSnapshot(
                    context.Message.Body.Address,
                    body.CorrelationId,
                    body.Values,
                    body.IsComplete));
            }
            catch (Exception exception)
            {
                completion.TrySetException(exception);
                throw;
            }
        });

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        var stopped = false;
        try
        {
            foreach (int count in new[] { 1_000, 50_000, 1_000_000 })
            {
                Guid correlationId = NewId.NextGuid();
                int[] values = Enumerable.Range(0, count).Select(index => index % 10_007).ToArray();
                var completion = new TaskCompletionSource<LargePayloadSnapshot>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                Assert.True(observations.TryAdd(correlationId, completion));

                await harness.Bus.Publish<LargePayloadEvent>(new
                {
                    CorrelationId = correlationId,
                    Body = new LargePayload(correlationId, values, true),
                }, cancellationToken);

                LargePayloadSnapshot actual = await completion.Task.WaitAsync(timeout, cancellationToken);
                Assert.NotNull(actual.Address);
                Assert.Equal(correlationId, actual.CorrelationId);
                Assert.Equal(values, actual.Values);
                Assert.True(actual.IsComplete);
            }

            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
            stopped = true;
            Assert.Equal(3, observations.Count);
            Assert.All(observations.Values, completion => Assert.True(completion.Task.IsCompletedSuccessfully));
            Assert.Equal(observations.Keys.Order(), deliveryCounts.Keys.Order());
            Assert.All(deliveryCounts.Values, count => Assert.Equal(1, count));
        }
        finally
        {
            if (!stopped)
                await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-PUBLISH", "stored-string-address-and-exact-content")]
    public async Task Publish_StoresAndLoadsTheExactStringThroughTheConfiguredRepository()
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var defaults = StoredDefaults();
        var repository = new InMemoryMessageDataRepository();
        var observed = new TaskCompletionSource<PublishedSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        var deliveryCount = 0;
        using var harness = CreateHarness("message-data-publish", timeout, repository);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<DocumentPublished>(async context =>
        {
            try
            {
                Interlocked.Increment(ref deliveryCount);
                observed.TrySetResult(new PublishedSnapshot(
                    context.Message.CorrelationId,
                    context.Message.StringData.Address,
                    await context.Message.StringData.Value));
            }
            catch (Exception exception)
            {
                observed.TrySetException(exception);
                throw;
            }
        });
        Guid correlationId = NewId.NextGuid();
        const string expected = "published message data must survive the transport exactly";

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        var stopped = false;
        try
        {
            await harness.Bus.Publish<DocumentPublished>(new
            {
                CorrelationId = correlationId,
                StringData = expected,
            }, cancellationToken);

            PublishedSnapshot actual = await observed.Task.WaitAsync(timeout, cancellationToken);
            Assert.Equal(correlationId, actual.CorrelationId);
            Assert.NotNull(actual.Address);
            Assert.Equal(expected, actual.Value);
            MessageData<string> stored = await repository.GetString(actual.Address, cancellationToken);
            Assert.Equal(expected, await stored.Value);
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
            stopped = true;
            Assert.Equal(1, Volatile.Read(ref deliveryCount));
        }
        finally
        {
            if (!stopped)
                await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-MESSAGE-DATA-REQUEST", "created-and-connected-client-response-data")]
    public async Task RequestResponse_LoadsTheExactStoredValueForCreatedAndConnectedClients(bool connectedClient)
    {
        TimeSpan timeout = MessageDataTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var defaults = StoredDefaults();
        var repository = new InMemoryMessageDataRepository();
        var handlerCalls = 0;
        using var harness = CreateHarness("message-data-request", timeout, repository);
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint => endpoint.Handler<DataRequest>(context =>
        {
            Interlocked.Increment(ref handlerCalls);
            return context.RespondAsync<DataResponse>(new
            {
                context.Message.CorrelationId,
                context.Message.Key,
                Value = $"response:{context.Message.Key}",
            });
        });
        Guid correlationId = NewId.NextGuid();
        const string key = "request-key";

        await harness.Start(cancellationToken).WaitAsync(timeout, cancellationToken);
        var stopped = false;
        try
        {
            IRequestClient<DataRequest> client = connectedClient
                ? await harness.ConnectRequestClient<DataRequest>()
                : harness.Bus.CreateRequestClient<DataRequest>(harness.InputQueueAddress, timeout);

            Response<DataResponse> response = await client.GetResponse<DataResponse>(new
            {
                CorrelationId = correlationId,
                Key = key,
            }, cancellationToken);
            string value = await response.Message.Value.Value;

            Assert.Equal(correlationId, response.Message.CorrelationId);
            Assert.Equal(key, response.Message.Key);
            Assert.NotNull(response.Message.Value.Address);
            Assert.Equal($"response:{key}", value);
            MessageData<string> stored = await repository.GetString(response.Message.Value.Address, cancellationToken);
            Assert.Equal(value, await stored.Value);
            await harness.Stop().WaitAsync(timeout, CancellationToken.None);
            stopped = true;
            Assert.Equal(1, Volatile.Read(ref handlerCalls));
        }
        finally
        {
            if (!stopped)
                await harness.Stop().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static MessageDataDefaultsScope StoredDefaults()
    {
        var scope = new MessageDataDefaultsScope();
        MessageDataDefaults.AlwaysWriteToRepository = true;
        MessageDataDefaults.Threshold = 1;
        return scope;
    }

    private static InMemoryTestHarness CreateHarness(
        string prefix,
        TimeSpan timeout,
        IMessageDataRepository repository)
    {
        var harness = new InMemoryTestHarness($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureInMemoryBus += configurator => configurator.UseMessageData(repository);
        return harness;
    }

    public interface LargePayloadEvent
    {
        Guid CorrelationId { get; }
        MessageData<LargePayload> Body { get; }
    }

    public sealed record LargePayload(Guid CorrelationId, int[] Values, bool IsComplete);

    public interface DocumentPublished
    {
        Guid CorrelationId { get; }
        MessageData<string> StringData { get; }
    }

    public interface DataRequest
    {
        Guid CorrelationId { get; }
        string Key { get; }
    }

    public interface DataResponse
    {
        Guid CorrelationId { get; }
        string Key { get; }
        MessageData<string> Value { get; }
    }

    private sealed record LargePayloadSnapshot(Uri? Address, Guid CorrelationId, int[] Values, bool IsComplete);

    private sealed record PublishedSnapshot(Guid CorrelationId, Uri? Address, string Value);
}
