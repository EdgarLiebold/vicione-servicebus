using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.Tests.MessageData;

internal static class MessageDataTestSupport
{
    public static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;
}
