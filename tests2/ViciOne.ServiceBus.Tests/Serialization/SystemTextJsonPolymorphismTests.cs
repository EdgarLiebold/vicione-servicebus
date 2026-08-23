using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonPolymorphismTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-POLYMORPHISM", "single-property")]
    public async Task AbstractProperty_PreservesItsConcreteTypeAndValue()
    {
        ISinglePayloadMessage actual = await RoundTrip<ISinglePayloadMessage>(
            new SinglePayloadMessage { Data = new DerivedPayload { Value = 27 } });

        var data = Assert.IsType<DerivedPayload>(actual.Data);
        Assert.Equal(27, data.Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-POLYMORPHISM", "array-property")]
    public async Task AbstractArrayProperty_PreservesConcreteTypesOrderAndValues()
    {
        IArrayPayloadMessage actual = await RoundTrip<IArrayPayloadMessage>(
            new ArrayPayloadMessage
            {
                Data = [new DerivedPayload { Value = 27 }, new DerivedPayload { Value = 42 }],
            });

        Assert.Equal(2, actual.Data.Length);
        Assert.Equal(27, Assert.IsType<DerivedPayload>(actual.Data[0]).Value);
        Assert.Equal(42, Assert.IsType<DerivedPayload>(actual.Data[1]).Value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-POLYMORPHISM", "list-property")]
    public async Task AbstractListProperty_PreservesConcreteTypesOrderAndValues()
    {
        IListPayloadMessage actual = await RoundTrip<IListPayloadMessage>(
            new ListPayloadMessage
            {
                Data = [new DerivedPayload { Value = 27 }, new DerivedPayload { Value = 42 }],
            });

        Assert.Equal(2, actual.Data.Count);
        Assert.Equal(27, Assert.IsType<DerivedPayload>(actual.Data[0]).Value);
        Assert.Equal(42, Assert.IsType<DerivedPayload>(actual.Data[1]).Value);
    }

    private static async Task<TMessage> RoundTrip<TMessage>(TMessage message)
        where TMessage : class
    {
        TimeSpan operationTimeout = TestConfigurationProvider.ForCurrentTestRun()
            .GetValidatedOptions()
            .OperationTimeout!.Value;
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var received = new TaskCompletionSource<ConsumeContext<TMessage>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var faulted = new TaskCompletionSource<ConsumeContext<ReceiveFault>>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"json-polymorphism-{NewId.NextGuid():N}")
        {
            TestTimeout = operationTimeout,
        };
        harness.BeginTestScope();
        harness.OnConfigureInMemoryReceiveEndpoint += configurator =>
        {
            configurator.Handler<TMessage>(context =>
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
            await harness.InputQueueSendEndpoint.Send(message, cancellationToken)
                .WaitAsync(operationTimeout, cancellationToken);

            Task completed = await Task.WhenAny(received.Task, faulted.Task)
                .WaitAsync(operationTimeout, cancellationToken);
            if (ReferenceEquals(completed, faulted.Task))
            {
                ReceiveFault fault = (await faulted.Task).Message;
                ExceptionInfo exception = Assert.Single(fault.Exceptions);
                Assert.Fail($"Expected {TypeCache<TMessage>.ShortName}, received {exception.ExceptionType}: {exception.Message}");
            }

            ConsumeContext<TMessage> context = await received.Task;
            Assert.Equal(SystemTextJsonMessageSerializer.JsonContentType, context.ReceiveContext.ContentType);

            return context.Message;
        }
        finally
        {
            await harness.Stop().WaitAsync(operationTimeout, CancellationToken.None);
        }
    }

    public interface ISinglePayloadMessage
    {
        PolymorphicPayload Data { get; }
    }

    public sealed class SinglePayloadMessage : ISinglePayloadMessage
    {
        public PolymorphicPayload Data { get; set; } = null!;
    }

    public interface IArrayPayloadMessage
    {
        PolymorphicPayload[] Data { get; }
    }

    public sealed class ArrayPayloadMessage : IArrayPayloadMessage
    {
        public PolymorphicPayload[] Data { get; set; } = [];
    }

    public interface IListPayloadMessage
    {
        IList<PolymorphicPayload> Data { get; }
    }

    public sealed class ListPayloadMessage : IListPayloadMessage
    {
        public IList<PolymorphicPayload> Data { get; set; } = [];
    }

    [JsonDerivedType(typeof(DerivedPayload), "derived")]
    public abstract class PolymorphicPayload;

    public sealed class DerivedPayload : PolymorphicPayload
    {
        public int Value { get; set; }
    }
}
