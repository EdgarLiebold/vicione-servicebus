namespace ViciOne.ServiceBus.Diagnostics;

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Testing;


/// <summary>
/// The broker of this run, read from the environment the canonical runner publishes.
/// <para>
/// There is deliberately no default host, port or account. A diagnostic that silently addressed
/// localhost would report about whatever broker happens to run on the machine, and its numbers would
/// look exactly like the ones from the pinned fixture.
/// </para>
/// </summary>
static class RunScopedBroker
{
    public static (string Host, int Port, string Username, string Password) Read()
    {
        var host = Value(RabbitMqTestHarness.HostVariable);
        var username = Value(RabbitMqTestHarness.UsernameVariable);
        var password = Value(RabbitMqTestHarness.PasswordVariable);

        if (!int.TryParse(Environment.GetEnvironmentVariable(RabbitMqTestHarness.PortVariable), out var port)
            || port <= 0 || port > 65535)
            throw new InvalidOperationException(Missing(RabbitMqTestHarness.PortVariable));

        return (host, port, username, password);
    }

    static string Value(string variable)
    {
        var value = Environment.GetEnvironmentVariable(variable);

        return string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException(Missing(variable)) : value;
    }

    /// <summary>
    /// The virtual host these scenarios run in, created the same way the test suite creates it. It is
    /// created here rather than assumed, because a scenario that is started outside NUnit has no set up
    /// fixture to do it, and the broker of a fresh run carries no virtual host but the default one.
    /// </summary>
    public static async Task CreateVirtualHost(string name, CancellationToken cancellationToken)
    {
        (var host, _, var username, var password) = Read();
        var managementPort = ManagementPort();

        using var client = new HttpClient();
        var credentials = Encoding.ASCII.GetBytes($"{username}:{password}");
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Basic", Convert.ToBase64String(credentials));

        var uri = new UriBuilder("http", host, managementPort, $"api/vhosts/{name}").Uri;

        using var response = await client.PutAsync(uri, new StringContent("{}", Encoding.UTF8, "application/json"),
            cancellationToken);

        // PUT is idempotent here: 201 on creation, 204 when the virtual host already exists.
        if (response.StatusCode != HttpStatusCode.Created && response.StatusCode != HttpStatusCode.NoContent)
        {
            throw new InvalidOperationException(
                $"creating the virtual host '{name}' failed with {(int)response.StatusCode} {response.ReasonPhrase} "
                + $"at {uri}, so this scenario has nowhere to run");
        }
    }

    static int ManagementPort()
    {
        return int.TryParse(Environment.GetEnvironmentVariable(RabbitMqTestHarness.ManagementPortVariable),
            out var port) && port > 0
            ? port
            : throw new InvalidOperationException(Missing(RabbitMqTestHarness.ManagementPortVariable));
    }

    static string Missing(string variable)
    {
        return $"{variable} is not set, so this scenario does not know which broker to measure. "
            + "Start the pinned fixture with tools/ci/run_broker_category.py --broker rabbitmq and run "
            + "this inside that environment. There is no default host, port or account to fall back to.";
    }
}
