using System;
using System.Collections.Generic;
using Apache.NMS;
using NDesk.Options;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.ActiveMq.Configuration;

#nullable enable
namespace ViciOneServiceBusBenchmark;

internal sealed class ActiveMqOptionSet :
    OptionSet,
    ActiveMqHostSettings
{
    string? _host;
    ConfigurationHostSettings? _hostSettings;
    string? _password;
    int? _port;
    ActiveMqTransportProtocol? _protocol;
    string? _username;
    bool _useSsl;

    public ActiveMqOptionSet()
    {
        Add<string>("h|host:", "The host name of the broker (required)", value => _host = value);
        Add<string>("protocol:", "The broker protocol: openwire or amqp (required)", SetProtocol);
        Add<int>("port:", "The broker port (required)", SetPort);
        Add<string>("u|username:", "Username (if using basic credentials)", value => _username = value);
        Add<string>("p|password:", "Password (if using basic credentials)", value => _password = value);
        Add<bool>("ssl:", "Use TLS for the selected protocol", value => _useSsl = value);
    }

    ConfigurationHostSettings HostSettings => _hostSettings
        ?? throw new OptionException("ActiveMQ options must be parsed before their effective settings are used.", "active-mq");

    public string Host => HostSettings.Host;

    public int Port => HostSettings.Port;

    public string VirtualHost => HostSettings.VirtualHost;

    public string Username => HostSettings.Username;

    public string Password => HostSettings.Password;

    public Uri HostAddress => HostSettings.HostAddress;

    public bool UseSsl => HostSettings.UseSsl;

    public Uri BrokerAddress => HostSettings.BrokerAddress;

    public ActiveMqTransportProtocol Protocol => _protocol
        ?? throw new OptionException("The ActiveMQ protocol was not configured.", "protocol");

    public Apache.NMS.IConnection CreateConnection()
    {
        return HostSettings.CreateConnection();
    }

    public new List<string> Parse(IEnumerable<string> arguments)
    {
        _host = null;
        _hostSettings = null;
        _password = null;
        _port = null;
        _protocol = null;
        _username = null;
        _useSsl = false;
        List<string> remaining = base.Parse(arguments);
        _hostSettings = CreateHostSettings();
        return remaining;
    }

    public override string ToString()
    {
        return HostAddress.ToString();
    }

    public void ShowOptions()
    {
        Console.WriteLine("Host: {0} ({1})", HostAddress, Protocol);
    }

    ConfigurationHostSettings CreateHostSettings()
    {
        if (string.IsNullOrWhiteSpace(_host))
            throw new OptionException("The ActiveMQ host is required.", "host");
        if (!_protocol.HasValue)
            throw new OptionException("The ActiveMQ protocol is required and must be openwire or amqp.", "protocol");
        if (!_port.HasValue)
            throw new OptionException("The ActiveMQ port is required.", "port");

        Uri address;
        try
        {
            address = new ActiveMqHostAddress(_protocol.Value, _host, _port.Value, "/");
        }
        catch (Exception exception) when (exception is ArgumentException or ActiveMqTransportConfigurationException)
        {
            throw new OptionException(exception.Message, "active-mq");
        }

        ConfigurationHostSettings settings = _protocol.Value switch
        {
            ActiveMqTransportProtocol.OpenWire => new OpenWireHostSettings(address),
            ActiveMqTransportProtocol.Amqp => new AmqpHostSettings(address),
            _ => throw new OptionException($"The ActiveMQ protocol is not supported: {_protocol.Value}.", "protocol")
        };

        settings.Username = _username ?? "";
        settings.Password = _password ?? "";
        settings.UseSsl = _useSsl;
        return settings;
    }

    void SetPort(int port)
    {
        if (port is < 1 or > 65535)
            throw new OptionException($"The ActiveMQ port {port} is outside 1..65535.", "port");

        _port = port;
    }

    void SetProtocol(string value)
    {
        ActiveMqTransportProtocol? protocol = value switch
        {
            var name when name.Equals("openwire", StringComparison.OrdinalIgnoreCase) => ActiveMqTransportProtocol.OpenWire,
            var name when name.Equals("amqp", StringComparison.OrdinalIgnoreCase) => ActiveMqTransportProtocol.Amqp,
            _ => null
        };

        if (!protocol.HasValue)
        {
            throw new OptionException(
                $"The ActiveMQ protocol '{value}' is not supported; use openwire or amqp.",
                "protocol");
        }

        _protocol = protocol.Value;
    }
}
