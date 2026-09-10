using System.Net;
using System.Net.Http.Headers;
using System.Text;
using ViciOne.ServiceBus.RabbitMq.Testing;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Configuration;

public sealed class RabbitMqTestHarnessBoundaryTests
{
    [Fact]
    public void ConstructorAndMutableSettings_RejectEveryInvalidBoundary()
    {
        Assert.Equal("inputQueueName", Assert.Throws<ArgumentException>(() => new RabbitMqTestHarness(" ")).ParamName);

        var harness = new RabbitMqTestHarness();
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => harness.HostAddress = null!).ParamName);

        Uri[] invalidAddresses =
        [
            new("localhost", UriKind.Relative),
            new("http://localhost/test"),
            new("rabbitmq://user@localhost/test"),
            new("rabbitmq://localhost/test?query=value"),
            new("rabbitmq://localhost/test#fragment"),
        ];
        foreach (Uri invalidAddress in invalidAddresses)
        {
            Assert.Equal("value", Assert.Throws<ArgumentException>(() => harness.HostAddress = invalidAddress).ParamName);
        }

        Assert.Equal("value", Assert.Throws<ArgumentException>(() => harness.Username = " ").ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentNullException>(() => harness.Password = null!).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => harness.ManagementPort = 0).ParamName);
        Assert.Equal("value", Assert.Throws<ArgumentOutOfRangeException>(() => harness.ManagementPort = 65536).ParamName);

        string[] invalidNodes = [" ", "rabbitmq://node.example", "node.example/path", "user@node.example", "node.example?query=value"];
        foreach (string invalidNode in invalidNodes)
        {
            Assert.Equal("value", Assert.Throws<ArgumentException>(() => harness.ClusterNodeAddress = invalidNode).ParamName);
        }
    }

    [Fact]
    public void MutableSettings_PreserveValidExplicitValues()
    {
        var harness = new RabbitMqTestHarness("orders")
        {
            HostAddress = new Uri("rabbitmqs://broker.example/team/"),
            Username = "operator",
            Password = string.Empty,
            ManagementPort = 15673,
            ClusterNodeAddress = "node.example:5673",
            CleanVirtualHostOnStart = false,
        };

        Assert.Equal("orders", harness.InputQueueName);
        Assert.Equal(new Uri("rabbitmqs://broker.example/team/"), harness.HostAddress);
        Assert.Equal("operator", harness.Username);
        Assert.Equal(string.Empty, harness.Password);
        Assert.Equal(15673, harness.ManagementPort);
        Assert.Equal("node.example:5673", harness.ClusterNodeAddress);
        Assert.False(harness.CleanVirtualHostOnStart);
    }

    [Fact]
    public void ManagementClient_UsesUtf8BasicAuthenticationForConfiguredCredentials()
    {
        var handler = new RecordingHandler(HttpStatusCode.NoContent, HttpStatusCode.Created);
        var harness = new TestableHarness(handler)
        {
            Username = "üser",
            Password = "päss",
        };

        using HttpClient client = harness.CreateClientForTest();

        AuthenticationHeaderValue? authorization = client.DefaultRequestHeaders.Authorization;
        Assert.NotNull(authorization);
        Assert.Equal("Basic", authorization.Scheme);
        Assert.Equal(Convert.ToBase64String(Encoding.UTF8.GetBytes("üser:päss")), authorization.Parameter);
    }

    [Fact]
    public async Task RecreateVirtualHostAsync_UsesTheEscapedDedicatedVirtualHostAndCancelableRequestsAsync()
    {
        var handler = new RecordingHandler(HttpStatusCode.NotFound, HttpStatusCode.Created);
        var harness = new TestableHarness(handler)
        {
            HostAddress = new Uri("rabbitmqs://broker.example/team%2Fblue/"),
            ClusterNodeAddress = "node.example:5673",
            ManagementPort = 15673,
        };
        using var cancellationSource = new CancellationTokenSource();

        await harness.RecreateVirtualHostAsync(cancellationSource.Token);

        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Delete, request.Method);
                Assert.Equal("https://node.example:15673/api/vhosts/team%2Fblue", request.Uri.OriginalString);
                Assert.Null(request.Body);
                Assert.True(request.CancellationToken.CanBeCanceled);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://node.example:15673/api/vhosts/team%2Fblue", request.Uri.OriginalString);
                Assert.Equal("{}", request.Body);
                Assert.True(request.CancellationToken.CanBeCanceled);
            });
        Assert.False(harness.CleanVirtualHostOnStart);
    }

    [Fact]
    public async Task RecreateVirtualHostAsync_RefusesRootAndPreservesManagementFailuresAsync()
    {
        var rootHandler = new RecordingHandler(HttpStatusCode.NoContent);
        var rootHarness = new TestableHarness(rootHandler)
        {
            HostAddress = new Uri("rabbitmq://localhost/"),
        };

        InvalidOperationException rootFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            rootHarness.RecreateVirtualHostAsync(TestContext.Current.CancellationToken));
        Assert.Contains("root virtual host", rootFailure.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(rootHandler.Requests);

        var deleteHandler = new RecordingHandler(HttpStatusCode.BadGateway);
        var deleteHarness = new TestableHarness(deleteHandler);
        InvalidOperationException deleteFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            deleteHarness.RecreateVirtualHostAsync(TestContext.Current.CancellationToken));
        Assert.Contains("502", deleteFailure.Message, StringComparison.Ordinal);
        Assert.Single(deleteHandler.Requests);

        var createHandler = new RecordingHandler(HttpStatusCode.NoContent, HttpStatusCode.BadRequest);
        var createHarness = new TestableHarness(createHandler);
        InvalidOperationException createFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            createHarness.RecreateVirtualHostAsync(TestContext.Current.CancellationToken));
        Assert.Contains("400", createFailure.Message, StringComparison.Ordinal);
        Assert.Equal(2, createHandler.Requests.Count);
    }

    [Fact]
    public async Task RecreateVirtualHostAsync_WithPreCanceledToken_DoesNotCreateAClientAsync()
    {
        var handler = new RecordingHandler(HttpStatusCode.NoContent);
        var harness = new TestableHarness(handler);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.RecreateVirtualHostAsync(cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.Equal(0, harness.ClientCreationCount);
        Assert.Empty(handler.Requests);
    }

    private sealed class TestableHarness(RecordingHandler handler) : RabbitMqTestHarness
    {
        public int ClientCreationCount { get; private set; }

        public HttpClient CreateClientForTest() => base.CreateManagementHttpClient();

        protected override HttpClient CreateManagementHttpClient()
        {
            ClientCreationCount++;
            HttpClient client = new(handler, disposeHandler: false);
            byte[] credentials = Encoding.UTF8.GetBytes($"{Username}:{Password}");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));
            return client;
        }
    }

    private sealed class RecordingHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        private int _requestIndex;

        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string? body = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri ?? throw new InvalidOperationException("The request URI was not configured."),
                body,
                cancellationToken));

            HttpStatusCode status = statuses[Math.Min(_requestIndex, statuses.Length - 1)];
            _requestIndex++;
            return new HttpResponseMessage(status) { ReasonPhrase = status.ToString() };
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        Uri Uri,
        string? Body,
        CancellationToken CancellationToken);
}
