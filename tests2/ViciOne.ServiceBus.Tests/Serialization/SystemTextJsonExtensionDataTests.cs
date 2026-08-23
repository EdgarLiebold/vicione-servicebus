using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

[Collection(SystemTextJsonGlobalOptionsCollection.Name)]
public sealed class SystemTextJsonExtensionDataTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-EXTENSION-DATA", "envelope")]
    public Task EnvelopeSerializer_PreservesEveryExtensionValue() =>
        AssertExtensionDataRoundTrip(JsonTransportMode.Envelope);

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-EXTENSION-DATA", "raw")]
    public Task RawSerializer_PreservesEveryExtensionValue() =>
        AssertExtensionDataRoundTrip(JsonTransportMode.Raw);

    private static async Task AssertExtensionDataRoundTrip(JsonTransportMode mode)
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        JsonSerializerOptions originalOptions = SystemTextJsonMessageSerializer.Options;
        var received = new TaskCompletionSource<ConsumeContext<ExtensibleMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var faulted = new TaskCompletionSource<ConsumeContext<ReceiveFault>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"extension-data-{mode}-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        harness.OnConfigureInMemoryBus += configurator =>
        {
            if (mode == JsonTransportMode.Raw)
            {
                configurator.ClearSerialization();
                configurator.UseRawJsonSerializer(RawSerializerOptions.All);
            }

            configurator.ConfigureJsonSerializerOptions(options =>
            {
                options.SetMessageSerializerOptions<ExtensibleMessage>();
                return options;
            });
        };
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.Handler<ExtensibleMessage>(context =>
            {
                received.TrySetResult(context);
                return Task.CompletedTask;
            });
            configurator.Handler<ReceiveFault>(context =>
            {
                faulted.TrySetResult(context);
                return Task.CompletedTask;
            });
        };

        try
        {
            await harness.Start(cancellationToken).WaitAsync(operationTimeout, cancellationToken);
            await harness.Bus.Publish(
                    new ExtensibleMessage
                    {
                        Extra = new Dictionary<string, object>
                        {
                            ["text"] = "Value",
                            ["number"] = 2,
                        },
                    },
                    cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            Task completed = await Task.WhenAny(received.Task, faulted.Task)
                .WaitAsync(operationTimeout, cancellationToken);
            if (ReferenceEquals(completed, faulted.Task))
            {
                ReceiveFault fault = (await faulted.Task).Message;
                ExceptionInfo exception = Assert.Single(fault.Exceptions);
                Assert.Fail($"Expected extension data, received {exception.ExceptionType}: {exception.Message}");
            }

            ConsumeContext<ExtensibleMessage> context = await received.Task;
            string expectedMediaType = mode == JsonTransportMode.Envelope
                ? SystemTextJsonMessageSerializer.JsonContentType.MediaType
                : SystemTextJsonRawMessageSerializer.JsonContentType.MediaType;

            Assert.Equal(expectedMediaType, context.ReceiveContext.ContentType.MediaType);
            Assert.Equal(["number", "text"], context.Message.Extra.Keys.Order(StringComparer.Ordinal));
            JsonElement text = Assert.IsType<JsonElement>(context.Message.Extra["text"]);
            JsonElement number = Assert.IsType<JsonElement>(context.Message.Extra["number"]);
            Assert.Equal(JsonValueKind.String, text.ValueKind);
            Assert.Equal("Value", text.GetString());
            Assert.Equal(JsonValueKind.Number, number.ValueKind);
            Assert.Equal(2, number.GetInt32());
        }
        finally
        {
            try
            {
                await harness.Stop().WaitAsync(operationTimeout, CancellationToken.None);
            }
            finally
            {
                SystemTextJsonMessageSerializer.Options = originalOptions;
            }
        }
    }

    public sealed class ExtensibleMessage
    {
        [JsonExtensionData]
        public Dictionary<string, object> Extra { get; set; } = new(StringComparer.Ordinal);
    }

    private enum JsonTransportMode
    {
        Envelope,
        Raw,
    }
}
