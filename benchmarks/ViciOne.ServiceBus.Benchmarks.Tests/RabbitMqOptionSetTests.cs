namespace ViciOne.ServiceBus.Benchmarks.Tests;

using System.Net.Security;
using System.Security.Authentication;
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
    public void A_port_inside_the_host_is_refused_rather_than_silently_wrong()
    {
        // The spelling a caller reaches for when there is no port option. UriBuilder cannot parse it,
        // and the run then died with "the hostname could not be parsed", far from its cause.
        var options = new RabbitMqOptionSet();

        options.Parse(new[] { "--host=127.0.0.1:32774" });

        Assert.That(() => options.HostAddress, Throws.Exception);
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
}
