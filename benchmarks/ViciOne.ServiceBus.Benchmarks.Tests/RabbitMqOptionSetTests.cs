namespace ViciOne.ServiceBus.Benchmarks.Tests;

using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using NDesk.Options;
using NUnit.Framework;
using ViciOneServiceBusBenchmark;


public class RabbitMqOptionSetTests
{
    [Test]
    public void Defaults_DoNotWeakenTlsValidation()
    {
        var options = new RabbitMqOptionSet();

        Assert.Multiple(() =>
        {
            Assert.That(options.SslProtocol, Is.EqualTo(SslProtocols.None));
            Assert.That(options.AcceptablePolicyErrors, Is.EqualTo(SslPolicyErrors.None));
        });
    }

    [TestCase(true, 5671)]
    [TestCase(false, 5672)]
    public void SslOption_ControlsTlsAndDefaultPort(bool enabled, int expectedPort)
    {
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { $"--ssl={enabled}" });

        Assert.Multiple(() =>
        {
            Assert.That(options.Ssl, Is.EqualTo(enabled));
            Assert.That(options.Port, Is.EqualTo(expectedPort));
        });
    }

    [Test]
    public void A_non_default_port_reaches_the_address()
    {
        // The address builder always had a Port, but no option could set it, so the tool could only
        // ever reach a broker on the default one. The repository's own pinned fixture publishes an
        // ephemeral loopback port by design, so the benchmark could not be pointed at the very
        // fixture it exists to measure.
        var options = new RabbitMqOptionSet();

        // Both orders, because these are independent options and --ssl also touches the port. My
        // first version of this case passed no --ssl at all and stayed green while the real run still
        // dialled 5672.
        options.Parse(new[] { "--host=127.0.0.1", "--port=32774", "--ssl=false" });

        Assert.Multiple(() =>
        {
            Assert.That(options.HostAddress.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(options.HostAddress.Port, Is.EqualTo(32774));
        });
    }

    [Test]
    public void A_port_inside_the_host_is_refused_while_it_is_parsed()
    {
        // The spelling a caller reaches for when there is no port option. It used to reach UriBuilder
        // and die with "the hostname could not be parsed", far from the option that caused it, so it is
        // refused at parse time and names the option instead.
        var options = new RabbitMqOptionSet();

        Assert.That(() => options.Parse(new[] { "--host=127.0.0.1:32774" }),
            Throws.TypeOf<OptionException>().With.Message.Contains("--port"));
    }

    [TestCase("rabbit.example.invalid")]
    [TestCase("127.0.0.1")]
    [TestCase("[::1]")]
    public void A_host_without_a_port_is_accepted_in_every_form(string host)
    {
        // The refusal above may not cost the legitimate spellings, and a bracketed IPv6 literal carries
        // colons of its own.
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { $"--host={host}" });

        Assert.That(options.Host, Is.EqualTo(host));
    }

    [TestCase("0")]
    [TestCase("65536")]
    [TestCase("-1")]
    public void A_port_outside_the_valid_range_is_refused(string port)
    {
        var options = new RabbitMqOptionSet();

        Assert.That(() => options.Parse(new[] { $"--port={port}" }),
            Throws.TypeOf<OptionException>().With.Message.Contains("65535"));
    }

    [Test]
    public void A_given_port_survives_an_earlier_ssl_switch()
    {
        // The order this file claimed to cover and did not: --ssl first, --port second. Only the other
        // order was ever executed, so the comment was false.
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--ssl=true", "--port=32774" });

        Assert.That(options.Port, Is.EqualTo(32774));
    }

    [Test]
    public void The_tls_server_name_follows_the_host_that_was_given()
    {
        // It was captured in the constructor, where the host is still the default, so it stayed
        // localhost whatever broker was addressed and a certificate would have been checked against
        // the wrong name.
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--host=rabbit.example.invalid", "--ssl=true" });

        Assert.That(options.SslServerName, Is.EqualTo("rabbit.example.invalid"));
    }

    [Test]
    public void An_explicit_tls_server_name_wins_over_the_host()
    {
        // A host given as an address has no name to present, so the name can be stated deliberately.
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--host=127.0.0.1", "--ssl=true", "--ssl-server-name=rabbit.example.invalid" });

        Assert.Multiple(() =>
        {
            Assert.That(options.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(options.SslServerName, Is.EqualTo("rabbit.example.invalid"));
        });
    }

    [Test]
    public void A_given_port_survives_a_later_ssl_switch()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--port=32774", "--ssl=true" });

        Assert.That(options.Port, Is.EqualTo(32774));
    }

    [Test]
    public void Ssl_still_moves_the_default_port_when_none_was_given()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--ssl=true" });

        Assert.That(options.Port, Is.EqualTo(5671));
    }

    // The settings the bus is actually built from are BatchSettings, not the two input properties.
    // They were materialized once in the constructor, so --batch-limit and --batch-timeout wrote to
    // properties nobody read again and the run published with 100 and one millisecond while reporting
    // the numbers it had been given. Asserting the inputs would have stayed green through all of it,
    // which is exactly what my previous version did.
    [Test]
    public void The_parsed_batch_limit_reaches_the_effective_settings()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--batch-limit=7" });

        Assert.That(options.BatchSettings.MessageLimit, Is.EqualTo(7));
    }

    [Test]
    public void The_parsed_batch_timeout_reaches_the_effective_settings()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--batch-timeout=23" });

        Assert.That(options.BatchSettings.Timeout, Is.EqualTo(TimeSpan.FromMilliseconds(23)));
    }

    [TestCase("--batch=true", "--batch-limit=7", "--batch-timeout=23")]
    [TestCase("--batch-limit=7", "--batch-timeout=23", "--batch=true")]
    [TestCase("--batch-limit=7", "--batch=true", "--batch-timeout=23")]
    public void Every_batch_option_order_ends_in_one_effective_configuration(string first, string second, string third)
    {
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { first, second, third });

        var settings = options.BatchSettings;

        Assert.Multiple(() =>
        {
            Assert.That(settings.Enabled, Is.True);
            Assert.That(settings.MessageLimit, Is.EqualTo(7));
            Assert.That(settings.Timeout, Is.EqualTo(TimeSpan.FromMilliseconds(23)));
        });
    }

    [Test]
    public void Switching_batching_off_reaches_the_effective_settings()
    {
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--batch=false" });

        Assert.That(options.BatchSettings.Enabled, Is.False);
    }

    [Test]
    public void A_bracketed_ipv6_host_reaches_the_address_with_its_port()
    {
        // Accepting the spelling is not the same as reaching the broker: the address is what the bus
        // dials, and my earlier case only asserted the parsed string.
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--host=[::1]", "--port=32774" });

        Assert.Multiple(() =>
        {
            Assert.That(options.HostAddress.Host, Is.EqualTo("[::1]"));
            Assert.That(options.HostAddress.Port, Is.EqualTo(32774));
        });
    }

    [Test]
    public void The_reported_options_are_the_effective_ones()
    {
        // ShowOptions is the only account anybody gets of a run, so it is worth an assertion of its
        // own rather than trusting that it reads the same fields the bus does.
        var options = new RabbitMqOptionSet();
        options.Parse(new[]
        {
            "--host=rabbit.example.invalid", "--ssl=true", "--port=32774", "--sni=front.example.invalid",
            "--username=benchmark", "--password=" + Secret, "--batch-limit=7", "--batch-timeout=23"
        });

        var printed = Capture(options.ShowOptions);

        Assert.Multiple(() =>
        {
            Assert.That(printed, Does.Contain("Port: 32774"));
            Assert.That(printed, Does.Contain("TLS: enabled=True"));
            Assert.That(printed, Does.Contain("protocol=None"));
            Assert.That(printed, Does.Contain("server name=front.example.invalid"));
            Assert.That(printed, Does.Contain("limit=7"));
            Assert.That(printed, Does.Contain("Password configured: True"));
            Assert.That(printed, Does.Not.Contain(Secret), "the secret itself never belongs in the output");
        });
    }

    [Test]
    public void The_reported_options_say_when_no_password_was_given()
    {
        var options = new RabbitMqOptionSet();
        options.Parse(new[] { "--host=rabbit.example.invalid" });

        var printed = Capture(options.ShowOptions);

        Assert.That(printed, Does.Contain("Password configured: False"));
    }

    const string Secret = "not-a-real-secret-0d6f2a";

    static string Capture(Action write)
    {
        var original = Console.Out;
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
