namespace ViciOneServiceBusBenchmark;

using System;
using NDesk.Options;


public class SqlOptionSet :
    OptionSet
{
    const string DefaultAdminPasswordEnvironmentVariable = "VICIONE_BENCHMARK_POSTGRES_ADMIN_PASSWORD";

    public SqlOptionSet()
    {
        Add<string>("h|host:", "The PostgreSQL host name", x => Host = x);
        Add<string>("u|username:", "Username (if using basic credentials)", value => Username = value);
        Add<string>("admin-password-env:", "Environment variable containing the PostgreSQL administrator password",
            value => AdminPasswordEnvironmentVariable = value);
        Add<string>("db|database:", "Database to use", value => Database = value);
        Add<string>("role:", "Database role to use", value => Role = value);
        Add<string>("schema:", "Schema to use", value => Schema = value);

        Host = "localhost";
        Username = "postgres";
        AdminPasswordEnvironmentVariable = DefaultAdminPasswordEnvironmentVariable;
        Database = "benchmark";
        Role = "transport";
        Schema = "transport";
    }

    public string Host { get; set; }
    public string Username { get; set; }
    public string AdminPasswordEnvironmentVariable { get; private set; }
    public string Role { get; set; }
    public string Database { get; set; }
    public string Schema { get; set; }

    public void ShowOptions()
    {
        Console.WriteLine("Host: {0}", Host);
        Console.WriteLine("Username: {0}", Username);
        Console.WriteLine("Administrator password source: environment variable {0}", AdminPasswordEnvironmentVariable);
        Console.WriteLine("Database: {0}", Database);
        Console.WriteLine("Role: {0}", Role);
        Console.WriteLine("Schema: {0}", Schema);
    }

    public string ResolveAdminPassword()
    {
        if (string.IsNullOrWhiteSpace(AdminPasswordEnvironmentVariable))
            throw new OptionException("The PostgreSQL administrator password environment variable name must not be empty.",
                "admin-password-env");

        return Environment.GetEnvironmentVariable(AdminPasswordEnvironmentVariable)
            ?? throw new OptionException(
                $"Environment variable '{AdminPasswordEnvironmentVariable}' must contain the PostgreSQL administrator password.",
                "admin-password-env");
    }
}
