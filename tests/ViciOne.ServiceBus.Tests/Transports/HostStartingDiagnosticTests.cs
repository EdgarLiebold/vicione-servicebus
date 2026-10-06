using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Advanced.Middleware;
using ViciOne.ServiceBus.Advanced.Topology;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class HostStartingDiagnosticTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [RequirementCoverage("REQ-VSB-HOST-LIFECYCLE", "host-start-and-idempotent-start-optional-diagnostics")]
    public async Task Start_OptionalDiagnosticDoesNotPreventGenerationAdmissionAsync(bool alreadyStarted, bool loggerThrows)
    {
        string template = alreadyStarted
            ? "Start called, but the host was already started: {Address} ({Reason})"
            : "Starting bus: {HostAddress}";
        var sentinel = new IOException("unique owning host-start diagnostic failure");
        var logger = new SelectedLogger(template, loggerThrows ? sentinel : null);
        ILogContext? previous = LogContext.Current;
        var configuration = DispatchProxy.Create<IHostConfiguration, ConfigurationProxy>();
        var proxy = (ConfigurationProxy)(object)configuration;
        proxy.Address = new Uri($"loopback://localhost/host-start-{Guid.NewGuid():N}");
        var host = new ProbeHost(configuration, DispatchProxy.Create<IBusTopology, TopologyProxy>());
        var actualTasks = new List<Task>();
        IHostHandle? first = null;
        IHostHandle? admitted = null;
        Exception? testFailure = null;
        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            proxy.Context = LogContext.Current;
            if (alreadyStarted)
            {
                first = host.Start(CancellationToken.None);
                Task<HostReady> firstReady = first.Ready;
                actualTasks.Add(firstReady);
                HostReady initial = await firstReady.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
                Assert.Equal(proxy.Address, initial.HostAddress);
                Assert.Empty(logger.Entries);
            }

            Exception? invocationFailure = Record.Exception(() => { admitted = host.Start(CancellationToken.None); });
            SelectedLogger.Entry entry = Assert.Single(logger.Entries);
            Assert.Equal(alreadyStarted ? LogLevel.Warning : LogLevel.Debug, entry.Level);
            Assert.Equal(template, entry.Template);
            Assert.Equal(proxy.Address, entry.Values[alreadyStarted ? "Address" : "HostAddress"]);
            if (alreadyStarted)
                Assert.Equal("Already Started", entry.Values["Reason"]);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            Assert.Equal(loggerThrows ? sentinel : null, logger.ThrownFailure);
            if (invocationFailure is not null && loggerThrows)
                Assert.Same(sentinel, invocationFailure);
            Assert.Equal(0, host.AgentCollectionCalls);

            // Both controls exercise the real BaseHost generation, after the exact selected diagnostic.
            Assert.Null(invocationFailure);
            Assert.NotNull(admitted);
            if (alreadyStarted)
                Assert.Same(first, admitted);
            Task<HostReady> actualReady = admitted.Ready;
            actualTasks.Add(actualReady);
            HostReady ready = await actualReady.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.Equal(proxy.Address, ready.HostAddress);
            Task actualStop = admitted.StopAsync(CancellationToken.None);
            actualTasks.Add(actualStop);
            await actualStop.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None);
            Assert.True(actualStop.IsCompletedSuccessfully);
            Assert.Equal(1, host.AgentCollectionCalls);
            Assert.Single(logger.Entries);
        }
        catch (Exception failure)
        {
            testFailure = failure;
            throw;
        }
        finally
        {
            var cleanupFailures = new List<Exception>();
            try
            {
                try
                {
                    Task cleanupStop = host.StopAsync(CancellationToken.None);
                    actualTasks.Add(cleanupStop);
                }
                catch (Exception failure) { cleanupFailures.Add(failure); }
                foreach (Task actualTask in actualTasks.Distinct())
                {
                    try { await actualTask.WaitAsync(TimeSpan.FromSeconds(10), CancellationToken.None); }
                    catch (Exception failure) { cleanupFailures.Add(failure); }
                }
            }
            finally { LogContext.Current = previous; }
            if (cleanupFailures.Count > 0)
            {
                if (testFailure is not null)
                    cleanupFailures.Insert(0, testFailure);
                if (cleanupFailures.Count == 1)
                    ExceptionDispatchInfo.Capture(cleanupFailures[0]).Throw();
                throw new AggregateException("Host-start fixture and cleanup encountered failures.", cleanupFailures);
            }
        }
    }

    sealed class ProbeHost(IHostConfiguration configuration, IBusTopology topology) : BaseHost(configuration, topology)
    {
        public int AgentCollectionCalls { get; private set; }
        public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(IEndpointDefinition definition,
            IEndpointNameFormatter? endpointNameFormatter, Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
            => throw new InvalidOperationException("This generation-admission fixture does not create endpoints.");
        public override IHostReceiveEndpointHandle ConnectReceiveEndpoint(string queueName,
            Action<IReceiveEndpointConfigurator>? configureEndpoint = null)
            => throw new InvalidOperationException("This generation-admission fixture does not create endpoints.");
        protected override void Probe(ProbeContext context) => throw new InvalidOperationException("Unexpected probe.");
        protected override IAgent[] GetAgentHandles()
        {
            AgentCollectionCalls++;
            return [];
        }
    }

    public class ConfigurationProxy : DispatchProxy
    {
        public Uri Address { get; set; } = null!;
        public ILogContext? Context { get; set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => targetMethod?.Name switch
        {
            "get_HostAddress" => Address,
            "get_LogContext" => Context,
            _ => throw new InvalidOperationException($"Unexpected host configuration SPI: {targetMethod}")
        };
    }

    public class TopologyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => throw new InvalidOperationException($"Unexpected topology SPI: {targetMethod}");
    }

    sealed class SelectedLogger(string selectedTemplate, IOException? failure) : ILogger
    {
        public sealed record Entry(LogLevel Level, string Template, IReadOnlyDictionary<string, object?> Values);
        public List<Entry> Entries { get; } = new();
        public int ThrowCount { get; private set; }
        public IOException? ThrownFailure { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (state is not IEnumerable<KeyValuePair<string, object?>> pairs)
                return;
            Dictionary<string, object?> fields = pairs.ToDictionary(pair => pair.Key, pair => pair.Value);
            if (!fields.TryGetValue("{OriginalFormat}", out object? template) || !Equals(template, selectedTemplate))
                return;
            Entries.Add(new Entry(logLevel, selectedTemplate, fields));
            if (failure is not null)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
    }
}
