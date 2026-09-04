using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class CancellationTokenExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-CANCELLATION-REGISTRATION", "requires-cancelable-token")]
    public void RegisterTask_RejectsANonCancelableToken()
    {
        ArgumentException exception = Assert.Throws<ArgumentException>(() =>
            CancellationToken.None.RegisterTask(out _));

        Assert.Equal("cancellationToken", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-CANCELLATION-REGISTRATION", "completion-signal")]
    public async Task RegisterTask_CompletesItsSignalWhenCancellationIsRequestedAsync()
    {
        using var cancellation = new CancellationTokenSource();
        using CancellationTokenRegistration registration =
            cancellation.Token.RegisterTask(out Task cancellationSignal);

        Assert.False(cancellationSignal.IsCompleted);
        cancellation.Cancel();

        await cancellationSignal.WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.True(cancellationSignal.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-CANCELLATION-REGISTRATION", "already-canceled-token")]
    public async Task RegisterTask_ImmediatelySignalsAnAlreadyCanceledTokenAsync()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        using CancellationTokenRegistration registration =
            cancellation.Token.RegisterTask(out Task cancellationSignal);

        await cancellationSignal.WaitAsync(OperationTimeout, TestCancellationToken);
        Assert.True(cancellationSignal.IsCompletedSuccessfully);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-CANCELLATION-REGISTRATION", "linked-source")]
    public void RegisterIfCanBeCanceled_CancelsTheTargetSource()
    {
        using var trigger = new CancellationTokenSource();
        using var target = new CancellationTokenSource();
        using CancellationTokenRegistration registration =
            trigger.Token.RegisterIfCanBeCanceled(target);

        trigger.Cancel();

        Assert.True(target.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-CANCELLATION-REGISTRATION", "noncancelable-noop")]
    public void RegisterIfCanBeCanceled_ReturnsAnEmptyRegistrationForANonCancelableToken()
    {
        using var target = new CancellationTokenSource();

        CancellationTokenRegistration registration =
            CancellationToken.None.RegisterIfCanBeCanceled(target);

        Assert.Equal(default, registration);
        Assert.False(target.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-CANCELLATION-REGISTRATION", "null-source")]
    public void RegisterIfCanBeCanceled_RejectsANullTargetForEveryTokenShape()
    {
        using var cancellation = new CancellationTokenSource();

        ArgumentNullException cancelable = Assert.Throws<ArgumentNullException>(() =>
            cancellation.Token.RegisterIfCanBeCanceled(null!));
        ArgumentNullException nonCancelable = Assert.Throws<ArgumentNullException>(() =>
            CancellationToken.None.RegisterIfCanBeCanceled(null!));

        Assert.Equal("source", cancelable.ParamName);
        Assert.Equal("source", nonCancelable.ParamName);
    }

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;
}
