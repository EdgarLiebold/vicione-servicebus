using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Providers.Configuration;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>Configures SignalR scale-out behavior over ViciOne.ServiceBus.</summary>
public sealed class SignalRBackplaneOptions
{
    private static readonly TimeSpan FirstUnsupportedTimerDuration = TimeSpan.FromMilliseconds(uint.MaxValue);

    /// <summary>Gets or sets the maximum time allowed for an acknowledged group-membership operation on another server.</summary>
    public TimeSpan RemoteGroupOperationTimeout { get; set; } = TimeSpan.FromSeconds(20);

    /// <summary>Verifies that the configured time limits can be enforced by the request pipeline.</summary>
    internal void Validate()
    {
        if (RemoteGroupOperationTimeout <= TimeSpan.Zero)
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "SignalR backplane",
                "registration",
                $"{nameof(RemoteGroupOperationTimeout)} must be greater than zero",
                $"Set {nameof(RemoteGroupOperationTimeout)} to a positive duration"));
        }

        if (RemoteGroupOperationTimeout >= FirstUnsupportedTimerDuration)
        {
            throw new ConfigurationException(ConfigurationMessages.Create(
                "SignalR backplane",
                "registration",
                $"{nameof(RemoteGroupOperationTimeout)} exceeds the maximum supported timer duration",
                $"Set {nameof(RemoteGroupOperationTimeout)} to less than {FirstUnsupportedTimerDuration.TotalMilliseconds:0} milliseconds"));
        }
    }
}
