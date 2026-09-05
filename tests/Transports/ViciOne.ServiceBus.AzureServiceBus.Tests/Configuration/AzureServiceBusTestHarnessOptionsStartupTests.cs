using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ViciOne.ServiceBus.AzureServiceBus.Testing;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.Configuration;

public sealed class AzureServiceBusTestHarnessOptionsStartupTests
{
    [Fact]
    public void HarnessOptions_AreBoundToTheStartupValidator()
    {
        using ServiceProvider provider = new ServiceCollection()
            .ConfigureServiceBusTestOptions(options => options.CleanNamespace = true)
            .BuildServiceProvider();

        provider.GetRequiredService<IStartupValidator>().Validate();
        Assert.True(provider.GetRequiredService<IOptions<AzureServiceBusTestHarnessOptions>>().Value.CleanNamespace);
    }
}
