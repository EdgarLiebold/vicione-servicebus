using System.Collections.Generic;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Exposes transport-specific metadata required for diagnostics and scheduled-message replay.</summary>
public interface TransportReceiveContext
{
    /// <summary>Gets the OpenTelemetry messaging-system identifier, or an empty value when the transport has no explicit identifier.</summary>
    string ActivitySystem => string.Empty;

    /// <summary>Gets transport properties that must be retained when the message is replayed.</summary>
    /// <returns>The retained transport properties, or <see langword="null" /> when none are required.</returns>
    IDictionary<string, object>? GetTransportProperties();
}
