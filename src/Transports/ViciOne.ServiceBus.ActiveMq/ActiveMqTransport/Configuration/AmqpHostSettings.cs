using System;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>
/// Provides an amqp host settings implementation.
/// </summary>
public class AmqpHostSettings :
    ConfigurationHostSettings
{
    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="address">The address value.</param>
    public AmqpHostSettings(Uri address)
        : base(address)
    {
    }

    /// <summary>
    /// Gets the host scheme value.
    /// </summary>
    public override string HostScheme => UseSsl ? $"{NmsScheme}s" : NmsScheme;

    /// <summary>
    /// Gets the failover scheme value.
    /// </summary>
    public override string FailoverScheme => "failover";

    /// <summary>
    /// Gets the scheme value.
    /// </summary>
    public override string Scheme => HostScheme;

    /// <summary>
    /// Gets the nms scheme value.
    /// </summary>
    public override string NmsScheme => "amqp";

    /// <summary>
    /// Gets the failover connection setting prefix value.
    /// </summary>
    public override string FailoverConnectionSettingPrefix => "failover.";
}
