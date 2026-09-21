using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Npgsql;
using ViciOne.ServiceBus.SqlTransport.Configuration;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Defines PostgreSQL connection and maintenance settings for a SQL transport host.</summary>
internal sealed class PostgreSqlHostSettings :
    ConfigurationSqlHostSettings
{
    readonly NpgsqlDataSource? _dataSource;
    NpgsqlConnectionStringBuilder? _builder;
    bool _inlinePortSpecified;

    /// <summary>Initializes the settings from a PostgreSQL host address.</summary>
    /// <param name="hostAddress">The PostgreSQL host address.</param>
    public PostgreSqlHostSettings(Uri hostAddress)
        : base(hostAddress)
    {
    }

    /// <summary>Initializes the settings from a PostgreSQL connection string.</summary>
    /// <param name="connectionString">The PostgreSQL connection string.</param>
    public PostgreSqlHostSettings(string connectionString)
    {
        ConnectionString = connectionString;
    }

    /// <summary>Initializes the settings with a caller-owned PostgreSQL data source.</summary>
    /// <param name="dataSource">The preconfigured data source used to open connections.</param>
    public PostgreSqlHostSettings(NpgsqlDataSource dataSource)
    {
        if (dataSource == null)
            throw new ArgumentNullException(nameof(dataSource));

        _dataSource = dataSource;

        IsProvidedDataSource = true;

        ConnectionString = dataSource.ConnectionString;
    }

    /// <summary>Initializes the settings from SQL transport options.</summary>
    /// <param name="options">The SQL transport options.</param>
    public PostgreSqlHostSettings(SqlTransportOptions options)
    {
        var builder = PostgreSqlTransportConnection.CreateBuilder(options);

        ParseHost(builder.Host);
        if (!_inlinePortSpecified && !Port.HasValue && builder.Port > 0 && builder.Port != NpgsqlConnection.DefaultPort)
            Port = options.Port;

        Database = builder.Database;
        Schema = options.Schema;

        Username = builder.Username;
        Password = builder.Password;

        _builder = builder;

        if (options.ConnectionLimit.HasValue)
            ConnectionLimit = options.ConnectionLimit.Value;

        MaintenanceEnabled = !options.DisableMaintenance;
    }

    /// <summary>Gets or sets the comma-separated PostgreSQL host list, including any per-host ports.</summary>
    public string? MultipleHosts { get; set; }

    /// <inheritdoc />
    public override string? Host
    {
        get => base.Host;
        set
        {
            base.Host = value;
            MultipleHosts = null;
            _inlinePortSpecified = false;
        }
    }

    /// <summary>Gets whether the data source was supplied by the caller and therefore is not owned by the transport.</summary>
    public bool IsProvidedDataSource { get; private set; }

    /// <summary>Sets the PostgreSQL connection string and updates the corresponding host settings.</summary>
    public string? ConnectionString
    {
        set
        {
            var builder = new NpgsqlConnectionStringBuilder(value);

            ParseHost(builder.Host);
            if (!_inlinePortSpecified && !Port.HasValue && builder.Port > 0 && builder.Port != NpgsqlConnection.DefaultPort)
                Port = builder.Port;

            Database = builder.Database;

            Username = builder.Username;
            Password = builder.Password;

            Schema = builder.SearchPath ?? "transport";

            _builder = builder;
        }
    }

    /// <summary>Gets the supplied data source or creates one from the current connection settings.</summary>
    /// <returns>The PostgreSQL data source used to open transport connections.</returns>
    public NpgsqlDataSource GetDataSource()
    {
        if (_dataSource != null)
            return _dataSource;

        var builder = _builder is null
            ? new NpgsqlConnectionStringBuilder()
            : new NpgsqlConnectionStringBuilder(_builder.ConnectionString);

        builder.Host = GetConfiguredHost();
        builder.Username = Username;
        builder.Password = Password;
        builder.Database = Database;
        builder.SearchPath = Schema;

        if (Port.HasValue)
            builder.Port = Port.Value;
        else
            builder.Remove("Port");

        return NpgsqlDataSource.Create(builder);
    }

    /// <summary>Creates a PostgreSQL connection-context factory for the specified host configuration.</summary>
    /// <param name="hostConfiguration">The host configuration used by new connection contexts.</param>
    /// <returns>The PostgreSQL connection-context factory.</returns>
    public override ConnectionContextFactory CreateConnectionContextFactory(ISqlHostConfiguration hostConfiguration)
    {
        return new PostgreSqlConnectionContextFactory(hostConfiguration);
    }

    void ParseHost(string? host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            base.Host = host;
            MultipleHosts = null;
            _inlinePortSpecified = false;
            Port = null;
            return;
        }

        string[] hostSegments = host.Split(',', StringSplitOptions.TrimEntries);
        if (hostSegments.Any(string.IsNullOrWhiteSpace))
            throw InvalidHost(host);

        (string firstHost, int? firstPort) = ParseHostSegment(hostSegments[0], host);
        for (var index = 1; index < hostSegments.Length; index++)
            _ = ParseHostSegment(hostSegments[index], host);

        bool inlinePortSpecified = hostSegments.Length == 1 && firstPort.HasValue;

        base.Host = firstHost;
        MultipleHosts = hostSegments.Length > 1 ? host.Trim() : null;
        _inlinePortSpecified = inlinePortSpecified;
        Port = inlinePortSpecified && firstPort != NpgsqlConnection.DefaultPort ? firstPort : null;
    }

    string? GetConfiguredHost()
    {
        return MultipleHosts ?? Host;
    }

    static (string Host, int? Port) ParseHostSegment(string segment, string originalHost)
    {
        if (segment[0] is '/' or '@')
            throw InvalidHost(originalHost);

        if (segment[0] == '[')
            return ParseBracketedIpv6Host(segment, originalHost);

        int firstColon = segment.IndexOf(':');
        if (firstColon < 0)
            return (segment, null);

        return firstColon == segment.LastIndexOf(':')
            ? ParseHostAndPort(segment, firstColon, originalHost)
            : ParseUnbracketedIpv6Host(segment, originalHost);
    }

    static (string Host, int? Port) ParseBracketedIpv6Host(string segment, string originalHost)
    {
        int closingBracket = segment.IndexOf(']');
        if (closingBracket <= 1
            || !IPAddress.TryParse(segment[1..closingBracket], out IPAddress? address)
            || address.AddressFamily != AddressFamily.InterNetworkV6)
            throw InvalidHost(originalHost);

        string suffix = segment[(closingBracket + 1)..];
        if (suffix.Length == 0)
            return (address.ToString(), null);

        if (suffix[0] != ':' || !TryParsePort(suffix.AsSpan(1), out int port))
            throw InvalidHost(originalHost);

        return (address.ToString(), port);
    }

    static (string Host, int? Port) ParseHostAndPort(string segment, int separator, string originalHost)
    {
        if (separator == 0 || !TryParsePort(segment.AsSpan(separator + 1), out int port))
            throw InvalidHost(originalHost);

        return (segment[..separator], port);
    }

    static (string Host, int? Port) ParseUnbracketedIpv6Host(string segment, string originalHost)
    {
        if (!IPAddress.TryParse(segment, out IPAddress? ipv6Address)
            || ipv6Address.AddressFamily != AddressFamily.InterNetworkV6)
            throw InvalidHost(originalHost);

        return (ipv6Address.ToString(), null);
    }

    static bool TryParsePort(ReadOnlySpan<char> value, out int port)
    {
        return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out port)
            && port is > 0 and <= 65535;
    }

    static ArgumentException InvalidHost(string host)
    {
        return new ArgumentException(
            $"The PostgreSQL host list contains an invalid host or port: '{host}'.",
            nameof(host));
    }
}
