using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class RoutingSlipPayloadIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-PAYLOAD", "message-data-overrides-variable")]
    public async Task MessageDataArgument_IsLoadedAndOverridesTheExistingVariableAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        var observed = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-message-data");
        harness.InMemoryBusConfiguring += configurator => configurator.UseMessageData(repository);
        ExecuteActivityTestHarness<MessageDataActivity, MessageDataArguments> activity = harness.ExecuteActivity<
            MessageDataActivity,
            MessageDataArguments>(_ => new MessageDataActivity(observed));
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(1);
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        activityCompleted.Configure(harness);
        completed.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            Guid trackingNumber = NewId.NextGuid();
            MessageData<string> storedValue = await repository.PutStringAsync("Frank", cancellationToken);
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.SetVariable("Name", "variable");
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new { Key = "Name", Value = storedValue });

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                observed.Task.WaitAsync(timeout, cancellationToken),
                activityCompleted.WaitAsync(timeout, cancellationToken),
                completed.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal("Frank", await observed.Task);
            Assert.Equal(trackingNumber, Assert.Single(activityCompleted.Messages).Message.TrackingNumber);
            Assert.Equal("Frank", Assert.Single(completed.Messages).GetVariable<string>("Name"));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-PAYLOAD", "nested-object-graph-and-dictionary-keys")]
    public async Task NestedObjectGraphAndDictionaryKeys_RoundTripWithoutShapeLossAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observed = new TaskCompletionSource<ObjectGraphSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-object-graph");
        ExecuteActivityTestHarness<ObjectGraphActivity, ObjectGraphArguments> activity = harness.ExecuteActivity<
            ObjectGraphActivity,
            ObjectGraphArguments>(_ => new ObjectGraphActivity(observed));
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        completed.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            var argumentsDictionary = new Dictionary<string, string>
            {
                ["good_jpath_key"] = "val1",
                ["bad jpath key"] = "val2",
            };
            var variableDictionary = new Dictionary<string, string>
            {
                ["good_jpath_key"] = "val3",
                ["bad jpath key"] = "val4",
            };
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new ObjectGraphArguments(
                new OuterGraph(27, "Hello, World.", 123.45m),
                ["Albert", "Chris"],
                argumentsDictionary));
            builder.SetVariable("ArgumentsDictionary", variableDictionary);

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                observed.Task.WaitAsync(timeout, cancellationToken),
                completed.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            ObjectGraphSnapshot actual = await observed.Task;
            Assert.Equal(27, actual.IntValue);
            Assert.Equal("Hello, World.", actual.StringValue);
            Assert.Equal(123.45m, actual.DecimalValue);
            Assert.Equal(["Albert", "Chris"], actual.Names);
            Assert.Equal(
                [new KeyValuePair<string, string>("bad jpath key", "val2"), new("good_jpath_key", "val1")],
                actual.Arguments);
            IReadOnlyDictionary<string, string>? variables = Assert.Single(completed.Messages)
                .GetVariable<IReadOnlyDictionary<string, string>>("ArgumentsDictionary");
            Assert.NotNull(variables);
            Assert.Equal(variableDictionary.OrderBy(x => x.Key), variables.OrderBy(x => x.Key));
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-PAYLOAD", "nullable-enum-default-value")]
    public async Task NullableEnum_DefaultValueRemainsPresentWhenNullsAreIgnoredAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observed = new TaskCompletionSource<PayloadEnumeration?>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-nullable-enum");
        harness.InMemoryBusConfiguring += configurator => configurator.ConfigureJsonSerializerOptions(options =>
        {
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            return options;
        });
        ExecuteActivityTestHarness<NullableEnumActivity, NullableEnumArguments> activity = harness.ExecuteActivity<
            NullableEnumActivity,
            NullableEnumArguments>(_ => new NullableEnumActivity(observed));
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        completed.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new NullableEnumArguments(
                [new NullableEnumItem(PayloadEnumeration.DefaultValue)]));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                observed.Task.WaitAsync(timeout, cancellationToken),
                completed.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(PayloadEnumeration.DefaultValue, await observed.Task);
            Assert.Single(completed.Messages);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-PAYLOAD", "uri-argument-log-and-compensation")]
    public async Task UriVariable_RoundTripsThroughActivityLogAndCompensationAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-uri");
        ActivityTestHarness<UriActivity, UriArguments, UriLog> activity = harness.Activity<
            UriActivity,
            UriArguments,
            UriLog>();
        ExecuteActivityTestHarness<FaultingCourierActivity, FaultingCourierArguments> failing = harness.ExecuteActivity<
            FaultingCourierActivity,
            FaultingCourierArguments>();
        using var activityCompleted = new CourierMessageRecorder<RoutingSlipActivityCompleted>(2);
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        using var compensated = new CourierMessageRecorder<RoutingSlipActivityCompensated>(1);
        using var faulted = new CourierMessageRecorder<RoutingSlipFaulted>(1);
        activityCompleted.Configure(harness);
        completed.Configure(harness);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            var expectedAddress = new Uri("https://example.test/courier/resource?id=27");
            var success = new RoutingSlipBuilder(NewId.NextGuid());
            success.SetVariable(nameof(UriArguments.Address), expectedAddress);
            success.AddActivity(activity.Name, activity.ExecuteAddress);

            var failure = new RoutingSlipBuilder(NewId.NextGuid());
            failure.SetVariable(nameof(UriArguments.Address), expectedAddress);
            failure.AddActivity(activity.Name, activity.ExecuteAddress);
            failure.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("force-compensation"));

            await harness.Bus.ExecuteAsync(success.Build(), cancellationToken);
            await completed.WaitAsync(timeout, cancellationToken);
            await harness.Bus.ExecuteAsync(failure.Build(), cancellationToken);
            await Task.WhenAll(
                activityCompleted.WaitAsync(timeout, cancellationToken),
                compensated.WaitAsync(timeout, cancellationToken),
                faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(2, activityCompleted.Count);
            Assert.All(activityCompleted.Messages,
                context => Assert.Equal(expectedAddress, context.GetResult<Uri>(nameof(UriLog.UsedAddress))));
            Assert.Equal(expectedAddress,
                Assert.Single(compensated.Messages).GetResult<Uri>(nameof(UriLog.UsedAddress)));
            Assert.Single(completed.Messages);
            Assert.Single(faulted.Messages);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-PAYLOAD", "custom-json-converter-is-used")]
    public async Task OpaqueDoublePayload_UsesTheConfiguredJsonConverterAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observed = new TaskCompletionSource<OpaquePointSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-custom-converter");
        harness.InMemoryBusConfiguring += configurator => configurator.ConfigureJsonSerializerOptions(options =>
        {
            options.Converters.Add(new OpaquePointConverter());
            return options;
        });
        ExecuteActivityTestHarness<OpaquePointActivity, OpaquePointArguments> activity = harness.ExecuteActivity<
            OpaquePointActivity,
            OpaquePointArguments>(_ => new OpaquePointActivity(observed));
        using var completed = new CourierMessageRecorder<RoutingSlipCompleted>(1);
        completed.Configure(harness);
        await harness.StartAsync(cancellationToken);

        try
        {
            var builder = new RoutingSlipBuilder(NewId.NextGuid());
            builder.AddActivity(activity.Name, activity.ExecuteAddress,
                new OpaquePointArguments(new OpaquePoint(1.2, 2.3)));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken);
            await Task.WhenAll(
                observed.Task.WaitAsync(timeout, cancellationToken),
                completed.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(TestContext.Current.CancellationToken);

            Assert.Equal(new OpaquePointSnapshot(1.2, 2.3), await observed.Task);
            Assert.Single(completed.Messages);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    public sealed record MessageDataArguments(string Key, MessageData<string> Value);

    public sealed class MessageDataActivity(TaskCompletionSource<string> observed) :
        IExecuteActivity<MessageDataArguments>
    {
        public async Task<ExecutionResult> ExecuteAsync(ExecuteContext<MessageDataArguments> context)
        {
            string value = await context.Arguments.Value.Value
                ?? throw new Xunit.Sdk.XunitException("Expected the activity message-data value to be non-null.");
            observed.TrySetResult(value);
            return context.CompletedWithVariables(new Dictionary<string, object>
            {
                [context.Arguments.Key] = value,
            });
        }
    }

    public sealed record OuterGraph(int IntValue, string StringValue, decimal DecimalValue);

    public sealed record ObjectGraphArguments(
        OuterGraph Outer,
        string[] Names,
        Dictionary<string, string> ArgumentsDictionary);

    public sealed record ObjectGraphSnapshot(
        int IntValue,
        string StringValue,
        decimal DecimalValue,
        string[] Names,
        KeyValuePair<string, string>[] Arguments);

    public sealed class ObjectGraphActivity(TaskCompletionSource<ObjectGraphSnapshot> observed) :
        IExecuteActivity<ObjectGraphArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ObjectGraphArguments> context)
        {
            observed.TrySetResult(new ObjectGraphSnapshot(
                context.Arguments.Outer.IntValue,
                context.Arguments.Outer.StringValue,
                context.Arguments.Outer.DecimalValue,
                context.Arguments.Names,
                context.Arguments.ArgumentsDictionary.OrderBy(x => x.Key).ToArray()));
            return Task.FromResult(context.Completed());
        }
    }

    public enum PayloadEnumeration
    {
        DefaultValue,
        OtherValue,
    }

    public sealed record NullableEnumItem(PayloadEnumeration? Enumeration);

    public sealed record NullableEnumArguments(List<NullableEnumItem> Payload);

    public sealed class NullableEnumActivity(TaskCompletionSource<PayloadEnumeration?> observed) :
        IExecuteActivity<NullableEnumArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<NullableEnumArguments> context)
        {
            PayloadEnumeration? value = Assert.Single(context.Arguments.Payload).Enumeration;
            observed.TrySetResult(value);
            return Task.FromResult(context.Completed());
        }
    }

    public sealed record UriArguments(Uri Address);

    public sealed record UriLog(Uri UsedAddress);

    public sealed class UriActivity : IActivity<UriArguments, UriLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<UriArguments> context) =>
            Task.FromResult(context.Completed(new UriLog(context.Arguments.Address)));

        public Task<CompensationResult> CompensateAsync(CompensateContext<UriLog> context) =>
            Task.FromResult(context.Compensated());
    }

    public sealed record OpaquePointArguments(OpaquePoint Point);

    public sealed record OpaquePointSnapshot(double X, double Y);

    public sealed class OpaquePoint
    {
        private readonly double _x;
        private readonly double _y;

        public OpaquePoint()
        {
        }

        public OpaquePoint(double x, double y)
        {
            _x = x;
            _y = y;
        }

        public double GetX() => _x;

        public double GetY() => _y;
    }

    public sealed class OpaquePointActivity(TaskCompletionSource<OpaquePointSnapshot> observed) :
        IExecuteActivity<OpaquePointArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<OpaquePointArguments> context)
        {
            observed.TrySetResult(new OpaquePointSnapshot(
                context.Arguments.Point.GetX(),
                context.Arguments.Point.GetY()));
            return Task.FromResult(context.Completed());
        }
    }

    public sealed class OpaquePointConverter : JsonConverter<OpaquePoint>
    {
        public override OpaquePoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            string value = document.RootElement.GetProperty("coordinate").GetString()
                ?? throw new JsonException("The coordinate value is missing.");
            string[] components = value.Split('|');
            if (components.Length != 2)
                throw new JsonException("The coordinate must contain exactly two components.");

            return new OpaquePoint(
                double.Parse(components[0], CultureInfo.InvariantCulture),
                double.Parse(components[1], CultureInfo.InvariantCulture));
        }

        public override void Write(Utf8JsonWriter writer, OpaquePoint value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteString(
                "coordinate",
                FormattableString.Invariant($"{value.GetX():R}|{value.GetY():R}"));
            writer.WriteEndObject();
        }
    }
}
