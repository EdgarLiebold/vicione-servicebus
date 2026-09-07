using System.Reflection;
using ViciOne.ServiceBus.Observables;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Observers;

public sealed class BusObservableTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-BUS-OBSERVATION", "every-notification-validates-required-inputs")]
    public async Task LifecycleNotifications_RejectEveryNullRequiredInputAsync()
    {
        var observable = new BusObservable();
        IBus bus = DispatchProxy.Create<IBus, EmptyProxy>();
        var exception = new InvalidOperationException("expected");
        Task<BusReady> ready = Task.FromResult<BusReady>(null!);

        Assert.Equal("bus", Assert.Throws<ArgumentNullException>(() => observable.PostCreate(null!)).ParamName);
        Assert.Equal("exception", Assert.Throws<ArgumentNullException>(() => observable.CreateFaulted(null!)).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.PreStartAsync(null!))).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.PostStartAsync(null!, ready))).ParamName);
        Assert.Equal("busReady", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.PostStartAsync(bus, null!))).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.StartFaultedAsync(null!, exception))).ParamName);
        Assert.Equal("exception", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.StartFaultedAsync(bus, null!))).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.PreStopAsync(null!))).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.PostStopAsync(null!))).ParamName);
        Assert.Equal("bus", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.StopFaultedAsync(null!, exception))).ParamName);
        Assert.Equal("exception", (await Assert.ThrowsAsync<ArgumentNullException>(() => observable.StopFaultedAsync(bus, null!))).ParamName);
    }

    private class EmptyProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException("The proxy is used only as a non-null observer argument.");
    }
}
