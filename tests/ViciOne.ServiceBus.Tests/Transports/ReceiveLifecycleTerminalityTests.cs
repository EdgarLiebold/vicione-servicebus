using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Events.Receiving;
using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.RetryPolicies;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.InternalAccess.Transports;
using ViciOne.ServiceBus.Tests.Testing;
using ViciOne.ServiceBus.Transports;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class ReceiveLifecycleTerminalityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-ENDPOINT-LIFETIME", "required-constructor-dependencies")]
    public void Constructor_RejectsEachMissingRequiredDependency()
    {
        Assert.Equal(
            "transport",
            Assert.Throws<ArgumentNullException>(() => new ReceiveEndpoint(null!, new TestReceiveEndpointContext())).ParamName);
        Assert.Equal(
            "context",
            Assert.Throws<ArgumentNullException>(() => new ReceiveEndpoint(new ScriptedReceiveTransport(), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-ENDPOINT-LIFETIME", "coherent-initial-health")]
    public void Constructor_ExposesACompleteUnhealthyObservationBeforeStartup()
    {
        var endpoint = new ReceiveEndpoint(new ScriptedReceiveTransport(), new TestReceiveEndpointContext());

        Assert.Equal(ReceiveEndpoint.State.Initial, endpoint.CurrentState);
        Assert.Equal("not ready", endpoint.Message);
        Assert.Equal(BusHealthStatus.Unhealthy, endpoint.HealthResult.Status);
        Assert.Equal("not ready", endpoint.HealthResult.Description);
        Assert.Same(endpoint, endpoint.HealthResult.ReceiveEndpoint);
        Assert.Equal(InputAddress, endpoint.HealthResult.InputAddress);
        Assert.Null(endpoint.HealthResult.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-ENDPOINT-LIFETIME", "stop-awaits-context-reset")]
    public async Task Stop_AwaitsOwnedProviderReleaseBeforeCompletingAsync()
    {
        var context = new TestReceiveEndpointContext(pauseReset: true);
        var endpoint = new ReceiveEndpoint(new ScriptedReceiveTransport(), context);
        endpoint.Start(CancellationToken.None);

        Task stop = endpoint.StopAsync(TestContext.Current.CancellationToken);
        await context.ResetStarted.Task.WaitAsync(TestContext.Current.CancellationToken);

        Assert.False(stop.IsCompleted);
        Assert.Equal(1, context.ResetCount);

        context.ReleaseReset();
        await stop.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(1, context.ResetCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-ENDPOINT-LIFETIME", "stop-terminalizes-pending-handle-readiness")]
    public async Task StopBeforeReady_CancelsHandleReadinessAndRejectsLateReadinessAsync()
    {
        var transport = new ScriptedReceiveTransport();
        var endpoint = new ReceiveEndpoint(transport, new TestReceiveEndpointContext());
        IReceiveEndpointHandle handle = endpoint.Start(CancellationToken.None);

        await handle.StopAsync(TestContext.Current.CancellationToken);

        Assert.True(handle.Ready.IsCanceled);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handle.Ready);
        Assert.Equal(CancellationToken.None, exception.CancellationToken);

        await transport.NotifyReadyAsync().WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(handle.Ready.IsCanceled);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "explicit-terminality-contract")]
    public void FaultEvents_PreserveTheExactCauseAndExplicitTerminality(bool isTerminal)
    {
        var expected = new ExpectedTransportException("expected");
        var endpoint = new StubReceiveEndpoint();
        var transportFault = new ReceiveTransportFaultedEvent(InputAddress, expected, isTerminal);
        var endpointFault = new ReceiveEndpointFaultedEvent(transportFault, endpoint);

        Assert.Equal(InputAddress, transportFault.InputAddress);
        Assert.Same(expected, transportFault.Exception);
        Assert.Equal(isTerminal, transportFault.IsTerminal);
        Assert.Equal(InputAddress, endpointFault.InputAddress);
        Assert.Same(expected, endpointFault.Exception);
        Assert.Equal(isTerminal, endpointFault.IsTerminal);
        Assert.Same(endpoint, endpointFault.ReceiveEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-START", "synchronous-fault-rolls-back-handle")]
    public async Task SynchronousStartFailure_RollsBackTheHandleAndAllowsAHealthyRetryAsync()
    {
        var expected = new ExpectedTransportException("start failed");
        var transport = new ScriptedReceiveTransport(expected);
        var context = new TestReceiveEndpointContext();
        var endpoint = new ReceiveEndpoint(transport, context);

        ExpectedTransportException actual = Assert.Throws<ExpectedTransportException>(() => endpoint.Start(CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(ReceiveEndpoint.State.Faulted, endpoint.CurrentState);
        Assert.Contains(expected.Message, endpoint.Message, StringComparison.Ordinal);

        IReceiveEndpointHandle handle = endpoint.Start(CancellationToken.None);
        await transport.NotifyReadyAsync().WaitAsync(TestContext.Current.CancellationToken);
        ReceiveEndpointReady ready = await handle.Ready.WaitAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, transport.StartCount);
        Assert.Equal(InputAddress, ready.InputAddress);
        Assert.Same(endpoint, ready.ReceiveEndpoint);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-START", "synchronous-completion-remains-stopped")]
    public void SynchronousTransportCompletion_IsNotOverwrittenByTheStartingState()
    {
        var endpoint = new ReceiveEndpoint(
            new SynchronouslyCompletingReceiveTransport(),
            new TestReceiveEndpointContext());

        endpoint.Start(CancellationToken.None);

        Assert.Equal(ReceiveEndpoint.State.Stopped, endpoint.CurrentState);
        Assert.Contains("stopped", endpoint.Message, StringComparison.Ordinal);
        Assert.Equal(BusHealthStatus.Degraded, endpoint.HealthResult.Status);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-START", "failed-dynamic-start-retains-registration")]
    public async Task CollectionStartFailure_RetainsTheEndpointForAHealthyRetryAsync()
    {
        var expected = new ExpectedTransportException("start failed");
        var transport = new ScriptedReceiveTransport(expected);
        var collection = new ReceiveEndpointCollection();
        collection.Add("orders", new ReceiveEndpoint(transport, new TestReceiveEndpointContext()));

        Assert.Throws<ExpectedTransportException>(() => collection.Start("orders", CancellationToken.None));

        IHostReceiveEndpointHandle handle = collection.Start("orders", CancellationToken.None);
        Assert.Equal(2, transport.StartCount);
        await handle.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-STOP", "failed-stop-retries-before-dynamic-removal")]
    public async Task CollectionHandleStopFailure_RetainsTheEndpointUntilAHealthyRetryAsync()
    {
        var expected = new ExpectedTransportException("stop failed");
        var transport = new FailingStopReceiveTransport(expected);
        var collection = new ReceiveEndpointCollection();
        collection.Add("orders", new ReceiveEndpoint(transport, new TestReceiveEndpointContext()));
        IHostReceiveEndpointHandle handle = collection.Start("orders", CancellationToken.None);

        ExpectedTransportException actual = await Assert.ThrowsAsync<ExpectedTransportException>(() =>
            handle.StopAsync(TestContext.Current.CancellationToken));

        Assert.Same(expected, actual);
        Assert.Equal(1, transport.StopCount);
        Assert.Throws<ArgumentException>(() => collection.Start("orders", CancellationToken.None));

        await handle.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, transport.StopCount);
        Assert.Throws<ConfigurationException>(() => collection.Start("orders", CancellationToken.None));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-STOP", "failed-provider-reset-is-retryable")]
    public async Task StopResetFailure_RemainsPendingUntilAHealthyRetryAsync()
    {
        var expected = new ExpectedTransportException("reset failed");
        var context = new TestReceiveEndpointContext(resetFailure: expected);
        var endpoint = new ReceiveEndpoint(new ScriptedReceiveTransport(), context);
        endpoint.Start(CancellationToken.None);

        ExpectedTransportException actual = await Assert.ThrowsAsync<ExpectedTransportException>(() =>
            endpoint.StopAsync(TestContext.Current.CancellationToken));
        Assert.Same(expected, actual);
        Assert.Equal(1, context.ResetCount);

        await endpoint.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, context.ResetCount);
        Assert.Equal(ReceiveEndpoint.State.Stopped, endpoint.CurrentState);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-RESTART", "pause-and-restart-have-explicit-states")]
    public async Task PolicyPauseAndRestart_ExposeTheirActualLifecycleStatesAsync()
    {
        var transport = new ScriptedReceiveTransport();
        var endpoint = new ReceiveEndpoint(transport, new TestReceiveEndpointContext());
        endpoint.Start(CancellationToken.None);
        var restartable = (IRestartableReceiveEndpoint)endpoint;

        await restartable.PauseAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReceiveEndpoint.State.Paused, endpoint.CurrentState);
        Assert.Equal("paused", endpoint.Message);

        await restartable.RestartAsync(TestContext.Current.CancellationToken);

        Assert.Equal(ReceiveEndpoint.State.Starting, endpoint.CurrentState);
        Assert.Equal(2, transport.StartCount);
        await endpoint.StopAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-START", "already-canceled-token-keeps-identity")]
    public async Task AlreadyCanceledStart_RejectsBeforeTransportStartAndKeepsTheExactTokenAsync()
    {
        using var canceled = new CancellationTokenSource();
        canceled.Cancel();
        var transport = new ScriptedReceiveTransport();
        var endpoint = new ReceiveEndpoint(transport, new TestReceiveEndpointContext());

        OperationCanceledException exception = Assert.ThrowsAny<OperationCanceledException>(() => endpoint.Start(canceled.Token));

        Assert.Equal(canceled.Token, exception.CancellationToken);
        Assert.Equal(0, transport.StartCount);
        Assert.Equal(ReceiveEndpoint.State.Initial, endpoint.CurrentState);

        IReceiveEndpointHandle handle = endpoint.Start(CancellationToken.None);
        await transport.NotifyReadyAsync().WaitAsync(TestContext.Current.CancellationToken);
        Assert.Equal(InputAddress, (await handle.Ready.WaitAsync(TestContext.Current.CancellationToken)).InputAddress);
        Assert.Equal(1, transport.StartCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-FAULT", "nonterminal-does-not-fail-readiness")]
    public async Task NonterminalFault_LeavesReadinessPendingUntilATerminalFaultPreservesItsCauseAsync()
    {
        var transport = new ScriptedReceiveTransport();
        var endpoint = new ReceiveEndpoint(transport, new TestReceiveEndpointContext());
        IReceiveEndpointHandle handle = endpoint.Start(CancellationToken.None);
        var transient = new ExpectedTransportException("transient");
        var terminal = new ExpectedTransportException("terminal");

        await transport.NotifyFaultedAsync(transient, false).WaitAsync(TestContext.Current.CancellationToken);
        Assert.False(handle.Ready.IsCompleted);

        await transport.NotifyFaultedAsync(terminal, true).WaitAsync(TestContext.Current.CancellationToken);
        ExpectedTransportException actual = await Assert.ThrowsAsync<ExpectedTransportException>(() => handle.Ready);

        Assert.Same(terminal, actual);
        Assert.NotSame(transient, actual);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-CANCELLATION", "pending-readiness-keeps-token")]
    public async Task CancellationAfterStart_CancelsPendingReadinessWithTheExactTokenAsync()
    {
        using var cancellation = new CancellationTokenSource();
        var transport = new ScriptedReceiveTransport();
        var endpoint = new ReceiveEndpoint(transport, new TestReceiveEndpointContext());
        IReceiveEndpointHandle handle = endpoint.Start(cancellation.Token);

        cancellation.Cancel();
        Assert.True(handle.Ready.IsCanceled);
        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => handle.Ready);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        await transport.NotifyFaultedAsync(new ExpectedTransportException("late terminal"), true)
            .WaitAsync(TestContext.Current.CancellationToken);
        Assert.True(handle.Ready.IsCanceled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-RETRY", "attempt-faults-and-terminal-exhaustion")]
    public async Task RetryAttempts_AreNonterminalUntilTheRetryOwnerPublishesExhaustionAsync()
    {
        var first = new ConnectionException("first", isTransient: true);
        var terminal = new ConnectionException("terminal", isTransient: true);
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var retryTrace = new List<string>();
        var host = new TestHostConfiguration(new TrackingReceiveRetryPolicy(retryTrace, 1));
        var pipe = new ScriptedTransportPipe(first, terminal);
        var transport = new ReceiveTransport<TestPipeContext>(
            host,
            context,
            () => new TestTransportSupervisor(new TestPipeContext()),
            pipe);

        ReceiveTransportHandle handle = transport.Start();
        ReceiveTransportFaulted terminalEvent = await observer.ThirdFault.WaitAsync(TestContext.Current.CancellationToken);
        await handle.StopAsync(TestContext.Current.CancellationToken);

        ReceiveTransportFaulted[] faults = observer.Faults;
        Assert.Equal(3, faults.Length);
        Assert.Collection(
            faults,
            fault =>
            {
                Assert.False(fault.IsTerminal);
                Assert.Same(first, fault.Exception);
            },
            fault =>
            {
                Assert.False(fault.IsTerminal);
                Assert.Same(terminal, fault.Exception);
            },
            fault =>
            {
                Assert.True(fault.IsTerminal);
                Assert.Same(terminal, fault.Exception);
            });
        Assert.Same(terminalEvent, faults[^1]);
        Assert.Equal(2, pipe.AttemptCount);
        Assert.Equal(["before:1", "terminal:terminal"], retryTrace);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-RETRY", "policy-context-start-failure-is-terminal")]
    public async Task RetryPolicyContextStartFailure_FaultsEndpointReadinessAndPublishesOneTerminalCauseAsync(bool returnsNull)
    {
        var expected = new InvalidOperationException("retry policy failed to initialize");
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        int supervisorCreations = 0;
        var transport = new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(new FailingContextReceiveRetryPolicy(expected, returnsNull)),
            context,
            () =>
            {
                supervisorCreations++;
                return new TestTransportSupervisor(new TestPipeContext());
            },
            new ScriptedTransportPipe());
        var endpoint = new ReceiveEndpoint(transport, context);

        IReceiveEndpointHandle handle = endpoint.Start(TestContext.Current.CancellationToken);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handle.Ready.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        ReceiveTransportFaulted fault = await observer.TerminalFault.WaitAsync(
            TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        await handle.StopAsync(TestContext.Current.CancellationToken);

        if (!returnsNull)
            Assert.Same(expected, actual);
        else
            Assert.Contains("null policy context", actual.Message, StringComparison.Ordinal);
        Assert.Same(actual, fault.Exception);
        Assert.True(fault.IsTerminal);
        Assert.Single(observer.Faults);
        Assert.Equal(0, supervisorCreations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-RETRY", "policy-decision-failure-is-terminal")]
    public async Task RetryDecisionFailure_PreservesPolicyCauseAndStopsAfterTheFirstAttemptAsync()
    {
        var attemptFailure = new ConnectionException("transport attempt failed", isTransient: true);
        var policyFailure = new InvalidOperationException("retry decision failed");
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var pipe = new ScriptedTransportPipe(attemptFailure);
        var transport = new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(new FailingDecisionReceiveRetryPolicy(policyFailure)),
            context,
            () => new TestTransportSupervisor(new TestPipeContext()),
            pipe);
        var endpoint = new ReceiveEndpoint(transport, context);

        IReceiveEndpointHandle handle = endpoint.Start(TestContext.Current.CancellationToken);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handle.Ready.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        await handle.StopAsync(TestContext.Current.CancellationToken);

        Assert.Same(policyFailure, actual);
        Assert.Collection(observer.Faults,
            fault =>
            {
                Assert.False(fault.IsTerminal);
                Assert.Same(attemptFailure, fault.Exception);
            },
            fault =>
            {
                Assert.True(fault.IsTerminal);
                Assert.Same(policyFailure, fault.Exception);
            });
        Assert.Equal(1, pipe.AttemptCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-RETRY", "throwing-error-logger-preserves-terminal-fault")]
    public async Task ThrowingErrorLogger_DoesNotStrandReadinessOrReplaceTheTransportFailureAsync()
    {
        var attemptFailure = new ConnectionException("transport attempt failed", isTransient: true);
        var loggerFailure = new InvalidOperationException("logger failed");
        var logger = new ThrowingErrorLogger(loggerFailure);
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var pipe = new ScriptedTransportPipe(attemptFailure);
        var endpoint = new ReceiveEndpoint(new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(Retry.None), context,
            () => new TestTransportSupervisor(new TestPipeContext()), pipe), context);
        ILogContext? previous = LogContext.Current;

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            IReceiveEndpointHandle handle = endpoint.Start(TestContext.Current.CancellationToken);
            ConnectionException actual = await Assert.ThrowsAsync<ConnectionException>(
                () => handle.Ready.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            await handle.StopAsync(TestContext.Current.CancellationToken);

            Assert.Same(attemptFailure, actual);
            Assert.Same(attemptFailure, logger.ObservedFailure);
            Assert.Equal(1, logger.CallCount);
            Assert.Collection(observer.Faults,
                fault => { Assert.False(fault.IsTerminal); Assert.Same(attemptFailure, fault.Exception); },
                fault => { Assert.True(fault.IsTerminal); Assert.Same(attemptFailure, fault.Exception); });
            Assert.Equal(1, pipe.AttemptCount);
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-RETRY", "throwing-info-logger-does-not-consume-retry")]
    public async Task ThrowingInfoLogger_DoesNotConsumeTheConfiguredRetryOrReplaceItsFailureAsync()
    {
        var firstFailure = new ConnectionException("first attempt failed", isTransient: true);
        var terminalFailure = new ConnectionException("second attempt failed", isTransient: true);
        var logger = new ThrowingInfoLogger(new InvalidOperationException("info logger failed"));
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var pipe = new ScriptedTransportPipe(firstFailure, terminalFailure);
        var endpoint = new ReceiveEndpoint(new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(Retry.Interval(1, TimeSpan.Zero)), context,
            () => new TestTransportSupervisor(new TestPipeContext()), pipe), context);
        ILogContext? previous = LogContext.Current;

        try
        {
            LogContext.ConfigureCurrentLogContext(logger);
            IReceiveEndpointHandle handle = endpoint.Start(TestContext.Current.CancellationToken);
            ConnectionException actual = await Assert.ThrowsAsync<ConnectionException>(
                () => handle.Ready.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
            await handle.StopAsync(TestContext.Current.CancellationToken);

            Assert.Same(terminalFailure, actual);
            Assert.Equal(1, logger.CallCount);
            Assert.Same(firstFailure, logger.ObservedFailure);
            Assert.Equal(2, pipe.AttemptCount);
            Assert.Collection(observer.Faults,
                fault => { Assert.False(fault.IsTerminal); Assert.Same(firstFailure, fault.Exception); },
                fault => { Assert.False(fault.IsTerminal); Assert.Same(terminalFailure, fault.Exception); },
                fault => { Assert.True(fault.IsTerminal); Assert.Same(terminalFailure, fault.Exception); });
        }
        finally
        {
            LogContext.Current = previous;
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-RECEIVE-RETRY", "terminal-policy-callback-failure-keeps-readiness-terminal")]
    public async Task TerminalRetryCallbackFailure_ReportsItsOwnCauseAfterTheAttemptAsync(bool returnsNullTask)
    {
        var attemptFailure = new ConnectionException("transport attempt failed", isTransient: true);
        var callbackFailure = new InvalidOperationException("retry fault callback failed");
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var pipe = new ScriptedTransportPipe(attemptFailure);
        var endpoint = new ReceiveEndpoint(new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(new TerminalCallbackReceiveRetryPolicy(callbackFailure, returnsNullTask)),
            context, () => new TestTransportSupervisor(new TestPipeContext()), pipe), context);

        IReceiveEndpointHandle handle = endpoint.Start(TestContext.Current.CancellationToken);
        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handle.Ready.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        await handle.StopAsync(TestContext.Current.CancellationToken);

        if (returnsNullTask)
            Assert.Contains("null retry-faulted task", actual.Message, StringComparison.Ordinal);
        else
            Assert.Same(callbackFailure, actual);
        Assert.Collection(observer.Faults,
            fault => { Assert.False(fault.IsTerminal); Assert.Same(attemptFailure, fault.Exception); },
            fault => { Assert.True(fault.IsTerminal); Assert.Same(actual, fault.Exception); });
        Assert.Equal(1, pipe.AttemptCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-RETRY", "foreign-policy-cancellation-is-terminal")]
    public async Task ForeignPolicyCancellation_IsTerminalWithoutAStopRequestAsync()
    {
        using var foreignCancellation = new CancellationTokenSource();
        foreignCancellation.Cancel();
        var expected = new OperationCanceledException("foreign policy canceled", foreignCancellation.Token);
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var endpoint = new ReceiveEndpoint(new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(new FailingContextReceiveRetryPolicy(expected, returnsNull: false)),
            context, () => new TestTransportSupervisor(new TestPipeContext()), new ScriptedTransportPipe()), context);

        IReceiveEndpointHandle handle = endpoint.Start(TestContext.Current.CancellationToken);
        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handle.Ready.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        await handle.StopAsync(TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        ReceiveTransportFaulted fault = Assert.Single(observer.Faults);
        Assert.True(fault.IsTerminal);
        Assert.Same(expected, fault.Exception);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-STOP", "stop-cancels-terminal-policy-callback-silently")]
    public async Task StopDuringTerminalRetryCallback_DoesNotPublishASecondFaultAsync()
    {
        var attemptFailure = new ConnectionException("transport attempt failed", isTransient: true);
        var policy = new AwaitingStopReceiveRetryPolicy();
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var pipe = new ScriptedTransportPipe(attemptFailure);
        var transport = new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(policy), context,
            () => new TestTransportSupervisor(new TestPipeContext()), pipe);

        ReceiveTransportHandle handle = transport.Start();
        CancellationToken callbackToken = await policy.CallbackEntered.WaitAsync(
            TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        await handle.StopAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        Assert.True(callbackToken.CanBeCanceled);
        Assert.True(callbackToken.IsCancellationRequested);
        ReceiveTransportFaulted fault = Assert.Single(observer.Faults);
        Assert.False(fault.IsTerminal);
        Assert.Same(attemptFailure, fault.Exception);
        Assert.Equal(1, pipe.AttemptCount);
        Assert.False(observer.TerminalFault.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-STOP", "stop-cancels-pre-retry-callback-parameter")]
    public async Task StopDuringPreRetryCallback_CancelsItsParameterWithoutAnotherAttemptAsync()
    {
        var attemptFailure = new ConnectionException("transport attempt failed", isTransient: true);
        var policy = new AwaitingStopReceiveRetryPolicy(retryScheduled: true);
        var context = new TestReceiveEndpointContext();
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var pipe = new ScriptedTransportPipe(attemptFailure);
        var transport = new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(policy), context,
            () => new TestTransportSupervisor(new TestPipeContext()), pipe);

        ReceiveTransportHandle handle = transport.Start();
        CancellationToken callbackToken = await policy.CallbackEntered.WaitAsync(
            TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        await handle.StopAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        Assert.True(callbackToken.CanBeCanceled);
        Assert.True(callbackToken.IsCancellationRequested);
        ReceiveTransportFaulted fault = Assert.Single(observer.Faults);
        Assert.False(fault.IsTerminal);
        Assert.Same(attemptFailure, fault.Exception);
        Assert.Equal(1, pipe.AttemptCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-STOP", "stop-cancels-delay-with-independent-policy-token")]
    public async Task StopDuringRetryDelay_CancelsEvenWhenThePolicyTokenIsIndependentAsync()
    {
        var timeProvider = new ObservableTimeProvider(new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var attemptFailure = new ConnectionException("transport attempt failed", isTransient: true);
        var policy = new AwaitingStopReceiveRetryPolicy(retryScheduled: true, retryDelay: TimeSpan.FromHours(1));
        var context = new TestReceiveEndpointContext();
        context.SetTimeProvider(timeProvider);
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var pipe = new ScriptedTransportPipe(attemptFailure);
        var transport = new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(policy), context,
            () => new TestTransportSupervisor(new TestPipeContext()), pipe);

        ReceiveTransportHandle handle = transport.Start();
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        await handle.StopAsync(TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        ReceiveTransportFaulted fault = Assert.Single(observer.Faults);
        Assert.False(fault.IsTerminal);
        Assert.Same(attemptFailure, fault.Exception);
        Assert.Equal(1, pipe.AttemptCount);
        Assert.False(policy.CallbackEntered.IsCompleted);
    }

    [Theory]
    [InlineData(PolicyCancellationStage.Delay)]
    [InlineData(PolicyCancellationStage.PreRetry)]
    [InlineData(PolicyCancellationStage.TerminalCallback)]
    [RequirementCoverage("REQ-VSB-RECEIVE-RETRY", "policy-cancellation-retains-original-token")]
    public async Task IndependentPolicyCancellation_ReportsItsOriginalTokenAndDoesNotRetryAsync(PolicyCancellationStage stage)
    {
        using var policyCancellation = new CancellationTokenSource();
        var timeProvider = new ObservableTimeProvider(new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var attemptFailure = new ConnectionException("transport attempt failed", isTransient: true);
        var policy = new CancelableReceiveRetryPolicy(policyCancellation.Token, stage);
        var context = new TestReceiveEndpointContext();
        context.SetTimeProvider(timeProvider);
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var pipe = new ScriptedTransportPipe(attemptFailure);
        var endpoint = new ReceiveEndpoint(new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(policy), context,
            () => new TestTransportSupervisor(new TestPipeContext()), pipe), context);

        IReceiveEndpointHandle handle = endpoint.Start(TestContext.Current.CancellationToken);
        if (stage == PolicyCancellationStage.Delay)
            await timeProvider.WaitForTimerCountAsync(1).WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);
        else
            await policy.CallbackEntered.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken);

        policyCancellation.Cancel();
        OperationCanceledException actual = await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => handle.Ready.WaitAsync(TimeSpan.FromSeconds(3), TestContext.Current.CancellationToken));
        await handle.StopAsync(TestContext.Current.CancellationToken);

        Assert.Equal(policyCancellation.Token, actual.CancellationToken);
        Assert.Collection(observer.Faults,
            fault => { Assert.False(fault.IsTerminal); Assert.Same(attemptFailure, fault.Exception); },
            fault => { Assert.True(fault.IsTerminal); Assert.Same(actual, fault.Exception); });
        Assert.Equal(1, pipe.AttemptCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-STOP", "stop-cancels-backoff-without-terminal-fault")]
    public async Task StopDuringRetryBackoff_CancelsTheOwnedRunWithoutAnotherAttemptOrTerminalFaultAsync()
    {
        var timeProvider = new ObservableTimeProvider(new DateTimeOffset(2031, 2, 3, 4, 5, 6, TimeSpan.Zero));
        var context = new TestReceiveEndpointContext();
        context.SetTimeProvider(timeProvider);
        var observer = new RecordingTransportObserver();
        using ConnectHandle observerHandle = context.ConnectReceiveTransportObserver(observer);
        var expected = new ConnectionException("attempt", isTransient: true);
        var pipe = new ScriptedTransportPipe(expected);
        var transport = new ReceiveTransport<TestPipeContext>(
            new TestHostConfiguration(Retry.Interval(1, TimeSpan.FromHours(1))),
            context,
            () => new TestTransportSupervisor(new TestPipeContext()),
            pipe);

        ReceiveTransportHandle handle = transport.Start();
        ReceiveTransportFaulted attempt = await observer.FirstFault.WaitAsync(TestContext.Current.CancellationToken);
        await timeProvider.WaitForTimerCountAsync(1).WaitAsync(TestContext.Current.CancellationToken);
        await handle.StopAsync(TestContext.Current.CancellationToken);

        ReceiveTransportFaulted singleFault = Assert.Single(observer.Faults);
        Assert.Same(attempt, singleFault);
        Assert.Same(expected, singleFault.Exception);
        Assert.False(singleFault.IsTerminal);
        Assert.Equal(1, pipe.AttemptCount);
        Assert.False(observer.TerminalFault.IsCompleted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-RECEIVE-START", "caller-cancellation-is-not-reported-as-transport-completion")]
    public async Task TransportStartup_PreservesCallerCancellationWhileWaitingForTheReceivePipeAsync()
    {
        var connection = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var context = new TestReceiveEndpointContext(receivePipe: new ConnectedReceivePipe(connection.Task));
        var supervisor = new TestTransportSupervisor(new TestPipeContext());
        using var cancellation = new CancellationTokenSource();

        Task startup = context.OnTransportStartupAsync(supervisor, cancellation.Token);
        cancellation.Cancel();

        OperationCanceledException exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => startup);

        Assert.Equal(cancellation.Token, exception.CancellationToken);
    }

    private static readonly Uri InputAddress = new("loopback://receive-lifecycle/input");

    private sealed class ExpectedTransportException(string message) : Exception(message);

    private sealed class ScriptedReceiveTransport(params Exception[] startFailures) : IReceiveTransport
    {
        private readonly Queue<Exception> _startFailures = new(startFailures);
        private readonly ReceiveTransportObservable _observers = new();

        public int StartCount { get; private set; }

        public ReceiveTransportHandle Start()
        {
            StartCount++;
            if (_startFailures.TryDequeue(out Exception? exception))
                throw exception;

            return new StubReceiveTransportHandle();
        }

        public Task NotifyReadyAsync() => _observers.ReadyAsync(new ReceiveTransportReadyEvent(InputAddress, true));

        public Task NotifyFaultedAsync(Exception exception, bool isTerminal) =>
            _observers.FaultedAsync(new ReceiveTransportFaultedEvent(InputAddress, exception, isTerminal));

        public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer) => _observers.Connect(observer);

        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class StubReceiveTransportHandle : ReceiveTransportHandle
    {
        public Task StopAsync(CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
    }

    private sealed class SynchronouslyCompletingReceiveTransport : IReceiveTransport
    {
        private readonly ReceiveTransportObservable _observers = new();

        public ReceiveTransportHandle Start()
        {
            _observers.CompletedAsync(new CompletedTransportEvent()).GetAwaiter().GetResult();
            return new StubReceiveTransportHandle();
        }

        public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer) => _observers.Connect(observer);
        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => new EmptyConnectHandle();
        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new EmptyConnectHandle();
        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();
        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class FailingStopReceiveTransport(params Exception[] stopFailures) : IReceiveTransport
    {
        private readonly ReceiveTransportObservable _observers = new();
        private readonly Queue<Exception> _stopFailures = new(stopFailures);
        private int _stopCount;

        public int StopCount => Volatile.Read(ref _stopCount);

        public ReceiveTransportHandle Start() => new FailingStopHandle(this);
        public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer) => _observers.Connect(observer);
        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => new EmptyConnectHandle();
        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new EmptyConnectHandle();
        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();
        public void Probe(ProbeContext context)
        {
        }

        private sealed class FailingStopHandle(FailingStopReceiveTransport transport) : ReceiveTransportHandle
        {
            public Task StopAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Interlocked.Increment(ref transport._stopCount);
                return transport._stopFailures.TryDequeue(out Exception? exception)
                    ? Task.FromException(exception)
                    : Task.CompletedTask;
            }
        }
    }

    private sealed record CompletedTransportEvent : ReceiveTransportCompleted
    {
        public Uri InputAddress => ReceiveLifecycleTerminalityTests.InputAddress;
        public long DeliveryCount => 0;
        public int MaxConcurrentDeliveryCount => 0;
    }

    private sealed class TestReceiveEndpointContext(
        bool pauseReset = false,
        Exception? resetFailure = null,
        IReceivePipe? receivePipe = null) : BasePipeContext, ReceiveEndpointContext
    {
        private readonly ReceiveEndpointObservable _endpointObservers = new();
        private readonly Queue<Exception> _resetFailures = resetFailure is null ? new Queue<Exception>() : new Queue<Exception>([resetFailure]);
        private readonly ReceiveTransportObservable _transportObservers = new();
        private readonly TaskCompletionSource _releaseReset = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _resetCount;

        public TaskCompletionSource ResetStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int ResetCount => Volatile.Read(ref _resetCount);

        public TimeSpan? ConsumerStopTimeout => null;
        public TimeSpan? StopTimeout => null;
        public Uri InputAddress => ReceiveLifecycleTerminalityTests.InputAddress;
        public bool IsBusEndpoint => false;
        public IReceiveEndpointObserver EndpointObservers => _endpointObservers;
        public IReceiveObserver ReceiveObservers => throw new NotSupportedException();
        public IReceiveTransportObserver TransportObservers => _transportObservers;
        public ILogContext LogContext { get; } = new BusLogContext(NullLoggerFactory.Instance);
        public IPublishTopology Publish => throw new NotSupportedException();
        public IReceivePipe ReceivePipe { get; } = receivePipe ?? new ConnectedReceivePipe();
        public IPublishEndpointProvider PublishEndpointProvider => throw new NotSupportedException();
        public ISendEndpointProvider SendEndpointProvider => throw new NotSupportedException();
        public IMessageRouteTable MessageRoutes { get; } = new MessageRouteTable();
        public Task DependenciesReady => Task.CompletedTask;
        public Task DependentsCompleted => Task.CompletedTask;
        public bool PublishFaults => true;
        public int PrefetchCount => 1;
        public int? ConcurrentMessageLimit => 1;
        public ISerialization Serialization => throw new NotSupportedException();

        public Exception ConvertException(Exception exception, string message) => exception;

        public IReceivePipeDispatcher CreateReceivePipeDispatcher() => throw new NotSupportedException();

        public ValueTask ResetAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.ValueTask.FromCanceled(cancellationToken); Interlocked.Increment(ref _resetCount);
            ResetStarted.TrySetResult();
            if (_resetFailures.TryDequeue(out Exception? exception))
                return ValueTask.FromException(exception);

            return pauseReset ? new ValueTask(_releaseReset.Task) : default;
        }

        public void ReleaseReset() => _releaseReset.TrySetResult();

        public void AddConsumeAgent(IAgent agent)
        {
        }

        public void AddSendAgent(IAgent agent)
        {
        }

        public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer) => _endpointObservers.Connect(observer);

        public ConnectHandle ConnectReceiveTransportObserver(IReceiveTransportObserver observer) => _transportObservers.Connect(observer);

        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class ConnectedReceivePipe(Task? connected = null) : IReceivePipe
    {
        public Task Connected { get; } = connected ?? Task.CompletedTask;

        public Task SendAsync(ReceiveContext context) => Task.CompletedTask;

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => new EmptyConnectHandle();

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class RecordingTransportObserver : IReceiveTransportObserver
    {
        private readonly object _gate = new();
        private readonly List<ReceiveTransportFaulted> _faults = [];
        private readonly TaskCompletionSource<ReceiveTransportFaulted> _firstFault =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ReceiveTransportFaulted> _thirdFault =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<ReceiveTransportFaulted> _terminalFault =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public ReceiveTransportFaulted[] Faults
        {
            get
            {
                lock (_gate)
                    return _faults.ToArray();
            }
        }

        public Task<ReceiveTransportFaulted> FirstFault => _firstFault.Task;
        public Task<ReceiveTransportFaulted> ThirdFault => _thirdFault.Task;
        public Task<ReceiveTransportFaulted> TerminalFault => _terminalFault.Task;

        public Task ReadyAsync(ReceiveTransportReady ready) => Task.CompletedTask;

        public Task CompletedAsync(ReceiveTransportCompleted completed) => Task.CompletedTask;

        public Task FaultedAsync(ReceiveTransportFaulted faulted)
        {
            int count;
            lock (_gate)
            {
                _faults.Add(faulted);
                count = _faults.Count;
            }

            _firstFault.TrySetResult(faulted);
            if (count == 3)
                _thirdFault.TrySetResult(faulted);

            if (faulted.IsTerminal)
                _terminalFault.TrySetResult(faulted);

            return Task.CompletedTask;
        }
    }

    private sealed class ScriptedTransportPipe(params ConnectionException[] failures) : IPipe<TestPipeContext>
    {
        private readonly Queue<ConnectionException> _failures = new(failures);

        public int AttemptCount { get; private set; }

        public Task SendAsync(TestPipeContext context)
        {
            AttemptCount++;
            throw _failures.Dequeue();
        }

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class TestPipeContext : BasePipeContext;

    private sealed class TrackingReceiveRetryPolicy(List<string> trace, int retryLimit) : IRetryPolicy
    {
        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new TrackingReceivePolicyContext<T>(context, trace, retryLimit);

        public bool IsHandled(Exception exception) => true;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class FailingContextReceiveRetryPolicy(Exception failure, bool returnsNull) : IRetryPolicy
    {
        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => returnsNull ? null! : throw failure;

        public bool IsHandled(Exception exception) => true;

        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed class FailingDecisionReceiveRetryPolicy(Exception failure) : IRetryPolicy
    {
        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new FailingDecisionContext<T>(context, failure);

        public bool IsHandled(Exception exception) => true;

        public void Probe(ProbeContext context)
        {
        }

        private sealed class FailingDecisionContext<T>(T context, Exception failure) : RetryPolicyContext<T>
            where T : class, PipeContext
        {
            public T Context => context;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext) => throw failure;

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Cancel()
            {
            }

            public void Dispose()
            {
            }
        }
    }

    private sealed class ThrowingErrorLogger(Exception failure) : ILogger
    {
        public int CallCount { get; private set; }
        public Exception? ObservedFailure { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Error;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            CallCount++;
            ObservedFailure = exception;
            throw failure;
        }
    }

    private sealed class ThrowingInfoLogger(Exception failure) : ILogger
    {
        public int CallCount { get; private set; }
        public Exception? ObservedFailure { get; private set; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel == LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            CallCount++;
            ObservedFailure = exception;
            throw failure;
        }
    }

    private sealed class TerminalCallbackReceiveRetryPolicy(Exception failure, bool returnsNullTask) : IRetryPolicy
    {
        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new CallbackPolicyContext<T>(context, failure, returnsNullTask);

        public bool IsHandled(Exception exception) => true;

        public void Probe(ProbeContext context)
        {
        }

        private sealed class CallbackPolicyContext<T>(T context, Exception failure, bool returnsNullTask) : RetryPolicyContext<T>
            where T : class, PipeContext
        {
            public T Context => context;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
            {
                retryContext = new CallbackRetryContext<T>(context, exception, failure, returnsNullTask);
                return false;
            }

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Cancel()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class CallbackRetryContext<T>(T context, Exception exception, Exception failure, bool returnsNullTask)
            : BaseRetryContext<T>(context, exception, 0, CancellationToken.None), RetryContext<T>
            where T : class, PipeContext
        {
            public override Task RetryFaultedAsync(Exception terminalException, CancellationToken cancellationToken = default) =>
                returnsNullTask ? null! : Task.FromException(failure);

            public bool CanRetry(Exception terminalException, out RetryContext<T> retryContext)
            {
                retryContext = this;
                return false;
            }
        }
    }

    private sealed class AwaitingStopReceiveRetryPolicy(bool retryScheduled = false, TimeSpan? retryDelay = null) : IRetryPolicy
    {
        private readonly TaskCompletionSource<CancellationToken> _callbackEntered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<CancellationToken> CallbackEntered => _callbackEntered.Task;

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new StopPolicyContext<T>(context, _callbackEntered, retryScheduled, retryDelay);

        public bool IsHandled(Exception exception) => true;

        public void Probe(ProbeContext context)
        {
        }

        private sealed class StopPolicyContext<T>(T context, TaskCompletionSource<CancellationToken> callbackEntered,
            bool retryScheduled, TimeSpan? retryDelay) : RetryPolicyContext<T>
            where T : class, PipeContext
        {
            public T Context => context;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
            {
                retryContext = new StopRetryContext<T>(context, exception, callbackEntered, retryDelay);
                return retryScheduled;
            }

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Cancel()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class StopRetryContext<T>(T context, Exception exception, TaskCompletionSource<CancellationToken> callbackEntered,
            TimeSpan? retryDelay)
            : BaseRetryContext<T>(context, exception, 0, CancellationToken.None), RetryContext<T>
            where T : class, PipeContext
        {
            public override TimeSpan? Delay => retryDelay;

            public override async Task PreRetryAsync(CancellationToken cancellationToken = default)
            {
                callbackEntered.TrySetResult(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            public override async Task RetryFaultedAsync(Exception terminalException, CancellationToken cancellationToken = default)
            {
                callbackEntered.TrySetResult(cancellationToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }

            public bool CanRetry(Exception terminalException, out RetryContext<T> retryContext)
            {
                retryContext = this;
                return false;
            }
        }
    }

    public enum PolicyCancellationStage
    {
        Delay,
        PreRetry,
        TerminalCallback,
    }

    private sealed class CancelableReceiveRetryPolicy(CancellationToken policyToken, PolicyCancellationStage stage) : IRetryPolicy
    {
        private readonly TaskCompletionSource _callbackEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task CallbackEntered => _callbackEntered.Task;

        public RetryPolicyContext<T> CreatePolicyContext<T>(T context)
            where T : class, PipeContext => new CancelablePolicyContext<T>(context, policyToken, stage, _callbackEntered);

        public bool IsHandled(Exception exception) => true;

        public void Probe(ProbeContext context)
        {
        }

        private sealed class CancelablePolicyContext<T>(T context, CancellationToken policyToken,
            PolicyCancellationStage stage, TaskCompletionSource callbackEntered) : RetryPolicyContext<T>
            where T : class, PipeContext
        {
            public T Context => context;

            public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
            {
                retryContext = new CancelableRetryContext<T>(context, exception, policyToken, stage, callbackEntered);
                return stage != PolicyCancellationStage.TerminalCallback;
            }

            public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) => Task.CompletedTask;

            public void Cancel()
            {
            }

            public void Dispose()
            {
            }
        }

        private sealed class CancelableRetryContext<T>(T context, Exception exception, CancellationToken policyToken,
            PolicyCancellationStage stage, TaskCompletionSource callbackEntered)
            : BaseRetryContext<T>(context, exception, 0, policyToken), RetryContext<T>
            where T : class, PipeContext
        {
            public override TimeSpan? Delay => stage == PolicyCancellationStage.Delay ? TimeSpan.FromHours(1) : null;

            public override async Task PreRetryAsync(CancellationToken cancellationToken = default)
            {
                if (stage == PolicyCancellationStage.PreRetry)
                {
                    callbackEntered.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
            }

            public override async Task RetryFaultedAsync(Exception terminalException, CancellationToken cancellationToken = default)
            {
                if (stage == PolicyCancellationStage.TerminalCallback)
                {
                    callbackEntered.TrySetResult();
                    await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
                }
            }

            public bool CanRetry(Exception terminalException, out RetryContext<T> retryContext)
            {
                retryContext = null!;
                return false;
            }
        }
    }

    private sealed class TrackingReceivePolicyContext<T>(T context, List<string> trace, int retryLimit) : RetryPolicyContext<T>
        where T : class, PipeContext
    {
        public T Context { get; } = context;

        public bool CanRetry(Exception exception, out RetryContext<T> retryContext)
        {
            retryContext = new TrackingReceiveRetryContext<T>(Context, exception, 0, trace, retryLimit);
            return retryLimit > 0;
        }

        public Task RetryFaultedAsync(Exception exception, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public void Cancel()
        {
        }

        public void Dispose()
        {
        }
    }

    private sealed class TrackingReceiveRetryContext<T>(
        T context,
        Exception exception,
        int retryCount,
        List<string> trace,
        int retryLimit) : BaseRetryContext<T>(context, exception, retryCount, CancellationToken.None), RetryContext<T>
        where T : class, PipeContext
    {
        public override Task PreRetryAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); trace.Add($"before:{RetryAttempt}");
            return Task.CompletedTask;
        }

        public override Task RetryFaultedAsync(Exception terminalException, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); trace.Add($"terminal:{terminalException.Message}");
            return Task.CompletedTask;
        }

        public bool CanRetry(Exception terminalException, out RetryContext<T> retryContext)
        {
            retryContext = new TrackingReceiveRetryContext<T>(Context, terminalException, RetryCount + 1, trace, retryLimit);
            return RetryAttempt < retryLimit;
        }
    }

    private sealed class TestTransportSupervisor(TestPipeContext context) : ITransportSupervisor<TestPipeContext>
    {
        private readonly TaskCompletionSource _completed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenSource _stopped = new();
        private readonly CancellationTokenSource _stopping = new();

        public CancellationToken ConsumeStopping => _stopping.Token;
        public CancellationToken SendStopping => _stopping.Token;
        public Task Ready => Task.CompletedTask;
        public Task Completed => _completed.Task;
        public CancellationToken Stopping => _stopping.Token;
        public CancellationToken Stopped => _stopped.Token;
        public int PeakActiveCount => 0;
        public long TotalCount => 0;

        public Task SendAsync(IPipe<TestPipeContext> pipe, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return pipe.SendAsync(context); }
        public Task StopAsync(StopContext stopContext, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); _stopping.Cancel();
            _completed.TrySetResult();
            _stopped.Cancel();
            return Task.CompletedTask;
        }

        public void Add(IAgent agent)
        {
        }

        public void AddConsumeAgent<TAgent>(TAgent agent)
            where TAgent : IAgent
        {
        }

        public void AddSendAgent<TAgent>(TAgent agent)
            where TAgent : IAgent
        {
        }

        public void Probe(ProbeContext probeContext)
        {
        }
    }

    private sealed class TestHostConfiguration(IRetryPolicy retryPolicy) : IHostConfiguration
    {
        public IRetryPolicy ReceiveTransportRetryPolicy { get; } = retryPolicy;
        public Uri HostAddress => InputAddress;
        public IBusConfiguration BusConfiguration => throw new NotSupportedException();
        public bool DeployTopologyOnly { get; set; }
        public bool DeployPublishTopology { get; set; }
        public ISendObserver SendObservers => throw new NotSupportedException();
        public ILogContext? LogContext { get; set; }
        public ILogContext? ReceiveLogContext => null;
        public ILogContext? SendLogContext => null;
        public IBusTopology Topology => throw new NotSupportedException();
        public IRetryPolicy SendTransportRetryPolicy => throw new NotSupportedException();
        public TimeSpan? ConsumerStopTimeout { get; set; }
        public TimeSpan? StopTimeout { get; set; }

        public IReceiveEndpointConfiguration CreateReceiveEndpointConfiguration(
            string queueName,
            Action<IReceiveEndpointConfigurator>? configure = null) => throw new NotSupportedException();

        public ConnectHandle ConnectReceiveEndpointContext(ReceiveEndpointContext context) => new EmptyConnectHandle();

        public IHost Build() => throw new NotSupportedException();

        public ConnectHandle ConnectEndpointConfigurationObserver(IEndpointConfigurationObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer)
            where T : class => new EmptyConnectHandle();

        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new EmptyConnectHandle();

        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class StubReceiveEndpoint : IReceiveEndpoint
    {
        public Uri InputAddress => ReceiveLifecycleTerminalityTests.InputAddress;
        public Task<ReceiveEndpointReady> Started => Task.FromResult<ReceiveEndpointReady>(
            new StubReceiveEndpointReady(this));
        public IReceiveEndpointHandle Start(CancellationToken cancellationToken) { cancellationToken.ThrowIfCancellationRequested(); throw new NotSupportedException(); }
        public bool IsStarted() => true;
        public ConnectHandle ConnectConsumeObserver(IConsumeObserver observer) => new EmptyConnectHandle();
        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe) where T : class => new EmptyConnectHandle();
        public ConnectHandle ConnectConsumePipe<T>(IPipe<ConsumeContext<T>> pipe, ConnectPipeOptions options) where T : class => new EmptyConnectHandle();
        public ConnectHandle ConnectRequestPipe<T>(Guid requestId, IPipe<ConsumeContext<T>> pipe) where T : class => new EmptyConnectHandle();
        public ConnectHandle ConnectPublishObserver(IPublishObserver observer) => new EmptyConnectHandle();
        public ConnectHandle ConnectSendObserver(ISendObserver observer) => new EmptyConnectHandle();
        public Task<ISendEndpoint> GetSendEndpointAsync(Uri address, CancellationToken cancellationToken = default) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); throw new NotSupportedException(); }
        public Task<ISendEndpoint> GetPublishSendEndpointAsync<T>(CancellationToken cancellationToken = default) where T : class { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled<global::ViciOne.ServiceBus.ISendEndpoint>(cancellationToken); throw new NotSupportedException(); }
        public ConnectHandle ConnectReceiveObserver(IReceiveObserver observer) => new EmptyConnectHandle();
        public ConnectHandle ConnectConsumeMessageObserver<T>(IConsumeMessageObserver<T> observer) where T : class => new EmptyConnectHandle();
        public ConnectHandle ConnectReceiveEndpointObserver(IReceiveEndpointObserver observer) => new EmptyConnectHandle();
        public Task StopAsync(CancellationToken cancellationToken) { if (cancellationToken.IsCancellationRequested) return global::System.Threading.Tasks.Task.FromCanceled(cancellationToken); return Task.CompletedTask; }
        public void Probe(ProbeContext context)
        {
        }
    }

    private sealed record StubReceiveEndpointReady(IReceiveEndpoint ReceiveEndpoint) : ReceiveEndpointReady
    {
        public Uri InputAddress => ReceiveEndpoint.InputAddress;
        public bool IsStarted => true;
    }
}
