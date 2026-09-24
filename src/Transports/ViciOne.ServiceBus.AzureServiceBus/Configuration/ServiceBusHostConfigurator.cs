using System;
using Azure;
using Azure.Core;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using ViciOne.ServiceBus.AzureServiceBus.Configuration;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds Azure Service Bus namespace connection and retry settings.</summary>
public class ServiceBusHostConfigurator :
    IServiceBusHostConfigurator
{
    readonly HostSettings _settings;

    /// <summary>Creates a host configuration for an Azure Service Bus namespace address.</summary>
    /// <param name="serviceAddress">The namespace address, optionally followed by an entity-path scope.</param>
    public ServiceBusHostConfigurator(Uri serviceAddress)
    {
        var hostAddress = new ServiceBusHostAddress(serviceAddress);

        _settings = new HostSettings { ServiceUri = hostAddress };
    }

    /// <summary>Creates a host configuration backed by caller-owned Azure SDK clients.</summary>
    /// <param name="serviceAddress">The namespace address, optionally followed by an entity-path scope.</param>
    /// <param name="serviceBusClient">The client used for message operations.</param>
    /// <param name="serviceBusAdministrationClient">The client used for namespace administration.</param>
    public ServiceBusHostConfigurator(Uri serviceAddress,
        ServiceBusClient serviceBusClient,
        ServiceBusAdministrationClient serviceBusAdministrationClient)
    {
        var hostAddress = new ServiceBusHostAddress(serviceAddress);

        _settings = new HostSettings
        {
            ServiceUri = hostAddress,
            ServiceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient)),
            ServiceBusAdministrationClient = serviceBusAdministrationClient ?? throw new ArgumentNullException(nameof(serviceBusAdministrationClient)),
        };
    }

    /// <summary>Creates a host configuration from an Azure Service Bus connection string.</summary>
    /// <param name="connectionString">The connection string containing at least an endpoint.</param>
    public ServiceBusHostConfigurator(string connectionString)
    {
        (Uri endpoint, bool hasCredentials) = ValidateConnectionString(connectionString);

        _settings = new HostSettings
        {
            ConnectionString = hasCredentials ? connectionString : null,
            ServiceUri = endpoint,
        };
    }

    /// <summary>Gets the accumulated namespace settings.</summary>
    public ServiceBusHostSettings Settings => _settings;

    /// <summary>Sets a credential-bearing connection string, or clears it when the string contains only an endpoint.</summary>
    public string ConnectionString
    {
        set
        {
            (Uri endpoint, bool hasCredentials) = ValidateConnectionString(value);
            if (!string.Equals(endpoint.Host, _settings.ServiceUri.Host, StringComparison.OrdinalIgnoreCase)
                || endpoint.Port != _settings.ServiceUri.Port)
                throw new ArgumentException("The connection string endpoint must match the configured Service Bus namespace and port", nameof(value));

            if (hasCredentials && (_settings.NamedKeyCredential != null
                || _settings.SasCredential != null
                || _settings.TokenCredential != null))
                throw new ArgumentException("Another type of authentication is already being used");

            _settings.ConnectionString = hasCredentials ? value : null;
        }
    }

    /// <summary>Sets the shared access key credential, provided no other authentication method is configured.</summary>
    public AzureNamedKeyCredential NamedKeyCredential
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_settings.ConnectionString != null
                || _settings.SasCredential != null
                || _settings.TokenCredential != null)
                throw new ArgumentException("Another type of authentication is already being used");

            _settings.NamedKeyCredential = value;
        }
    }

    /// <summary>Sets a precomputed shared access signature, provided no other authentication method is configured.</summary>
    public AzureSasCredential SasCredential
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_settings.ConnectionString != null
                || _settings.NamedKeyCredential != null
                || _settings.TokenCredential != null)
                throw new ArgumentException("Another type of authentication is already being used");

            _settings.SasCredential = value;
        }
    }

    /// <summary>Sets the Azure token credential, provided no other authentication method is configured.</summary>
    public TokenCredential TokenCredential
    {
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (_settings.ConnectionString != null
                || _settings.SasCredential != null
                || _settings.NamedKeyCredential != null)
                throw new ArgumentException("Another type of authentication is already being used");

            _settings.TokenCredential = value;
        }
    }

    /// <summary>Sets whether the Azure SDK uses AMQP over TCP or WebSockets.</summary>
    public ServiceBusTransportType TransportType
    {
        set => _settings.TransportType = value;
    }

    /// <summary>Sets the minimum delay used by the exponential retry policy.</summary>
    public TimeSpan RetryMinBackoff
    {
        set => _settings.RetryMinBackoff = value;
    }

    /// <summary>Sets the maximum delay used by the exponential retry policy.</summary>
    public TimeSpan RetryMaxBackoff
    {
        set => _settings.RetryMaxBackoff = value;
    }

    /// <summary>Sets the maximum number of retries for an Azure SDK operation.</summary>
    public int RetryLimit
    {
        set => _settings.RetryLimit = value;
    }

    static (Uri Endpoint, bool HasCredentials) ValidateConnectionString(string connectionString)
    {
        var properties = ServiceBusConnectionStringProperties.Parse(connectionString);
        if (!string.IsNullOrWhiteSpace(properties.EntityPath))
            throw new FormatException("An entity-bound connection string cannot configure a Service Bus host");

        bool hasCredentials = HasValidSharedAccessCredentials(properties);
        Uri endpoint = ParseEndpoint(connectionString) ?? throw new FormatException("Invalid connection string: missing endpoint");
        bool emulator = IsDevelopmentEmulator(connectionString);
        if (!endpoint.IsDefaultPort && !emulator)
            throw new FormatException("A custom-port connection string requires emulator mode");
        if (!hasCredentials && emulator)
            throw new FormatException("A credentialless emulator or custom-port connection string cannot configure a Service Bus host");

        return (endpoint, hasCredentials);
    }

    static bool HasValidSharedAccessCredentials(ServiceBusConnectionStringProperties properties)
    {
        bool hasKeyName = !string.IsNullOrWhiteSpace(properties.SharedAccessKeyName);
        bool hasKey = !string.IsNullOrWhiteSpace(properties.SharedAccessKey);
        bool hasSignature = !string.IsNullOrWhiteSpace(properties.SharedAccessSignature);

        if (hasKeyName != hasKey || hasSignature && hasKeyName)
            throw new FormatException("The connection string must contain a complete shared-access key pair or a signature, not both");

        return hasKeyName || hasSignature;
    }

    internal static bool IsDevelopmentEmulator(string connectionString)
    {
        bool emulator = false;
        foreach (string segment in connectionString.Split(';'))
        {
            int separator = segment.IndexOf('=');
            if (separator > 0 && string.Equals(segment.Substring(0, separator).Trim(), "UseDevelopmentEmulator",
                    StringComparison.OrdinalIgnoreCase))
            {
                if (bool.TryParse(segment.Substring(separator + 1).Trim(), out bool enabled))
                    emulator = enabled;
            }
        }

        return emulator;
    }

    /// <summary>Extracts and normalizes the namespace endpoint from an Azure connection string.</summary>
    /// <param name="connectionString">The Azure Service Bus connection string.</param>
    /// <returns>The normalized <c>sb</c> endpoint, or <see langword="null"/> when no endpoint is present or its URI is invalid.</returns>
    public static Uri? ParseEndpoint(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        if (connectionString.Length == 0)
            return null;

        var itemIndex = connectionString[0] == ';' ? 0 : 1;
        var startIndex = 0;
        var separatorIndex = 0;
        Uri? endpoint = null;
        bool endpointSeen = false;
        while (separatorIndex != -1)
        {
            separatorIndex = connectionString.IndexOf(';', startIndex + 1);
            var item = separatorIndex < 0 ? connectionString.Substring(startIndex) : connectionString.Substring(startIndex, separatorIndex - startIndex);
            var index = item.IndexOf('=');
            if (index >= 0)
            {
                var key = item.Substring(1 - itemIndex, index - 1 + itemIndex);
                var value = item.Substring(index + 1);
                if (string.IsNullOrWhiteSpace(key))
                    throw new FormatException("Invalid connection string");

                key = key.Trim();
                if (!string.IsNullOrEmpty(value) && (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[value.Length - 1])))
                    value = value.Trim();
                if (string.IsNullOrEmpty(value))
                    throw new FormatException("Invalid connection string");

                if (string.Compare("Endpoint", key, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    if (endpointSeen)
                        throw new FormatException("Invalid connection string: duplicate endpoint");

                    endpointSeen = true;
                    if ((!Uri.TryCreate(value, UriKind.Absolute, out var result) || string.IsNullOrEmpty(result.Host))
                        && !value.Contains("://", StringComparison.Ordinal))
                        Uri.TryCreate("sb://" + value, UriKind.Absolute, out result);

                    if (result != null && !string.IsNullOrEmpty(result.Host))
                    {
                        var builder = new UriBuilder
                        {
                            Scheme = "sb",
                            Host = result.Host,
                            Path = result.AbsolutePath,
                            Port = result.IsDefaultPort ? -1 : result.Port
                        };

                        if (Uri.CheckHostName(builder.Host) == UriHostNameType.Unknown)
                            throw new FormatException("Invalid connection string");

                        endpoint = builder.Uri;
                    }
                }
            }
            else if (item.Length != 1 || item[0] != ';')
                throw new FormatException("Invalid connection string");

            itemIndex = 0;
            startIndex = separatorIndex;
        }

        return endpoint;
    }
}
