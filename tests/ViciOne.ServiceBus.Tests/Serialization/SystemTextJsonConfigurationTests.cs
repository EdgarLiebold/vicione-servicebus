using System.Text.Json;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonPerMessageConfigurationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-PER-MESSAGE-OPTIONS", "replacement-result")]
    public void CallbackReplacement_IsUsedForTheConfiguredMessageType()
    {
        var options = new JsonSerializerOptions();

        options.SetMessageSerializerOptions<ConfiguredMessage>(
            _ => new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        AssertSerializedPropertyName(options, "messageId");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-PER-MESSAGE-OPTIONS", "mutated-result")]
    public void MutatedCallbackInput_IsUsedForTheConfiguredMessageType()
    {
        var options = new JsonSerializerOptions();

        options.SetMessageSerializerOptions<ConfiguredMessage>(callbackOptions =>
        {
            callbackOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            return callbackOptions;
        });

        AssertSerializedPropertyName(options, "messageId");
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-PER-MESSAGE-OPTIONS", "null-result")]
    public void NullCallbackResult_IsRejectedWithoutInstallingAConverter()
    {
        var options = new JsonSerializerOptions();

        ConfigurationException exception = Assert.Throws<ConfigurationException>(
            () => options.SetMessageSerializerOptions<ConfiguredMessage>(_ => null!));

        Assert.Contains("returned null", exception.Message, StringComparison.Ordinal);
        Assert.Empty(options.Converters);
    }

    private static void AssertSerializedPropertyName(JsonSerializerOptions options, string expectedName)
    {
        string json = JsonSerializer.Serialize(new ConfiguredMessage { MessageId = 27 }, options);
        using JsonDocument document = JsonDocument.Parse(json);

        JsonProperty property = Assert.Single(document.RootElement.EnumerateObject());
        Assert.Equal(expectedName, property.Name);
        Assert.Equal(27, property.Value.GetInt32());
    }

    public sealed class ConfiguredMessage
    {
        public int MessageId { get; init; }
    }
}

public sealed class SystemTextJsonIsolationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "independent-configurations")]
    public void IndependentConfigurations_MaterializeDifferentStablePayloadPolicies()
    {
        var snakeCase = new SerializationConfiguration();
        snakeCase.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            return options;
        });
        var kebabCase = new SerializationConfiguration();
        kebabCase.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
            return options;
        });

        ISerialization snakeCollection = snakeCase.CreateSerializerCollection();
        ISerialization kebabCollection = kebabCase.CreateSerializerCollection();

        Assert.Equal("message_id", SinglePropertyName(snakeCollection));
        Assert.Equal("message-id", SinglePropertyName(kebabCollection));
        Assert.Equal("message_id", SinglePropertyName(snakeCollection));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "child-inherits-and-overrides")]
    public void ChildConfiguration_InheritsThenOverridesWithoutMutatingParentOrSibling()
    {
        var parent = new SerializationConfiguration();
        parent.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.WriteIndented = true;
            return options;
        });
        var child = (SerializationConfiguration)parent.CreateSerializationConfiguration();
        var sibling = (SerializationConfiguration)parent.CreateSerializationConfiguration();
        bool? inheritedWriteIndented = null;
        JsonNamingPolicy? inheritedNamingPolicy = null;
        child.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            inheritedWriteIndented = options.WriteIndented;
            inheritedNamingPolicy = options.PropertyNamingPolicy;
            options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
            options.WriteIndented = false;
            return options;
        });

        ISerialization childCollection = child.CreateSerializerCollection();
        ISerialization parentCollection = parent.CreateSerializerCollection();
        ISerialization siblingCollection = sibling.CreateSerializerCollection();

        Assert.True(inheritedWriteIndented);
        Assert.Equal(JsonNamingPolicy.SnakeCaseLower, inheritedNamingPolicy);
        Assert.Equal("message-id", SinglePropertyName(childCollection));
        Assert.Equal("message_id", SinglePropertyName(parentCollection));
        Assert.Equal("message_id", SinglePropertyName(siblingCollection));
        Assert.DoesNotContain('\n', SerializeObject(childCollection));
        Assert.Contains('\n', SerializeObject(parentCollection));
        Assert.Contains('\n', SerializeObject(siblingCollection));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "envelope-and-raw-share-policy")]
    public void EnvelopeAndRawFactories_BindTheSameLocalPayloadPolicy()
    {
        var configuration = new SerializationConfiguration();
        configuration.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            return options;
        });
        var rawFactory = new SystemTextJsonRawMessageSerializerFactory(RawSerializerOptions.AnyMessageType);
        configuration.AddSerializer(rawFactory, isSerializer: false);
        configuration.AddDeserializer(rawFactory);
        ISerialization serializers = configuration.CreateSerializerCollection();
        IMessageSerializer envelope = serializers.GetMessageSerializer();
        Assert.True(serializers.TryGetMessageSerializer(SystemTextJsonRawMessageSerializer.JsonContentType, out IMessageSerializer? raw));
        Assert.True(serializers.TryGetMessageDeserializer(SystemTextJsonMessageSerializer.JsonContentType, out IMessageDeserializer? envelopeDeserializer));
        Assert.True(serializers.TryGetMessageDeserializer(SystemTextJsonRawMessageSerializer.JsonContentType, out IMessageDeserializer? rawDeserializer));
        var message = new ConfiguredMessage { MessageId = 27 };
        var envelopeContext = new MessageSendContext<ConfiguredMessage>(message) { Serializer = envelope };
        var rawContext = new MessageSendContext<ConfiguredMessage>(message) { Serializer = raw };

        using JsonDocument envelopeDocument = JsonDocument.Parse(envelope.GetMessageBody(envelopeContext).GetBytes());
        using JsonDocument rawDocument = JsonDocument.Parse(raw.GetMessageBody(rawContext).GetBytes());

        Assert.Equal(27, envelopeDocument.RootElement.GetProperty("message").GetProperty("message_id").GetInt32());
        Assert.Equal(27, rawDocument.RootElement.GetProperty("message_id").GetInt32());
        Assert.False(envelopeDocument.RootElement.GetProperty("message").TryGetProperty("messageId", out _));
        Assert.False(rawDocument.RootElement.TryGetProperty("messageId", out _));
        Assert.Same(envelope, envelopeDeserializer);
        Assert.Same(raw, rawDeserializer);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "replacement-defensive-snapshot")]
    public void ReplacementOptions_AreDefensivelyFrozenAtMaterialization()
    {
        var replacement = SystemTextJsonSerializerOptions.CreateDefault();
        replacement.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        var configuration = new SerializationConfiguration();
        configuration.ConfigureSystemTextJsonSerializerOptions(_ => replacement);

        ISerialization collection = configuration.CreateSerializerCollection();
        replacement.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;

        Assert.Equal("message_id", SinglePropertyName(collection));
        Assert.Equal(JsonNamingPolicy.KebabCaseLower, replacement.PropertyNamingPolicy);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "materialization-closes-configuration")]
    public void Materialization_ClosesEveryConfigurationMutationPath()
    {
        var configuration = new SerializationConfiguration();
        _ = configuration.CreateSerializerCollection();

        ConfigurationException configure = Assert.Throws<ConfigurationException>(() =>
            configuration.ConfigureSystemTextJsonSerializerOptions(options => options));
        ConfigurationException clear = Assert.Throws<ConfigurationException>(configuration.Clear);
        ConfigurationException serializer = Assert.Throws<ConfigurationException>(() =>
            configuration.AddSerializer(new SystemTextJsonMessageSerializerFactory()));

        Assert.Equal("The serializer collection was already created.", configure.Message);
        Assert.Equal(configure.Message, clear.Message);
        Assert.Equal(configure.Message, serializer.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "null-transform-result")]
    public void NullTransformResult_FailsDuringMaterializationWithoutPublishingACollection()
    {
        var configuration = new SerializationConfiguration();
        configuration.ConfigureSystemTextJsonSerializerOptions(_ => null!);

        ConfigurationException first = Assert.Throws<ConfigurationException>(configuration.CreateSerializerCollection);
        ConfigurationException second = Assert.Throws<ConfigurationException>(configuration.CreateSerializerCollection);

        Assert.Contains("returned null", first.Message, StringComparison.Ordinal);
        Assert.Same(first, second);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "immutable-construction-boundary")]
    public void MutableOptions_AreRejectedByEnvelopeAndRawSerializerConstructors()
    {
        var options = SystemTextJsonSerializerOptions.CreateDefault();

        ArgumentException envelope = Assert.Throws<ArgumentException>(() => new SystemTextJsonMessageSerializer(options));
        ArgumentException raw = Assert.Throws<ArgumentException>(() => new SystemTextJsonRawMessageSerializer(options));

        Assert.Equal("options", envelope.ParamName);
        Assert.Equal("serializerOptions", raw.ParamName);
        Assert.Contains("immutable", envelope.Message, StringComparison.Ordinal);
        Assert.Contains("immutable", raw.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-ISOLATION", "factory-binding-required")]
    public void JsonFactories_RejectDirectUseBeforeConfigurationBinding()
    {
        var envelopeFactory = new SystemTextJsonMessageSerializerFactory();
        var rawFactory = new SystemTextJsonRawMessageSerializerFactory();

        ConfigurationException envelopeSerializer = Assert.Throws<ConfigurationException>(envelopeFactory.CreateSerializer);
        ConfigurationException envelopeDeserializer = Assert.Throws<ConfigurationException>(envelopeFactory.CreateDeserializer);
        ConfigurationException rawSerializer = Assert.Throws<ConfigurationException>(rawFactory.CreateSerializer);
        ConfigurationException rawDeserializer = Assert.Throws<ConfigurationException>(rawFactory.CreateDeserializer);

        Assert.Contains("must be bound", envelopeSerializer.Message, StringComparison.Ordinal);
        Assert.Equal(envelopeSerializer.Message, envelopeDeserializer.Message);
        Assert.Contains("must be bound", rawSerializer.Message, StringComparison.Ordinal);
        Assert.Equal(rawSerializer.Message, rawDeserializer.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-METADATA-JSON-STABILITY", "payload-policy-does-not-leak")]
    public void PayloadPolicy_CannotAlterTheStableMetadataCodec()
    {
        var metadata = new ConfiguredMessage { MessageId = 27 };
        string before = ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(metadata).GetString();
        var configuration = new SerializationConfiguration();
        configuration.ConfigureSystemTextJsonSerializerOptions(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower;
            options.WriteIndented = true;
            return options;
        });

        ISerialization payload = configuration.CreateSerializerCollection();
        string after = ServiceBusMetadataJson.ObjectDeserializer.SerializeObject(metadata).GetString();

        Assert.Equal(before, after);
        using JsonDocument metadataDocument = JsonDocument.Parse(after);
        Assert.Equal(27, metadataDocument.RootElement.GetProperty("messageId").GetInt32());
        Assert.Equal("message-id", SinglePropertyName(payload));
    }

    private static string SinglePropertyName(ISerialization serialization)
    {
        using JsonDocument document = JsonDocument.Parse(SerializeObject(serialization));
        return Assert.Single(document.RootElement.EnumerateObject()).Name;
    }

    private static string SerializeObject(ISerialization serialization) =>
        Assert.IsAssignableFrom<IObjectDeserializer>(serialization.GetMessageSerializer())
            .SerializeObject(new ConfiguredMessage { MessageId = 27 })
            .GetString();

    public sealed class ConfiguredMessage
    {
        public int MessageId { get; init; }
    }
}

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
        harness.OnConfigureInMemoryBus += configuration =>
            configuration.ConfigureJsonSerializerOptions(options =>
            {
                options.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
                return options;
            });
        harness.OnConfigureInMemoryReceiveEndpoint += endpoint =>
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
