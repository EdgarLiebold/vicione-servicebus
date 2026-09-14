using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Monitoring;

public static class MessagingSystemNormalizerTestDriver
{
    public static string Normalize(string? value) =>
        LogContextMetricsExtensions.NormalizeSystem(value);
}
