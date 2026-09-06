using System.Text;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>Formats ActiveMQ host settings for diagnostics.</summary>
public static class ActiveMqHostSettingsExtensions
{
    /// <summary>Creates a credential-safe broker description.</summary>
    /// <param name="settings">The ActiveMQ host settings.</param>
    /// <returns>The broker host and optional user name and port, without a password.</returns>
    public static string ToDescription(this ActiveMqHostSettings settings)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(settings.Username))
            sb.Append(settings.Username).Append('@');

        sb.Append(settings.Host);

        if (settings.Port != -1)
            sb.Append(':').Append(settings.Port);

        return sb.ToString();
    }
}
