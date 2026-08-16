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
}
