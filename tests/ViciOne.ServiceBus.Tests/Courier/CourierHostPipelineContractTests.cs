using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Courier;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Operations;
using ViciOne.ServiceBus.Serialization;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.InMemoryOutbox;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierHostPipelineContractTests
{
    private static readonly DateTimeOffset CreatedAt = new(2044, 5, 6, 7, 8, 9, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execute-host-evaluates-notifies-and-continues-in-order")]
    public async Task ExecuteHost_EvaluatesThenNotifiesAndContinuesExactlyOnceAsync()
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateExecuteObservation(trace, TestContext.Current.CancellationToken);
        var result = new RecordingExecutionResult(trace);
        var host = new ExecuteActivityHost<TestActivity, ActivityArguments>(
            new DelegatePipe<ExecuteContext<ActivityArguments>>(context =>
            {
                trace.Add("pipe");
                context.Result = result;
                return Task.CompletedTask;
            }),
            new Uri("loopback://localhost/compensate"));

        await host.SendAsync(observation.Context, Next(trace));

        Assert.Equal(["pipe", "evaluate", "consumed", "next"], trace);
        Assert.Equal(1, result.EvaluationCount);
        Assert.Null(observation.Fault);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensate-host-evaluates-notifies-and-continues-in-order")]
    public async Task CompensateHost_EvaluatesThenNotifiesAndContinuesExactlyOnceAsync()
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateCompensateObservation(trace, TestContext.Current.CancellationToken);
        var result = new RecordingCompensationResult(trace);
        var host = new CompensateActivityHost<TestActivity, ActivityLog>(
            new DelegatePipe<CompensateContext<ActivityLog>>(context =>
            {
                trace.Add("pipe");
                context.Result = result;
                return Task.CompletedTask;
            }));

        await host.SendAsync(observation.Context, Next(trace));

        Assert.Equal(["pipe", "evaluate", "consumed", "next"], trace);
        Assert.Equal(1, result.EvaluationCount);
        Assert.Null(observation.Fault);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execute-result-dispatch-failure-is-not-reclassified")]
    public async Task ExecuteHost_ResultDispatchFailureIsObservedOnceAndSkipsTheNextPipeAsync()
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateExecuteObservation(trace, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("execute result dispatch failed");
        var result = new RecordingExecutionResult(trace, evaluationFailure: expected);
        var host = new ExecuteActivityHost<TestActivity, ActivityArguments>(
            new DelegatePipe<ExecuteContext<ActivityArguments>>(context =>
            {
                trace.Add("pipe");
                context.Result = result;
                return Task.CompletedTask;
            }),
            null);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.SendAsync(observation.Context, Next(trace)));

        Assert.Same(expected, actual);
        Assert.Same(expected, observation.Fault);
        Assert.Equal(["pipe", "evaluate", "faulted"], trace);
        Assert.Equal(1, result.EvaluationCount);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensate-result-dispatch-failure-is-not-reclassified")]
    public async Task CompensateHost_ResultDispatchFailureIsObservedOnceAndSkipsTheNextPipeAsync()
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateCompensateObservation(trace, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("compensation result dispatch failed");
        var result = new RecordingCompensationResult(trace, evaluationFailure: expected);
        var host = new CompensateActivityHost<TestActivity, ActivityLog>(
            new DelegatePipe<CompensateContext<ActivityLog>>(context =>
            {
                trace.Add("pipe");
                context.Result = result;
                return Task.CompletedTask;
            }));

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            host.SendAsync(observation.Context, Next(trace)));

        Assert.Same(expected, actual);
        Assert.Same(expected, observation.Fault);
        Assert.Equal(["pipe", "evaluate", "faulted"], trace);
        Assert.Equal(1, result.EvaluationCount);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execute-host-preserves-the-recorded-failure-result")]
    public async Task ExecuteHost_PreservesTheMatchingFailureResultBeforeContinuingAsync()
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateExecuteObservation(trace, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("activity execution failed");
        var result = new RecordingExecutionResult(trace, activityFailure: expected);
        var host = new ExecuteActivityHost<TestActivity, ActivityArguments>(
            new DelegatePipe<ExecuteContext<ActivityArguments>>(context =>
            {
                trace.Add("pipe");
                context.Result = result;
                return Task.FromException(expected);
            }),
            null);

        await host.SendAsync(observation.Context, Next(trace));

        Assert.Equal(["pipe", "evaluate", "consumed", "next"], trace);
        Assert.Equal(1, result.EvaluationCount);
        Assert.Null(observation.Fault);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensate-host-preserves-the-recorded-failure-result")]
    public async Task CompensateHost_PreservesTheMatchingFailureResultBeforeContinuingAsync()
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateCompensateObservation(trace, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("activity compensation failed");
        var result = new RecordingCompensationResult(trace, activityFailure: expected);
        var host = new CompensateActivityHost<TestActivity, ActivityLog>(
            new DelegatePipe<CompensateContext<ActivityLog>>(context =>
            {
                trace.Add("pipe");
                context.Result = result;
                return Task.FromException(expected);
            }));

        await host.SendAsync(observation.Context, Next(trace));

        Assert.Equal(["pipe", "evaluate", "consumed", "next"], trace);
        Assert.Equal(1, result.EvaluationCount);
        Assert.Null(observation.Fault);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execute-host-replaces-nonmatching-recorded-results")]
    public async Task ExecuteHost_ReplacesNonFaultedAndDifferentlyFaultedRecordedResultsAsync(bool differentlyFaulted)
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateExecuteObservation(trace, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("activity execution failed");
        Exception? recordedFailure = differentlyFaulted
            ? new InvalidOperationException("different activity execution failure")
            : null;
        var recordedResult = new RecordingExecutionResult(trace, activityFailure: recordedFailure);
        var host = new ExecuteActivityHost<TestActivity, ActivityArguments>(
            new DelegatePipe<ExecuteContext<ActivityArguments>>(context =>
            {
                trace.Add("pipe");
                context.Result = recordedResult;
                return Task.FromException(expected);
            }),
            null);

        await host.SendAsync(observation.Context, Next(trace));

        Assert.Equal(["pipe", "consumed", "next"], trace);
        Assert.Equal(0, recordedResult.EvaluationCount);
        Assert.Null(observation.Fault);
        Assert.Collection(
            observation.Outgoing.Messages,
            message =>
            {
                var faulted = Assert.IsAssignableFrom<IRoutingSlipActivityFaulted>(message);
                Assert.Equal(TypeCache<InvalidOperationException>.ShortName, faulted.ExceptionInfo.ExceptionType);
                Assert.Equal(expected.Message, faulted.ExceptionInfo.Message);
            },
            message => Assert.IsAssignableFrom<IRoutingSlipFaulted>(message));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensate-host-replaces-nonmatching-recorded-results")]
    public async Task CompensateHost_ReplacesNonFailedAndDifferentlyFailedRecordedResultsAsync(bool differentlyFailed)
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateCompensateObservation(trace, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("activity compensation failed");
        Exception? recordedFailure = differentlyFailed
            ? new InvalidOperationException("different activity compensation failure")
            : null;
        var recordedResult = new RecordingCompensationResult(trace, activityFailure: recordedFailure);
        var host = new CompensateActivityHost<TestActivity, ActivityLog>(
            new DelegatePipe<CompensateContext<ActivityLog>>(context =>
            {
                trace.Add("pipe");
                context.Result = recordedResult;
                return Task.FromException(expected);
            }));

        await host.SendAsync(observation.Context, Next(trace));

        Assert.Equal(["pipe", "consumed", "next"], trace);
        Assert.Equal(0, recordedResult.EvaluationCount);
        Assert.Null(observation.Fault);
        Assert.Collection(
            observation.Outgoing.Messages,
            message =>
            {
                var failed = Assert.IsAssignableFrom<IRoutingSlipActivityCompensationFailed>(message);
                Assert.Equal(TypeCache<InvalidOperationException>.ShortName, failed.ExceptionInfo.ExceptionType);
                Assert.Equal(expected.Message, failed.ExceptionInfo.Message);
            },
            message =>
            {
                var failed = Assert.IsAssignableFrom<IRoutingSlipCompensationFailed>(message);
                Assert.Equal(TypeCache<InvalidOperationException>.ShortName, failed.ExceptionInfo.ExceptionType);
                Assert.Equal(expected.Message, failed.ExceptionInfo.Message);
            });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "execute-host-synthesizes-missing-and-thrown-fault-results-once")]
    public async Task ExecuteHost_MissingResultAndPipeFailureEachProduceOneFaultTransitionAsync(bool throwFromPipe)
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateExecuteObservation(trace, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("activity execution failed");
        var host = new ExecuteActivityHost<TestActivity, ActivityArguments>(
            new DelegatePipe<ExecuteContext<ActivityArguments>>(_ =>
            {
                trace.Add("pipe");
                return throwFromPipe ? Task.FromException(expected) : Task.CompletedTask;
            }),
            null);

        await host.SendAsync(observation.Context, Next(trace));

        Assert.Equal(["pipe", "consumed", "next"], trace);
        Assert.Null(observation.Fault);
        Assert.Collection(
            observation.Outgoing.Messages,
            message =>
            {
                var faulted = Assert.IsAssignableFrom<IRoutingSlipActivityFaulted>(message);
                Assert.Equal(
                    throwFromPipe
                        ? TypeCache<InvalidOperationException>.ShortName
                        : TypeCache<ActivityExecutionException>.ShortName,
                    faulted.ExceptionInfo.ExceptionType);
                Assert.Equal(
                    throwFromPipe
                        ? expected.Message
                        : "The activity execute did not return a result",
                    faulted.ExceptionInfo.Message);
            },
            message => Assert.IsAssignableFrom<IRoutingSlipFaulted>(message));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-ACTIVITIES", "compensate-host-synthesizes-missing-and-thrown-failure-results-once")]
    public async Task CompensateHost_MissingResultAndPipeFailureEachProduceOneFailureTransitionAsync(bool throwFromPipe)
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateCompensateObservation(trace, TestContext.Current.CancellationToken);
        var expected = new InvalidOperationException("activity compensation failed");
        var host = new CompensateActivityHost<TestActivity, ActivityLog>(
            new DelegatePipe<CompensateContext<ActivityLog>>(_ =>
            {
                trace.Add("pipe");
                return throwFromPipe ? Task.FromException(expected) : Task.CompletedTask;
            }));

        await host.SendAsync(observation.Context, Next(trace));

        Assert.Equal(["pipe", "consumed", "next"], trace);
        Assert.Null(observation.Fault);
        Assert.Collection(
            observation.Outgoing.Messages,
            message =>
            {
                var failed = Assert.IsAssignableFrom<IRoutingSlipActivityCompensationFailed>(message);
                Assert.Equal(
                    throwFromPipe
                        ? TypeCache<InvalidOperationException>.ShortName
                        : TypeCache<ActivityCompensationException>.ShortName,
                    failed.ExceptionInfo.ExceptionType);
                Assert.Equal(
                    throwFromPipe
                        ? expected.Message
                        : "The activity compensation did not return a result",
                    failed.ExceptionInfo.Message);
            },
            message => Assert.IsAssignableFrom<IRoutingSlipCompensationFailed>(message));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "host-classifies-activity-and-delivery-cancellation-separately")]
    public async Task Hosts_ClassifyActivityAndDeliveryCancellationSeparatelyAsync()
    {
        var executeTrace = new List<string>();
        HostContextObservation executeObservation = CreateExecuteObservation(executeTrace, TestContext.Current.CancellationToken);
        var activityCancellation = new OperationCanceledException("activity canceled");
        var executeHost = new ExecuteActivityHost<TestActivity, ActivityArguments>(
            new DelegatePipe<ExecuteContext<ActivityArguments>>(_ => Task.FromException(activityCancellation)),
            null);

        ConsumerCanceledException converted = await Assert.ThrowsAsync<ConsumerCanceledException>(() =>
            executeHost.SendAsync(executeObservation.Context, Next(executeTrace)));

        Assert.Same(activityCancellation, converted.InnerException);
        Assert.Same(converted, executeObservation.Fault);
        Assert.Equal(["faulted"], executeTrace);
        Assert.Empty(executeObservation.Outgoing.Messages);

        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var compensateTrace = new List<string>();
        HostContextObservation compensateObservation = CreateCompensateObservation(compensateTrace, cancellation.Token);
        var deliveryCancellation = new OperationCanceledException("delivery canceled", cancellation.Token);
        var compensateHost = new CompensateActivityHost<TestActivity, ActivityLog>(
            new DelegatePipe<CompensateContext<ActivityLog>>(_ => Task.FromException(deliveryCancellation)));

        OperationCanceledException preserved = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            compensateHost.SendAsync(compensateObservation.Context, Next(compensateTrace)));

        Assert.Same(deliveryCancellation, preserved);
        Assert.Same(deliveryCancellation, compensateObservation.Fault);
        Assert.Equal(["faulted"], compensateTrace);
        Assert.Empty(compensateObservation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "execute-host-preserves-delivery-cancellation")]
    public async Task ExecuteHost_PreservesDeliveryTokenCancellationAndSkipsTheNextPipeAsync()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var trace = new List<string>();
        HostContextObservation observation = CreateExecuteObservation(trace, cancellation.Token);
        var expected = new OperationCanceledException("delivery canceled", cancellation.Token);
        var host = new ExecuteActivityHost<TestActivity, ActivityArguments>(
            new DelegatePipe<ExecuteContext<ActivityArguments>>(_ => Task.FromException(expected)),
            null);

        OperationCanceledException actual = await Assert.ThrowsAsync<OperationCanceledException>(() =>
            host.SendAsync(observation.Context, Next(trace)));

        Assert.Same(expected, actual);
        Assert.Same(expected, observation.Fault);
        Assert.Equal(["faulted"], trace);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-CANCELLATION", "compensate-host-converts-activity-owned-cancellation")]
    public async Task CompensateHost_ConvertsActivityOwnedCancellationAndSkipsTheNextPipeAsync()
    {
        var trace = new List<string>();
        HostContextObservation observation = CreateCompensateObservation(trace, TestContext.Current.CancellationToken);
        var expected = new OperationCanceledException("activity compensation canceled");
        var host = new CompensateActivityHost<TestActivity, ActivityLog>(
            new DelegatePipe<CompensateContext<ActivityLog>>(_ => Task.FromException(expected)));

        ConsumerCanceledException actual = await Assert.ThrowsAsync<ConsumerCanceledException>(() =>
            host.SendAsync(observation.Context, Next(trace)));

        Assert.Same(expected, actual.InnerException);
        Assert.Same(actual, observation.Fault);
        Assert.Equal(["faulted"], trace);
        Assert.Empty(observation.Outgoing.Messages);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-DISPATCH", "execute-dispatcher-configures-the-exact-activity-contract")]
    public void ExecuteDispatcher_UsesTheFormattedQueueAndConfiguresTheExactActivityContract()
    {
        var factory = new RecordingDispatcherFactory();
        IEndpointNameFormatter formatter = KebabCaseEndpointNameFormatter.Instance;
        var dispatcherFactory = new ExecuteActivityReceiveEndpointDispatcher<TestActivity, ActivityArguments>();

        IReceiveEndpointDispatcher dispatcher = dispatcherFactory.Create(factory, formatter);

        Assert.Same(factory.Dispatcher, dispatcher);
        Assert.Equal(1, factory.CreateReceiverCalls);
        Assert.Equal(formatter.ExecuteActivity<TestActivity, ActivityArguments>(), factory.QueueName);
        Action<IReceiveEndpointConfigurator, IRegistrationContext> configure = Assert.IsType<
            Action<IReceiveEndpointConfigurator, IRegistrationContext>>(factory.Configure);
        IReceiveEndpointConfigurator endpoint = DispatchProxy.Create<IReceiveEndpointConfigurator, UnsupportedProxy>();
        IRegistrationContext registration = DispatchProxy.Create<IRegistrationContext, RegistrationContextProxy>();

        configure(endpoint, registration);

        var registrationProxy = (RegistrationContextProxy)(object)registration;
        Assert.Equal(typeof(TestActivity), registrationProxy.ActivityType);
        Assert.Same(endpoint, registrationProxy.Endpoint);
    }

    private static HostContextObservation CreateExecuteObservation(List<string> trace, CancellationToken cancellationToken)
    {
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        builder.AddActivity("Execute", new Uri("loopback://localhost/execute"), new ActivityArguments("input"));
        return CreateObservation(builder.Build(), trace, cancellationToken);
    }

    private static HostContextObservation CreateCompensateObservation(List<string> trace, CancellationToken cancellationToken)
    {
        Guid executionId = NewId.NextGuid();
        var builder = new RoutingSlipBuilder(NewId.NextGuid(), new FakeTimeProvider(CreatedAt));
        builder.AddActivityLog(HostMetadataCache.Host, "Compensate", executionId, CreatedAt, TimeSpan.Zero);
        builder.AddCompensateLog(executionId, new Uri("loopback://localhost/compensate"),
            new Dictionary<string, object> { [nameof(ActivityLog.Value)] = "input" });
        return CreateObservation(builder.Build(), trace, cancellationToken);
    }

    private static HostContextObservation CreateObservation(
        IRoutingSlip routingSlip,
        List<string> trace,
        CancellationToken cancellationToken)
    {
        SerializerContext serializerContext = CreateSerializerContext(routingSlip);
        var outgoing = new OutgoingMessageRecorder();
        ConsumeContext<IRoutingSlip> inner = InMemoryOutboxTestContextFactory.Create(
            routingSlip,
            cancellationToken,
            outgoingMessages: outgoing,
            serializerContext: serializerContext);
        inner.SetTimeProvider(new FakeTimeProvider(CreatedAt));

        HostConsumeContext context = DispatchProxy.Create<HostConsumeContext, HostConsumeContextProxy>();
        var proxy = (HostConsumeContextProxy)(object)context;
        proxy.Inner = inner;
        proxy.Trace = trace;

        return new HostContextObservation(context, proxy, outgoing);
    }

    private static SerializerContext CreateSerializerContext(IRoutingSlip routingSlip)
    {
        IObjectDeserializer deserializer = ServiceBusMetadataJson.ObjectDeserializer;
        var metadata = new EnvelopeMessageContext(new JsonMessageEnvelope(), deserializer);
        return new SystemTextJsonSerializerContext(
            deserializer,
            ServiceBusMetadataJson.Options,
            SystemTextJsonMessageSerializer.JsonContentType,
            metadata,
            [MessageUrn.ForTypeString<IRoutingSlip>()],
            message: routingSlip);
    }

    private static IPipe<ConsumeContext<IRoutingSlip>> Next(List<string> trace) =>
        new DelegatePipe<ConsumeContext<IRoutingSlip>>(_ =>
        {
            trace.Add("next");
            return Task.CompletedTask;
        });

    private sealed record HostContextObservation(
        HostConsumeContext Context,
        HostConsumeContextProxy Proxy,
        OutgoingMessageRecorder Outgoing)
    {
        public Exception? Fault => Proxy.Fault;
    }

    private interface HostConsumeContext : ConsumeContext<IRoutingSlip>, ConsumeContext;

    private class HostConsumeContextProxy : DispatchProxy
    {
        public Exception? Fault { get; private set; }
        public ConsumeContext<IRoutingSlip> Inner { get; set; } = null!;
        public List<string> Trace { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            ArgumentNullException.ThrowIfNull(targetMethod);

            switch (targetMethod.Name)
            {
                case "NotifyConsumedAsync":
                    Trace.Add("consumed");
                    return Task.CompletedTask;
                case "NotifyFaultedAsync":
                    Trace.Add("faulted");
                    Fault = (Exception)args![3]!;
                    return Task.CompletedTask;
                default:
                    try
                    {
                        return targetMethod.Invoke(Inner, args);
                    }
                    catch (TargetInvocationException exception) when (exception.InnerException is not null)
                    {
                        ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                        throw;
                    }
            }
        }
    }

    private sealed class RecordingExecutionResult(
        List<string> trace,
        Exception? evaluationFailure = null,
        Exception? activityFailure = null) : ExecutionResult
    {
        public int EvaluationCount { get; private set; }

        public Task EvaluateAsync(CancellationToken cancellationToken = default)
        {
            EvaluationCount++;
            trace.Add("evaluate");
            return evaluationFailure is null ? Task.CompletedTask : Task.FromException(evaluationFailure);
        }

        public bool IsFaulted([NotNullWhen(true)] out Exception? exception)
        {
            exception = activityFailure;
            return exception is not null;
        }
    }

    private sealed class RecordingCompensationResult(
        List<string> trace,
        Exception? evaluationFailure = null,
        Exception? activityFailure = null) : CompensationResult
    {
        public int EvaluationCount { get; private set; }

        public Task EvaluateAsync(CancellationToken cancellationToken = default)
        {
            EvaluationCount++;
            trace.Add("evaluate");
            return evaluationFailure is null ? Task.CompletedTask : Task.FromException(evaluationFailure);
        }

        public bool IsFailed([NotNullWhen(true)] out Exception? exception)
        {
            exception = activityFailure;
            return exception is not null;
        }
    }

    private sealed class DelegatePipe<TContext>(Func<TContext, Task> callback) : IPipe<TContext>
        where TContext : class, PipeContext
    {
        public Task SendAsync(TContext context) => callback(context);

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class TestActivity : IActivity<ActivityArguments, ActivityLog>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<ActivityArguments> context) =>
            throw new NotSupportedException();

        public Task<CompensationResult> CompensateAsync(CompensateContext<ActivityLog> context) =>
            throw new NotSupportedException();
    }

    private sealed record ActivityArguments(string Value);

    private sealed record ActivityLog(string Value);

    private sealed class RecordingDispatcherFactory : IReceiveEndpointDispatcherFactory
    {
        public RecordingDispatcherFactory()
        {
            Dispatcher = DispatchProxy.Create<IReceiveEndpointDispatcher, UnsupportedProxy>();
        }

        public Action<IReceiveEndpointConfigurator, IRegistrationContext>? Configure { get; private set; }
        public int CreateReceiverCalls { get; private set; }
        public IReceiveEndpointDispatcher Dispatcher { get; }
        public string? QueueName { get; private set; }

        public IReceiveEndpointDispatcher CreateReceiver(string queueName) => throw new NotSupportedException();

        public IReceiveEndpointDispatcher CreateReceiver(
            string queueName,
            Action<IReceiveEndpointConfigurator, IRegistrationContext> configure)
        {
            CreateReceiverCalls++;
            QueueName = queueName;
            Configure = configure;
            return Dispatcher;
        }

        public IReceiveEndpointDispatcher CreateRegistrationReceiver(
            Type registrationType,
            string fallbackQueueName,
            IEndpointNameFormatter formatter) => throw new NotSupportedException();

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class RegistrationContextProxy : DispatchProxy
    {
        public Type? ActivityType { get; private set; }
        public IReceiveEndpointConfigurator? Endpoint { get; private set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "ConfigureExecuteActivity")
            {
                ActivityType = (Type)args![0]!;
                Endpoint = (IReceiveEndpointConfigurator)args[1]!;
                return null;
            }

            throw new NotSupportedException($"Unexpected registration member: {targetMethod?.Name}");
        }
    }

    private class UnsupportedProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"Unexpected member: {targetMethod?.Name}");
    }
}
