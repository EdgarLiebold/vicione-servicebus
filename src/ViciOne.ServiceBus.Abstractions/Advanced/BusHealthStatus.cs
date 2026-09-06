namespace ViciOne.ServiceBus.Advanced;

/// <summary>Specifies the severity of a bus or receive-endpoint health observation.</summary>
public enum BusHealthStatus
{
    /// <summary>The bus or endpoint cannot provide its configured service.</summary>
    Unhealthy = 0,

    /// <summary>The bus or endpoint remains available with reduced health.</summary>
    Degraded = 1,

    /// <summary>The bus or endpoint is fully available.</summary>
    Healthy = 2
}
