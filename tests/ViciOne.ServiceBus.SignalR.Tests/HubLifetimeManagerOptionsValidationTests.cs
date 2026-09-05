using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ViciOne.ServiceBus.SignalR.Tests;

public sealed class HubLifetimeManagerOptionsValidationTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Registration_RejectsAnEmptyServerNameBeforeAddingHubServices(string? serverName)
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        ConfigurationException exception = Assert.Throws<ConfigurationException>(() =>
            configurator.AddSignalRHub<TestHub>(options => options.ServerName = serverName!));

        Assert.Equal(
            "SignalR hub lifetime for bus 'default': ServerName must not be empty. Set a stable non-empty server name.",
            exception.Message);
    }

    [Fact]
    public void Registration_AcceptsTheCoherentDefaultOptions()
    {
        var services = new ServiceCollection();
        var configurator = new ServiceCollectionBusConfigurator(services);

        configurator.AddSignalRHub<TestHub>();

        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(HubLifetimeManager<TestHub>));
    }

    public sealed class TestHub : Hub;
}
