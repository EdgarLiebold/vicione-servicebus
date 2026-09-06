using System;
using System.Net;
using System.Text;
using Microsoft.Data.SqlClient;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

/// <summary>Defines SQL Server connection and maintenance settings for a SQL transport host.</summary>
public class SqlServerSqlHostSettings :
    ConfigurationSqlHostSettings
{
    SqlConnectionStringBuilder? _builder;

    /// <summary>Initializes the settings from a SQL Server host address.</summary>
    /// <param name="hostAddress">The SQL Server host address.</param>
    public SqlServerSqlHostSettings(Uri hostAddress)
        : base(hostAddress)
    {
        var address = new SqlHostAddress(hostAddress);

        Host = address.Host;
        InstanceName = address.InstanceName;
    }

    /// <summary>Initializes the settings from a SQL Server connection string.</summary>
    /// <param name="connectionString">The SQL Server connection string.</param>
    public SqlServerSqlHostSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>Initializes the settings from SQL transport options.</summary>
    /// <param name="options">The SQL transport options.</param>
    public SqlServerSqlHostSettings(SqlTransportOptions options)
    {
        var builder = SqlServerSqlTransportConnection.CreateBuilder(options);

        ParseDataSource(builder.DataSource);

        Database = builder.InitialCatalog;
        Schema = options.Schema;

        Username = builder.UserID;
        Password = builder.Password;

        _builder = builder;

        if (options.ConnectionLimit.HasValue)
            ConnectionLimit = options.ConnectionLimit.Value;

        MaintenanceEnabled = !options.DisableMaintenance;
    }

    /// <summary>Sets the SQL Server connection string and updates the corresponding host settings.</summary>
    public string? ConnectionString
    {
        set
        {
            var builder = new SqlConnectionStringBuilder(value);

            ParseDataSource(builder.DataSource);

            Username = builder.UserID;
            Password = builder.Password;

            Database = builder.InitialCatalog;

            _builder = builder;
        }
    }

    /// <summary>Creates a SQL Server connection-context factory for the specified host configuration.</summary>
    /// <param name="hostConfiguration">The host configuration used by new connection contexts.</param>
    /// <returns>The SQL Server connection-context factory.</returns>
    public override ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration hostConfiguration)
    {
        return new SqlServerConnectionContextFactory(hostConfiguration);
    }

    /// <summary>Gets a connection string assembled from the current SQL Server host settings.</summary>
    /// <returns>The SQL Server connection string.</returns>
    public string GetConnectionString()
    {
        var builder = _builder ??= new SqlConnectionStringBuilder
        {
            DataSource = FormatDataSource(),
            UserID = Username,
            Password = Password,
            InitialCatalog = Database,
            TrustServerCertificate = true
        };

        return builder.ToString();
    }

    void ParseDataSource(string? source)
    {
        var split = source?.Split(',');
        if (split?.Length == 2)
        {
            ParseHost(split[0].Trim());
            if (int.TryParse(split[1].Trim(), out var port))
                Port = port;
        }
        else
            ParseHost(source);
    }

    void ParseHost(string? host)
    {
        var hostSegments = host?.Split('\\');
        if (hostSegments?.Length == 2)
        {
            Host = TrimHost(hostSegments[0]);
            InstanceName = hostSegments[1].Trim();
        }
        else
            Host = TrimHost(host);
    }

    string? TrimHost(string? host)
    {
        if (host == null)
            return null;

        if (IPAddress.TryParse(host, out var endpoint))
            return endpoint.ToString();

        var hostSplit = host.Trim().Split(':');
        return hostSplit.Length == 1 ? hostSplit[0] : hostSplit[1];
    }

    string? FormatDataSource()
    {
        if (string.IsNullOrWhiteSpace(Host))
            return null;

        var sb = new StringBuilder();
        sb.Append(Host);
        if (!string.IsNullOrWhiteSpace(InstanceName))
            sb.Append('\\').Append(InstanceName);

        if (Port.HasValue)
            sb.Append(',').Append(Port.Value);

        return sb.ToString();
    }
}
