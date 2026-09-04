using Microsoft.Extensions.DependencyInjection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ReceiveEndpointDispatcherTests
{
    [Theory]
    [InlineData(DispatchBody.RawJson)]
    [InlineData(DispatchBody.Empty)]
    [RequirementCoverage("REQ-VSB-RECEIVE-DISPATCHER", "raw-json-and-empty-body-dispatch-through-consumer-pipeline")]
    public async Task Dispatch_DeliversRawJsonAndEmptyBodiesThroughTheConfiguredConsumerAsync(DispatchBody body)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var observation = new DispatchObservation();
        await using ServiceProvider provider = new ServiceCollection()
            .AddSingleton(observation)
            .AddViciOneServiceBusTestHarness(configuration =>
            {
                configuration.SetTestTimeouts(timeout, timeout);
                configuration.AddConsumer<DispatchCommandConsumer>();
                configuration.AddConfigureEndpointsCallback((_, endpoint) => endpoint.UseRawJsonSerializer());
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });
        ITestHarness harness = await provider.StartTestHarnessAsync(cancellationToken: TestContext.Current.CancellationToken).WaitAsync(timeout, cancellationToken);

        try
        {
            Guid messageId = NewId.NextGuid();
            string? expectedValue = body == DispatchBody.RawJson ? "dispatcher-value" : null;
            (byte[] bytes, Dictionary<string, object> headers) = Serialize(body, messageId, expectedValue);
            IReceiveEndpointDispatcher<DispatchCommandConsumer> dispatcher =
                provider.GetRequiredService<IReceiveEndpointDispatcher<DispatchCommandConsumer>>();

            await dispatcher.DispatchAsync(bytes, headers, [], cancellationToken).WaitAsync(timeout, cancellationToken);
            DispatchResult result = await observation.Completed.Task.WaitAsync(timeout, cancellationToken);

            Assert.Equal(messageId, result.MessageId);
            Assert.Equal(expectedValue, result.Value);
            Assert.Equal(SystemTextJsonRawMessageSerializer.JsonContentType.ToString(), result.ContentType);
            Assert.Equal(1, observation.InvocationCount);
        }
        finally
        {
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    private static (byte[], Dictionary<string, object>) Serialize(
        DispatchBody body,
        Guid messageId,
        string? value)
    {
        var sendContext = new MessageSendContext<DispatchCommand>(new DispatchCommand { Value = value })
        {
            MessageId = messageId,
        };
        byte[] bytes = body == DispatchBody.Empty
            ? []
            : new SystemTextJsonRawMessageSerializer(ServiceBusMetadataJson.Options).GetMessageBody(sendContext).GetBytes();
        var headers = new Dictionary<string, object>
        {
            [MessageHeaders.ContentType] = SystemTextJsonRawMessageSerializer.JsonContentType,
            [MessageHeaders.MessageId] = messageId,
        };
        headers.Set(sendContext.Headers);
        return (bytes, headers);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum DispatchBody
    {
        RawJson,
        Empty,
    }

    public sealed class DispatchCommand
    {
        public string? Value { get; init; }
    }

    public sealed record DispatchResult(Guid? MessageId, string? Value, string? ContentType);

    public sealed class DispatchObservation
    {
        private int _invocationCount;

        public int InvocationCount => Volatile.Read(ref _invocationCount);

        public TaskCompletionSource<DispatchResult> Completed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Record(DispatchResult result)
        {
            Interlocked.Increment(ref _invocationCount);
            Completed.TrySetResult(result);
        }
    }

    public sealed class DispatchCommandConsumer(DispatchObservation observation) : IConsumer<DispatchCommand>
    {
        public Task ConsumeAsync(ConsumeContext<DispatchCommand> context)
        {
            observation.Record(new DispatchResult(
                context.MessageId,
                context.Message.Value,
                context.Advanced().ReceiveContext.ContentType?.ToString()));
            return Task.CompletedTask;
        }
    }
}
