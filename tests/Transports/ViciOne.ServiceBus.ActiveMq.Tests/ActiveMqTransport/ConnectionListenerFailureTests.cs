using System.Collections.Concurrent;
using Apache.NMS;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.ActiveMq.Configuration;
using ViciOne.ServiceBus.ActiveMq.Tests.TestDoubles;
using ViciOne.ServiceBus.ActiveMq.Topology;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.ActiveMqTransport;

public sealed class ConnectionListenerFailureTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "connection-factory-rejects-null-host")]
    public void Constructor_RejectsNullHost()
    {
        var failure = Assert.Throws<ArgumentNullException>(() => new ConnectionContextFactory(null!));
        Assert.Equal("hostConfiguration", failure.ParamName);
    }

    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, false, true, true)]
    [RequirementCoverage("REQ-VSB-ACTIVEMQ-LIFECYCLE", "listener-accessor-failure-preserves-owned-cleanup")]
    public async Task ListenerFailure_RetiresConnectionThroughItsOwnerAsync(bool addFails, bool removeFails, bool loggerThrows,
        bool firstCloseFails = false)
    {
        var stages = new ConcurrentQueue<string>();
        var added = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var removed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var addFailure = new IOException("partial listener registration failed");
        var removeFailure = new ObjectDisposedException("native connection");
        var closeFailure = new IOException("first close failed");
        int closeAttempts = 0;
        ExceptionListener? listener = null;
        var logger = new FailureLogger(loggerThrows);
        IConnection connection = InterfaceProxy<IConnection>.Create((method, arguments) =>
        {
            switch (method.Name)
            {
                case nameof(IConnection.Start):
                    stages.Enqueue("start");
                    return null;
                case "add_ExceptionListener":
                    stages.Enqueue("add");
                    listener += Assert.IsType<ExceptionListener>(arguments![0]);
                    added.TrySetResult();
                    if (addFails)
                        throw addFailure;
                    return null;
                case "remove_ExceptionListener":
                    stages.Enqueue("remove");
                    listener -= Assert.IsType<ExceptionListener>(arguments![0]);
                    removed.TrySetResult();
                    if (removeFails)
                        throw removeFailure;
                    return null;
                case nameof(IConnection.CloseAsync):
                    stages.Enqueue("close");
                    if (Interlocked.Increment(ref closeAttempts) == 1 && firstCloseFails)
                        return Task.FromException(closeFailure);
                    return Task.CompletedTask;
                case nameof(IDisposable.Dispose):
                    stages.Enqueue("dispose");
                    return null;
                default:
                    throw new InvalidOperationException($"Unexpected native call: {method.Name}");
            }
        });
        var topology = new ActiveMqTopologyConfiguration(ActiveMqBusFactory.CreateMessageTopology());
        var configuration = new ActiveMqBusConfiguration(topology);
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
            "get_Topology" => configuration.HostConfiguration.Topology,
            _ => throw new InvalidOperationException($"Unexpected host call: {method.Name}"),
        });
        var supervisor = new Supervisor();
        ILogContext? previous = LogContext.Current;
        LogContext.ConfigureCurrentLogContext(logger);
        try
        {
            IPipeContextFactory<ConnectionContext> factory = new ConnectionContextFactory(host);
            IPipeContextAgent<ConnectionContext> handle = factory.CreateContext(supervisor);
            await added.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (firstCloseFails)
            {
                ExceptionListener activeListener = Assert.IsType<ExceptionListener>(listener);
                activeListener(new NMSException("connection lost"));
                await logger.ErrorLogged.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.False(handle.Completed.IsCompleted);
                Assert.Same(activeListener, listener);
                Assert.Equal(2, logger.Failures.Count(exception => ReferenceEquals(exception, closeFailure)));
            }
            if (!addFails)
                await handle.StopAsync("test requested stop", TestContext.Current.CancellationToken)
                    .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            await handle.Completed.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            await removed.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
            if (removeFails)
            {
                await logger.RemovalLogged.Task.WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);
                Assert.Contains(logger.Failures, exception => ReferenceEquals(exception, removeFailure));
            }
            Assert.Null(listener);
            if (addFails)
                Assert.Contains(logger.Failures, exception => ReferenceEquals(exception, addFailure));
            string[] expected = firstCloseFails
                ? ["create", "start", "add", "close", "dispose", "close", "dispose", "remove"]
                : addFails
                    ? ["create", "start", "add", "remove", "close", "dispose"]
                    : ["create", "start", "add", "close", "dispose", "remove"];
            Assert.Equal(expected, stages.ToArray());
            Assert.True(handle.Completed.IsCompletedSuccessfully);
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
                LogContext.Current = previous;
            }
        }

        IConnection Open()
        {
            stages.Enqueue("create");
            return connection;
        }
    }

    sealed class FailureLogger(bool throws) : ILogger
    {
        public ConcurrentQueue<Exception> Failures { get; } = new();
        public TaskCompletionSource RemovalLogged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource ErrorLogged { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool IsEnabled(LogLevel level) => level >= LogLevel.Warning && level != LogLevel.None;
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (exception is not null)
            {
                Failures.Enqueue(exception);
                if (exception is ObjectDisposedException)
                    RemovalLogged.TrySetResult();
                if (level == LogLevel.Error)
                    ErrorLogged.TrySetResult();
            }
            if (throws)
                throw new InvalidOperationException("diagnostic sink failed");
        }
    }
}
