namespace ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.SqlServer;

using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.SqlTransport.SqlServer.LocalIntegration.Tests.Infrastructure;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

public sealed class SqlServerSerializationAndRequestTests
{
    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0091", "sqlserver-native-owner")]
    public async Task JsonExtensionData_RoundTripsStringAndNumberForElementAndObjectDictionaries()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "json-extension-data",
            cancellationToken);
        string queueName = fixture.Name("json-input");
        var elementReceived = NewObservation<ConsumeContext<WithElement>>();
        var objectReceived = NewObservation<ConsumeContext<WithObject>>();
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ConfigureJsonSerializerOptions(options =>
            {
                options.SetMessageSerializerOptions<WithElement>();
                options.SetMessageSerializerOptions<WithObject>();
                return options;
            });
            configurator.ReceiveEndpoint(queueName, endpoint =>
            {
                endpoint.Handler<WithElement>(context =>
                {
                    elementReceived.TrySetResult(context);
                    return Task.CompletedTask;
                });
                endpoint.Handler<WithObject>(context =>
                {
                    objectReceived.TrySetResult(context);
                    return Task.CompletedTask;
                });
            });
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            await bus.Publish(
                    new WithElement
                    {
                        Extra = new Dictionary<string, JsonElement>
                        {
                            ["text"] = JsonSerializer.SerializeToElement("Value"),
                            ["number"] = JsonSerializer.SerializeToElement(2),
                        },
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            await bus.Publish(
                    new WithObject
                    {
                        Extra = new Dictionary<string, object>
                        {
                            ["text"] = "Value",
                            ["number"] = 2,
                        },
                    },
                    cancellationToken)
                .WaitAsync(fixture.OperationTimeout, cancellationToken);
            WithElement element = (await elementReceived.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken)).Message;
            WithObject boxed = (await objectReceived.Task
                .WaitAsync(fixture.OperationTimeout, cancellationToken)).Message;

            Assert.Equal(["number", "text"], element.Extra.Keys.Order(StringComparer.Ordinal));
            Assert.Equal("Value", element.Extra["text"].GetString());
            Assert.Equal(2, element.Extra["number"].GetInt32());
            Assert.Equal(["number", "text"], boxed.Extra.Keys.Order(StringComparer.Ordinal));
            JsonElement boxedText = Assert.IsType<JsonElement>(boxed.Extra["text"]);
            JsonElement boxedNumber = Assert.IsType<JsonElement>(boxed.Extra["number"]);
            Assert.Equal("Value", boxedText.GetString());
            Assert.Equal(2, boxedNumber.GetInt32());
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    [Fact]
    [RequirementCoverage("OBL-R0-SQL-0101", "sqlserver-native-owner")]
    public async Task RequestClient_FiveSequentialRequestsReturnTheirExactResponses()
    {
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        await using SqlServerTestDatabase fixture = await SqlServerTestDatabase.CreateAsync(
            "request-response",
            cancellationToken);
        string queueName = fixture.Name("request-input");
        IBusControl bus = SqlBusFactory.Create(configurator =>
        {
            fixture.ConfigureHost(configurator);
            configurator.ReceiveEndpoint(queueName, endpoint =>
                endpoint.Handler<RequestMessage>(context => context.RespondAsync(
                    new ResponseMessage(context.Message.Sequence, context.Message.CorrelationId))));
        });
        bool started = false;

        try
        {
            await bus.StartAsync(cancellationToken).WaitAsync(fixture.OperationTimeout, cancellationToken);
            started = true;
            IRequestClient<RequestMessage> client = bus.CreateRequestClient<RequestMessage>(
                new Uri($"queue:{queueName}"),
                RequestTimeout.After(ms: checked((int)fixture.OperationTimeout.TotalMilliseconds)));
            var observed = new List<(int Sequence, Guid CorrelationId)>();
            for (var sequence = 0; sequence < 5; sequence++)
            {
                var request = new RequestMessage(sequence, Guid.NewGuid());
                Response<ResponseMessage> response = await client.GetResponse<ResponseMessage>(request, cancellationToken)
                    .WaitAsync(fixture.OperationTimeout, cancellationToken);
                observed.Add((response.Message.Sequence, response.Message.CorrelationId));
            }

            Assert.Equal(Enumerable.Range(0, 5), observed.Select(item => item.Sequence));
            Assert.Equal(5, observed.Select(item => item.CorrelationId).Distinct().Count());
        }
        finally
        {
            if (started)
                await bus.StopAsync(CancellationToken.None).WaitAsync(fixture.OperationTimeout, CancellationToken.None);
        }
    }

    private static TaskCompletionSource<T> NewObservation<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed record RequestMessage(int Sequence, Guid CorrelationId);
    private sealed record ResponseMessage(int Sequence, Guid CorrelationId);

    private sealed record WithObject
    {
        [JsonExtensionData]
        public Dictionary<string, object> Extra { get; init; } = new(StringComparer.Ordinal);
    }

    private sealed record WithElement
    {
        [JsonExtensionData]
        public Dictionary<string, JsonElement> Extra { get; init; } = new(StringComparer.Ordinal);
    }
}
