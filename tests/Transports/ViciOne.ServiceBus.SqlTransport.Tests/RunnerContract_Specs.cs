namespace ViciOne.ServiceBus.DbTransport.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using ViciOne.ServiceBus.TestInfrastructure;


/// <summary>
/// The runner contract itself, asserted directly rather than only through the fixtures that read it.
/// <para>
/// Every fixture in this suite depends on the contract being fail closed. Until now that property was
/// only visible when a fixture happened to run without a published endpoint, which is exactly the
/// situation nobody arranges deliberately. These cases arrange it.
/// </para>
/// </summary>
[TestFixture]
public class The_runner_contract
{
    [Test]
    public void Should_describe_a_complete_endpoint()
    {
        var endpoint = new DatabaseEndpoint("PostgreSQL", "127.0.0.1", 32768, "vicione_ci", "a-secret");

        Assert.Multiple(() =>
        {
            Assert.That(endpoint.Engine, Is.EqualTo("PostgreSQL"));
            Assert.That(endpoint.Host, Is.EqualTo("127.0.0.1"));
            Assert.That(endpoint.Port, Is.EqualTo(32768));
            Assert.That(endpoint.Username, Is.EqualTo("vicione_ci"));
            Assert.That(endpoint.Password, Is.EqualTo("a-secret"));
        });
    }

    [Test]
    public void Should_refuse_an_incomplete_endpoint()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => new DatabaseEndpoint(null, "127.0.0.1", 1, "u", "p"), Throws.ArgumentNullException,
                "an endpoint without an engine was accepted");
            Assert.That(() => new DatabaseEndpoint("PostgreSQL", null, 1, "u", "p"), Throws.ArgumentNullException,
                "an endpoint without a host was accepted");
            Assert.That(() => new DatabaseEndpoint("PostgreSQL", "127.0.0.1", 1, null, "p"), Throws.ArgumentNullException,
                "an endpoint without an account was accepted");
            Assert.That(() => new DatabaseEndpoint("PostgreSQL", "127.0.0.1", 1, "u", null), Throws.ArgumentNullException,
                "an endpoint without a secret was accepted");
        });
    }

    [TestCase(0, Description = "no port at all")]
    [TestCase(-1, Description = "below the range")]
    [TestCase(65536, Description = "above the range")]
    public void Should_refuse_a_port_outside_the_range(int port)
    {
        Assert.That(() => new DatabaseEndpoint("PostgreSQL", "127.0.0.1", port, "u", "p"),
            Throws.TypeOf<ArgumentOutOfRangeException>(),
            $"port {port} was accepted, so a fixture could address something nobody published");
    }

    [Test]
    public void Should_not_disclose_the_secret_when_it_is_described()
    {
        var endpoint = new DatabaseEndpoint("SQL Server", "127.0.0.1", 32769, "sa", "S3cret-that-must-not-leak");

        var described = endpoint.ToString();

        Assert.Multiple(() =>
        {
            Assert.That(described, Does.Not.Contain("S3cret-that-must-not-leak"),
                "the endpoint printed its secret, so it would reach any log that prints an endpoint");
            Assert.That(described, Does.Contain("127.0.0.1"), "the description names no host, so it identifies nothing");
            Assert.That(described, Does.Contain("32769"), "the description names no port");
        });
    }

    [Test]
    public void Should_name_every_missing_variable_and_no_secret_when_the_contract_is_incomplete()
    {
        var kept = new Dictionary<string, string>();
        string[] variables =
        {
            TestRunnerContract.PostgresHostVariable, TestRunnerContract.PostgresPortVariable,
            TestRunnerContract.PostgresUsernameVariable, TestRunnerContract.PostgresPasswordVariable
        };

        foreach (var variable in variables)
        {
            kept[variable] = Environment.GetEnvironmentVariable(variable);
            Environment.SetEnvironmentVariable(variable, null);
        }

        try
        {
            // The contract caches its endpoints, so this reads the same code through a fresh instance
            // rather than the property, which a previous case in this process may already have resolved.
            var exception = Assert.Throws<TargetInvocationException>(() => Read("PostgreSQL", variables));

            Assert.That(exception.InnerException, Is.TypeOf<TestRunnerContractException>(),
                "an incomplete contract did not raise the contract exception");

            var message = exception.InnerException!.Message;

            Assert.Multiple(() =>
            {
                foreach (var variable in variables)
                {
                    Assert.That(message, Does.Contain(variable),
                        $"the message does not name {variable}, so the reader cannot tell what to publish");
                }

                Assert.That(message, Does.Contain("no connection is attempted"),
                    "the message does not say that nothing was attempted");
            });
        }
        finally
        {
            foreach ((var variable, var value) in kept)
                Environment.SetEnvironmentVariable(variable, value);
        }
    }

    [Test]
    public void Should_refuse_a_port_the_runner_published_as_nonsense()
    {
        var kept = Environment.GetEnvironmentVariable(TestRunnerContract.PostgresPortVariable);
        Environment.SetEnvironmentVariable(TestRunnerContract.PostgresPortVariable, "not-a-port");
        try
        {
            var exception = Assert.Throws<TargetInvocationException>(() => Read("PostgreSQL", new[]
            {
                TestRunnerContract.PostgresHostVariable, TestRunnerContract.PostgresPortVariable,
                TestRunnerContract.PostgresUsernameVariable, TestRunnerContract.PostgresPasswordVariable
            }));

            Assert.That(exception.InnerException!.Message, Does.Contain(TestRunnerContract.PostgresPortVariable),
                "a port that is not a number was accepted or was not named");
        }
        finally
        {
            Environment.SetEnvironmentVariable(TestRunnerContract.PostgresPortVariable, kept);
        }
    }

    [Test]
    public void Should_refuse_a_configuration_it_does_not_know()
    {
        Assert.Multiple(() =>
        {
            Assert.That(() => TransportInspection.DialectOf(null), Throws.ArgumentNullException,
                "a null configuration was resolved to a dialect");
            Assert.That(() => TransportInspection.DialectOf(new UnknownConfiguration()),
                Throws.TypeOf<ArgumentOutOfRangeException>(),
                "an unknown configuration was read as PostgreSQL, so every assertion would address the wrong tables");
        });
    }

    static void Read(string engine, string[] variables)
    {
        MethodInfo read = typeof(TestRunnerContract)
            .GetMethod("Read", BindingFlags.NonPublic | BindingFlags.Static)!;

        read.Invoke(null, new object[] { engine, variables[0], variables[1], variables[2], variables[3] });
    }


    class UnknownConfiguration :
        IDatabaseTestConfiguration
    {
        public ILockStatementProvider LockStatementProvider => throw new NotSupportedException();

        public IServiceCollection Create() => throw new NotSupportedException();

        public void Configure(IBusRegistrationConfigurator configurator,
            Action<IBusRegistrationContext, ISqlBusFactoryConfigurator> callback) => throw new NotSupportedException();

        public void Apply<TDbContext>(DbContextOptionsBuilder builder)
            where TDbContext : DbContext => throw new NotSupportedException();
    }
}
