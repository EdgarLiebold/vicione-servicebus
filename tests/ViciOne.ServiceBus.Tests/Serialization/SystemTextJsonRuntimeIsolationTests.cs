using System.Text.Json;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonRuntimeIsolationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "simultaneous-bus-runtime")]
    public async Task SimultaneousBuses_KeepTheirOwnPayloadPoliciesForEverySendAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        IBusControl snakeBus = CreateBus(JsonNamingPolicy.SnakeCaseLower);
        IBusControl kebabBus = CreateBus(JsonNamingPolicy.KebabCaseLower);
        var snakeObserver = new SerializedBodyObserver<RuntimeMessage>();
        var kebabObserver = new SerializedBodyObserver<RuntimeMessage>();
        using ConnectHandle snakeHandle = snakeBus.ConnectPublishObserver(snakeObserver);
        using ConnectHandle kebabHandle = kebabBus.ConnectPublishObserver(kebabObserver);

        await Task.WhenAll(snakeBus.StartAsync(cancellationToken), kebabBus.StartAsync(cancellationToken))
            .WaitAsync(timeout, cancellationToken);

        try
        {
            await snakeBus.PublishAsync(new RuntimeMessage { MessageId = 11 }, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await kebabBus.PublishAsync(new RuntimeMessage { MessageId = 22 }, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await snakeBus.PublishAsync(new RuntimeMessage { MessageId = 33 }, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await Task.WhenAll(snakeBus.StopAsync(CancellationToken.None), kebabBus.StopAsync(CancellationToken.None))
                .WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(["message_id", "message_id"], snakeObserver.PayloadPropertyNames);
        Assert.Equal([11, 33], snakeObserver.PayloadValues);
        Assert.Equal(["message-id"], kebabObserver.PayloadPropertyNames);
        Assert.Equal([22], kebabObserver.PayloadValues);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "endpoint-runtime-override")]
    public async Task ReceiveEndpointOverride_AppliesOnlyToMessagesPublishedFromThatEndpointAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var handled = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var harness = new InMemoryTestHarness($"json-isolation-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.BeginTestScope();
        harness.InMemoryBusConfiguring += configuration =>
            configuration.ConfigureJsonSerializerOptions(options =>
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                return options;
            });
        harness.InMemoryReceiveEndpointConfiguring += endpoint =>
        {
            endpoint.ConfigureJsonSerializerOptions(options =>
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
                return options;
            });
            endpoint.Handler<EndpointTrigger>(async context =>
            {
                try
                {
                    await context.Advanced().PublishAsync(new EndpointResult { MessageId = 27 }, context.CancellationToken);
                    handled.TrySetResult();
                }
                catch (Exception exception)
                {
                    handled.TrySetException(exception);
                    throw;
                }
            });
        };
        var resultObserver = new SerializedBodyObserver<EndpointResult>();
        var busObserver = new SerializedBodyObserver<RuntimeMessage>();

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        using ConnectHandle resultHandle = harness.Bus.ConnectPublishObserver(resultObserver);
        using ConnectHandle busHandle = harness.Bus.ConnectPublishObserver(busObserver);
        JsonSerializerOptions endpointOptions = SystemTextJsonSerializerOptions.CreateDefault();
        endpointOptions.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
        endpointOptions.MakeReadOnly();
        var endpointSerializer = new SystemTextJsonMessageSerializer(endpointOptions);

        try
        {
            await harness.InputQueueSendEndpoint.SendAsync(
                    new EndpointTrigger(),
                    context => context.Serializer = endpointSerializer,
                    cancellationToken)
                .WaitAsync(timeout, cancellationToken);
            await handled.Task.WaitAsync(timeout, cancellationToken);
            await harness.Bus.PublishAsync(new RuntimeMessage { MessageId = 33 }, cancellationToken)
                .WaitAsync(timeout, cancellationToken);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Equal(["message-id"], resultObserver.PayloadPropertyNames);
        Assert.Equal([27], resultObserver.PayloadValues);
        Assert.Equal(["message_id"], busObserver.PayloadPropertyNames);
        Assert.Equal([33], busObserver.PayloadValues);
    }

    private static IBusControl CreateBus(JsonNamingPolicy namingPolicy) =>
        Bus.Factory.CreateUsingInMemory(configuration =>
            configuration.ConfigureJsonSerializerOptions(options =>
            {
                options.PropertyNamingPolicy = namingPolicy;
                return options;
            }));

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public sealed class RuntimeMessage
    {
        public int MessageId { get; init; }
    }

    public sealed class EndpointTrigger
    {
        [System.Text.Json.Serialization.JsonPropertyName("marker")]
        public int MarkerValue { get; init; } = 1;
    }

    public sealed class EndpointResult
    {
        public int MessageId { get; init; }
    }

    private sealed class SerializedBodyObserver<TMessage> : IPublishObserver
        where TMessage : class
    {
        readonly List<string> _payloadPropertyNames = [];
        readonly List<int> _payloadValues = [];

        public string[] PayloadPropertyNames
        {
            get
            {
                lock (_payloadPropertyNames)
                    return [.. _payloadPropertyNames];
            }
        }

        public int[] PayloadValues
        {
            get
            {
                lock (_payloadPropertyNames)
                    return [.. _payloadValues];
            }
        }

        public Task PrePublishAsync<T>(PublishContext<T> context)
            where T : class
        {
            if (typeof(T) != typeof(TMessage))
                return Task.CompletedTask;

            using JsonDocument document = JsonDocument.Parse(context.Serializer.GetMessageBody(context).GetBytes());
            JsonProperty property = Assert.Single(document.RootElement.GetProperty("message").EnumerateObject());

            lock (_payloadPropertyNames)
            {
                _payloadPropertyNames.Add(property.Name);
                _payloadValues.Add(property.Value.GetInt32());
            }

            return Task.CompletedTask;
        }

        public Task PostPublishAsync<T>(PublishContext<T> context)
            where T : class => Task.CompletedTask;

        public Task PublishFaultAsync<T>(PublishContext<T> context, Exception exception)
            where T : class => Task.CompletedTask;
    }
}
