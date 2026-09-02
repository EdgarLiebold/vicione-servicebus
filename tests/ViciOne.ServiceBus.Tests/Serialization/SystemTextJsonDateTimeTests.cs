using System.Text.Json;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Serialization;

public sealed class SystemTextJsonDateTimeTests
{
    private const string IsoTimestamp = "1994-11-05T13:15:30Z";
    private const string Json = "{\"isoDate\":\"" + IsoTimestamp + "\"}";

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DATETIME", "literal-string")]
    public void IsoTimestampString_DeserializesWithoutChangingItsLiteralText()
    {
        StringTimestampMessage? result = JsonSerializer.Deserialize<StringTimestampMessage>(
            Json,
            ServiceBusMetadataJson.Options);

        Assert.NotNull(result);
        Assert.Equal(IsoTimestamp, result.IsoDate);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SYSTEM-TEXT-JSON-DATETIME", "utc-instant")]
    public void IsoTimestampDateTime_DeserializesAsTheExactUtcInstant()
    {
        DateTimeTimestampMessage? result = JsonSerializer.Deserialize<DateTimeTimestampMessage>(
            Json,
            ServiceBusMetadataJson.Options);

        Assert.NotNull(result);
        Assert.Equal(DateTimeKind.Utc, result.IsoDate.Kind);
        Assert.Equal(new DateTime(1994, 11, 5, 13, 15, 30, DateTimeKind.Utc), result.IsoDate);
    }
}

public sealed class StringTimestampMessage
{
    public string? IsoDate { get; init; }
}

public sealed class DateTimeTimestampMessage
{
    public DateTime IsoDate { get; init; }
}
