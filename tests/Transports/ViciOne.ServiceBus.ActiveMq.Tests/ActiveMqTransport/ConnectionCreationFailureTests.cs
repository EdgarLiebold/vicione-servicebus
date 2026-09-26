using Apache.NMS;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ConnectionCreationFailureTests
{
    [Theory]
    [InlineData("cancel", false, false, false)]
    [InlineData("nms", false, false, false)]
    [InlineData("other", false, false, false)]
    [InlineData("cancel", true, false, false)]
    [InlineData("nms", true, false, false)]
    [InlineData("other", true, false, false)]
    [InlineData("cancel", true, true, false)]
    [InlineData("nms", true, true, false)]
    [InlineData("other", true, true, false)]
    [InlineData("cancel", true, true, true)]
    [InlineData("nms", true, true, true)]
    [InlineData("other", true, true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "connection-failure-preserves-primary-cause-through-cleanup")]
    public async Task CreateContext_PreservesFailureClassificationAndDisposesOnlyAnAcquiredConnectionAsync(
        string failureKind, bool acquired, bool cleanupFails, bool diagnosticThrows)
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        Exception cause = failureKind switch
        {
            "cancel" => new OperationCanceledException("provider canceled", canceled.Token),
            "nms" => new NMSConnectionException("provider connection failed"),
            "other" => new InvalidOperationException("provider initialization failed"),
            _ => throw new ArgumentOutOfRangeException(nameof(failureKind)),
        };
        var stages = new List<string>();
        IConnection connection = InterfaceProxy<IConnection>.Create((method, _) =>
        {
            if (method.Name == nameof(IConnection.Start))
            {
                stages.Add("start");
                throw cause;
            }
            if (method.Name == nameof(IDisposable.Dispose))
            {
                stages.Add("dispose");
                if (cleanupFails)
                    throw new IOException("cleanup failed");
                return null;
            }
            throw new InvalidOperationException($"Unexpected connection call: {method.Name}");
        });
        ActiveMqHostSettings settings = InterfaceProxy<ActiveMqHostSettings>.Create((method, _) => method.Name switch
        {
            "get_Host" => "broker.test",
            "get_Port" => 61616,
            "get_Username" => "",
            nameof(ActiveMqHostSettings.CreateConnection) => Open(),
            _ => throw new InvalidOperationException($"Unexpected settings call: {method.Name}"),
        });
        IActiveMqHostConfiguration host = InterfaceProxy<IActiveMqHostConfiguration>.Create((method, _) => method.Name switch
        {
            "get_Settings" => settings,
            "get_HostAddress" => new Uri("activemq://broker.test:61616"),
            _ => throw new InvalidOperationException($"Unexpected host call: {method.Name}"),
        });
        IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(host);
        var supervisor = new Supervisor();
        ILogContext? previousLogContext = LogContext.Current;
        var logger = new ThrowingLogger();
        try
        {
            if (diagnosticThrows)
                LogContext.ConfigureCurrentLogContext(logger);
            IPipeContextAgent<ConnectionContext> handle = factory.CreateContext(supervisor);
            if (failureKind == "cancel")
            {
                var observed = await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                    handle.Context.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                Assert.Same(cause, observed);
                Assert.Equal(canceled.Token, observed.CancellationToken);
            }
            else
            {
                var observed = await Assert.ThrowsAsync<ActiveMqConnectionException>(() =>
                    handle.Context.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken));
                Assert.Same(cause, observed.InnerException);
                Assert.Equal(failureKind == "nms"
                    ? "Connection exception: broker.test:61616"
                    : "Create Connection Faulted: broker.test:61616", observed.Message);
            }
            Assert.Equal(acquired ? ["create", "start", "dispose"] : new[] { "create" }, stages);
            if (diagnosticThrows)
                Assert.Equal(failureKind == "cancel" ? 1 : 2, logger.Calls);
        }
        finally
        {
            try
            {
                await supervisor.StopAsync("test complete", CancellationToken.None)
                    .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            }
            finally
            {
                LogContext.Current = previousLogContext;
            }
        }

        IConnection Open()
        {
            stages.Add("create");
            return acquired ? connection : throw cause;
        }
    }

    private sealed class ThrowingLogger : ILogger
    {
        public int Calls { get; private set; }
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Warning && logLevel != LogLevel.None;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Calls++;
            throw new InvalidOperationException("diagnostic sink failed");
        }
    }
}
