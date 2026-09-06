using System;
using Npgsql;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;
/// <summary>
/// Classifies PostgreSQL send failures from Npgsql's typed transient contract.
/// </summary>
public sealed class PostgresSendFailureClassifier : ITransportSendFailureClassifier
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
                case UnauthorizedAccessException:
                    failureKind = TransportSendFailureKind.Permanent;
                    return true;

                case NpgsqlException { IsTransient: false }:
                    failureKind = TransportSendFailureKind.Permanent;
                    return true;

                case NpgsqlException:
                    sawTransient = true;
                    break;
            }
        }

        failureKind = sawTransient ? TransportSendFailureKind.Transient : TransportSendFailureKind.Unclassified;
        return sawTransient;
    }
}
