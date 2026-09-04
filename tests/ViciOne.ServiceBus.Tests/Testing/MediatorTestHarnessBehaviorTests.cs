using Microsoft.Extensions.Time.Testing;
using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class MediatorTestHarnessBehaviorTests
{
    private static readonly DateTimeOffset StartTime =
        new(2026, 8, 24, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MEDIATOR", "request-response-and-observations")]
    public async Task RequestResponse_RecordsTheExactRequestAndResponseOnTheConfiguredClockAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var timeProvider = new FakeTimeProvider(StartTime);
        using var harness = new MediatorTestHarness(timeProvider)
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureMediator += configurator => configurator.Handler<MediatorRequest>(context =>
            context.RespondAsync(new MediatorResponse($"response:{context.Message.Value}")));

        await harness.StartAsync(TestContext.Current.CancellationToken);
        IRequestClient<MediatorRequest> client = harness.CreateRequestClient<MediatorRequest>();
        Response<MediatorResponse> response = await client.GetResponseAsync<MediatorResponse>(
            new MediatorRequest("expected"),
            cancellationToken);

        IReceivedMessage<MediatorRequest> consumed = await harness.Consumed
            .SelectAsync<MediatorRequest>(cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
        ISentMessage<MediatorResponse> sent = await harness.Sent
            .SelectAsync<MediatorResponse>(cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(timeProvider, harness.TimeProvider);
        Assert.Equal("expected", consumed.Context.Message.Value);
        Assert.Null(consumed.Exception);
        Assert.Equal("response:expected", response.Message.Value);
        Assert.Equal("response:expected", sent.Context.Message.Value);
        Assert.Equal(response.RequestId, sent.Context.RequestId);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-MEDIATOR", "exact-failure-propagation")]
    public async Task HandlerFailure_IsPropagatedAndRecordedWithoutWrappingAsync()
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        var expected = new InvalidOperationException("expected mediator failure");
        using var harness = new MediatorTestHarness
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };
        harness.OnConfigureMediator += configurator => configurator.Handler<MediatorFailureMessage>(_ => Task.FromException(expected));

        await harness.StartAsync(TestContext.Current.CancellationToken);

        InvalidOperationException actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            harness.Mediator.SendAsync(new MediatorFailureMessage(), cancellationToken));
        IReceivedMessage<MediatorFailureMessage> observed = await harness.Consumed
            .SelectAsync<MediatorFailureMessage>(cancellationToken)
            .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

        Assert.Same(expected, actual);
        Assert.Same(expected, observed.Exception);
    }

    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private sealed record MediatorRequest(string Value);

    private sealed record MediatorResponse(string Value);

    private sealed record MediatorFailureMessage;
}
