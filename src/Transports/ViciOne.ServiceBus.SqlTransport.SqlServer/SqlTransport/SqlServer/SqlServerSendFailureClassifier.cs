#nullable enable
namespace ViciOne.ServiceBus.SqlTransport.SqlServer;

using System;
using Microsoft.Data.SqlClient;

/// <summary>
/// Classifies SQL Server send failures from the provider's numeric error codes.
/// </summary>
public sealed class SqlServerSendFailureClassifier : ITransportSendFailureClassifier
{
    /// <inheritdoc />
    public bool TryClassify(Exception exception, out TransportSendFailureKind failureKind)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var sawTransient = false;
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqlException sqlException)
            {
                TransportSendFailureKind classified = ClassifyErrorNumber(sqlException.Number);
                if (classified == TransportSendFailureKind.Permanent)
                {
                    failureKind = classified;
                    return true;
                }

                sawTransient = true;
            }
        }

        failureKind = sawTransient ? TransportSendFailureKind.Transient : TransportSendFailureKind.Unclassified;
        return sawTransient;
    }

    internal static TransportSendFailureKind ClassifyErrorNumber(int errorNumber) =>
        SqlServerDbConnectionContext.IsTransientErrorNumber(errorNumber)
            ? TransportSendFailureKind.Transient
            : TransportSendFailureKind.Permanent;
}
