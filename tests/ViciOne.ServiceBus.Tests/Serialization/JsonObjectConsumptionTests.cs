using System.Text.Json.Nodes;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class JsonObjectConsumptionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-RAW-OBJECT", "typed-message-body")]
    public async Task TypedMessage_IsDeliveredAsAnExactRawJsonObjectAsync()
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<JsonObject>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"raw-json-object-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        harness.InMemoryReceiveEndpointConfiguring += configurator =>
            configurator.Handler<JsonObject>(context =>
            {
                received.TrySetResult(context.Message);
                return Task.CompletedTask;
            });

        var expected = new RawJsonContract(NewId.NextGuid(), "control-room", 27);

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync(expected, cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);
            JsonObject actual = await received.Task.WaitAsync(operationTimeout, cancellationToken);

            Assert.Equal(3, actual.Count);
            Assert.Equal(
                new[] { "correlationId", "count", "label" },
                actual.Select(property => property.Key).Order(StringComparer.Ordinal));
            Assert.Equal(expected.CorrelationId, actual["correlationId"]!.GetValue<Guid>());
            Assert.Equal(expected.Label, actual["label"]!.GetValue<string>());
            Assert.Equal(expected.Count, actual["count"]!.GetValue<int>());
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(operationTimeout, cancellationToken);
        }
    }

    private sealed record RawJsonContract(Guid CorrelationId, string Label, int Count);
}
