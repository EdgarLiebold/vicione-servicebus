using System.Globalization;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.InMemoryTransport;

public sealed class InMemoryTransportHeaderMapperTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "in-memory-transport-scalar-projection")]
    public void Copy_PreservesEverySupportedScalarAndTransportOwnedValue()
    {
        var timestamp = new DateTimeOffset(2038, 5, 6, 7, 8, 9, TimeSpan.FromHours(2));
        var date = new DateTime(2039, 6, 7, 8, 9, 10, DateTimeKind.Utc);
        var source = new DictionarySendHeaders();
        source.Set("text", "value");
        source.Set("enabled", true);
        source.Set("disabled", false);
        source.Set("count", 42);
        source.Set("amount", 12.5m);
        source.Set("timestamp", timestamp);
        source.Set("date", date);
        source.Set("address", new Uri("https://example.test/path?q=one"));
        source.Set("unsupported", new CultureAwareValue());
        source.Set("owned", "application");

        var destination = new DictionarySendHeaders();
        destination.Set("owned", "transport");

        InMemoryTransportHeaderMapper.Copy(source, destination);

        Assert.Equal("value", Assert.IsType<string>(GetRequired(destination, "text")));
        Assert.True(Assert.IsType<bool>(GetRequired(destination, "enabled")));
        Assert.False(Assert.IsType<bool>(GetRequired(destination, "disabled")));
        Assert.Equal(42, Assert.IsType<int>(GetRequired(destination, "count")));
        Assert.Equal(12.5m, Assert.IsType<decimal>(GetRequired(destination, "amount")));
        Assert.Equal(timestamp, Assert.IsType<DateTimeOffset>(GetRequired(destination, "timestamp")));
        Assert.Equal(date, Assert.IsType<DateTime>(GetRequired(destination, "date")));
        Assert.Equal("https://example.test/path?q=one", Assert.IsType<string>(GetRequired(destination, "address")));
        Assert.Equal("transport", Assert.IsType<string>(GetRequired(destination, "owned")));
        Assert.False(destination.TryGetHeader("unsupported", out _));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SEND-HEADERS", "in-memory-transport-required-inputs")]
    public void Copy_RejectsEveryMissingRequiredInput()
    {
        var headers = new DictionarySendHeaders();

        Assert.Equal("source", Assert.Throws<ArgumentNullException>(() =>
            InMemoryTransportHeaderMapper.Copy(null!, headers)).ParamName);
        Assert.Equal("destination", Assert.Throws<ArgumentNullException>(() =>
            InMemoryTransportHeaderMapper.Copy(headers, null!)).ParamName);
    }

    private static object GetRequired(DictionarySendHeaders headers, string key)
    {
        Assert.True(headers.TryGetHeader(key, out object? value));
        Assert.NotNull(value);
        return value;
    }

    private sealed class CultureAwareValue : IFormattable
    {
        public string ToString(string? format, IFormatProvider? formatProvider) =>
            ReferenceEquals(formatProvider, CultureInfo.InvariantCulture) ? "invariant" : "ambient";
    }
}
