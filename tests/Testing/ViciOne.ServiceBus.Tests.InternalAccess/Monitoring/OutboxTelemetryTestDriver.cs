using ViciOne.ServiceBus.Logging;
using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Monitoring;

public static class OutboxTelemetryTestDriver
{
    public static void RecordDelivery(ILogContext logContext, Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(logContext);

        var instrument = logContext.TryStartOutboxDeliveryMetrics();
        if (exception is not null)
            instrument?.RecordException(exception);
        instrument?.Complete();
    }

    public static void RecordDeliveryTwice(
        ILogContext logContext,
        Exception firstException,
        Exception secondException)
    {
        ArgumentNullException.ThrowIfNull(logContext);
        ArgumentNullException.ThrowIfNull(firstException);
        ArgumentNullException.ThrowIfNull(secondException);

        var instrument = logContext.TryStartOutboxDeliveryMetrics();
        instrument?.RecordException(firstException);
        instrument?.RecordException(secondException);
        instrument?.Complete();
        instrument?.Complete();
    }
}
