using System.Text.Json;
using Azure.Storage.Blobs;
using ViciOne.ServiceBus.EventHubs.Middleware;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.EventHubs.LocalIntegration.Tests;

public sealed class EventHubCheckpointProbeSecurityTests
{
    [Theory]
    [InlineData("https://checkpoint.blob.core.windows.net:8443/checkpoints")]
    [InlineData("https://checkpoint.blob.core.windows.net:8443/checkpoints?sv=2024-11-04&sig=query-canary&custom=unknown-canary")]
    [InlineData("https://checkpoint.blob.core.windows.net:8443/checkpoints?sig=encoded%2Dcanary#fragment-canary")]
    [InlineData("https://user-canary:password-canary@checkpoint.blob.core.windows.net:8443/checkpoints?sig=query-canary#fragment-canary")]
    [RequirementCoverage("REQ-VSB-EVENTHUB-CONFIGURATION-OWNERSHIP", "checkpoint-probe-removes-uri-credentials-and-retains-sdk-endpoint")]
    public void SerializedProbe_RemovesCredentialsWithoutChangingSdkUri(string address)
    {
        var client = new BlobContainerClient(new Uri(address));
        Uri original = client.Uri;
        string originalName = client.Name;
        var filter = new EventHubBlobContainerFactoryFilter(client);

        using JsonDocument document = JsonDocument.Parse(JsonSerializer.Serialize(filter.GetProbeResult(TestContext.Current.CancellationToken).Results));
        JsonElement topology = document.RootElement.GetProperty("filters");
        Assert.Equal("configureTopology", topology.GetProperty("filterType").GetString());
        var endpoint = new Uri(Assert.IsType<string>(topology.GetProperty("Uri").GetString()));

        Assert.Equal(original.Scheme, endpoint.Scheme);
        Assert.Equal(original.Host, endpoint.Host);
        Assert.Equal(original.Port, endpoint.Port);
        Assert.Equal(original.AbsolutePath, endpoint.AbsolutePath);
        Assert.Equal("checkpoints", topology.GetProperty("Name").GetString());
        Assert.Equal(string.Empty, endpoint.Query);
        Assert.Equal(string.Empty, endpoint.Fragment);
        Assert.Equal(string.Empty, endpoint.UserInfo);
        foreach (string secret in new[] { "query-canary", "unknown-canary", "encoded-canary", "encoded%2Dcanary", "fragment-canary", "user-canary", "password-canary" })
            Assert.DoesNotContain(secret, document.RootElement.GetRawText(), StringComparison.Ordinal);
        Assert.Same(original, client.Uri);
        Assert.Equal(address, client.Uri.OriginalString);
        Assert.Equal(originalName, client.Name);
    }
}
