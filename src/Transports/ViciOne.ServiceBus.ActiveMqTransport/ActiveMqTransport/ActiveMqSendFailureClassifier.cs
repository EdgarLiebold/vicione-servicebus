using System;
using Apache.NMS;

#nullable enable
namespace ViciOne.ServiceBus.ActiveMqTransport;
/// <summary>
/// Classifies ActiveMQ send failures from typed NMS and transport data without inspecting exception text.
/// </summary>
public sealed class ActiveMqSendFailureClassifier : ITransportSendFailureClassifier
{
    /// <inheritdoc />
    public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var sawTransient = false;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            switch (current)
            {
                case ActiveMqTransportConfigurationException:
                case UnauthorizedAccessException:
                    failureKind = TransportSendFailureKind.Permanent;
                    return true;

                case ActiveMqConnectionException { IsTransient: false }:
                    failureKind = TransportSendFailureKind.Permanent;
                    return true;

                case ActiveMqConnectionException:
                case NMSConnectionException:
                    sawTransient = true;
                    break;
            }
        }

        failureKind = sawTransient ? TransportSendFailureKind.Transient : TransportSendFailureKind.Unclassified;
        return sawTransient;
    }
}
