using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

[Collection(OpenTelemetryGlobalCollection.Name)]
public sealed class TestHarnessObservationPolicyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-RETENTION", "all-bounded-and-none-apply-to-nested-observers")]
    public async Task ContextRetention_AllBoundedAndNoneApplyToBusAndHandlerHistoriesAsync()
    {
        await VerifyRetentionAsync(TestContextSaveMode.All, [1, 2, 3]);
        await VerifyRetentionAsync(TestContextSaveMode.Bounded, [2, 3]);
        await VerifyRetentionAsync(TestContextSaveMode.None, []);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-ACTIVE-OBSERVATION", "trace-isolated-and-retention-independent")]
    public async Task ActiveScope_CapturesOnlyCausalTraceWhenHistoryIsDisabledAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, TestContextSaveMode.None, 4);
        harness.TestInactivityTimeout = TimeSpan.FromMilliseconds(100);
        var trackedCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var unrelatedCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        harness.Handler<ActiveMessage>(async context =>
        {
            await context.Advanced()
                .PublishAsync(new ActivePublished(context.Message.Value), context.CancellationToken)
                .ConfigureAwait(false);
            TaskCompletionSource completion = context.Message.Value == "tracked"
                ? trackedCompleted
                : unrelatedCompleted;
            completion.TrySetResult();
        });

        await harness.StartAsync(cancellationToken);
        try
        {
            ActiveTestResult result = await harness.ActAsync(async () =>
            {
                Task unrelated;
                using (ExecutionContext.SuppressFlow())
                {
                    unrelated = Task.Run(
                        () => harness.InputQueueSendEndpoint.SendAsync(new ActiveMessage("unrelated"), cancellationToken),
                        cancellationToken);
                }

                await harness.InputQueueSendEndpoint.SendAsync(new ActiveMessage("tracked"), cancellationToken);
                await unrelated;
                await Task.WhenAll(trackedCompleted.Task, unrelatedCompleted.Task)
                    .WaitAsync(timeout, cancellationToken);
            }, "trace-isolation", cancellationToken: TestContext.Current.CancellationToken);

            Assert.Empty(harness.Consumed.Snapshot());
            Assert.Empty(harness.Published.Snapshot());
            Assert.Empty(harness.Sent.Snapshot());

            Assert.Collection(
                result.Consumed.Where(message => message.MessageObject is ActiveMessage),
                message => Assert.Equal(new ActiveMessage("tracked"), message.MessageObject));
            Assert.Collection(
                result.Published.Where(message => message.MessageObject is ActivePublished),
                message => Assert.Equal(new ActivePublished("tracked"), message.MessageObject));
            Assert.Contains(result.Sent, message => message.MessageObject is ActiveMessage("tracked"));
            Assert.DoesNotContain(result.Sent, message => message.MessageObject is ActiveMessage("unrelated"));

            IList<IConsumedMessage> exposedConsumed = Assert.IsAssignableFrom<IList<IConsumedMessage>>(result.Consumed);
            Assert.Throws<NotSupportedException>(() => exposedConsumed.Clear());
            Assert.Throws<NotSupportedException>(() => exposedConsumed[0] = result.Consumed[0]);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-ACTIVE-OBSERVATION", "action-failure-preserves-identity")]
    public async Task ActiveScope_PropagatesTheExactActionFailureAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected action failure");
        using var harness = CreateHarness(timeout, TestContextSaveMode.None, 1);
        harness.TestInactivityTimeout = TimeSpan.FromMilliseconds(100);

        await harness.StartAsync(cancellationToken);
        try
        {
            InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(
                () => harness.ActAsync(() => Task.FromException(expected), "fault-propagation", cancellationToken: TestContext.Current.CancellationToken));

            Assert.Same(expected, actual);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken);
        }
    }

    private static async Task VerifyRetentionAsync(TestContextSaveMode saveMode, int[] expectedValues)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness(timeout, saveMode, 2);
        HandlerTestHarness<RetentionMessage> handler = harness.Handler<RetentionMessage>(context =>
            context.Advanced().PublishAsync(new RetentionPublished(context.Message.Value), context.CancellationToken));

        await harness.StartAsync(cancellationToken);
        try
        {
            for (var value = 1; value <= 3; value++)
            {
                int expected = value;
                Task<bool> busConsumed = harness.Consumed.AnyAsync<RetentionMessage>(
                    message => message.Context.Message.Value == expected,
                    cancellationToken);
                Task<bool> handlerConsumed = handler.Consumed.AnyAsync(
                    message => message.Context.Message.Value == expected,
                    cancellationToken);
                Task<bool> published = harness.Published.AnyAsync<RetentionPublished>(
                    message => message.Context.Message.Value == expected,
                    cancellationToken);

                await harness.InputQueueSendEndpoint.SendAsync(new RetentionMessage(value), cancellationToken);

                Assert.True(await busConsumed.WaitAsync(timeout, cancellationToken));
                Assert.True(await handlerConsumed.WaitAsync(timeout, cancellationToken));
                Assert.True(await published.WaitAsync(timeout, cancellationToken));
            }

            Assert.Equal(saveMode, harness.Consumed.SaveMode);
            Assert.Equal(saveMode, harness.Published.SaveMode);
            Assert.Equal(saveMode, harness.Sent.SaveMode);
            Assert.Equal(saveMode, handler.Consumed.SaveMode);
            Assert.Equal(2, harness.Consumed.MaximumSavedElements);
            Assert.Equal(2, harness.Published.MaximumSavedElements);
            Assert.Equal(2, harness.Sent.MaximumSavedElements);
            Assert.Equal(2, handler.Consumed.MaximumSavedElements);

            Assert.Equal(expectedValues, harness.Consumed.Snapshot()
                .OfType<IConsumedMessage<RetentionMessage>>()
                .Select(message => message.Context.Message.Value));
            Assert.Equal(expectedValues, handler.Consumed.Snapshot()
                .Select(message => message.Context.Message.Value));
            Assert.Equal(expectedValues, harness.Published.Snapshot()
                .OfType<IPublishedMessage<RetentionPublished>>()
                .Select(message => message.Context.Message.Value));

            if (saveMode == TestContextSaveMode.None)
                Assert.Empty(harness.Sent.Snapshot());
            else if (saveMode == TestContextSaveMode.Bounded)
                Assert.Equal(2, harness.Sent.Count);
            else
                Assert.True(harness.Sent.Count >= 3);
        }
        finally
        {
            await harness.StopAsync();
        }
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static InMemoryTestHarness CreateHarness(
        TimeSpan timeout,
        TestContextSaveMode saveMode,
        int maximumSavedContexts) =>
        new($"observation-policy-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
            ContextSaveMode = saveMode,
            MaximumSavedContexts = maximumSavedContexts,
        };

    public sealed record RetentionMessage(int Value);

    public sealed record RetentionPublished(int Value);

    public sealed record ActiveMessage(string Value);

    public sealed record ActivePublished(string Value);
}
