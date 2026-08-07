// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.DbTransport.Tests.PgSql;

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SqlTransport.PostgreSql;


[TestFixture]
public class MultiHost_Specs
{
    [Test]
    public async Task Should_allow_multiple_host_names()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddOptions<SqlTransportOptions>().Configure(options =>
                {
                    options.Host = "local,remote";
                    options.Database = "ViciOneServiceBus_transport_tests";
                    options.Schema = "transport";
                    options.Role = "transport";
                    options.Username = "unit_tests";
                    options.Password = "H4rd2Gu3ss!";
                    options.AdminUsername = "sa";
                    options.AdminPassword = "Password12!";
                });

                x.UsingPostgres();
            })
            .BuildServiceProvider(true);

        var connection = PostgresSqlTransportConnection.GetDatabaseConnection(provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value);

        Assert.That(connection.Connection.ConnectionString, Does.Contain("local,remote;"));

        var bus = provider.GetRequiredService<IBus>();
        Assert.That(bus.Address.Host, Is.EqualTo("local"));
    }

    [Test]
    public async Task Should_allow_multiple_host_names_with_custom_port()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddOptions<SqlTransportOptions>().Configure(options =>
                {
                    options.Host = "local,remote";
                    options.Port = 1234;
                    options.Database = "ViciOneServiceBus_transport_tests";
                    options.Schema = "transport";
                    options.Role = "transport";
                    options.Username = "unit_tests";
                    options.Password = "H4rd2Gu3ss!";
                    options.AdminUsername = "sa";
                    options.AdminPassword = "Password12!";
                });

                x.UsingPostgres();
            })
            .BuildServiceProvider(true);

        var connection = PostgresSqlTransportConnection.GetDatabaseConnection(provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value);

        Assert.That(connection.Connection.ConnectionString, Does.Contain("local,remote;"));
        Assert.That(connection.Connection.ConnectionString, Does.Contain("Port=1234"));

        var bus = provider.GetRequiredService<IBus>();
        Assert.That(bus.Address.Host, Is.EqualTo("local"));
        Assert.That(bus.Address.Port, Is.EqualTo(1234));
    }

    [Test]
    public async Task Should_allow_multiple_host_names_with_custom_ports()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddOptions<SqlTransportOptions>().Configure(options =>
                {
                    options.Host = "local:1234,remote:5678";
                    options.Database = "ViciOneServiceBus_transport_tests";
                    options.Schema = "transport";
                    options.Role = "transport";
                    options.Username = "unit_tests";
                    options.Password = "H4rd2Gu3ss!";
                    options.AdminUsername = "sa";
                    options.AdminPassword = "Password12!";
                });

                x.UsingPostgres();
            })
            .BuildServiceProvider(true);

        var transportOptions = provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value;

        var connection = PostgresSqlTransportConnection.GetDatabaseConnection(transportOptions);

        Assert.That(connection.Connection.ConnectionString, Does.Contain("local:1234,remote:5678;"));

        var bus = provider.GetRequiredService<IBus>();
        Assert.That(bus.Address.Host, Is.EqualTo("local"));
    }

    [Test]
    public async Task Should_allow_single_host_names_with_custom_port()
    {
        await using var provider = new ServiceCollection()
            .AddViciOneServiceBusTestHarness(x =>
            {
                x.AddOptions<SqlTransportOptions>().Configure(options =>
                {
                    options.Host = "local:1234";
                    options.Database = "ViciOneServiceBus_transport_tests";
                    options.Schema = "transport";
                    options.Role = "transport";
                    options.Username = "unit_tests";
                    options.Password = "H4rd2Gu3ss!";
                    options.AdminUsername = "sa";
                    options.AdminPassword = "Password12!";
                });

                x.UsingPostgres();
            })
            .BuildServiceProvider(true);

        var transportOptions = provider.GetRequiredService<IOptions<SqlTransportOptions>>().Value;

        var connection = PostgresSqlTransportConnection.GetDatabaseConnection(transportOptions);

        Assert.That(connection.Connection.ConnectionString, Does.Contain("local:1234;"));

        var bus = provider.GetRequiredService<IBus>();
        Assert.That(bus.Address.Host, Is.EqualTo("local"));
        Assert.That(bus.Address.Port, Is.EqualTo(1234));
    }
}
