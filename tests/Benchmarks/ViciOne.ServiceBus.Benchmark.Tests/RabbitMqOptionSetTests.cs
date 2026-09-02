using System.Net.Security;
using System.Security.Authentication;
using NDesk.Options;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOneServiceBusBenchmark;
using Xunit;

namespace ViciOne.ServiceBus.Benchmark.Tests;

[Collection(ProcessStateCollection.Name)]
public sealed class RabbitMqOptionSetTests
{
    private const string Secret = "not-a-real-secret-0d6f2a";

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "tls-defaults-do-not-weaken-validation")]
    public void Defaults_DoNotWeakenTlsValidation()
    {
        var options = new RabbitMqOptionSet();

        Assert.Equal(SslProtocols.None, options.SslProtocol);
        Assert.Equal(SslPolicyErrors.None, options.AcceptablePolicyErrors);
    }

    [Theory]
    [InlineData(true, 5671)]
    [InlineData(false, 5672)]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "ssl-controls-default-port")]
    public void SslOption_ControlsTlsAndDefaultPort(bool enabled, int expectedPort)
    {
        var options = new RabbitMqOptionSet();

        options.Parse([$"--ssl={enabled}"]);

        Assert.Equal(enabled, options.Ssl);
        Assert.Equal(expectedPort, options.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "nondefault-port-reaches-address")]
    public void A_non_default_port_reaches_the_address()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--host=127.0.0.1", "--port=32774", "--ssl=false"]);

        Assert.Equal("127.0.0.1", options.HostAddress.Host);
        Assert.Equal(32774, options.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "port-in-host-is-rejected")]
    public void A_port_inside_the_host_is_refused_while_it_is_parsed()
    {
        var options = new RabbitMqOptionSet();

        OptionException exception = Assert.Throws<OptionException>(
            () => options.Parse(["--host=127.0.0.1:32774"]));

        Assert.Equal("host", exception.OptionName);
        Assert.Contains("--port", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("rabbit.example.invalid")]
    [InlineData("127.0.0.1")]
    [InlineData("[::1]")]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "host-forms-without-port")]
    public void A_host_without_a_port_is_accepted_in_every_form(string host)
    {
        var options = new RabbitMqOptionSet();

        options.Parse([$"--host={host}"]);

        Assert.Equal(host, options.Host);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("65536")]
    [InlineData("-1")]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "port-range-is-enforced")]
    public void A_port_outside_the_valid_range_is_refused(string port)
    {
        var options = new RabbitMqOptionSet();

        OptionException exception = Assert.Throws<OptionException>(() => options.Parse([$"--port={port}"]));

        Assert.Equal("port", exception.OptionName);
        Assert.Contains("65535", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "port-survives-earlier-ssl")]
    public void A_given_port_survives_an_earlier_ssl_switch()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--ssl=true", "--port=32774"]);

        Assert.Equal(32774, options.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "tls-server-name-follows-host")]
    public void The_tls_server_name_follows_the_host_that_was_given()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--host=rabbit.example.invalid", "--ssl=true"]);

        Assert.Equal("rabbit.example.invalid", options.SslServerName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "explicit-tls-server-name-wins")]
    public void An_explicit_tls_server_name_wins_over_the_host()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--host=127.0.0.1", "--ssl=true", "--ssl-server-name=rabbit.example.invalid"]);

        Assert.Equal("127.0.0.1", options.Host);
        Assert.Equal("rabbit.example.invalid", options.SslServerName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "port-survives-later-ssl")]
    public void A_given_port_survives_a_later_ssl_switch()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--port=32774", "--ssl=true"]);

        Assert.Equal(32774, options.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "ssl-moves-implicit-port")]
    public void Ssl_still_moves_the_default_port_when_none_was_given()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--ssl=true"]);

        Assert.Equal(5671, options.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "batch-limit-reaches-effective-settings")]
    public void The_parsed_batch_limit_reaches_the_effective_settings()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--batch-limit=7"]);

        Assert.Equal(7, options.BatchSettings.MessageLimit);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "batch-timeout-reaches-effective-settings")]
    public void The_parsed_batch_timeout_reaches_the_effective_settings()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--batch-timeout=23"]);

        Assert.Equal(TimeSpan.FromMilliseconds(23), options.BatchSettings.Timeout);
    }

    [Theory]
    [InlineData("--batch=true", "--batch-limit=7", "--batch-timeout=23")]
    [InlineData("--batch-limit=7", "--batch-timeout=23", "--batch=true")]
    [InlineData("--batch-limit=7", "--batch=true", "--batch-timeout=23")]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "batch-option-order-is-invariant")]
    public void Every_batch_option_order_ends_in_one_effective_configuration(
        string first,
        string second,
        string third)
    {
        var options = new RabbitMqOptionSet();

        options.Parse([first, second, third]);

        Assert.True(options.BatchSettings.Enabled);
        Assert.Equal(7, options.BatchSettings.MessageLimit);
        Assert.Equal(TimeSpan.FromMilliseconds(23), options.BatchSettings.Timeout);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "batch-disable-reaches-effective-settings")]
    public void Switching_batching_off_reaches_the_effective_settings()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--batch=false"]);

        Assert.False(options.BatchSettings.Enabled);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "ipv6-address-keeps-port")]
    public void A_bracketed_ipv6_host_reaches_the_address_with_its_port()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(["--host=[::1]", "--port=32774"]);

        Assert.Equal("[::1]", options.HostAddress.Host);
        Assert.Equal(32774, options.HostAddress.Port);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "report-shows-effective-settings-without-secret")]
    public void The_reported_options_are_the_effective_ones()
    {
        var options = new RabbitMqOptionSet();
        options.Parse(
        [
            "--host=rabbit.example.invalid",
            "--ssl=true",
            "--port=32774",
            "--sni=front.example.invalid",
            "--username=benchmark",
            "--password=" + Secret,
            "--batch-limit=7",
            "--batch-timeout=23",
        ]);

        string printed = Capture(options.ShowOptions);

        Assert.Contains("Port: 32774", printed, StringComparison.Ordinal);
        Assert.Contains("TLS: enabled=True", printed, StringComparison.Ordinal);
        Assert.Contains("protocol=None", printed, StringComparison.Ordinal);
        Assert.Contains("server name=front.example.invalid", printed, StringComparison.Ordinal);
        Assert.Contains("limit=7", printed, StringComparison.Ordinal);
        Assert.Contains("Password configured: True", printed, StringComparison.Ordinal);
        Assert.DoesNotContain(Secret, printed, StringComparison.Ordinal);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-BENCHMARK-RABBITMQ", "report-shows-missing-password")]
    public void The_reported_options_say_when_no_password_was_given()
    {
        var options = new RabbitMqOptionSet();
        options.Parse(["--host=rabbit.example.invalid"]);

        string printed = Capture(options.ShowOptions);

        Assert.Contains("Password configured: False", printed, StringComparison.Ordinal);
    }

    private static string Capture(Action write)
    {
        TextWriter original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try
        {
            write();
        }
        finally
        {
            Console.SetOut(original);
        }

        return writer.ToString();
    }
}
