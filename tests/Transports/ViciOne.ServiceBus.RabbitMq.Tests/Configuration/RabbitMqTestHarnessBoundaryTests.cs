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
            AllowRootVirtualHostCleanup = true,
        };

        Assert.Equal("orders", harness.InputQueueName);
        Assert.Equal(new Uri("rabbitmqs://broker.example/team/"), harness.HostAddress);
        Assert.Equal("operator", harness.Username);
        Assert.Equal(string.Empty, harness.Password);
        Assert.Equal(15673, harness.ManagementPort);
        Assert.Equal("node.example:5673", harness.ClusterNodeAddress);
        Assert.False(harness.CleanVirtualHostOnStart);
        Assert.True(harness.AllowRootVirtualHostCleanup);
        Assert.Equal(new Uri("queue:orders"), harness.InputQueueAddress);
        Assert.Null(harness.CleanupVirtualHostAsync);

        harness.ClusterNodeAddress = null;
        Assert.Null(harness.ClusterNodeAddress);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(65535)]
    public void ManagementPort_AcceptsEveryInclusiveBoundary(int port)
    {
        var harness = new RabbitMqTestHarness { ManagementPort = port };

        Assert.Equal(port, harness.ManagementPort);
    }

    [Fact]
    public void EnvironmentVariableNames_AreStableAndProviderSpecific()
    {
        Assert.Equal("VICIONE_SERVICEBUS_RMQ_USER", RabbitMqTestHarness.UsernameVariable);
        Assert.Equal("VICIONE_SERVICEBUS_RMQ_PASS", RabbitMqTestHarness.PasswordVariable);
        Assert.Equal("VICIONE_SERVICEBUS_RMQ_HOST", RabbitMqTestHarness.HostVariable);
        Assert.Equal("VICIONE_SERVICEBUS_RMQ_PORT", RabbitMqTestHarness.PortVariable);
        Assert.Equal("VICIONE_SERVICEBUS_RMQ_MGMT_PORT", RabbitMqTestHarness.ManagementPortVariable);
    }

    [Fact]
    public async Task CleanAsync_RefusesRootBeforeCreatingBrokerResourcesAsync()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var harness = new TestableHarness(handler)
        {
            HostAddress = new Uri("rabbitmq://localhost/"),
        };

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.CleanAsync(TestContext.Current.CancellationToken));

        Assert.Contains("root virtual host", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(RabbitMqTestHarness.AllowRootVirtualHostCleanup), exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, harness.ClientCreationCount);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task CleanAsync_RefreshesConnectionFactoryBeforeOpeningTheBrokerConnectionAsync()
    {
        var expected = new InvalidOperationException("refresh failed");
        var harness = new RabbitMqTestHarness();
        var refreshCalls = 0;
        harness.RabbitMqHostConfiguring += configurator =>
        {
            configurator.OnRefreshConnectionFactory = _ =>
            {
                refreshCalls++;
                return Task.FromException(expected);
            };
        };

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.CleanAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, refreshCalls);
    }

    [Fact]
    public async Task CleanAsync_WithPreCanceledToken_DoesNotCreateBrokerResourcesAsync()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var harness = new TestableHarness(handler);
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.CleanAsync(cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.Equal(0, harness.ClientCreationCount);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task StartAsync_CleansBeforeConstructingTheBusAsync()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        await using var harness = new TestableHarness(handler)
        {
            HostAddress = new Uri("rabbitmq://localhost/"),
        };
        var providerConfigurationCalls = 0;
        harness.RabbitMqConfiguring += _ => providerConfigurationCalls++;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.StartAsync(TestContext.Current.CancellationToken));

        Assert.Equal(0, providerConfigurationCalls);
        Assert.Equal(0, harness.ClientCreationCount);
    }

    [Fact]
    public async Task GetHostSettings_AppliesCredentialsClusterSelectionAndHostCallbacksAsync()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var harness = new TestableHarness(handler)
        {
            HostAddress = new Uri("rabbitmqs://broker.example/team/"),
            Username = "operator",
            Password = "secret",
            ClusterNodeAddress = "node.example:5673",
        };
        var callbackCalls = 0;
        var refreshCalls = 0;
        harness.RabbitMqHostConfiguring += configurator =>
        {
            callbackCalls++;
            configurator.Heartbeat(TimeSpan.FromSeconds(17));
            configurator.OnRefreshConnectionFactory = _ =>
            {
                refreshCalls++;
                return Task.CompletedTask;
            };
        };

        RabbitMqHostSettings settings = harness.GetHostSettings();
        await settings.RefreshAsync(new RabbitMQ.Client.ConnectionFactory(), TestContext.Current.CancellationToken);

        Assert.Equal(1, callbackCalls);
        Assert.Equal(1, refreshCalls);
        Assert.Equal("broker.example", settings.Host);
        Assert.Equal("team", settings.VirtualHost);
        Assert.Equal("operator", settings.Username);
        Assert.Equal("secret", settings.Password);
        Assert.True(settings.Ssl);
        Assert.Equal(TimeSpan.FromSeconds(17), settings.Heartbeat);
        Assert.NotNull(settings.EndpointResolver);
    }

    [Fact]
    public async Task CreateBusAsync_RaisesEveryConfigurationEventAndCapturesTheInputAddressAsync()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var harness = new TestableHarness(handler)
        {
            HostAddress = new Uri("rabbitmq://broker.example/team/"),
            CleanVirtualHostOnStart = false,
        };
        var busCalls = 0;
        var endpointCalls = 0;
        var providerBusCalls = 0;
        var providerEndpointCalls = 0;
        var hostCalls = 0;
        harness.BusConfiguring += _ => busCalls++;
        harness.ReceiveEndpointConfiguring += _ => endpointCalls++;
        harness.RabbitMqConfiguring += _ => providerBusCalls++;
        harness.RabbitMqReceiveEndpointConfiguring += _ => providerEndpointCalls++;
        harness.RabbitMqHostConfiguring += _ => hostCalls++;

        IBusControl bus = await harness.CreateBusForTestAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(bus);
        Assert.Equal(1, busCalls);
        Assert.Equal(1, endpointCalls);
        Assert.Equal(1, providerBusCalls);
        Assert.Equal(1, providerEndpointCalls);
        Assert.Equal(1, hostCalls);
        Assert.Equal(new Uri("rabbitmq://broker.example/team/input_queue"), harness.InputQueueAddress);
    }

    [Fact]
    public async Task CreateBusAsync_WithPreCanceledToken_DoesNotInvokeConfigurationAsync()
    {
        var handler = new RecordingHandler(HttpStatusCode.OK);
        var harness = new TestableHarness(handler) { CleanVirtualHostOnStart = false };
        var configurationCalls = 0;
        harness.RabbitMqConfiguring += _ => configurationCalls++;
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            harness.CreateBusForTestAsync(cancellationSource.Token));

        Assert.Equal(cancellationSource.Token, exception.CancellationToken);
        Assert.Equal(0, configurationCalls);
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
        foreach (Uri rootAddress in new[]
        {
            new Uri("rabbitmq://localhost/"),
            new Uri("rabbitmq://localhost/%2F/"),
        })
        {
            var rootHandler = new RecordingHandler(HttpStatusCode.NoContent);
            var rootHarness = new TestableHarness(rootHandler)
            {
                HostAddress = rootAddress,
            };

            InvalidOperationException rootFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                rootHarness.RecreateVirtualHostAsync(TestContext.Current.CancellationToken));
            Assert.Contains("root virtual host", rootFailure.Message, StringComparison.OrdinalIgnoreCase);
            Assert.Empty(rootHandler.Requests);
        }

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

        public Task<IBusControl> CreateBusForTestAsync(CancellationToken cancellationToken) =>
            base.CreateBusAsync(cancellationToken);

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
