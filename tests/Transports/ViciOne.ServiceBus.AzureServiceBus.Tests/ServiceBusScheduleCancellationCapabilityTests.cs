using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Scheduling;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests;

public sealed class ServiceBusScheduleCancellationCapabilityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-ASB-SCHEDULER-CANCELLATION", "advertises-provider-assigned-cancellation-token-mode")]
    public void CancellationMode_AdvertisesProviderAssignedToken()
    {
        ISendEndpointProvider endpoints = DispatchProxy.Create<ISendEndpointProvider, UnexpectedEndpointProxy>();

        Assert.Equal(ScheduleCancellationMode.ProviderAssignedToken,
            new ServiceBusScheduleMessageProvider(endpoints).CancellationMode);
    }

    public class UnexpectedEndpointProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? method, object?[]? arguments) =>
            throw new InvalidOperationException($"Endpoint resolution was unexpected: {method?.Name}");
    }
}
