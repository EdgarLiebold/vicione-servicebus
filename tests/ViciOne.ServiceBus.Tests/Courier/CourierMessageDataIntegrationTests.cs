using System.Collections.Concurrent;
using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Courier.Contracts;
using ViciOne.ServiceBus.MessageData;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Courier;

public sealed class CourierMessageDataIntegrationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-COURIER-MESSAGE-DATA", "stored-arguments-and-compensation-log-survive-routing-slip")]
    public async Task ExternalArgumentsAndLog_AreLoadedByTheirActivitiesWithExactIdentityAndContentAsync()
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var repository = new InMemoryMessageDataRepository();
        var policy = new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1);
        const string argumentText = "execute payload — original order";
        const string logText = "compensate payload — undo original order";
        MessageData<string> argument = await repository.PutStringAsync(argumentText, null, policy, cancellationToken);
        MessageData<string> log = await repository.PutStringAsync(logText, null, policy, cancellationToken);
        Assert.NotNull(argument.Address);
        Assert.NotNull(log.Address);
        Assert.NotEqual(argument.Address, log.Address);
        var observations = new ConcurrentQueue<Observation>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-message-data");
        harness.InMemoryBusConfiguring += bus => bus.UseMessageData(repository, policy);
        ActivityTestHarness<StoredDataActivity, StoredArguments, StoredLog> activity = harness.AddActivity<
            StoredDataActivity, StoredArguments, StoredLog>(
            _ => new StoredDataActivity(log, observations),
            _ => new StoredDataActivity(log, observations));
        ExecuteActivityTestHarness<FaultingCourierActivity, FaultingCourierArguments> failing = harness.AddExecuteActivity<
            FaultingCourierActivity, FaultingCourierArguments>();
        using var compensated = new CourierMessageRecorder<IRoutingSlipActivityCompensated>(1);
        using var faulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);
        compensated.Configure(harness);
        faulted.Configure(harness);
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        bool stopped = false;
        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new StoredArguments { Data = argument });
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("trigger stored-log compensation"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken).WaitAsync(timeout, cancellationToken);
            await Task.WhenAll(compensated.WaitAsync(timeout, cancellationToken), faulted.WaitAsync(timeout, cancellationToken));
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            stopped = true;

            Assert.Equal(
                new[]
                {
                    new Observation("execute", trackingNumber, argument.Address, argumentText),
                    new Observation("compensate", trackingNumber, log.Address, logText),
                }, observations.ToArray());
            IRoutingSlipActivityCompensated actualCompensation = Assert.Single(compensated.Messages).Message;
            Assert.Equal(trackingNumber, actualCompensation.TrackingNumber);
            Assert.Equal(activity.Name, actualCompensation.ActivityName);
            IRoutingSlipFaulted actualFailure = Assert.Single(faulted.Messages).Message;
            Assert.Equal(trackingNumber, actualFailure.TrackingNumber);
            ExceptionInfo exception = Assert.Single(actualFailure.ActivityExceptions).ExceptionInfo;
            Assert.Equal(TypeCache<CourierExpectedException>.ShortName, exception.ExceptionType);
            Assert.Equal("trigger stored-log compensation", exception.Message);
            Assert.Empty(harness.Published.Snapshot<Fault<IRoutingSlip>>());
            Assert.Empty(harness.Published.Snapshot<IRoutingSlipCompensationFailed>());
        }
        finally
        {
            if (!stopped)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-COURIER-MESSAGE-DATA", "expired-arguments-or-log-fault-at-the-owning-activity-stage")]
    public async Task ExpiredReference_FaultsItsOwningStageWithoutReportingASuccessfulEffectAsync(bool expireLog)
    {
        TimeSpan timeout = CourierTestSupport.OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var clock = new FakeTimeProvider(new DateTimeOffset(2044, 1, 2, 3, 4, 5, TimeSpan.Zero));
        var repository = new InMemoryMessageDataRepository(clock);
        var policy = new MessageDataPolicy(alwaysWriteToRepository: true, threshold: 1);
        TimeSpan lifetime = TimeSpan.FromMinutes(1);
        MessageData<string> argument = await repository.PutStringAsync("valid execution data", expireLog ? null : lifetime, policy, cancellationToken);
        MessageData<string> log = await repository.PutStringAsync("expired compensation data", expireLog ? lifetime : null, policy, cancellationToken);
        var observations = new ConcurrentQueue<Observation>();
        using InMemoryTestHarness harness = CourierTestSupport.CreateHarness("courier-expired-message-data");
        harness.InMemoryBusConfiguring += bus => bus.UseMessageData(repository, policy);
        ActivityTestHarness<StoredDataActivity, StoredArguments, StoredLog> activity = harness.AddActivity<
            StoredDataActivity, StoredArguments, StoredLog>(
            _ => new StoredDataActivity(log, observations),
            _ => new StoredDataActivity(log, observations));
        ExecuteActivityTestHarness<ExpireThenFaultActivity, FaultingCourierArguments> failing = harness.AddExecuteActivity<
            ExpireThenFaultActivity, FaultingCourierArguments>(_ => new ExpireThenFaultActivity(() => clock.Advance(lifetime)));
        using var faulted = new CourierMessageRecorder<IRoutingSlipFaulted>(1);
        using var compensationFailed = new CourierMessageRecorder<IRoutingSlipCompensationFailed>(1);
        faulted.Configure(harness);
        compensationFailed.Configure(harness);
        if (!expireLog)
            clock.Advance(lifetime);
        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        bool stopped = false;
        try
        {
            Guid trackingNumber = NewId.NextGuid();
            var builder = new RoutingSlipBuilder(trackingNumber);
            builder.AddActivity(activity.Name, activity.ExecuteAddress, new StoredArguments { Data = argument });
            builder.AddActivity(failing.Name, failing.ExecuteAddress, new FaultingCourierArguments("expire before compensation"));

            await harness.Bus.ExecuteAsync(builder.Build(), cancellationToken).WaitAsync(timeout, cancellationToken);
            if (expireLog)
                await compensationFailed.WaitAsync(timeout, cancellationToken);
            else
                await faulted.WaitAsync(timeout, cancellationToken);
            await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
            stopped = true;

            ExceptionInfo exception;
            if (expireLog)
            {
                Assert.Equal(new Observation("execute", trackingNumber, argument.Address, "valid execution data"), Assert.Single(observations));
                IRoutingSlipCompensationFailed failure = Assert.Single(compensationFailed.Messages).Message;
                Assert.Equal(trackingNumber, failure.TrackingNumber);
                exception = failure.ExceptionInfo;
                IRoutingSlipActivityCompensationFailed activityFailure = Assert.Single(
                    harness.Published.Snapshot<IRoutingSlipActivityCompensationFailed>()).Context.Message;
                Assert.Equal(activity.Name, activityFailure.ActivityName);
                Assert.Equal(trackingNumber, activityFailure.TrackingNumber);
                Assert.Equal(TypeCache<MessageDataNotFoundException>.ShortName, activityFailure.ExceptionInfo.ExceptionType);
                Assert.Empty(faulted.Messages);
                Assert.Single(harness.Published.Snapshot<IRoutingSlipActivityCompleted>());
            }
            else
            {
                Assert.Empty(observations);
                IRoutingSlipFaulted failure = Assert.Single(faulted.Messages).Message;
                Assert.Equal(trackingNumber, failure.TrackingNumber);
                exception = Assert.Single(failure.ActivityExceptions).ExceptionInfo;
                IRoutingSlipActivityFaulted activityFailure = Assert.Single(harness.Published.Snapshot<IRoutingSlipActivityFaulted>()).Context.Message;
                Assert.Equal(activity.Name, activityFailure.ActivityName);
                Assert.Equal(trackingNumber, activityFailure.TrackingNumber);
                Assert.Equal(TypeCache<MessageDataNotFoundException>.ShortName, activityFailure.ExceptionInfo.ExceptionType);
                Assert.Empty(compensationFailed.Messages);
                Assert.Empty(harness.Published.Snapshot<IRoutingSlipActivityCompleted>());
            }
            Assert.Equal(TypeCache<MessageDataNotFoundException>.ShortName, exception.ExceptionType);
            Assert.Contains((expireLog ? log.Address : argument.Address)!.ToString(), exception.Message);
            Assert.Empty(harness.Published.Snapshot<IRoutingSlipActivityCompensated>());
            Assert.Empty(harness.Published.Snapshot<IRoutingSlipCompleted>());
            Assert.Empty(harness.Published.Snapshot<Fault<IRoutingSlip>>());
        }
        finally
        {
            if (!stopped)
                await harness.StopAsync(CancellationToken.None).WaitAsync(timeout, CancellationToken.None);
        }
    }

    public sealed class StoredArguments
    {
        public MessageData<string> Data { get; set; } = null!;
    }

    public sealed class StoredLog
    {
        public MessageData<string> Data { get; set; } = null!;
    }

    private sealed record Observation(string Stage, Guid TrackingNumber, Uri? Address, string? Value);

    private sealed class ExpireThenFaultActivity(Action expire) : IExecuteActivity<FaultingCourierArguments>
    {
        public Task<ExecutionResult> ExecuteAsync(ExecuteContext<FaultingCourierArguments> context)
        {
            expire();
            return Task.FromResult(context.Faulted(new CourierExpectedException(context.Arguments.Reason)));
        }
    }

    private sealed class StoredDataActivity(MessageData<string> log, ConcurrentQueue<Observation> observations) :
        IActivity<StoredArguments, StoredLog>
    {
        public async Task<ExecutionResult> ExecuteAsync(ExecuteContext<StoredArguments> context)
        {
            string? value = await context.Arguments.Data.Value;
            observations.Enqueue(new Observation("execute", context.TrackingNumber, context.Arguments.Data.Address, value));
            return context.Completed(new StoredLog { Data = log });
        }

        public async Task<CompensationResult> CompensateAsync(CompensateContext<StoredLog> context)
        {
            string? value = await context.Log.Data.Value;
            observations.Enqueue(new Observation("compensate", context.TrackingNumber, context.Log.Data.Address, value));
            return context.Compensated();
        }
    }
}
