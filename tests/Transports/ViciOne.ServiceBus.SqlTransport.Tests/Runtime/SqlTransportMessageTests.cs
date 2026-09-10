using System.Text.Json;
using ViciOne.ServiceBus.SqlTransport;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.SqlTransport.Tests.Runtime;

public sealed class SqlTransportMessageTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HEADER-MATERIALIZATION", "serialized-header-replacement-invalidates-cache")]
    public void ReplacingSerializedHeaders_InvalidatesBothMaterializedCaches()
    {
        var message = new SqlTransportMessage
        {
            Headers = HeaderJson("application", "first"),
            TransportHeaders = HeaderJson("transport", "first"),
        };

        Assert.Equal("first", message.GetHeaders().Get("application", string.Empty));
        Assert.Equal("first", message.GetTransportHeaders().Get("transport", string.Empty));

        message.Headers = HeaderJson("application", "second");
        message.TransportHeaders = HeaderJson("transport", "second");

        Assert.Equal("second", message.GetHeaders().Get("application", string.Empty));
        Assert.Equal("second", message.GetTransportHeaders().Get("transport", string.Empty));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SQL-HEADER-MATERIALIZATION", "transport-headers-win-without-duplicates")]
    public void HeaderProvider_MergesDuplicateNamesWithTransportPrecedence()
    {
        var message = new SqlTransportMessage
        {
            Headers = HeaderJson("shared", "application"),
            TransportHeaders = HeaderJson("shared", "transport"),
        };
        var provider = new SqlHeaderProvider(message);

        KeyValuePair<string, object> header = Assert.Single(provider.GetAll());

        Assert.Equal("shared", header.Key, ignoreCase: true);
        Assert.Equal("transport", Assert.IsType<string>(header.Value));
        Assert.True(provider.TryGetHeader("SHARED", out object? value));
        Assert.Equal("transport", Assert.IsType<string>(value));
        Assert.Throws<ArgumentException>(() => provider.TryGetHeader(" ", out _));
        Assert.Throws<ArgumentNullException>(() => new SqlHeaderProvider(null!));
    }

    private static string HeaderJson(string key, string value) =>
        JsonSerializer.Serialize(new[] { new KeyValuePair<string, object>(key, value) });
}
