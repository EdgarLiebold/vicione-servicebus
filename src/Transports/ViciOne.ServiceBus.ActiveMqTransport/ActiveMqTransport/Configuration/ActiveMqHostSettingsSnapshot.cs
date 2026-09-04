using System;
using Apache.NMS;

namespace ViciOne.ServiceBus.ActiveMqTransport.Configuration;

sealed class ActiveMqHostSettingsSnapshot : ActiveMqHostSettings
{
    readonly Uri _brokerAddress;
    readonly Uri _hostAddress;

    public ActiveMqHostSettingsSnapshot(ActiveMqHostSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Host = settings.Host;
        Port = settings.Port;
        VirtualHost = settings.VirtualHost;
        Username = settings.Username;
        Password = settings.Password;
        UseSsl = settings.UseSsl;
        _hostAddress = new Uri(settings.HostAddress.OriginalString, UriKind.Absolute);
        _brokerAddress = new Uri(settings.BrokerAddress.OriginalString, UriKind.Absolute);
    }

    public string Host { get; }
    public int Port { get; }
    public string VirtualHost { get; }
    public string Username { get; }
    public string Password { get; }
    public bool UseSsl { get; }
    public Uri HostAddress => _hostAddress;
    public Uri BrokerAddress => _brokerAddress;

    public IConnection CreateConnection()
    {
        var factory = new NMSConnectionFactory(BrokerAddress);
        return string.IsNullOrEmpty(Username) && string.IsNullOrEmpty(Password)
            ? factory.ConnectionFactory.CreateConnection()
            : factory.ConnectionFactory.CreateConnection(Username, Password);
    }
}
