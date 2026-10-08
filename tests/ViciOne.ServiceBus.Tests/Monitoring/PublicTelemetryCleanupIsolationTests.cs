using System.Diagnostics;
using System.Reflection;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Tests.Testing;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Monitoring;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class PublicTelemetryCleanupIsolationTests
{
    public enum Family { Publish, Send, Request1, Request2, Request3 }
    public enum Scenario { Healthy, StandaloneCleanupFailure, ActionFailure, ActionAndCleanupFailure, WaitCancellationAndCleanupFailure }

    [Theory]
    [InlineData(Family.Publish, Scenario.Healthy)]
    [InlineData(Family.Publish, Scenario.StandaloneCleanupFailure)]
    [InlineData(Family.Publish, Scenario.ActionFailure)]
    [InlineData(Family.Publish, Scenario.ActionAndCleanupFailure)]
    [InlineData(Family.Publish, Scenario.WaitCancellationAndCleanupFailure)]
    [InlineData(Family.Send, Scenario.Healthy)]
    [InlineData(Family.Send, Scenario.StandaloneCleanupFailure)]
    [InlineData(Family.Send, Scenario.ActionFailure)]
    [InlineData(Family.Send, Scenario.ActionAndCleanupFailure)]
    [InlineData(Family.Send, Scenario.WaitCancellationAndCleanupFailure)]
    [InlineData(Family.Request1, Scenario.Healthy)]
    [InlineData(Family.Request1, Scenario.StandaloneCleanupFailure)]
    [InlineData(Family.Request1, Scenario.ActionFailure)]
    [InlineData(Family.Request1, Scenario.ActionAndCleanupFailure)]
    [InlineData(Family.Request1, Scenario.WaitCancellationAndCleanupFailure)]
    [InlineData(Family.Request2, Scenario.Healthy)]
    [InlineData(Family.Request2, Scenario.StandaloneCleanupFailure)]
    [InlineData(Family.Request2, Scenario.ActionFailure)]
    [InlineData(Family.Request2, Scenario.ActionAndCleanupFailure)]
    [InlineData(Family.Request2, Scenario.WaitCancellationAndCleanupFailure)]
    [InlineData(Family.Request3, Scenario.Healthy)]
    [InlineData(Family.Request3, Scenario.StandaloneCleanupFailure)]
    [InlineData(Family.Request3, Scenario.ActionFailure)]
    [InlineData(Family.Request3, Scenario.ActionAndCleanupFailure)]
    [InlineData(Family.Request3, Scenario.WaitCancellationAndCleanupFailure)]
    [RequirementCoverage("REQ-VSB-OBSERVABILITY-ISOLATION", "public-five-telemetry-callers-primary-and-required-cleanup")]
    public async Task CallerPreservesBodyOutcomeAndReleasesActualTracker(Family family, Scenario scenario)
    {
        var primary = new InvalidOperationException("chosen action failure");
        var secondary = new InvalidOperationException("chosen releasing timer failure");
        bool actionFault = scenario is Scenario.ActionFailure or Scenario.ActionAndCleanupFailure;
        bool cleanupFault = scenario is Scenario.StandaloneCleanupFailure or Scenario.ActionAndCleanupFailure
            or Scenario.WaitCancellationAndCleanupFailure;
        var clock = new ObservableTimeProvider(new DateTimeOffset(2026, 10, 7, 0, 0, 0, TimeSpan.Zero),
            cleanupFault ? secondary : null);
        using var operationCancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        CancellationToken token = operationCancellation.Token;
        using var caller = new Activity("caller-cleanup-control").Start();
        Assert.NotNull(caller);
        using var probe = new ActivitySource("caller-cleanup-unrelated-probe");
        Assert.Null(probe.StartActivity("before"));
        Activity? root = null;
        int actions = 0;
        object endpoint = family switch
        {
            Family.Publish => DispatchProxy.Create<IPublishEndpoint, StrictProxy>(),
            Family.Send => DispatchProxy.Create<ISendEndpoint, StrictProxy>(),
            _ => DispatchProxy.Create<IRequestClient<Request>, StrictProxy>(),
        };
        Task Action(object actualEndpoint)
        {
            Assert.Same(endpoint, actualEndpoint);
            actions++;
            root = Activity.Current;
            Assert.NotNull(root);
            Assert.Equal("ViciOne.ServiceBus.Testing.Monitor", root.Source.Name);
            Assert.Equal(caller.Id, root.ParentId);
            return actionFault ? Task.FromException(primary) : Task.CompletedTask;
        }

        var message = new ResponseMessage();
        Response<ResponseMessage> response = DispatchProxy.Create<Response<ResponseMessage>, StrictProxy>();
        ((StrictProxy)(object)response).Message = message;
        var chosenBranch = Task.FromResult(response);
        var other1 = new TaskCompletionSource<Response<OtherResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var other2 = new TaskCompletionSource<Response<ThirdResponse>>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool resultObserved = false;
        TimeSpan idle = TimeSpan.FromMinutes(1);
        TimeSpan timeout = TimeSpan.FromMinutes(10);
        Task operation = family switch
        {
            Family.Publish => ((IPublishEndpoint)endpoint).ExecuteAndWaitForIdleAsync(
                e => Action(e), timeout, idle, clock, token),
            Family.Send => ((ISendEndpoint)endpoint).ExecuteAndWaitForIdleAsync(
                e => Action(e), timeout, idle, clock, token),
            Family.Request1 => RequestOneAsync(),
            Family.Request2 => RequestTwoAsync(),
            Family.Request3 => RequestThreeAsync(),
            _ => throw new InvalidOperationException("Unknown family"),
        };
        try
        {
            Assert.Equal(1, actions);
            Assert.Equal(1, clock.TimerCount);
            if (actionFault)
                Assert.True(operation.IsCompleted);
            else
            {
                Assert.False(operation.IsCompleted);
                Assert.Equal(idle, clock.LastDueTime);
                if (scenario == Scenario.WaitCancellationAndCleanupFailure)
                    operationCancellation.Cancel();
                else
                {
                    clock.Advance(idle - TimeSpan.FromTicks(1));
                    Assert.False(operation.IsCompleted);
                    clock.Advance(TimeSpan.FromTicks(1));
                }
            }
            Exception? escaped = await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5),
                TestContext.Current.CancellationToken));
            Assert.Equal(0, clock.ActiveTimerCount);
            Assert.NotNull(root);
            Assert.True(root.IsStopped);
            Assert.Equal(0, ((StrictProxy)endpoint).UnexpectedCalls);
            Assert.Equal(0, ((StrictProxy)(object)response).UnexpectedCalls);
            Assert.Equal(scenario == Scenario.Healthy && (family is Family.Request1 or Family.Request2 or Family.Request3) ? 1 : 0,
                ((StrictProxy)(object)response).MessageReads);
            Assert.Null(probe.StartActivity("after"));
            if (actionFault)
                Assert.Same(primary, escaped);
            else if (scenario == Scenario.WaitCancellationAndCleanupFailure)
            {
                OperationCanceledException canceled = Assert.IsAssignableFrom<OperationCanceledException>(escaped);
                Assert.Equal(token, canceled.CancellationToken);
                Assert.False(TestContext.Current.CancellationToken.IsCancellationRequested);
            }
            else if (cleanupFault)
                Assert.Same(secondary, escaped);
            else
            {
                Assert.Null(escaped);
                if (family is Family.Request1 or Family.Request2 or Family.Request3)
                    Assert.True(resultObserved);
            }
        }
        finally
        {
            if (!operation.IsCompleted)
            {
                operationCancellation.Cancel();
                clock.Advance(timeout);
                await Record.ExceptionAsync(() => operation.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            }
        }

        async Task RequestOneAsync()
        {
            Response<ResponseMessage> result = await ((IRequestClient<Request>)endpoint)
                .ExecuteAndWaitForIdleAsync<Request, ResponseMessage>(async e =>
                { await Action(e); return response; }, timeout, idle, clock, token);
            Assert.Same(response, result);
            Assert.Same(message, result.Message);
            resultObserved = true;
        }
        async Task RequestTwoAsync()
        {
            var branches = new Response<OtherResponse, ResponseMessage>(other1.Task, chosenBranch);
            Response<OtherResponse, ResponseMessage> result = await ((IRequestClient<Request>)endpoint)
                .ExecuteAndWaitForIdleAsync<Request, OtherResponse, ResponseMessage>(async e =>
                { await Action(e); return branches; }, timeout, idle, clock, token);
            Assert.True(result.Is(out Response<ResponseMessage>? chosen));
            Assert.Same(response, chosen);
            Assert.False(result.Is(out Response<OtherResponse>? _));
            var (first, second) = result;
            Assert.Same(other1.Task, first);
            Assert.Same(chosenBranch, second);
            Assert.Same(message, result.Message);
            resultObserved = true;
        }
        async Task RequestThreeAsync()
        {
            var branches = new Response<OtherResponse, ThirdResponse, ResponseMessage>(other1.Task, other2.Task, chosenBranch);
            Response<OtherResponse, ThirdResponse, ResponseMessage> result = await ((IRequestClient<Request>)endpoint)
                .ExecuteAndWaitForIdleAsync<Request, OtherResponse, ThirdResponse, ResponseMessage>(async e =>
                { await Action(e); return branches; }, timeout, idle, clock, token);
            Assert.True(result.Is(out Response<ResponseMessage>? chosen));
            Assert.Same(response, chosen);
            Assert.False(result.Is(out Response<OtherResponse>? _));
            Assert.False(result.Is(out Response<ThirdResponse>? _));
            var (first, second, third) = result;
            Assert.Same(other1.Task, first);
            Assert.Same(other2.Task, second);
            Assert.Same(chosenBranch, third);
            Assert.Same(message, result.Message);
            resultObserved = true;
        }
    }

    public class StrictProxy : DispatchProxy
    {
        public object? Message { get; set; }
        public int UnexpectedCalls { get; private set; }
        public int MessageReads { get; private set; }
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == "get_Message" && Message is not null && args is { Length: 0 })
            {
                MessageReads++;
                return Message;
            }
            UnexpectedCalls++;
            throw new InvalidOperationException("Unexpected public SPI call: " + targetMethod?.Name);
        }
    }
    public sealed class Request { }
    public sealed class ResponseMessage { }
    public sealed class OtherResponse { }
    public sealed class ThirdResponse { }
}
