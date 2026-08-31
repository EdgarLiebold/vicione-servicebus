using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using Xunit;

namespace ViciOne.ServiceBus.Tests.MessageData;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class MessageDataDefaultsCollection
{
    public const string Name = "MessageData global defaults";
}

internal sealed class MessageDataDefaultsScope : IDisposable
{
    private readonly bool _alwaysWriteToRepository = MessageDataDefaults.AlwaysWriteToRepository;
    private readonly TimeSpan? _extraTimeToLive = MessageDataDefaults.ExtraTimeToLive;
    private readonly int _threshold = MessageDataDefaults.Threshold;
    private readonly TimeSpan? _timeToLive = MessageDataDefaults.TimeToLive;

    public void Dispose()
    {
        MessageDataDefaults.AlwaysWriteToRepository = _alwaysWriteToRepository;
        MessageDataDefaults.ExtraTimeToLive = _extraTimeToLive;
        MessageDataDefaults.Threshold = _threshold;
        MessageDataDefaults.TimeToLive = _timeToLive;
    }
}

internal static class MessageDataTestSupport
{
    public static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;
}
