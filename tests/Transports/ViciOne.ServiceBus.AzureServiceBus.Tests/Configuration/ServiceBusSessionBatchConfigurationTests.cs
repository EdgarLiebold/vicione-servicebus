using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.Configuration;

public sealed class ServiceBusSessionBatchConfigurationTests
{
    [Theory]
    [InlineData(false, 0, 0)]
    [InlineData(false, 3, 7)]
    [InlineData(false, 7, 7)]
    [InlineData(false, 19, 19)]
    [InlineData(true, 0, 0)]
    [InlineData(true, 3, 7)]
    [InlineData(true, 7, 7)]
    [InlineData(true, 19, 19)]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-BATCH", "session-limits-project-to-queue-and-subscription-with-prefetch-boundaries")]
    public void SessionBatching_ProjectsLimitsToBatchAndEndpointWithoutDiscardingPrefetch(
        bool subscription, int initialPrefetch, int expectedPrefetch)
    {
        IConsumerConfigurator<TestConsumer> consumer = CreateConsumer(out RecordingConsumerConfiguratorProxy recording);
        var idleTimeout = TimeSpan.FromSeconds(43);
        var timeLimit = TimeSpan.FromSeconds(2);

        consumer.SetServiceBusSessionBatchOptions(options =>
        {
            options.MessageLimitPerSession = 7;
            options.MaxConcurrentSessions = 3;
            options.SessionIdleTimeout = idleTimeout;
            options.TimeLimit = timeLimit;
            options.TimeLimitStart = BatchTimeLimitStart.FromLast;
        });

        BatchOptions batch = Assert.IsType<BatchOptions>(recording.Batch);
        Assert.Equal(1, recording.OptionsCalls);
        Assert.Equal(7, batch.MessageLimit);
        Assert.Equal(3, batch.ConcurrencyLimit);
        Assert.Equal(timeLimit, batch.TimeLimit);
        Assert.Equal(BatchTimeLimitStart.FromLast, batch.TimeLimitStart);
        Assert.Empty(batch.Validate());

        IReceiveEndpointConfigurator endpoint = subscription
            ? DispatchProxy.Create<IServiceBusSubscriptionEndpointConfigurator, RecordingEndpointProxy>()
            : DispatchProxy.Create<IServiceBusReceiveEndpointConfigurator, RecordingEndpointProxy>();
        var endpointRecording = (RecordingEndpointProxy)(object)endpoint;
        endpoint.PrefetchCount = initialPrefetch;
        endpointRecording.Writes.Clear();

        batch.Configure("orders", endpoint);

        Assert.True(endpointRecording.Value<bool>("RequiresSession"));
        Assert.Equal(3, endpointRecording.Value<int>("MaxConcurrentSessions"));
        Assert.Equal(7, endpointRecording.Value<int>("MaxConcurrentCallsPerSession"));
        Assert.Equal(idleTimeout, endpointRecording.Value<TimeSpan?>("SessionIdleTimeout"));
        Assert.Equal(expectedPrefetch, endpoint.PrefetchCount);
        Assert.Equal(initialPrefetch is > 0 and < 7 ? 5 : 4, endpointRecording.Writes.Count);
        Assert.DoesNotContain(endpointRecording.Writes, name => name == "ConcurrentMessageLimit");
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-BATCH", "caller-options-snapshot-survives-valid-and-invalid-late-mutations")]
    public void SessionBatching_SnapshotsCallerOptionsForEveryEndpointConfiguration(bool subscription, bool invalidMutation)
    {
        IConsumerConfigurator<TestConsumer> consumer = CreateConsumer(out RecordingConsumerConfiguratorProxy recording);
        ServiceBusSessionBatchOptions? retained = null;
        var idleTimeout = TimeSpan.FromSeconds(43);
        var timeLimit = TimeSpan.FromSeconds(2);
        consumer.SetServiceBusSessionBatchOptions(options =>
        {
            retained = options;
            options.MessageLimitPerSession = 7;
            options.MaxConcurrentSessions = 3;
            options.SessionIdleTimeout = idleTimeout;
            options.TimeLimit = timeLimit;
            options.TimeLimitStart = BatchTimeLimitStart.FromLast;
        });

        ServiceBusSessionBatchOptions callerOptions = Assert.IsType<ServiceBusSessionBatchOptions>(retained);
        BatchOptions batch = Assert.IsType<BatchOptions>(recording.Batch);
        callerOptions.MessageLimitPerSession = invalidMutation ? 0 : 13;
        callerOptions.MaxConcurrentSessions = invalidMutation ? 0 : 5;
        callerOptions.SessionIdleTimeout = invalidMutation ? TimeSpan.Zero : TimeSpan.FromSeconds(9);
        callerOptions.TimeLimit = invalidMutation ? TimeSpan.Zero : TimeSpan.FromSeconds(4);
        callerOptions.TimeLimitStart = invalidMutation ? (BatchTimeLimitStart)int.MaxValue : BatchTimeLimitStart.FromFirst;

        Assert.Equal(7, batch.MessageLimit);
        Assert.Equal(3, batch.ConcurrencyLimit);
        Assert.Equal(timeLimit, batch.TimeLimit);
        Assert.Equal(BatchTimeLimitStart.FromLast, batch.TimeLimitStart);
        Assert.Empty(batch.Validate());
        Assert.Equal(1, recording.OptionsCalls);

        for (int endpointIndex = 0; endpointIndex < 2; endpointIndex++)
        {
            IReceiveEndpointConfigurator endpoint = subscription
                ? DispatchProxy.Create<IServiceBusSubscriptionEndpointConfigurator, RecordingEndpointProxy>()
                : DispatchProxy.Create<IServiceBusReceiveEndpointConfigurator, RecordingEndpointProxy>();
            var endpointRecording = (RecordingEndpointProxy)(object)endpoint;
            endpoint.PrefetchCount = 1;

            batch.Configure($"orders-{endpointIndex}", endpoint);

            Assert.True(endpointRecording.Value<bool>("RequiresSession"));
            Assert.Equal(3, endpointRecording.Value<int>("MaxConcurrentSessions"));
            Assert.Equal(7, endpointRecording.Value<int>("MaxConcurrentCallsPerSession"));
            Assert.Equal(idleTimeout, endpointRecording.Value<TimeSpan?>("SessionIdleTimeout"));
            Assert.Equal(7, endpoint.PrefetchCount);

            callerOptions.MessageLimitPerSession = 17;
            callerOptions.MaxConcurrentSessions = 11;
            callerOptions.SessionIdleTimeout = TimeSpan.FromSeconds(19);
            callerOptions.TimeLimit = TimeSpan.FromSeconds(6);
            callerOptions.TimeLimitStart = BatchTimeLimitStart.FromFirst;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-BATCH", "group-key-uses-broker-session-instead-of-reply-session")]
    public void SessionBatching_GroupsByTheBrokerSessionIdentifier()
    {
        IConsumerConfigurator<TestConsumer> consumer = CreateConsumer(out RecordingConsumerConfiguratorProxy recording);
        consumer.SetServiceBusSessionBatchOptions(_ => { });
        BatchOptions batch = Assert.IsType<BatchOptions>(recording.Batch);
        PropertyInfo keyProperty = typeof(BatchOptions).GetProperty("GroupKeyProvider", BindingFlags.Instance | BindingFlags.NonPublic)!;
        object keyProvider = Assert.IsAssignableFrom<object>(keyProperty.GetValue(batch));
        MethodInfo selectKey = keyProvider.GetType().GetMethod("TryGetKey")!;
        TestConsumeContext context = DispatchProxy.Create<TestConsumeContext, SessionContextProxy>();
        var contextRecording = (SessionContextProxy)(object)context;
        contextRecording.BrokerSessionId = "orders-session-47";
        contextRecording.ReplySessionId = "reply-session-99";
        object?[] arguments = [context, null];

        bool grouped = Assert.IsType<bool>(selectKey.Invoke(keyProvider, arguments));

        Assert.True(grouped);
        Assert.Equal("orders-session-47", arguments[1]);
        Assert.Equal(1, contextRecording.PayloadLookups);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SESSION-BATCH", "invalid-options-and-non-service-bus-endpoint-fail-before-mutation")]
    public void SessionBatching_RejectsInvalidOptionsAndOtherEndpointKindsBeforeMutation()
    {
        IConsumerConfigurator<TestConsumer> consumer = CreateConsumer(out RecordingConsumerConfiguratorProxy recording);

        Assert.Equal("consumerConfigurator", Assert.Throws<ArgumentNullException>(() =>
            ServiceBusBatchingExtensions.SetServiceBusSessionBatchOptions<TestConsumer>(null!, _ => { })).ParamName);
        Assert.Equal("configure", Assert.Throws<ArgumentNullException>(() =>
            consumer.SetServiceBusSessionBatchOptions(null!)).ParamName);
        ConfigurationException invalid = Assert.Throws<ConfigurationException>(() =>
            consumer.SetServiceBusSessionBatchOptions(options => options.MessageLimitPerSession = 0));
        Assert.Contains(nameof(ServiceBusSessionBatchOptions.MessageLimitPerSession), invalid.Message, StringComparison.Ordinal);
        Assert.Equal(0, recording.OptionsCalls);

        consumer.SetServiceBusSessionBatchOptions(options => options.MessageLimitPerSession = 7);
        BatchOptions batch = Assert.IsType<BatchOptions>(recording.Batch);
        IReceiveEndpointConfigurator otherEndpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, UnexpectedEndpointProxy>();

        ArgumentException mismatch = Assert.Throws<ArgumentException>(() => batch.Configure("other-provider", otherEndpoint));
        Assert.Equal("configurator", mismatch.ParamName);
        Assert.Contains(nameof(IServiceBusEndpointConfigurator), mismatch.Message, StringComparison.Ordinal);
    }

    private static IConsumerConfigurator<TestConsumer> CreateConsumer(out RecordingConsumerConfiguratorProxy recording)
    {
        IConsumerConfigurator<TestConsumer> consumer = DispatchProxy.Create<IConsumerConfigurator<TestConsumer>, RecordingConsumerConfiguratorProxy>();
        recording = (RecordingConsumerConfiguratorProxy)(object)consumer;
        return consumer;
    }

    public sealed class TestConsumer;

    public interface TestConsumeContext : ConsumeContext<object>, ConsumeContext;

    private class RecordingConsumerConfiguratorProxy : DispatchProxy
    {
        public BatchOptions? Batch { get; private set; }

        public int OptionsCalls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name != nameof(IOptionsSet.Options) || targetMethod.GetGenericArguments().SingleOrDefault() != typeof(BatchOptions)
                || args is not { Length: 1 } || args[0] is not Action<BatchOptions> configure)
                throw new InvalidOperationException($"Unexpected consumer configuration call: {targetMethod?.Name}");

            OptionsCalls++;
            Batch = new BatchOptions();
            configure(Batch);
            return Batch;
        }
    }

    private class RecordingEndpointProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> _values = [];

        public List<string> Writes { get; } = [];

        public T Value<T>(string property) => (T)_values[property]!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_PrefetchCount")
                return _values.TryGetValue("PrefetchCount", out object? value) ? value : 0;
            if (targetMethod?.Name.StartsWith("set_", StringComparison.Ordinal) == true && args is { Length: 1 })
            {
                string property = targetMethod.Name[4..];
                _values[property] = args[0];
                Writes.Add(property);
                return null;
            }

            throw new InvalidOperationException($"Unexpected endpoint configuration call: {targetMethod?.Name}");
        }
    }

    private class UnexpectedEndpointProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The non-ServiceBus endpoint was accessed through {targetMethod?.Name}.");
    }

    private class SessionContextProxy : DispatchProxy
    {
        public string BrokerSessionId { get; set; } = string.Empty;

        public string ReplySessionId { get; set; } = string.Empty;

        public int PayloadLookups { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(PipeContext.TryGetPayload)
                && targetMethod.GetGenericArguments().SingleOrDefault() == typeof(ServiceBusMessageContext)
                && args is { Length: 1 })
            {
                PayloadLookups++;
                ServiceBusMessageContext metadata = DispatchProxy.Create<ServiceBusMessageContext, SessionMetadataProxy>();
                var proxy = (SessionMetadataProxy)(object)metadata;
                proxy.BrokerSessionId = BrokerSessionId;
                proxy.ReplySessionId = ReplySessionId;
                args[0] = metadata;
                return true;
            }

            throw new InvalidOperationException($"Unexpected consume context call: {targetMethod?.Name}");
        }
    }

    private class SessionMetadataProxy : DispatchProxy
    {
        public string BrokerSessionId { get; set; } = string.Empty;

        public string ReplySessionId { get; set; } = string.Empty;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_SessionId" => BrokerSessionId,
            "get_ReplyToSessionId" => ReplySessionId,
            _ => throw new InvalidOperationException($"Unexpected broker metadata call: {targetMethod?.Name}")
        };
    }
}
