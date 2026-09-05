using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace ViciOne.ServiceBus.ActiveMq.Tests.Configuration;

public sealed class ActiveMqTransportOptionsStartupTests
{
    [Theory]
    [InlineData(InvalidOption.Host, "Host")]
    [InlineData(InvalidOption.Protocol, "Protocol")]
    [InlineData(InvalidOption.Port, "Port")]
    public void PartialTransportOptions_FailAtStartup(InvalidOption invalid, string property)
    {
        using ServiceProvider provider = Provider(invalid);

        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IStartupValidator>().Validate());

        Assert.Contains(property, string.Join(Environment.NewLine, exception.Failures), StringComparison.Ordinal);
    }

    [Fact]
    public void EmptyOrCompleteTransportOptions_PassStartupValidation()
    {
        using ServiceProvider empty = Provider(null);
        empty.GetRequiredService<IStartupValidator>().Validate();

        using ServiceProvider complete = Provider(InvalidOption.None);
        complete.GetRequiredService<IStartupValidator>().Validate();
    }

    static ServiceProvider Provider(InvalidOption? invalid)
    {
        var services = new ServiceCollection();
        services.Configure<ActiveMqTransportOptions>(options =>
        {
            if (invalid is null)
                return;

            options.Host = invalid == InvalidOption.Host ? null : "localhost";
            options.Protocol = invalid == InvalidOption.Protocol ? null : ActiveMqTransportProtocol.OpenWire;
            options.Port = invalid == InvalidOption.Port ? null : (ushort)61616;
        });
        services.AddViciOneServiceBus(bus =>
        {
            bus.Limits(MessageLimits.Conservative);
            bus.UsingActiveMq();
        });
        return services.BuildServiceProvider();
    }

    public enum InvalidOption
    {
        None,
        Host,
        Protocol,
        Port,
    }
}
