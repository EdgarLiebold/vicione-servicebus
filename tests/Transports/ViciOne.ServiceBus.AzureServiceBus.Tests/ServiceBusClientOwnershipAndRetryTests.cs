using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Logging;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusClientOwnershipAndRetryTests
{
    const string ConnectionString = "Endpoint=sb://retry.servicebus.invalid/;SharedAccessKeyName=unit;SharedAccessKey=dGVzdA==";
    static readonly Uri Address = new("sb://retry.servicebus.invalid/");
    static TimeSpan Timeout => TestConfigurationProvider.ForCurrentTestRun().GetValidatedOptions().OperationTimeout!.Value;

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(2000)]
    [InlineData(5000)]
    [InlineData(300000)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "retry-minimum-reaches-both-created-sdk-clients")]
    public async Task MinimumBackoff_ReachesBothCreatedSdkClientsAsync(int milliseconds)
    {
        var settings = new HostSettings
        {
            ServiceUri = Address,
            ConnectionString = ConnectionString,
            RetryMinBackoff = TimeSpan.FromMilliseconds(milliseconds),
            RetryMaxBackoff = TimeSpan.FromSeconds(9),
            RetryLimit = 4,
        };
        (Supervisor supervisor, IPipeContextAgent<ConnectionContext> agent) = Create(settings);
        var context = Assert.IsType<ServiceBusConnectionContext>(await agent.Context.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        var client = Assert.IsAssignableFrom<ServiceBusClient>(Field(context, "_client"));
        try
        {
            ServiceBusClientOptions options = Assert.IsType<ServiceBusClientOptions>(Field(client, "_options"));
            Assert.Equal(settings.RetryMinBackoff, options.RetryOptions.Delay);
            Assert.Equal(settings.RetryMaxBackoff, options.RetryOptions.MaxDelay);
            Assert.Equal(4, options.RetryOptions.MaxRetries);
            Assert.Equal(ServiceBusRetryMode.Exponential, options.RetryOptions.Mode);
            AssertAdministrationRetry(Field(context, "_administrationClient"), settings.RetryMinBackoff, settings.RetryMaxBackoff, 4);
        }
        finally
        {
            await StopAsync(supervisor, agent);
        }
        Assert.True(client.IsClosed);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(9999L)]
    [InlineData(3000000001L)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "retry-minimum-rejects-sdk-invalid-bounds-atomically")]
    public void MinimumBackoff_RejectsSdkInvalidBoundsWithoutChangingSettings(long ticks)
    {
        var settings = new HostSettings { RetryMinBackoff = TimeSpan.FromSeconds(2) };
        var host = new ViciOne.ServiceBus.Configuration.ServiceBusHostConfigurator(Address);
        host.RetryMinBackoff = TimeSpan.FromSeconds(2);

        var direct = Assert.Throws<ArgumentOutOfRangeException>(() => settings.RetryMinBackoff = TimeSpan.FromTicks(ticks));
        var configured = Assert.Throws<ArgumentOutOfRangeException>(() => host.RetryMinBackoff = TimeSpan.FromTicks(ticks));

        Assert.Equal(nameof(HostSettings.RetryMinBackoff), direct.ParamName);
        Assert.Equal(nameof(HostSettings.RetryMinBackoff), configured.ParamName);
        Assert.Equal(TimeSpan.FromSeconds(2), settings.RetryMinBackoff);
        Assert.Equal(TimeSpan.FromSeconds(2), host.Settings.RetryMinBackoff);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "supplied-messaging-client-survives-terminal-stop-and-reuse")]
    public async Task SuppliedMessagingClient_SurvivesTwoTerminalContextsAsync(bool supplyAdministration)
    {
        var clientOptions = new ServiceBusClientOptions
        {
            RetryOptions = { Delay = TimeSpan.FromSeconds(3), MaxRetries = 7, MaxDelay = TimeSpan.FromSeconds(13) },
        };
        await using var client = new ServiceBusClient(ConnectionString, clientOptions);
        var administrationOptions = new ServiceBusAdministrationClientOptions
        {
            Retry = { Delay = TimeSpan.FromSeconds(7), MaxRetries = 8, MaxDelay = TimeSpan.FromSeconds(17) },
        };
        var administration = new ServiceBusAdministrationClient(ConnectionString, administrationOptions);
        var settings = new HostSettings
        {
            ServiceUri = Address,
            ConnectionString = ConnectionString,
            RetryMinBackoff = TimeSpan.FromSeconds(5),
            ServiceBusClient = client,
            ServiceBusAdministrationClient = supplyAdministration ? administration : null,
        };

        for (var index = 0; index < 2; index++)
        {
            (Supervisor supervisor, IPipeContextAgent<ConnectionContext> agent) = Create(settings);
            var context = Assert.IsType<ServiceBusConnectionContext>(await agent.Context.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Same(client, Field(context, "_client"));
            if (supplyAdministration)
                Assert.Same(administration, Field(context, "_administrationClient"));
            await StopAsync(supervisor, agent);

            Assert.False(client.IsClosed);
            await using ServiceBusSender sender = client.CreateSender($"reuse-{index}");
            Assert.Equal($"reuse-{index}", sender.EntityPath);
        }

        ServiceBusClientOptions retained = Assert.IsType<ServiceBusClientOptions>(Field(client, "_options"));
        Assert.Equal(TimeSpan.FromSeconds(3), retained.RetryOptions.Delay);
        Assert.Equal(7, retained.RetryOptions.MaxRetries);
        Assert.Equal(TimeSpan.FromSeconds(13), retained.RetryOptions.MaxDelay);
        AssertAdministrationRetry(administration, TimeSpan.FromSeconds(7), TimeSpan.FromSeconds(17), 8);
        await client.DisposeAsync();
        Assert.True(client.IsClosed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "created-messaging-client-is-owned-with-either-administration-route")]
    public async Task CreatedMessagingClient_ClosesAfterTerminalStopAsync(bool supplyAdministration)
    {
        var administration = new ServiceBusAdministrationClient(ConnectionString);
        var settings = new HostSettings
        {
            ServiceUri = Address,
            ConnectionString = ConnectionString,
            ServiceBusAdministrationClient = supplyAdministration ? administration : null,
        };
        (Supervisor supervisor, IPipeContextAgent<ConnectionContext> agent) = Create(settings);
        var context = Assert.IsType<ServiceBusConnectionContext>(await agent.Context.WaitAsync(Timeout, TestContext.Current.CancellationToken));
        ServiceBusClient client = Assert.IsAssignableFrom<ServiceBusClient>(Field(context, "_client"));
        if (supplyAdministration)
            Assert.Same(administration, Field(context, "_administrationClient"));
        Assert.False(client.IsClosed);

        await StopAsync(supervisor, agent);

        Assert.True(client.IsClosed);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "direct-context-constructor-retains-awaited-client-ownership")]
    public async Task DirectConstructor_AwaitsOwnedClientDisposalAsync()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new DisposalClient(async () =>
        {
            entered.SetResult();
            await release.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        });
        var context = new ServiceBusConnectionContext(client, new ServiceBusAdministrationClient(ConnectionString), CancellationToken.None);
        Task disposing = context.DisposeAsync().AsTask();
        await entered.Task.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        try
        {
            Assert.False(disposing.IsCompleted);
            Assert.Equal(1, client.DisposeCount);
        }
        finally
        {
            release.TrySetResult();
            await disposing.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        }
        Assert.Equal(1, client.DisposeCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "direct-owned-client-disposal-preserves-sdk-failure")]
    public async Task DirectConstructor_PreservesOwnedClientDisposalFailureAsync()
    {
        var failure = new InvalidOperationException("owned disposal");
        var client = new DisposalClient(() => Task.FromException(failure));
        var context = new ServiceBusConnectionContext(client, new ServiceBusAdministrationClient(ConnectionString), CancellationToken.None);

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => context.DisposeAsync().AsTask());

        Assert.Same(failure, actual);
        Assert.Equal(1, client.DisposeCount);
    }

    [Theory]
    [InlineData(true, "Disconnect: {Host}", true)]
    [InlineData(true, "Disconnected: {Host}", true)]
    [InlineData(false, "Disconnect: {Host}", true)]
    [InlineData(false, "Disconnected: {Host}", true)]
    [InlineData(true, "Disconnect: {Host}", false)]
    [InlineData(false, "Disconnect: {Host}", false)]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "disconnect-logging-cannot-prevent-owned-cleanup-or-dispose-borrowed-client")]
    public async Task DisconnectLogging_PreservesAwaitedOwnedCleanupAndBorrowedOwnershipAsync(
        bool ownsClient, string failingTemplate, bool debugEnabled)
    {
        ILogContext? previous = LogContext.Current;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var client = new DisposalClient(async () =>
        {
            entered.TrySetResult();
            await release.Task.WaitAsync(Timeout, CancellationToken.None);
        });
        var expectedLogFailure = new DisconnectLogFailure();
        var logger = new DisconnectLogger(failingTemplate, debugEnabled, expectedLogFailure);
        Supervisor? supervisor = null;
        IPipeContextAgent<ConnectionContext>? agent = null;
        Task? disposing = null;
        try
        {
            LogContext.ConfigureCurrentLogContext();
            ServiceBusConnectionContext context;
            if (ownsClient)
            {
                context = new ServiceBusConnectionContext(client,
                    new ServiceBusAdministrationClient(ConnectionString), CancellationToken.None);
            }
            else
            {
                var settings = new HostSettings
                {
                    ServiceUri = Address,
                    ServiceBusClient = client,
                    ServiceBusAdministrationClient = new ServiceBusAdministrationClient(ConnectionString),
                };
                (supervisor, agent) = Create(settings);
                context = Assert.IsType<ServiceBusConnectionContext>(await agent.Context
                    .WaitAsync(Timeout, TestContext.Current.CancellationToken));
                Assert.Same(client, Field(context, "_client"));
            }

            LogContext.ConfigureCurrentLogContext(logger);
            disposing = context.DisposeAsync().AsTask();
            Assert.Equal(ownsClient ? 1 : 0, client.DisposeCount);
            if (ownsClient)
            {
                // SDK cleanup starts synchronously and remains pending at the explicit gate.
                Assert.True(entered.Task.IsCompletedSuccessfully);
                Assert.False(disposing.IsCompleted);
                Assert.Equal(debugEnabled ? new[] { "Disconnect: {Host}" } : Array.Empty<string>(), logger.Templates);
                release.TrySetResult();
            }
            await disposing.WaitAsync(Timeout, TestContext.Current.CancellationToken);

            Assert.True(disposing.IsCompletedSuccessfully);
            Assert.Equal(ownsClient ? 1 : 0, client.DisposeCount);
            Assert.Equal(debugEnabled ? new[] { "Disconnect: {Host}", "Disconnected: {Host}" } : Array.Empty<string>(), logger.Templates);
            Assert.All(logger.Hosts, host => Assert.Equal("retry.servicebus.invalid", host));
            Assert.Equal(debugEnabled ? 1 : 0, logger.FailureCount);
        }
        finally
        {
            release.TrySetResult();
            LogContext.Current = previous;
            try
            {
                if (disposing is not null)
                {
                    try
                    {
                        await disposing.WaitAsync(Timeout, CancellationToken.None);
                    }
                    catch (DisconnectLogFailure error) when (ReferenceEquals(error, expectedLogFailure))
                    {
                        // Observe the original implementation's finite fault during cleanup;
                        // the main assertions/await still reject that outcome.
                    }
                }
            }
            finally
            {
                try
                {
                    if (supervisor is not null && agent is not null)
                        await StopAsync(supervisor, agent);
                }
                finally
                {
                    // Borrowed ownership always stays with the caller. If an original pre-log
                    // failure prevented owned cleanup, the test also releases that client.
                    if (!ownsClient || client.DisposeCount == 0)
                        await client.DisposeAsync().AsTask().WaitAsync(Timeout, CancellationToken.None);
                    Assert.Equal(1, client.DisposeCount);
                    LogContext.Current = previous;
                }
            }
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-HOST-CONFIGURATION", "disconnect-logging-preserves-owned-sdk-cleanup-failure")]
    public async Task DisconnectLogging_CannotReplaceAnOwnedClientDisposalFailureAsync()
    {
        ILogContext? previous = LogContext.Current;
        var sdkFailure = new InvalidOperationException("actual owned SDK cleanup failure");
        var logFailure = new DisconnectLogFailure();
        var logger = new DisconnectLogger("Disconnect: {Host}", true, logFailure);
        var client = new DisposalClient(() => Task.FromException(sdkFailure));
        var context = new ServiceBusConnectionContext(client,
            new ServiceBusAdministrationClient(ConnectionString), CancellationToken.None);
        Task? pending = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            pending = context.DisposeAsync().AsTask();
            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                pending.WaitAsync(Timeout, TestContext.Current.CancellationToken));
            Assert.Same(sdkFailure, actual);
            Assert.Equal(1, client.DisposeCount);
            Assert.Equal(new[] { "Disconnect: {Host}" }, logger.Templates);
            Assert.Equal(1, logger.FailureCount);
        }
        finally
        {
            LogContext.Current = previous;
            try
            {
                if (pending is not null)
                {
                    try
                    {
                        await pending.WaitAsync(Timeout, CancellationToken.None);
                    }
                    catch (Exception error) when (ReferenceEquals(error, logFailure) || ReferenceEquals(error, sdkFailure))
                    {
                        // Observe the actual cleanup outcome without hiding a main assertion failure.
                    }
                }
            }
            finally
            {
                if (client.DisposeCount == 0)
                {
                    var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                        client.DisposeAsync().AsTask().WaitAsync(Timeout, CancellationToken.None));
                    Assert.Same(sdkFailure, actual);
                }
                LogContext.Current = previous;
            }
        }
    }

    static (Supervisor, IPipeContextAgent<ConnectionContext>) Create(HostSettings settings)
    {
        IServiceBusHostConfiguration configuration = DispatchProxy.Create<IServiceBusHostConfiguration, ServiceBusConnectionContextTests.HostConfigurationProxy>();
        var proxy = (ServiceBusConnectionContextTests.HostConfigurationProxy)(object)configuration;
        proxy.Address = settings.ServiceUri;
        proxy.Settings = settings;
        var supervisor = new Supervisor();
        IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(configuration);
        return (supervisor, factory.CreateContext(supervisor));
    }

    static async Task StopAsync(Supervisor supervisor, IPipeContextAgent<ConnectionContext> agent)
    {
        await supervisor.StopAsync("retry and ownership regression", TestContext.Current.CancellationToken)
            .WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await supervisor.Completed.WaitAsync(Timeout, TestContext.Current.CancellationToken);
        await agent.Completed.WaitAsync(Timeout, TestContext.Current.CancellationToken);
    }

    static void AssertAdministrationRetry(object administration, TimeSpan delay, TimeSpan maximumDelay, int maximumRetries)
    {
        object requests = Field(administration, "_httpRequestAndResponse");
        object pipeline = Field(requests, "_pipeline");
        var policies = Assert.IsType<ReadOnlyMemory<HttpPipelinePolicy>>(Field(pipeline, "_pipeline"));
        RetryPolicy retry = Assert.Single(policies.ToArray().OfType<RetryPolicy>());
        Assert.Equal(maximumRetries, Assert.IsType<int>(Field(retry, "_maxRetries")));
        DelayStrategy strategy = Assert.IsAssignableFrom<DelayStrategy>(Field(retry, "_delayStrategy"));
        Assert.Equal(maximumDelay, Assert.IsType<TimeSpan>(Field(strategy, "_maxDelay")));
        DelayStrategy expected = DelayStrategy.CreateExponentialDelayStrategy(delay, maximumDelay);
        MethodInfo method = Assert.IsAssignableFrom<MethodInfo>(typeof(DelayStrategy).GetMethod("GetNextDelayCore", BindingFlags.NonPublic | BindingFlags.Instance));
        foreach (int attempt in new[] { 0, 1 })
            Assert.Equal(method.Invoke(expected, [null, attempt]), method.Invoke(strategy, [null, attempt]));
    }

    static object Field(object instance, string name)
    {
        for (Type? type = instance.GetType(); type != null; type = type.BaseType)
        {
            FieldInfo? field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (field != null)
                return Assert.IsAssignableFrom<object>(field.GetValue(instance));
        }
        throw new InvalidOperationException($"Pinned SDK field missing: {instance.GetType().FullName}.{name}");
    }

    sealed class DisconnectLogFailure : Exception;

    sealed class DisconnectLogger(string failingTemplate, bool debugEnabled, Exception failure) : ILogger
    {
        public List<string> Templates { get; } = [];
        public List<string> Hosts { get; } = [];
        public int FailureCount { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => debugEnabled && logLevel == LogLevel.Debug;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var fields = ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(item => item.Key, item => item.Value);
            string template = Assert.IsType<string>(fields["{OriginalFormat}"]);
            if (template is not ("Disconnect: {Host}" or "Disconnected: {Host}"))
                throw new InvalidOperationException($"Unexpected debug event: {template}");
            Assert.Null(exception);
            Templates.Add(template);
            Hosts.Add(Assert.IsType<string>(fields["Host"]));
            if (template == failingTemplate)
            {
                FailureCount++;
                throw failure;
            }
        }
    }

    sealed class DisposalClient(Func<Task> dispose) : ServiceBusClient
    {
        public override string FullyQualifiedNamespace => "retry.servicebus.invalid";
        public int DisposeCount { get; private set; }

        public override async ValueTask DisposeAsync()
        {
            DisposeCount++;
            await dispose();
        }
    }
}
