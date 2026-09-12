using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Providers.Configuration;

namespace ViciOne.ServiceBus.SignalR;

/// <summary>Configures SignalR scale-out behavior over ViciOne.ServiceBus.</summary>
public sealed class SignalRBackplaneOptions
{
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
    }
}
