using System.Reflection;
using ViciOne.ServiceBus.Context;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Contexts;

public sealed class OutgoingOptionsPipeTests
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(17);
    private static readonly Guid CorrelationId = Guid.Parse("11000000-0000-0000-0000-000000000001");
    private static readonly Guid ConversationId = Guid.Parse("22000000-0000-0000-0000-000000000002");
    private static readonly Guid MessageId = Guid.Parse("33000000-0000-0000-0000-000000000003");
    private static readonly Guid RequestId = Guid.Parse("44000000-0000-0000-0000-000000000004");

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-SEND-OPTIONS", "all-fields-reach-send-context")]
    public async Task SendOptionsPipe_AppliesEveryConfiguredFieldExactlyAsync()
    {
        SendContext<TestMessage> context = CreateContext<SendContext<TestMessage>>(supportsPartitionKey: true, out RecordingSendContextProxy recording);

        await new SendOptionsPipe<TestMessage>(CreateSendOptions()).SendAsync(context);

        AssertAllConfiguredFields(recording);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-PUBLISH-OPTIONS", "all-fields-reach-publish-context")]
    public async Task PublishOptionsPipe_AppliesEveryConfiguredFieldExactlyAsync()
    {
        PublishContext<TestMessage> context = CreateContext<PublishContext<TestMessage>>(supportsPartitionKey: true, out RecordingSendContextProxy recording);

        await new PublishOptionsPipe<TestMessage>(CreatePublishOptions()).SendAsync(context);

        AssertAllConfiguredFields(recording);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-SCHEDULE-OPTIONS", "all-fields-reach-scheduled-send-context")]
    public async Task ScheduleOptionsPipe_AppliesEveryConfiguredFieldExactlyAsync()
    {
        SendContext<TestMessage> context = CreateContext<SendContext<TestMessage>>(supportsPartitionKey: true, out RecordingSendContextProxy recording);

        await new ScheduleOptionsPipe<TestMessage>(CreateScheduleOptions()).SendAsync(context);

        AssertAllConfiguredFields(recording);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-OPTIONS-CAPABILITY", "unset-fields-preserve-existing-context")]
    public async Task EmptyOptions_PreserveExistingMetadataAndPartitionKeyAsync()
    {
        SendContext<TestMessage> context = CreateContext<SendContext<TestMessage>>(supportsPartitionKey: true, out RecordingSendContextProxy recording);
        TimeSpan originalLifetime = TimeSpan.FromSeconds(9);
        Guid originalCorrelationId = Guid.Parse("55000000-0000-0000-0000-000000000005");
        Guid originalConversationId = Guid.Parse("66000000-0000-0000-0000-000000000006");
        Guid originalMessageId = Guid.Parse("77000000-0000-0000-0000-000000000007");
        Guid originalRequestId = Guid.Parse("88000000-0000-0000-0000-000000000008");
        recording.SetInitial(nameof(SendContext.TimeToLive), originalLifetime);
        recording.SetInitial(nameof(SendContext.CorrelationId), originalCorrelationId);
        recording.SetInitial(nameof(SendContext.ConversationId), originalConversationId);
        recording.SetInitial(nameof(SendContext.MessageId), originalMessageId);
        recording.SetInitial(nameof(SendContext.RequestId), originalRequestId);
        recording.PartitionKey!.PartitionKey = "original";

        await new SendOptionsPipe<TestMessage>(new SendOptions()).SendAsync(context);

        Assert.Equal(originalLifetime, recording.Get<TimeSpan?>(nameof(SendContext.TimeToLive)));
        Assert.Equal(originalCorrelationId, recording.Get<Guid?>(nameof(SendContext.CorrelationId)));
        Assert.Equal(originalConversationId, recording.Get<Guid?>(nameof(SendContext.ConversationId)));
        Assert.Equal(originalMessageId, recording.Get<Guid?>(nameof(SendContext.MessageId)));
        Assert.Equal(originalRequestId, recording.Get<Guid?>(nameof(SendContext.RequestId)));
        Assert.Equal("original", recording.PartitionKey.PartitionKey);
        Assert.Empty(recording.Headers.Values);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-OPTIONS-CAPABILITY", "unsupported-partition-key-fails-explicitly")]
    public async Task ConfiguredPartitionKey_WithoutTransportCapabilityThrowsAsync()
    {
        SendContext<TestMessage> context = CreateContext<SendContext<TestMessage>>(supportsPartitionKey: false, out RecordingSendContextProxy recording);
        var options = new SendOptions { PartitionKey = "tenant-42" };

        NotSupportedException exception = await Assert.ThrowsAsync<NotSupportedException>(
            () => new SendOptionsPipe<TestMessage>(options).SendAsync(context));

        Assert.Contains("partition key", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(recording.Headers.Values);
        Assert.Equal([nameof(SendContext.Headers)], recording.Properties.Keys);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-APPLICATION-OPTIONS-CAPABILITY", "null-header-collection-fails-explicitly")]
    public async Task NullHeaderCollection_ThrowsBeforeMutatingTheContextAsync()
    {
        SendContext<TestMessage> context = CreateContext<SendContext<TestMessage>>(supportsPartitionKey: true, out RecordingSendContextProxy recording);
        var options = new SendOptions
        {
            Headers = null!,
            CorrelationId = CorrelationId,
        };

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => new SendOptionsPipe<TestMessage>(options).SendAsync(context));

        Assert.Empty(recording.Headers.Values);
        Assert.Null(recording.Get<Guid?>(nameof(SendContext.CorrelationId)));
    }

    private static SendOptions CreateSendOptions() => new()
    {
        Headers = Headers(),
        TimeToLive = Lifetime,
        CorrelationId = CorrelationId,
        ConversationId = ConversationId,
        MessageId = MessageId,
        RequestId = RequestId,
        PartitionKey = "tenant-42",
    };

    private static PublishOptions CreatePublishOptions() => new()
    {
        Headers = Headers(),
        TimeToLive = Lifetime,
        CorrelationId = CorrelationId,
        ConversationId = ConversationId,
        MessageId = MessageId,
        RequestId = RequestId,
        PartitionKey = "tenant-42",
    };

    private static ScheduleOptions CreateScheduleOptions() => new()
    {
        Headers = Headers(),
        TimeToLive = Lifetime,
        CorrelationId = CorrelationId,
        ConversationId = ConversationId,
        MessageId = MessageId,
        RequestId = RequestId,
        PartitionKey = "tenant-42",
    };

    private static Dictionary<string, object?> Headers() => new()
    {
        ["tenant"] = "north",
        ["attempt"] = 3,
        ["optional"] = null,
    };

    private static void AssertAllConfiguredFields(RecordingSendContextProxy recording)
    {
        Assert.Equal(Lifetime, recording.Get<TimeSpan?>(nameof(SendContext.TimeToLive)));
        Assert.Equal(CorrelationId, recording.Get<Guid?>(nameof(SendContext.CorrelationId)));
        Assert.Equal(ConversationId, recording.Get<Guid?>(nameof(SendContext.ConversationId)));
        Assert.Equal(MessageId, recording.Get<Guid?>(nameof(SendContext.MessageId)));
        Assert.Equal(RequestId, recording.Get<Guid?>(nameof(SendContext.RequestId)));
        Assert.Equal("tenant-42", Assert.IsType<RecordingPartitionKeyContext>(recording.PartitionKey).PartitionKey);
        Assert.Equal(3, recording.Headers.Values.Count);
        Assert.Equal("north", recording.Headers.Values["tenant"]);
        Assert.Equal(3, recording.Headers.Values["attempt"]);
        Assert.Null(recording.Headers.Values["optional"]);
    }

    private static TContext CreateContext<TContext>(bool supportsPartitionKey, out RecordingSendContextProxy recording)
        where TContext : class
    {
        TContext context = DispatchProxy.Create<TContext, RecordingSendContextProxy>();
        recording = (RecordingSendContextProxy)(object)context;
        recording.Initialize(supportsPartitionKey);
        return context;
    }

    private sealed record TestMessage(string Value);

    private sealed class RecordingPartitionKeyContext : PartitionKeySendContext
    {
        public string? PartitionKey { get; set; }
    }

    private class RecordingSendContextProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> _properties = [];

        public IReadOnlyDictionary<string, object?> Properties => _properties;

        public RecordingHeadersProxy Headers { get; private set; } = null!;

        public RecordingPartitionKeyContext? PartitionKey { get; private set; }

        public void Initialize(bool supportsPartitionKey)
        {
            SendHeaders headers = DispatchProxy.Create<SendHeaders, RecordingHeadersProxy>();
            Headers = (RecordingHeadersProxy)(object)headers;
            Headers.Initialize();
            _properties[nameof(SendContext.Headers)] = headers;
            PartitionKey = supportsPartitionKey ? new RecordingPartitionKeyContext() : null;
        }

        public void SetInitial(string propertyName, object value) => _properties[propertyName] = value;

        public T Get<T>(string propertyName) => (T)_properties.GetValueOrDefault(propertyName)!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name.StartsWith("set_", StringComparison.Ordinal))
            {
                _properties[targetMethod.Name[4..]] = args![0];
                return null;
            }

            if (targetMethod.Name.StartsWith("get_", StringComparison.Ordinal))
            {
                return _properties.TryGetValue(targetMethod.Name[4..], out object? value)
                    ? value
                    : DefaultValue(targetMethod.ReturnType);
            }

            if (targetMethod.Name == nameof(PipeContext.TryGetPayload))
            {
                bool supported = targetMethod.GetGenericArguments()[0] == typeof(PartitionKeySendContext)
                    && PartitionKey is not null;
                args![0] = supported ? PartitionKey : null;
                return supported;
            }

            if (targetMethod.Name == nameof(PipeContext.HasPayloadType))
                return args![0] as Type == typeof(PartitionKeySendContext) && PartitionKey is not null;

            throw new NotSupportedException(targetMethod.Name);
        }

        private static object? DefaultValue(Type type) => type.IsValueType ? Activator.CreateInstance(type) : null;
    }

    private class RecordingHeadersProxy : DispatchProxy
    {
        public Dictionary<string, object?> Values { get; private set; } = null!;

        public void Initialize() => Values = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name == nameof(SendHeaders.Set))
            {
                Values[(string)args![0]!] = args[1];
                return null;
            }

            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
