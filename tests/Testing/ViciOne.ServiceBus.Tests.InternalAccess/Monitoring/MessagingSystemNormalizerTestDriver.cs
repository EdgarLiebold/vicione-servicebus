using ViciOne.ServiceBus.Logging.Monitoring;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Monitoring;

public static class MessagingSystemNormalizerTestDriver
{
    public static string Normalize(string? value) =>
        LogContextInstrumentationExtensions.NormalizeSystem(value);
}
