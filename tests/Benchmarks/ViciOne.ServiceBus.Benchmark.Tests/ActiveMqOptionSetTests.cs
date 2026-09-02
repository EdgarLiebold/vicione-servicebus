using NDesk.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOneServiceBusBenchmark;
using Xunit;

namespace ViciOne.ServiceBus.Benchmark.Tests;

public sealed class ActiveMqOptionSetTests
{
    public static TheoryData<string[], string> MissingCoordinates => new()
    {
        { ["--protocol=openwire", "--port=61616"], "host" },
        { ["--host=broker.internal", "--port=61616"], "protocol" },
        { ["--host=broker.internal", "--protocol=openwire"], "port" },
    };

    public static TheoryData<string[]> CoordinateOrders => new()
    {
        { ["--host=broker.internal", "--protocol=amqp", "--port=5672"] },
        { ["--port=5672", "--host=broker.internal", "--protocol=amqp"] },
    };

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "option-set-remains-internal-and-sealed")]
    public void OptionSet_RemainsAnInternalSealedImplementationDetail()
    {
        Type optionSetType = typeof(ActiveMqOptionSet);

        Assert.True(optionSetType.IsNotPublic);
        Assert.True(optionSetType.IsSealed);
    }

    [Theory]
    [InlineData("openwire", ActiveMqTransportProtocol.OpenWire, "activemq", 61616, "activemq:tcp://")]
    [InlineData("amqp", ActiveMqTransportProtocol.Amqp, "amqp", 5672, "amqp://")]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "explicit-coordinates-select-exact-provider")]
    public void ExplicitCoordinates_SelectTheExactProvider(
        string protocolOption,
        ActiveMqTransportProtocol expectedProtocol,
        string expectedScheme,
        int port,
        string expectedBrokerPrefix)
    {
        var options = new ActiveMqOptionSet();

        IReadOnlyList<string> remaining = options.Parse(
            ["--host=broker.internal", $"--protocol={protocolOption}", $"--port={port}"]);

        Assert.Empty(remaining);
        Assert.Equal(expectedProtocol, options.Protocol);
        Assert.Equal("broker.internal", options.Host);
        Assert.Equal(port, options.Port);
        Assert.Equal(expectedScheme, options.HostAddress.Scheme);
        Assert.Equal(options.HostAddress.ToString(), options.ToString());
        Assert.StartsWith(expectedBrokerPrefix, options.BrokerAddress.AbsoluteUri, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(MissingCoordinates))]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "every-connection-coordinate-is-required")]
    public void MissingConnectionCoordinate_FailsDuringParsing(string[] arguments, string expectedOption)
    {
        var options = new ActiveMqOptionSet();

        OptionException exception = Assert.Throws<OptionException>(() => options.Parse(arguments));

        Assert.Equal(expectedOption, exception.OptionName);
        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("stomp")]
    [InlineData("0")]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "unknown-protocol-fails-fast")]
    public void UnknownProtocol_FailsAtItsOwnOption(string protocol)
    {
        var options = new ActiveMqOptionSet();

        OptionException exception = Assert.Throws<OptionException>(() => options.Parse(
            ["--host=broker.internal", $"--protocol={protocol}", "--port=61613"]));

        Assert.Equal("protocol", exception.OptionName);
        Assert.Contains("openwire or amqp", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [MemberData(nameof(MissingCoordinates))]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "reparse-does-not-retain-connection-coordinates")]
    public void Reparse_MakesEachConnectionCoordinateRequiredAgain(string[] arguments, string expectedOption)
    {
        var options = new ActiveMqOptionSet();
        options.Parse(["--host=broker.internal", "--protocol=openwire", "--port=61616"]);

        OptionException exception = Assert.Throws<OptionException>(() => options.Parse(arguments));

        Assert.Equal(expectedOption, exception.OptionName);
        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "reparse-does-not-retain-credentials-or-tls")]
    public void Reparse_DoesNotRetainCredentialsOrTls()
    {
        var options = new ActiveMqOptionSet();
        options.Parse(
        [
            "--host=broker.internal",
            "--protocol=openwire",
            "--port=61616",
            "--username=client",
            "--password=secret",
            "--ssl=true",
        ]);

        IReadOnlyList<string> remaining = options.Parse(
            ["--host=broker2.internal", "--protocol=amqp", "--port=5672"]);

        Assert.Empty(remaining);
        Assert.Equal("broker2.internal", options.Host);
        Assert.Equal(ActiveMqTransportProtocol.Amqp, options.Protocol);
        Assert.Equal(5672, options.Port);
        Assert.Equal("", options.Username);
        Assert.Equal("", options.Password);
        Assert.False(options.UseSsl);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "failed-reparse-invalidates-effective-settings")]
    public void FailedReparse_DoesNotExposePreviouslyEffectiveSettings()
    {
        var options = new ActiveMqOptionSet();
        options.Parse(["--host=broker.internal", "--protocol=openwire", "--port=61616"]);

        Assert.Throws<OptionException>(() => options.Parse([]));

        OptionException exception = Assert.Throws<OptionException>(() => _ = options.Host);
        Assert.Equal("active-mq", exception.OptionName);
        Assert.Contains("must be parsed", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(65536)]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "invalid-port-fails-fast")]
    public void InvalidPort_FailsAtItsOwnOption(int port)
    {
        var options = new ActiveMqOptionSet();

        OptionException exception = Assert.Throws<OptionException>(() => options.Parse(
            ["--host=broker.internal", "--protocol=openwire", $"--port={port}"]));

        Assert.Equal("port", exception.OptionName);
        Assert.Contains("1..65535", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("openwire", "activemq:ssl://")]
    [InlineData("amqp", "amqps://")]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "credentials-and-tls-reach-selected-provider")]
    public void CredentialsAndTls_ReachTheSelectedProvider(string protocol, string expectedBrokerPrefix)
    {
        var options = new ActiveMqOptionSet();

        options.Parse(
        [
            "--host=broker.internal",
            $"--protocol={protocol}",
            "--port=443",
            "--username=client",
            "--password=secret",
            "--ssl=true",
        ]);

        Assert.Equal("client", options.Username);
        Assert.Equal("secret", options.Password);
        Assert.True(options.UseSsl);
        Assert.StartsWith(expectedBrokerPrefix, options.BrokerAddress.AbsoluteUri, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(CoordinateOrders))]
    [RequirementCoverage("REQ-VSB-BENCHMARK-ACTIVEMQ", "coordinate-order-does-not-change-effective-settings")]
    public void CoordinateOrder_DoesNotChangeTheEffectiveSettings(string[] arguments)
    {
        var options = new ActiveMqOptionSet();

        options.Parse(arguments);

        Assert.Equal(ActiveMqTransportProtocol.Amqp, options.Protocol);
        Assert.Equal(new Uri("amqp://broker.internal:5672/"), options.HostAddress);
    }
}
