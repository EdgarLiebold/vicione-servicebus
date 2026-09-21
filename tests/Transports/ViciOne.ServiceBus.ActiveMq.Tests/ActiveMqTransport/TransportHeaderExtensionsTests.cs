using System.Globalization;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Apache.NMS;
using Apache.NMS.Util;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class TransportHeaderExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "native-and-provider-owned-values-have-precedence")]
    public void SetHeaders_PreservesNativeValuesAndSkipsProviderOwnedOrUnsupportedHeaders()
    {
        var headers = new DictionarySendHeaders();
        headers.Set("existing", "application");
        headers.Set("unsupported", new object());
        headers.Set("unsupported-bytes", new byte[] { 1, 2, 3 });
        headers.Set(MessageHeaders.TransportMessageId, "provider-owned");
        IPrimitiveMap properties = PrimitiveMapProbe.Create(out PrimitiveMapProbe probe);
        probe.Values["existing"] = "native";

        properties.SetHeaders(headers);

        Assert.Equal("native", probe.Values["existing"]);
        Assert.DoesNotContain("unsupported", probe.Values.Keys);
        Assert.DoesNotContain("unsupported-bytes", probe.Values.Keys);
        Assert.DoesNotContain(MessageHeaders.TransportMessageId, probe.Values.Keys);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "temporal-values-preserve-their-instants")]
    public void SetHeaders_ProjectsEveryDateTimeKindAsUnixMilliseconds()
    {
        DateTimeOffset instant = new(2026, 9, 21, 12, 34, 56, 789, TimeSpan.Zero);
        DateTime local = instant.LocalDateTime;
        DateTime unspecified = DateTime.SpecifyKind(instant.UtcDateTime, DateTimeKind.Unspecified);
        var headers = new DictionarySendHeaders();
        headers.Set("offset", instant);
        headers.Set("local", local);
        headers.Set("utc", instant.UtcDateTime);
        headers.Set("unspecified", unspecified);
        IPrimitiveMap properties = PrimitiveMapProbe.Create(out PrimitiveMapProbe probe);

        properties.SetHeaders(headers);

        Assert.Equal(instant.ToUnixTimeMilliseconds(), probe.Values["offset"]);
        Assert.Equal(new DateTimeOffset(local).ToUniversalTime().ToUnixTimeMilliseconds(), probe.Values["local"]);
        Assert.Equal(instant.ToUnixTimeMilliseconds(), probe.Values["utc"]);
        Assert.Equal(new DateTimeOffset(unspecified, TimeSpan.Zero).ToUnixTimeMilliseconds(), probe.Values["unspecified"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "scalar-values-have-provider-safe-wire-representations")]
    public void SetHeaders_ProjectsSupportedScalarWireRepresentations()
    {
        var headers = new DictionarySendHeaders();
        headers.Set("uri", new Uri("https://example.test/orders/27"));
        headers.Set("text", "value");
        headers.Set("true", true);
        headers.Set("false", false);
        headers.Set("decimal", 12.5m);
        var properties = new PrimitiveMap();

        properties.SetHeaders(headers);
        PrimitiveMap roundTripped = PrimitiveMap.Unmarshal(properties.Marshal());

        Assert.Equal("https://example.test/orders/27", roundTripped["uri"]);
        Assert.Equal("value", roundTripped["text"]);
        Assert.Equal(bool.TrueString, roundTripped["true"]);
        Assert.Equal(bool.FalseString, roundTripped["false"]);
        Assert.Equal("12.5", roundTripped["decimal"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "shared-provider-native-types-survive-openwire-marshalling")]
    public void SetHeaders_PreservesTheSharedOpenWireAndAmqpNativeTypes()
    {
        var headers = NativeHeaders();
        var properties = new PrimitiveMap();

        properties.SetHeaders(headers);
        PrimitiveMap roundTripped = PrimitiveMap.Unmarshal(properties.Marshal());

        Assert.Equal((byte)7, roundTripped["byte"]);
        Assert.Equal('V', roundTripped["char"]);
        Assert.Equal((short)-23, roundTripped["short"]);
        Assert.Equal(42, roundTripped["int"]);
        Assert.Equal(9_000_000_000L, roundTripped["long"]);
        Assert.Equal(1.25f, roundTripped["float"]);
        Assert.Equal(2.5d, roundTripped["double"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "reference-formattables-use-invariant-culture")]
    public void SetHeaders_FormatsReferenceFormattablesInvariantly()
    {
        var headers = new DictionarySendHeaders();
        headers.Set("formattable", new CultureSensitiveFormattable(12.5m));
        IPrimitiveMap properties = PrimitiveMapProbe.Create(out PrimitiveMapProbe probe);
        CultureInfo previousCulture = CultureInfo.CurrentCulture;

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            properties.SetHeaders(headers);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }

        Assert.Equal("12.5", probe.Values["formattable"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "scheduled-delay-is-transferred-once")]
    public void SetHeaders_TransfersAndConsumesTheClassicScheduledDelayHeader()
    {
        var headers = new DictionarySendHeaders();
        headers.Set("AMQ_SCHEDULED_DELAY", 2750L);
        headers.Set("after-delay", "still-enumerated");
        IPrimitiveMap properties = PrimitiveMapProbe.Create(out PrimitiveMapProbe probe);

        properties.SetHeaders(headers);

        Assert.Equal(2750L, probe.Values["AMQ_SCHEDULED_DELAY"]);
        Assert.Equal("still-enumerated", probe.Values["after-delay"]);
        Assert.False(headers.TryGetHeader("AMQ_SCHEDULED_DELAY", out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-HEADERS", "nonconforming-null-values-remove-native-properties")]
    public void SetHeaders_ContainsNullValuesFromExternalImplementations()
    {
        IPrimitiveMap properties = PrimitiveMapProbe.Create(out PrimitiveMapProbe probe);
        probe.Values["remove"] = "stale";
        var headers = new NullHeaderSource("remove", "already-absent");

        properties.SetHeaders(headers);

        Assert.DoesNotContain("remove", probe.Values.Keys);
        Assert.DoesNotContain("already-absent", probe.Values.Keys);
    }

    private sealed class CultureSensitiveFormattable(decimal value) : IFormattable
    {
        public override string ToString() => value.ToString(CultureInfo.CurrentCulture);

        public string ToString(string? format, IFormatProvider? formatProvider) =>
            value.ToString(format, formatProvider);
    }

    private static DictionarySendHeaders NativeHeaders()
    {
        var headers = new DictionarySendHeaders();
        headers.Set("byte", (byte)7);
        headers.Set("char", 'V');
        headers.Set("short", (short)-23);
        headers.Set("int", 42);
        headers.Set("long", 9_000_000_000L);
        headers.Set("float", 1.25f);
        headers.Set("double", 2.5d);
        return headers;
    }

    private sealed class NullHeaderSource(params string[] keys) : SendHeaders
    {
        public IEnumerable<KeyValuePair<string, object>> GetAll() =>
            keys.Select(static key => new KeyValuePair<string, object>(key, null!));

        public bool TryGetHeader(string key, [NotNullWhen(true)] out object? value)
        {
            value = null;
            return false;
        }

        public TValue? Get<TValue>(string key, TValue? defaultValue = default) where TValue : class => defaultValue;

        public TValue? Get<TValue>(string key, TValue? defaultValue = default) where TValue : struct => defaultValue;

        public void Set(string key, string? value) => throw new NotSupportedException();

        public void Set(string key, object? value, bool overwrite = true) => throw new NotSupportedException();

        public IEnumerator<HeaderValue> GetEnumerator() => Enumerable.Empty<HeaderValue>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private class PrimitiveMapProbe : DispatchProxy
    {
        internal Dictionary<string, object?> Values { get; } = new(StringComparer.Ordinal);

        internal static IPrimitiveMap Create(out PrimitiveMapProbe probe)
        {
            IPrimitiveMap map = DispatchProxy.Create<IPrimitiveMap, PrimitiveMapProbe>();
            probe = (PrimitiveMapProbe)(object)map;
            return map;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "set_Item":
                    Values[Assert.IsType<string>(args![0])] = args[1];
                    return null;
                case "get_Item":
                    return Values[Assert.IsType<string>(args![0])];
                case nameof(IPrimitiveMap.Contains):
                    return Values.ContainsKey(Assert.IsType<string>(args![0]));
                case nameof(IPrimitiveMap.Remove):
                    Values.Remove(Assert.IsType<string>(args![0]));
                    return null;
                default:
                    throw new NotSupportedException(targetMethod.Name);
            }
        }
    }
}
