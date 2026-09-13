using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Testing;
using Xunit;

namespace ViciOne.ServiceBus.RabbitMq.Tests.Internal;

public sealed class RabbitMqTestHarnessHostedServiceTests
{
    [Fact]
    public async Task DirectHarness_CleansAnExplicitlyAllowedRootAndRunsCustomCleanupAsync()
    {
        var handler = new ManagementHandler();
        var events = new List<string>();
        IChannel channel = CreateChannel(events);
        IConnection connection = CreateConnection(channel, events);
        RabbitMqHostSettings? observedSettings = null;
        var observedConnectionToken = default(CancellationToken);
        var observedCleanupToken = default(CancellationToken);
        var harness = new IsolatedCleanupHarness(
            handler,
            (settings, cancellationToken) =>
            {
                events.Add("connect");
                observedSettings = settings;
                observedConnectionToken = cancellationToken;
                return Task.FromResult(connection);
            })
        {
            HostAddress = new Uri("rabbitmq://broker.example/"),
            AllowRootVirtualHostCleanup = true,
        };
        harness.CleanupVirtualHostAsync = (cleanupChannel, cancellationToken) =>
        {
            Assert.Same(channel, cleanupChannel);
            observedCleanupToken = cancellationToken;
            events.Add("custom-cleanup");
            return Task.CompletedTask;
        };
        using var cancellationSource = new CancellationTokenSource();

        await harness.CleanAsync(cancellationSource.Token);

        Assert.Equal(
            ["connect", "exchange:orders-exchange", "queue:orders-queue", "custom-cleanup", "channel-close"],
            events);
        Assert.Equal("/", Assert.IsAssignableFrom<RabbitMqHostSettings>(observedSettings).VirtualHost);
        Assert.Equal(cancellationSource.Token, observedConnectionToken);
        Assert.Equal(cancellationSource.Token, observedCleanupToken);
        Assert.False(harness.CleanVirtualHostOnStart);
        Assert.Collection(
            handler.Requests,
            request => Assert.EndsWith("/api/exchanges/%2F", request.Uri.OriginalString, StringComparison.Ordinal),
            request => Assert.EndsWith("/api/queues/%2F", request.Uri.OriginalString, StringComparison.Ordinal));
    }

    [Fact]
    public async Task DirectHarness_NullConnectionResultFailsBeforeManagementRequestsAsync()
    {
        var handler = new ManagementHandler();
        var harness = new IsolatedCleanupHarness(
            handler,
            (_, _) => Task.FromResult<IConnection>(null!));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.CleanAsync(TestContext.Current.CancellationToken));

        Assert.Contains("connection factory returned null", exception.Message, StringComparison.Ordinal);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void DirectHarness_IsolatedConstructorRejectsANullConnectionFactory()
    {
        Assert.Equal("createConnection", Assert.Throws<ArgumentNullException>(() =>
            new RabbitMqTestHarness(inputQueueName: null, createConnection: null!)).ParamName);
    }

    [Fact]
    public async Task StartAsync_PerformsCreationCleanupAndConfigurationInOrderAsync()
    {
        var handler = new ManagementHandler();
        var events = new List<string>();
        IChannel channel = CreateChannel(events);
        IConnection connection = CreateConnection(channel, events);
        var observedConnectionToken = default(CancellationToken);
        var observedConfigurationToken = default(CancellationToken);
        IChannel? observedConfigurationChannel = null;
        var service = CreateService(
            new RabbitMqTestHarnessOptions
            {
                CreateVirtualHostIfMissing = true,
                CleanVirtualHostOnStart = true,
                ConfigureVirtualHostAsync = (configuredChannel, cancellationToken) =>
                {
                    events.Add("configure");
                    observedConfigurationChannel = configuredChannel;
                    observedConfigurationToken = cancellationToken;
                    return Task.CompletedTask;
                },
            },
            handler,
            cancellationToken =>
            {
                events.Add("connect");
                observedConnectionToken = cancellationToken;
                return Task.FromResult(connection);
            });
        using var cancellationSource = new CancellationTokenSource();

        await service.StartAsync(cancellationSource.Token);

        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Put, request.Method);
                Assert.Equal("https://broker.example:15673/api/vhosts/team%2Fblue", request.Uri.OriginalString);
                Assert.Equal("{}", request.Body);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://broker.example:15673/api/exchanges/team%2Fblue", request.Uri.OriginalString);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Get, request.Method);
                Assert.Equal("https://broker.example:15673/api/queues/team%2Fblue", request.Uri.OriginalString);
            });
        Assert.Equal(
            ["connect", "exchange:orders-exchange", "queue:orders-queue", "channel-close", "connection-close:200:Completed (Ok)", "connect", "configure", "channel-close", "connection-close:200:Completed (Ok)"],
            events);
        Assert.Same(channel, observedConfigurationChannel);
        Assert.Equal(cancellationSource.Token, observedConnectionToken);
        Assert.Equal(cancellationSource.Token, observedConfigurationToken);
    }

    [Fact]
    public async Task StartAsync_PreservesThePreparationFailureWhenFailureCloseAlsoFaultsAsync()
    {
        var expected = new InvalidOperationException(new string('ü', 300));
        var closeFailure = new IOException("close failed");
        var events = new List<string>();
        IChannel channel = CreateChannel(events);
        IConnection connection = CreateConnection(channel, events, closeFailure);
        var service = CreateService(
            new RabbitMqTestHarnessOptions
            {
                ConfigureVirtualHostAsync = (_, _) => Task.FromException(expected),
            },
            new ManagementHandler(),
            _ => Task.FromResult(connection));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.StartAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        string failureClose = Assert.Single(events, value => value.StartsWith("connection-close:500:", StringComparison.Ordinal));
        string reason = failureClose["connection-close:500:".Length..];
        Assert.True(Encoding.UTF8.GetByteCount(reason) <= 255);
    }

    [Fact]
    public async Task StartAsync_RejectsNullFactoryResultsWithActionableFailuresAsync()
    {
        var createService = CreateService(
            new RabbitMqTestHarnessOptions { CreateVirtualHostIfMissing = true },
            new ManagementHandler(),
            _ => Task.FromResult(CreateConnection(CreateChannel([]), [])),
            () => null!);

        InvalidOperationException clientFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            createService.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains("management-client factory returned null", clientFailure.Message, StringComparison.Ordinal);

        var connectService = CreateService(
            new RabbitMqTestHarnessOptions { ConfigureVirtualHostAsync = (_, _) => Task.CompletedTask },
            new ManagementHandler(),
            _ => Task.FromResult<IConnection>(null!));

        InvalidOperationException connectionFailure = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            connectService.StartAsync(TestContext.Current.CancellationToken));
        Assert.Contains("connection factory returned null", connectionFailure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void IsolatedConstructor_RejectsEveryMissingDependency()
    {
        RabbitMqTransportOptions transportOptions = CreateTransportOptions();
        var sslOptions = new RabbitMqSslOptions();
        var testOptions = new RabbitMqTestHarnessOptions();
        var logger = NullLogger<RabbitMqTestHarnessHostedService>.Instance;
        Func<HttpClient> clientFactory = () => new HttpClient(new ManagementHandler(), disposeHandler: false);
        Func<CancellationToken, Task<IConnection>> connectionFactory = _ =>
            Task.FromResult(CreateConnection(CreateChannel([]), []));

        Assert.Equal("transportOptions", Assert.Throws<ArgumentNullException>(() =>
            new RabbitMqTestHarnessHostedService(null!, sslOptions, testOptions, logger, clientFactory, connectionFactory)).ParamName);
        Assert.Equal("sslOptions", Assert.Throws<ArgumentNullException>(() =>
            new RabbitMqTestHarnessHostedService(transportOptions, null!, testOptions, logger, clientFactory, connectionFactory)).ParamName);
        Assert.Equal("testOptions", Assert.Throws<ArgumentNullException>(() =>
            new RabbitMqTestHarnessHostedService(transportOptions, sslOptions, null!, logger, clientFactory, connectionFactory)).ParamName);
        Assert.Equal("logger", Assert.Throws<ArgumentNullException>(() =>
            new RabbitMqTestHarnessHostedService(transportOptions, sslOptions, testOptions, null!, clientFactory, connectionFactory)).ParamName);
        Assert.Equal("createManagementHttpClient", Assert.Throws<ArgumentNullException>(() =>
            new RabbitMqTestHarnessHostedService(transportOptions, sslOptions, testOptions, logger, null!, connectionFactory)).ParamName);
        Assert.Equal("createConnection", Assert.Throws<ArgumentNullException>(() =>
            new RabbitMqTestHarnessHostedService(transportOptions, sslOptions, testOptions, logger, clientFactory, null!)).ParamName);
    }

    [Fact]
    public void CreateHostSettings_ProjectsTransportAndTlsOptions()
    {
        RabbitMqTransportOptions transportOptions = CreateTransportOptions();
        transportOptions.ConnectionName = "test-host";
        var sslOptions = new RabbitMqSslOptions
        {
            ServerName = "certificate.example",
            Trust = true,
            CertPath = "/certs/client.pfx",
            CertPassphrase = "certificate-secret",
            CertIdentity = true,
            Protocol = SslProtocols.Tls13,
        };
        var service = new RabbitMqTestHarnessHostedService(
            transportOptions,
            sslOptions,
            new RabbitMqTestHarnessOptions(),
            NullLogger<RabbitMqTestHarnessHostedService>.Instance,
            () => new HttpClient(new ManagementHandler(), disposeHandler: false),
            _ => Task.FromResult(CreateConnection(CreateChannel([]), [])));

        RabbitMqHostSettings settings = service.CreateHostSettings();

        Assert.Equal("broker.example", settings.Host);
        Assert.Equal(5671, settings.Port);
        Assert.Equal("/team/blue/", settings.VirtualHost);
        Assert.Equal("üser", settings.Username);
        Assert.Equal("päss", settings.Password);
        Assert.Equal("test-host", settings.ClientProvidedName);
        Assert.True(settings.Ssl);
        Assert.Equal("certificate.example", settings.SslServerName);
        Assert.Equal("/certs/client.pfx", settings.ClientCertificatePath);
        Assert.Equal("certificate-secret", settings.ClientCertificatePassphrase);
        Assert.True(settings.UseClientCertificateAsAuthenticationIdentity);
        Assert.Equal(SslProtocols.Tls13, settings.SslProtocol);
        Assert.NotEqual(SslPolicyErrors.None, settings.AcceptablePolicyErrors);
    }

    private static RabbitMqTestHarnessHostedService CreateService(
        RabbitMqTestHarnessOptions testOptions,
        ManagementHandler handler,
        Func<CancellationToken, Task<IConnection>> createConnection,
        Func<HttpClient>? createClient = null)
    {
        return new RabbitMqTestHarnessHostedService(
            CreateTransportOptions(),
            new RabbitMqSslOptions(),
            testOptions,
            NullLogger<RabbitMqTestHarnessHostedService>.Instance,
            createClient ?? (() => new HttpClient(handler, disposeHandler: false)),
            createConnection);
    }

    private static RabbitMqTransportOptions CreateTransportOptions()
    {
        return new RabbitMqTransportOptions
        {
            Host = "broker.example",
            VHost = "/team/blue/",
            User = "üser",
            Pass = "päss",
            UseSsl = true,
            ManagementPort = 15673,
        };
    }

    private sealed class IsolatedCleanupHarness(
        ManagementHandler handler,
        Func<RabbitMqHostSettings, CancellationToken, Task<IConnection>> createConnection)
        : RabbitMqTestHarness(inputQueueName: null, createConnection)
    {
        protected override HttpClient CreateManagementHttpClient() =>
            new(handler, disposeHandler: false);
    }

    private static IConnection CreateConnection(
        IChannel channel,
        List<string> events,
        Exception? failureCloseException = null)
    {
        return InterfaceProxy<IConnection>.Create((method, arguments) => method.Name switch
        {
            nameof(IConnection.CreateChannelAsync) => Task.FromResult(channel),
            "get_IsOpen" => true,
            nameof(IConnection.CloseAsync) => RecordConnectionClose(arguments, events, failureCloseException, method.ReturnType),
            nameof(IAsyncDisposable.DisposeAsync) => ValueTask.CompletedTask,
            _ => Default(method.ReturnType),
        });
    }

    private static IChannel CreateChannel(List<string> events)
    {
        return InterfaceProxy<IChannel>.Create((method, arguments) => method.Name switch
        {
            nameof(IChannel.ExchangeDeleteAsync) => Record(
                events,
                $"exchange:{Assert.IsType<string>(arguments![0])}",
                method.ReturnType),
            nameof(IChannel.QueueDeleteAsync) => Record(
                events,
                $"queue:{Assert.IsType<string>(arguments![0])}",
                method.ReturnType),
            nameof(IChannel.CloseAsync) => Record(events, "channel-close", method.ReturnType),
            "get_IsOpen" => true,
            nameof(IAsyncDisposable.DisposeAsync) => ValueTask.CompletedTask,
            _ => Default(method.ReturnType),
        });
    }

    private static object? RecordConnectionClose(
        object?[]? arguments,
        List<string> events,
        Exception? failureCloseException,
        Type returnType)
    {
        ushort code = Assert.IsType<ushort>(arguments![0]);
        string reason = Assert.IsType<string>(arguments[1]);
        events.Add($"connection-close:{code}:{reason}");
        if (code == 500 && failureCloseException != null)
            return Task.FromException(failureCloseException);

        return Default(returnType);
    }

    private static object? Record(List<string> events, string value, Type returnType)
    {
        events.Add(value);
        return Default(returnType);
    }

    private static object? Default(Type returnType)
    {
        if (returnType == typeof(void))
            return null;
        if (returnType == typeof(Task))
            return Task.CompletedTask;
        if (returnType == typeof(ValueTask))
            return ValueTask.CompletedTask;
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
        {
            Type resultType = returnType.GetGenericArguments()[0];
            object? result = resultType.IsValueType ? Activator.CreateInstance(resultType) : null;
            return typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(resultType)
                .Invoke(null, [result]);
        }
        if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(ValueTask<>))
            return Activator.CreateInstance(returnType);

        return returnType.IsValueType ? Activator.CreateInstance(returnType) : null;
    }

    private sealed class ManagementHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            string? body = request.Content == null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);
            Uri requestUri = request.RequestUri
                ?? throw new InvalidOperationException("The request URI was not configured.");
            Requests.Add(new RecordedRequest(request.Method, requestUri, body));

            if (request.Method == HttpMethod.Put)
                return new HttpResponseMessage(HttpStatusCode.Created);

            string json = requestUri.AbsolutePath.Contains("exchanges", StringComparison.Ordinal)
                ? """[{"name":"amq.direct"},{"name":"orders-exchange"}]"""
                : """[{"name":"orders-queue"},{"name":" "}]""";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            };
        }
    }

    private class InterfaceProxy<T> : DispatchProxy
        where T : class
    {
        private Func<MethodInfo, object?[]?, object?>? _handler;

        internal static T Create(Func<MethodInfo, object?[]?, object?> handler)
        {
            T value = Create<T, InterfaceProxy<T>>();
            ((InterfaceProxy<T>)(object)value)._handler = handler;
            return value;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            (_handler ?? throw new InvalidOperationException("The proxy handler has not been configured."))(
                targetMethod ?? throw new InvalidOperationException("The invoked method is unavailable."),
                args);
    }

    private sealed record RecordedRequest(HttpMethod Method, Uri Uri, string? Body);
}
