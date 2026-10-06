using System.Reflection;
using Microsoft.Extensions.Logging;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierHostRegistrationDiagnosticsTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    [InlineData(2, true)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [InlineData(4, false)]
    [InlineData(4, true)]
    [InlineData(5, false)]
    [InlineData(5, true)]
    [RequirementCoverage("REQ-VSB-COURIER-HOST-CONFIGURATION", "owning-debug-preserves-public-specification-admission")]
    public void Hosts_DebugFailureDoesNotPreventPublicSpecificationAdmission(int route, bool loggerThrows)
    {
        bool compensate = route is 0 or 3;
        string template = compensate
            ? "Configuring Compensate Activity: {ActivityType}, {LogType}"
            : "Configuring Execute Activity: {ActivityType}, {ArgumentType}";
        var diagnosticFailure = new IOException($"Courier host Debug failed for public route {route}");
        var logger = new HostLogger(template, loggerThrows ? diagnosticFailure : null);
        var order = new List<string>();
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, EndpointRecorder>();
        var recorder = (EndpointRecorder)(object)endpoint;
        recorder.Order = order;
        IExecuteActivityFactory<TestActivity, Arguments> executeFactory =
            DispatchProxy.Create<IExecuteActivityFactory<TestActivity, Arguments>, UnexpectedCallRecorder>();
        ICompensateActivityFactory<TestActivity, ActivityLog> compensateFactory =
            DispatchProxy.Create<ICompensateActivityFactory<TestActivity, ActivityLog>, UnexpectedCallRecorder>();
        TestRegistrationContext context = DispatchProxy.Create<TestRegistrationContext, UnexpectedCallRecorder>();
        var executeRecorder = (UnexpectedCallRecorder)(object)executeFactory;
        var compensateRecorder = (UnexpectedCallRecorder)(object)compensateFactory;
        var contextRecorder = (UnexpectedCallRecorder)(object)context;
        object? configured = null;
        int configureCalls = 0;
        Uri compensateAddress = new("loopback://localhost/courier-compensation");
        ILogContext? previous = LogContext.Current;

        void ConfigureExecute(IExecuteActivityConfigurator<TestActivity, Arguments> configurator)
        {
            configureCalls++;
            configured = configurator;
            configurator.ConcurrentMessageLimit = 2;
            order.Add("configure");
        }

        void ConfigureCompensate(ICompensateActivityConfigurator<TestActivity, ActivityLog> configurator)
        {
            configureCalls++;
            configured = configurator;
            configurator.ConcurrentMessageLimit = 2;
            order.Add("configure");
        }

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            Exception? invocationFailure = Record.Exception(() =>
            {
                switch (route)
                {
                    case 0:
                        endpoint.CompensateActivityHost(compensateFactory, ConfigureCompensate);
                        break;
                    case 1:
                        endpoint.ExecuteActivityHost(compensateAddress, executeFactory, ConfigureExecute);
                        break;
                    case 2:
                        endpoint.ExecuteActivityHost(executeFactory, ConfigureExecute);
                        break;
                    case 3:
                        endpoint.CompensateActivityHost<TestActivity, ActivityLog>(context, ConfigureCompensate);
                        break;
                    case 4:
                        endpoint.ExecuteActivityHost<TestActivity, Arguments>(compensateAddress, context, ConfigureExecute);
                        break;
                    case 5:
                        endpoint.ExecuteActivityHost<TestActivity, Arguments>(context, ConfigureExecute);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(route));
                }
            });

            HostLogger.Entry selected = Assert.Single(logger.Entries, entry => entry.Template == template);
            Assert.Equal(LogLevel.Debug, selected.Level);
            Assert.Equal(TypeCache<TestActivity>.ShortName, selected.ActivityType);
            Assert.Equal(compensate ? TypeCache<ActivityLog>.ShortName : TypeCache<Arguments>.ShortName, selected.DataType);
            Assert.Null(selected.Exception);
            Assert.Equal(loggerThrows ? 1 : 0, logger.ThrowCount);
            if (loggerThrows)
            {
                Assert.Same(diagnosticFailure, logger.ThrownFailure);
                if (invocationFailure is not null)
                    Assert.Same(diagnosticFailure, invocationFailure);
            }
            else
                Assert.Null(logger.ThrownFailure);
            Assert.Equal(0, executeRecorder.Calls);
            Assert.Equal(0, compensateRecorder.Calls);
            Assert.Equal(0, contextRecorder.Calls);

            Assert.Null(invocationFailure);
            Assert.Equal(1, configureCalls);
            IReceiveEndpointSpecification specification = Assert.Single(recorder.Specifications);
            Assert.Same(configured, specification);
            Assert.Equal(new[] { "configure", "add" }, order);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    public sealed record Arguments(string Value);
    public sealed record ActivityLog(string Value);

    public sealed class TestActivity : IActivity<Arguments, ActivityLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<Arguments> context) =>
            throw new NotSupportedException("Configuration must not execute an activity.");

        public Task<CompensationResult> CompensateAsync(CompensateContext<ActivityLog> context) =>
            throw new NotSupportedException("Configuration must not compensate an activity.");
    }

    public interface TestRegistrationContext : IRegistrationContext, ISetScopedConsumeContext;

    public class EndpointRecorder : DispatchProxy
    {
        public List<IReceiveEndpointSpecification> Specifications { get; } = [];
        public List<string> Order { get; set; } = [];

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);
            if (targetMethod.Name == nameof(IReceiveEndpointConfigurator.AddEndpointSpecification))
            {
                Specifications.Add((IReceiveEndpointSpecification)args![0]!);
                Order.Add("add");
                return null;
            }
            throw new NotSupportedException($"Unexpected public endpoint call: {targetMethod.Name}");
        }
    }

    public class UnexpectedCallRecorder : DispatchProxy
    {
        public int Calls { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            Calls++;
            throw new NotSupportedException($"No provider or activity operation is admitted by configuration: {targetMethod?.Name}");
        }
    }

    private sealed class HostLogger(string selectedTemplate, Exception? failure) : ILogger
    {
        public sealed record Entry(LogLevel Level, string? Template, string? ActivityType, string? DataType, Exception? Exception);
        public List<Entry> Entries { get; } = [];
        public int ThrowCount { get; private set; }
        public Exception? ThrownFailure { get; private set; }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => level == LogLevel.Debug;

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            IEnumerable<KeyValuePair<string, object?>> fields = state as IEnumerable<KeyValuePair<string, object?>>
                ?? Array.Empty<KeyValuePair<string, object?>>();
            string? template = fields.FirstOrDefault(field => field.Key == "{OriginalFormat}").Value as string;
            string? activityType = fields.FirstOrDefault(field => field.Key == "ActivityType").Value as string;
            string dataKey = selectedTemplate.Contains("{LogType}", StringComparison.Ordinal) ? "LogType" : "ArgumentType";
            string? dataType = fields.FirstOrDefault(field => field.Key == dataKey).Value as string;
            Entries.Add(new Entry(level, template, activityType, dataType, exception));
            if (template == selectedTemplate && failure is not null)
            {
                ThrowCount++;
                ThrownFailure = failure;
                throw failure;
            }
        }
    }
}
