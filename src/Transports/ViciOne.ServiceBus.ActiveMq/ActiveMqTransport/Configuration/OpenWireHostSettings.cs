using System;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Builds Apache NMS ActiveMQ OpenWire broker and failover addresses.</summary>
public class OpenWireHostSettings :
    ConfigurationHostSettings
{
    /// <summary>Creates OpenWire settings with tight wire-format encoding enabled.</summary>
    /// <param name="address">The validated broker address.</param>
    public OpenWireHostSettings(Uri address)
        : base(address)
    {
        TransportOptions["wireFormat.tightEncodingEnabled"] = "true";
    }

    /// <summary>Gets the base Apache NMS provider scheme.</summary>
    public override string NmsScheme => "activemq";

    /// <summary>Gets the provider host scheme for plain TCP or TLS.</summary>
    public override string HostScheme => UseSsl ? "ssl" : "tcp";

    /// <summary>Gets the OpenWire failover URI scheme.</summary>
    public override string FailoverScheme => $"{NmsScheme}:failover";

    /// <summary>Gets the OpenWire primary-broker URI scheme.</summary>
    public override string Scheme => $"{NmsScheme}:{HostScheme}";

    /// <summary>Gets the OpenWire prefix for connection-wide failover options.</summary>
    public override string FailoverConnectionSettingPrefix => "transport.";
}
