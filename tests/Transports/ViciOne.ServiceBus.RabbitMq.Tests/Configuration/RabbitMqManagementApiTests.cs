using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ViciOne.ServiceBus.RabbitMq.Testing;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Configuration;

public sealed class RabbitMqManagementApiTests
{
    [Fact]
    public void CreateClient_UsesUtf8BasicAuthentication()
    {
        using HttpClient client = RabbitMqManagementApi.CreateClient("üser", "päss");

        AuthenticationHeaderValue authorization = Assert.IsType<AuthenticationHeaderValue>(
            client.DefaultRequestHeaders.Authorization);
        Assert.Equal("Basic", authorization.Scheme);
        Assert.Equal(
            Convert.ToBase64String(Encoding.UTF8.GetBytes("üser:päss")),
            authorization.Parameter);
    }

    [Theory]
    [InlineData(false, "http://broker.example:15672/api/queues/%2F")]
    [InlineData(true, "https://broker.example:15672/api/queues/%2F")]
    public void BuildUri_UsesTheSelectedTransportSecurity(bool useTls, string expected)
    {
        Uri actual = RabbitMqManagementApi.BuildUri("broker.example", 15672, useTls, "api/queues/%2F");

        Assert.Equal(expected, actual.OriginalString);
    }

    [Theory]
    [InlineData(null, "%2F")]
    [InlineData("", "%2F")]
    [InlineData("/", "%2F")]
    [InlineData("///", "%2F")]
    [InlineData("/team/blue/", "team%2Fblue")]
    public void EncodeVirtualHost_ProducesOneManagementPathSegment(string? virtualHost, string expected)
    {
        Assert.Equal(expected, RabbitMqManagementApi.EncodeVirtualHost(virtualHost));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData(" ", true)]
    [InlineData("/", true)]
    [InlineData("///", true)]
    [InlineData("team", false)]
    public void IsRootVirtualHost_ClassifiesEverySupportedRepresentation(string? virtualHost, bool expected)
    {
        Assert.Equal(expected, RabbitMqManagementApi.IsRootVirtualHost(virtualHost));
    }

    [Fact]
    public async Task GetEntityNamesAsync_ReturnsOnlyNamedUserEntitiesAndForwardsCancellationAsync()
    {
        const string payload =
            """[{"name":"orders"},{"name":"amq.system"},{"name":" "},{"name":null},{"name":"billing"}]""";
        var handler = new JsonHandler(payload);
        using var client = new HttpClient(handler);
        var requestUri = new Uri("https://broker.example:15672/api/queues/team");
        using var cancellationSource = new CancellationTokenSource();

        IList<string> names = await RabbitMqManagementApi
            .GetEntityNamesAsync(client, requestUri, cancellationSource.Token);

        Assert.Equal(["orders", "billing"], names);
        Assert.Equal(requestUri, handler.RequestUri);
        Assert.True(handler.CancellationToken.CanBeCanceled);
    }

    [Fact]
    public async Task GetEntityNamesAsync_RejectsEveryMissingRequiredInputAsync()
    {
        using var client = new HttpClient(new JsonHandler("[]"));
        var requestUri = new Uri("https://broker.example/api/queues/%2F");

        Assert.Equal("client", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            RabbitMqManagementApi.GetEntityNamesAsync(null!, requestUri, TestContext.Current.CancellationToken))).ParamName);
        Assert.Equal("requestUri", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
            RabbitMqManagementApi.GetEntityNamesAsync(client, null!, TestContext.Current.CancellationToken))).ParamName);
    }

    [Fact]
    public void CreateConnectionCloseReason_PreservesShortTextAndTruncatesAtCompleteUtf8Scalars()
    {
        const string shortReason = "Completed (Ok)";
        string longAscii = new('a', 300);
        string longUnicode = string.Concat(Enumerable.Repeat("😀", 100));

        string asciiResult = RabbitMqManagementApi.CreateConnectionCloseReason(longAscii);
        string unicodeResult = RabbitMqManagementApi.CreateConnectionCloseReason(longUnicode);

        Assert.Same(shortReason, RabbitMqManagementApi.CreateConnectionCloseReason(shortReason));
        Assert.Equal(255, Encoding.UTF8.GetByteCount(asciiResult));
        Assert.True(Encoding.UTF8.GetByteCount(unicodeResult) <= 255);
        Assert.True(char.IsHighSurrogate(unicodeResult[^2]));
        Assert.True(char.IsLowSurrogate(unicodeResult[^1]));
        Assert.StartsWith(unicodeResult, longUnicode, StringComparison.Ordinal);
        Assert.Equal("text", Assert.Throws<ArgumentNullException>(() =>
            RabbitMqManagementApi.CreateConnectionCloseReason(null!)).ParamName);
    }

    private sealed class JsonHandler(string payload) : HttpMessageHandler
    {
        public CancellationToken CancellationToken { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestUri = request.RequestUri;
            CancellationToken = cancellationToken;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json"),
            });
        }
    }
}
