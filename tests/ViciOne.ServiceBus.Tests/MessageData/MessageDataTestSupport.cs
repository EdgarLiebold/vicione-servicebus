using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;

namespace ViciOne.ServiceBus.Tests.MessageData;

internal static class MessageDataTestSupport
{
    public static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public static T Require<T>(T? value, string memberName)
        where T : class =>
        value ?? throw new Xunit.Sdk.XunitException($"Expected message-data member '{memberName}' to be non-null.");
}
