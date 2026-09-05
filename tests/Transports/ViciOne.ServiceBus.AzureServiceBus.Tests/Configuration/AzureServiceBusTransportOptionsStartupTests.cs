using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ViciOne.ServiceBus.AzureServiceBus.Tests.Configuration;

public sealed class AzureServiceBusTransportOptionsStartupTests
{
    [Fact]
    public void EmptyConnectionString_FailsAtStartupBeforeClientCreation()
    {
        using ServiceProvider provider = Provider(" ");

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains("ConnectionString", string.Join(Environment.NewLine, exception.Failures), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("Endpoint=sb://example.servicebus.windows.net/;SharedAccessKeyName=name;SharedAccessKey=key")]
    public void UnsetOrCompleteConnectionString_PassesStartupValidation(string? connectionString)
    {
        using ServiceProvider provider = Provider(connectionString);

        provider.GetRequiredService<IStartupValidator>().Validate();
    }

    static ServiceProvider Provider(string? connectionString)
    {
        var services = new ServiceCollection();
        services.Configure<AzureServiceBusTransportOptions>(options => options.ConnectionString = connectionString);
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingAzureServiceBus();
        });
        return services.BuildServiceProvider();
    }
}
