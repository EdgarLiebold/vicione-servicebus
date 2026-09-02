namespace ViciOne.ServiceBus.Tests.InternalAccess.Monitoring;

using ViciOne.ServiceBus.Logging;


public static class MessagingSystemNormalizerTestDriver
{
    public static string Normalize(string? value) =>
        LogContextInstrumentationExtensions.NormalizeSystem(value);
}
