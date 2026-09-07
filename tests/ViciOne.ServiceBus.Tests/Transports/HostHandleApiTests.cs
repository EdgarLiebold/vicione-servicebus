using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Transports;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Transports;

public sealed class HostHandleApiTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-HOST-HANDLE-API", "canonical-interface-shape-and-hidden-implementation")]
    public void HostHandle_UsesCanonicalInterfaceShapeWithoutLeakingItsImplementation()
    {
        Type handleType = typeof(IHostHandle);

        Assert.True(handleType.IsPublic);
        Assert.True(handleType.IsInterface);
        Assert.Null(handleType.Assembly.GetType("ViciOne.ServiceBus.Transports.HostHandle", throwOnError: false));
        Type? implementationType = handleType.Assembly.GetType("ViciOne.ServiceBus.Transports.StartHostHandle", throwOnError: false);
        Assert.NotNull(implementationType);
        Assert.True(implementationType!.IsNotPublic);
        Assert.True(implementationType.IsSealed);

        PropertyInfo ready = Assert.Single(handleType.GetProperties(BindingFlags.Instance | BindingFlags.Public));
        Assert.Equal(nameof(IHostHandle.Ready), ready.Name);
        Assert.Equal(typeof(Task<HostReady>), ready.PropertyType);

        MethodInfo stop = Assert.Single(
            handleType.GetMethods(BindingFlags.Instance | BindingFlags.Public),
            static method => !method.IsSpecialName);
        Assert.Equal(nameof(IHostHandle.StopAsync), stop.Name);
        Assert.Equal(typeof(Task), stop.ReturnType);

        ParameterInfo cancellationToken = Assert.Single(stop.GetParameters());
        Assert.Equal("cancellationToken", cancellationToken.Name);
        Assert.Equal(typeof(CancellationToken), cancellationToken.ParameterType);
        Assert.True(cancellationToken.IsOptional);
    }
}
