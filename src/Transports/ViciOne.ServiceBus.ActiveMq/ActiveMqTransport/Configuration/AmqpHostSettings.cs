using System;

namespace ViciOne.ServiceBus.ActiveMq.Configuration;

/// <summary>Builds Apache NMS AMQP broker and failover addresses.</summary>
public class AmqpHostSettings :
    ConfigurationHostSettings
{
    /// <summary>Creates AMQP settings from a validated ActiveMQ host address.</summary>
    /// <param name="address">The broker address.</param>
    public AmqpHostSettings(Uri address)
        : base(address)
    {
    }

    /// <summary>Gets the provider scheme, including the TLS suffix when enabled.</summary>
    public override string HostScheme => UseSsl ? $"{NmsScheme}s" : NmsScheme;

    /// <summary>Gets the Apache NMS failover URI scheme.</summary>
    public override string FailoverScheme => "failover";

    /// <summary>Gets the broker URI scheme.</summary>
    public override string Scheme => HostScheme;

    /// <summary>Gets the base Apache NMS provider scheme.</summary>
    public override string NmsScheme => "amqp";

    /// <summary>Gets the option prefix used for failover connection settings.</summary>
    public override string FailoverConnectionSettingPrefix => "failover.";
}
