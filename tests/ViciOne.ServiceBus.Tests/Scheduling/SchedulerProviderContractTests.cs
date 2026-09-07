using System.Reflection;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Scheduling;

public sealed class SchedulerProviderContractTests
{
    private static readonly Uri Destination = new("loopback://localhost/scheduler-provider-boundary");
    private static readonly DateTimeOffset DueAt = new(2039, 1, 2, 3, 4, 5, TimeSpan.Zero);

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-PROVIDER-BOUNDARY", "constructors-reject-missing-collaborators")]
    public void Constructors_RejectMissingCollaborators()
    {
        Assert.Equal("sendEndpointProvider", Assert.Throws<ArgumentNullException>(() =>
            new DelayedScheduleMessageProvider(null!)).ParamName);
        Assert.Equal("schedulerEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new EndpointScheduleMessageProvider(null!)).ParamName);
        Assert.Equal("publishEndpoint", Assert.Throws<ArgumentNullException>(() =>
            new PublishScheduleMessageProvider(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SCHEDULER-PROVIDER-BOUNDARY", "schedule-inputs-fail-before-dispatch")]
    public async Task ScheduleInputs_FailBeforeProviderDispatchAsync()
    {
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, UnexpectedInvocationProxy>();
        IPublishEndpoint publisher = DispatchProxy.Create<IPublishEndpoint, UnexpectedInvocationProxy>();
        IScheduleMessageProvider[] providers =
        [
            new DelayedScheduleMessageProvider(endpoints),
            new EndpointScheduleMessageProvider(_ => throw new InvalidOperationException("The endpoint resolver must not run.")),
            new PublishScheduleMessageProvider(publisher),
        ];

        foreach (IScheduleMessageProvider provider in providers)
        {
            Assert.Equal("destinationAddress", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                provider.ScheduleSendAsync(null!, DueAt, new Probe(), Pipe.Empty<SendContext<Probe>>(),
                    TestContext.Current.CancellationToken))).ParamName);
            Assert.Equal("message", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                provider.ScheduleSendAsync<Probe>(Destination, DueAt, null!, Pipe.Empty<SendContext<Probe>>(),
                    TestContext.Current.CancellationToken))).ParamName);
            Assert.Equal("pipe", (await Assert.ThrowsAsync<ArgumentNullException>(() =>
                provider.ScheduleSendAsync(Destination, DueAt, new Probe(), null!,
                    TestContext.Current.CancellationToken))).ParamName);
        }
    }

    private sealed record Probe;

    private class UnexpectedInvocationProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new InvalidOperationException($"The invalid boundary invoked {targetMethod?.Name}.");
    }
}
