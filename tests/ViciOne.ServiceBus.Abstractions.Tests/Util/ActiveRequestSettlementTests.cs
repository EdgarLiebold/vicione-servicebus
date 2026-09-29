using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Util;

public sealed class ActiveRequestSettlementTests
{
    private static readonly TimeSpan CompletionTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "external-callers-cannot-create-unowned-request")]
    public async Task ExternalCallers_CannotConstructUnownedRequestAsync()
    {
        Assert.Empty(typeof(ActiveRequest).GetConstructors());

        CancellationToken testToken = TestContext.Current.CancellationToken;
        using var algorithm = CreateAlgorithm();
        using ActiveRequest owner = await algorithm.BeginRequestAsync(testToken).WaitAsync(CompletionTimeout, testToken);
        Assert.Equal(1, algorithm.ActiveRequestCount);
        await owner.CompleteAsync(0, testToken);
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "duplicate-completion-preserves-one-request-permit")]
    public async Task CompletingOneLeaseTwice_DoesNotCreateAnotherRequestPermitAsync()
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        using var algorithm = CreateAlgorithm();
        using (ActiveRequest completed = await algorithm.BeginRequestAsync(testToken))
        {
            await completed.CompleteAsync(0, testToken);
            await Assert.ThrowsAsync<InvalidOperationException>(() => completed.CompleteAsync(0, testToken));
        }
        Assert.Equal(0, algorithm.ActiveRequestCount);

        using ActiveRequest owner = await algorithm.BeginRequestAsync(testToken).WaitAsync(CompletionTimeout, testToken);
        Assert.Equal(1, algorithm.ActiveRequestCount);
        using var blockedCancellation = new CancellationTokenSource();
        Task<ActiveRequest> blocked = algorithm.BeginRequestAsync(blockedCancellation.Token);
        Assert.False(blocked.IsCompleted);
        blockedCancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => blocked.WaitAsync(CompletionTimeout, testToken));
        Assert.Equal(1, algorithm.ActiveRequestCount);
        await owner.CompleteAsync(0, testToken);
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "completion-after-disposal-preserves-owner-accounting")]
    public async Task CompletingDisposedLease_CannotReleaseOwnerCapacityAgainAsync()
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        using var algorithm = CreateAlgorithm();
        ActiveRequest abandoned = await algorithm.BeginRequestAsync(testToken);
        abandoned.Dispose();
        Assert.Equal(0, algorithm.ActiveRequestCount);

        await Assert.ThrowsAsync<ObjectDisposedException>(() => abandoned.CompleteAsync(0, testToken));
        Assert.Equal(0, algorithm.ActiveRequestCount);
        using ActiveRequest successor = await algorithm.BeginRequestAsync(testToken).WaitAsync(CompletionTimeout, testToken);
        Assert.Equal(1, algorithm.ActiveRequestCount);
        await successor.CompleteAsync(0, testToken);
        Assert.Equal(0, algorithm.ActiveRequestCount);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-REQUEST-RATE-CANCELLATION", "concurrent-completion-and-disposal-settle-once")]
    public async Task CompletingAndDisposingConcurrently_SettleOneLeaseExactlyOnceAsync()
    {
        CancellationToken testToken = TestContext.Current.CancellationToken;
        using var algorithm = CreateAlgorithm();
        for (var attempt = 0; attempt < 32; attempt++)
        {
            ActiveRequest lease = await algorithm.BeginRequestAsync(testToken).WaitAsync(CompletionTimeout, testToken);
            var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Task<Exception?> completion = Task.Run(async () =>
            {
                await start.Task;
                return await Record.ExceptionAsync(() => lease.CompleteAsync(0, testToken));
            }, testToken);
            Task disposal = Task.Run(async () =>
            {
                await start.Task;
                lease.Dispose();
            }, testToken);

            start.SetResult();
            await disposal.WaitAsync(CompletionTimeout, testToken);
            Exception? completionFailure = await completion.WaitAsync(CompletionTimeout, testToken);
            Assert.True(completionFailure is null or ObjectDisposedException);
            Assert.Equal(0, algorithm.ActiveRequestCount);
            using ActiveRequest successor = await algorithm.BeginRequestAsync(testToken).WaitAsync(CompletionTimeout, testToken);
            Assert.Equal(1, algorithm.ActiveRequestCount);
            await successor.CompleteAsync(0, testToken);
            Assert.Equal(0, algorithm.ActiveRequestCount);
        }
    }

    private static RequestRateAlgorithm CreateAlgorithm() => new(new RequestRateAlgorithmOptions
    {
        PrefetchCount = 1,
        RequestResultLimit = 1,
        ConcurrentResultLimit = 2,
    });
}
