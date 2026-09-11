using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Advanced.Contexts;

public sealed class RetryContextExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-METADATA", "retry-payload-counters")]
    public void RetryCounters_ReturnPayloadValuesOrZeroWhenNoRetryIsActive()
    {
        ConsumeContext withoutRetry = CreateContext();
        ConsumeContext withRetry = CreateContext();
        withRetry.GetOrAddPayload<ConsumeRetryContext>(() => new TestConsumeRetryContext(3, 2));

        Assert.Equal(0, withoutRetry.GetRetryAttempt());
        Assert.Equal(0, withoutRetry.GetRetryCount());
        Assert.Equal(3, withRetry.GetRetryAttempt());
        Assert.Equal(2, withRetry.GetRetryCount());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-METADATA", "redelivery-header-precedence")]
    public void RedeliveryCount_PrefersEnvelopeMetadataThenTransportMetadataThenZero()
    {
        ConsumeContext withoutCount = CreateContext();
        ConsumeContext withTransportCount = CreateContext(
            transportHeaders: new HeaderCollection((MessageHeaders.RedeliveryCount, 4)));
        ConsumeContext withBothCounts = CreateContext(
            headers: new HeaderCollection((MessageHeaders.RedeliveryCount, 7)),
            transportHeaders: new HeaderCollection((MessageHeaders.RedeliveryCount, 4)));

        Assert.Equal(0, withoutCount.GetRedeliveryCount());
        Assert.Equal(4, withTransportCount.GetRedeliveryCount());
        Assert.Equal(7, withBothCounts.GetRedeliveryCount());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RETRY-METADATA", "required-context-boundaries")]
    public void RetryMetadataReaders_RejectAMissingConsumeContext()
    {
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => RetryContextExtensions.GetRetryAttempt(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => RetryContextExtensions.GetRetryCount(null!)).ParamName);
        Assert.Equal("context", Assert.Throws<ArgumentNullException>(() => RetryContextExtensions.GetRedeliveryCount(null!)).ParamName);
    }

    private static ConsumeContext CreateContext(Headers? headers = null, Headers? transportHeaders = null)
    {
        ReceiveContext receiveContext = DispatchProxy.Create<ReceiveContext, ContextProxy>();
        ((ContextProxy)(object)receiveContext).Set(nameof(ReceiveContext.TransportHeaders), transportHeaders ?? new HeaderCollection());
        ConsumeContext context = DispatchProxy.Create<ConsumeContext, ContextProxy>();
        var state = (ContextProxy)(object)context;
        state.Set(nameof(ConsumeContext.Headers), headers ?? new HeaderCollection());
        state.Set(nameof(ConsumeContext.ReceiveContext), receiveContext);
        return context;
    }

    private class ContextProxy : DispatchProxy
    {
        private readonly Dictionary<string, object?> _properties = [];
        private readonly List<object> _payloads = [];

        public void Set(string name, object? value) => _properties[name] = value;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            if (targetMethod.Name.StartsWith("get_", StringComparison.Ordinal))
                return _properties[targetMethod.Name[4..]];

            Type? payloadType = targetMethod.IsGenericMethod ? targetMethod.GetGenericArguments()[0] : null;
            switch (targetMethod.Name)
            {
                case nameof(PipeContext.TryGetPayload):
                    object? payload = _payloads.LastOrDefault(payloadType!.IsInstanceOfType);
                    args![0] = payload;
                    return payload is not null;
                case nameof(PipeContext.GetOrAddPayload):
                    object? existing = _payloads.LastOrDefault(payloadType!.IsInstanceOfType);
                    if (existing is not null)
                        return existing;
                    object added = ((Delegate)args![0]!).DynamicInvoke()
                        ?? throw new InvalidOperationException("The payload factory returned null.");
                    _payloads.Add(added);
                    return added;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }

    private sealed class TestConsumeRetryContext(int retryAttempt, int retryCount) : ConsumeRetryContext
    {
        public int RetryAttempt => retryAttempt;
        public int RetryCount => retryCount;

        public TContext CreateNext<TContext>(RetryContext retryContext)
            where TContext : class, ConsumeRetryContext => throw new NotSupportedException();

        public Task NotifyPendingFaultsAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class HeaderCollection(params (string Key, object Value)[] values) : Headers
    {
        private readonly Dictionary<string, object> _values = values.ToDictionary(pair => pair.Key, pair => pair.Value);

        public IEnumerable<KeyValuePair<string, object>> GetAll() => _values;

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value) => _values.TryGetValue(key, out value);

        public TValue? Get<TValue>(string key, TValue? defaultValue = default)
            where TValue : class => TryGetHeader(key, out object? value) && value is TValue typed ? typed : defaultValue;

        public TValue? Get<TValue>(string key, TValue? defaultValue = default)
            where TValue : struct => TryGetHeader(key, out object? value) && value is TValue typed ? typed : defaultValue;

        public IEnumerator<HeaderValue> GetEnumerator() =>
            _values.Select(pair => new HeaderValue(pair)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
