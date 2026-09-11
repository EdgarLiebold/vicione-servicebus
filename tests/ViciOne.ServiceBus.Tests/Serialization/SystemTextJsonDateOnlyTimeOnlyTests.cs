using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonDateOnlyTimeOnlyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DATEONLY-TIMEONLY", "envelope-boundaries-and-null-values")]
    public void EnvelopeRoundTrip_PreservesTemporalBoundariesAndCanonicalWireValues()
    {
        var source = new TemporalBoundaryMessage(
            DateOnly.MinValue,
            DateOnly.MaxValue,
            TimeOnly.MinValue,
            TimeOnly.MaxValue,
            null,
            null);

        SystemTextJsonRoundTripResult<TemporalBoundaryMessage> result =
            SystemTextJsonRoundTrip.ExecuteWithContext(source);
        using JsonDocument document = JsonDocument.Parse(result.Bytes);
        JsonElement message = document.RootElement.GetProperty("message");

        Assert.Equal(source, result.Message);
        Assert.Equal("0001-01-01", message.GetProperty("firstDate").GetString());
        Assert.Equal("9999-12-31", message.GetProperty("lastDate").GetString());
        Assert.Equal("00:00:00", message.GetProperty("firstTime").GetString());
        Assert.Equal("23:59:59.9999999", message.GetProperty("lastTime").GetString());
        Assert.Equal(JsonValueKind.Null, message.GetProperty("optionalDate").ValueKind);
        Assert.Equal(JsonValueKind.Null, message.GetProperty("optionalTime").ValueKind);
        Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType.MediaType, result.ContentType);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DATEONLY-TIMEONLY", "in-memory-transport-roundtrip")]
    public async Task InMemoryTransport_ConsumesExactTemporalValuesWithoutCustomConvertersAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<ConsumeContext<TemporalTransportMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("temporal-roundtrip", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
            endpoint.Handler<TemporalTransportMessage>(context =>
            {
                received.TrySetResult(context);
                return Task.CompletedTask;
            });
        var source = new TemporalTransportMessage
        {
            Date = new DateOnly(2024, 2, 29),
            Time = new TimeOnly(23, 59, 59, 999).Add(TimeSpan.FromTicks(9_999)),
        };

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await harness.Bus.PublishAsync(source, cancellationToken).WaitAsync(timeout, cancellationToken);

            ConsumeContext<TemporalTransportMessage> context = await received.Task.WaitAsync(
                timeout,
                cancellationToken);
            IConsumedMessage<TemporalTransportMessage> observation = await harness.Consumed
                .SelectAsync<TemporalTransportMessage>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            Assert.Equal(source, context.Message);
            Assert.Equal(source, observation.Context.Message);
            Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType, context.Advanced().ReceiveContext.ContentType);
            Assert.Null(observation.Exception);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DATEONLY-TIMEONLY", "malformed-date-receive-fault")]
    public Task MalformedDate_PublishesAReceiveFaultWithoutDispatchingAsync() =>
        AssertMalformedTemporalInputProducesReceiveFaultAsync(
            "2024-02-30",
            "12:34:56.7890123",
            "System.DateOnly",
            "$.date");

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DATEONLY-TIMEONLY", "malformed-time-receive-fault")]
    public Task MalformedTime_PublishesAReceiveFaultWithoutDispatchingAsync() =>
        AssertMalformedTemporalInputProducesReceiveFaultAsync(
            "2024-02-29",
            "24:00:00",
            "System.TimeOnly",
            "$.time");

    private static async Task AssertMalformedTemporalInputProducesReceiveFaultAsync(
        string date,
        string time,
        string expectedType,
        string expectedPath)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var consumed = new TaskCompletionSource<ConsumeContext<TemporalTransportMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var faulted = new TaskCompletionSource<ConsumeContext<ReceiveFault>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = CreateHarness("malformed-temporal-value", timeout);
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.Handler<TemporalTransportMessage>(context =>
            {
                consumed.TrySetResult(context);
                return Task.CompletedTask;
            });
            endpoint.Handler<ReceiveFault>(context =>
            {
                faulted.TrySetResult(context);
                return Task.CompletedTask;
            });
        };
        Guid messageId = NewId.NextGuid();
        string envelope = CreateEnvelope(messageId, date, time);

        try
        {
            await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
            await harness.InputQueueSendEndpoint.SendAsync<TemporalTransportMessage>(
                    new { Date = DateOnly.MinValue, Time = TimeOnly.MinValue },
                    context =>
                    {
                        context.MessageId = messageId;
                        context.Serializer = new CopyBodySerializer(
                            SystemTextJsonMessageSerializer.JsonContentType,
                            new StringMessageBody(envelope));
                    },
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);

            ConsumeContext<ReceiveFault> context = await faulted.Task.WaitAsync(timeout, cancellationToken);
            ReceiveFault fault = context.Message;
            ExceptionInfo exception = Assert.Single(fault.Exceptions);

            Assert.Equal(messageId, fault.FaultedMessageId);
            Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType.MediaType, fault.ContentType);
            Assert.Equal(TypeCache<JsonException>.ShortName, exception.ExceptionType);
            Assert.Contains(expectedType, exception.Message, StringComparison.Ordinal);
            Assert.Contains(expectedPath, exception.Message, StringComparison.Ordinal);
            Assert.False(consumed.Task.IsCompleted);
        }
        finally
        {
            await harness.StopAsync().WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static InMemoryTestHarness CreateHarness(string name, TimeSpan timeout)
    {
        var harness = new InMemoryTestHarness($"{name}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.BeginTestScope();

        return harness;
    }

    private static string CreateEnvelope(Guid messageId, string date, string time) => $$"""
        {
          "messageId": "{{messageId:D}}",
          "messageTypes": [
            "{{MessageUrn.ForTypeString<TemporalTransportMessage>()}}"
          ],
          "message": {
            "date": "{{date}}",
            "time": "{{time}}"
          }
        }
        """;

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;
}

public sealed record TemporalBoundaryMessage(
    DateOnly FirstDate,
    DateOnly LastDate,
    TimeOnly FirstTime,
    TimeOnly LastTime,
    DateOnly? OptionalDate,
    TimeOnly? OptionalTime);

public sealed record TemporalTransportMessage
{
    public DateOnly Date { get; init; }

    public TimeOnly Time { get; init; }
}
