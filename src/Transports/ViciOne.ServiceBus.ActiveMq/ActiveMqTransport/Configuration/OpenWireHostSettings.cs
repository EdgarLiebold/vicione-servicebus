using System;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an open wire host settings implementation.
/// </summary>
public class OpenWireHostSettings :
    ConfigurationHostSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public OpenWireHostSettings(Uri address)
        : base(address)
    {
        TransportOptions["wireFormat.tightEncodingEnabled"] = "true";
    }

    /// <summary>
    /// Gets the nms scheme value.
    /// </summary>
    public override string NmsScheme => "activemq";

    /// <summary>
    /// Gets the host scheme value.
    /// </summary>
    public override string HostScheme => UseSsl ? "ssl" : "tcp";

    /// <summary>
    /// Gets the failover scheme value.
    /// </summary>
    public override string FailoverScheme => $"{NmsScheme}:failover";

    /// <summary>
    /// Gets the scheme value.
    /// </summary>
    public override string Scheme => $"{NmsScheme}:{HostScheme}";

    /// <summary>
    /// Gets the failover connection setting prefix value.
    /// </summary>
    public override string FailoverConnectionSettingPrefix => "transport.";
}
