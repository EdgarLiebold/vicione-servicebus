using System.Text;

namespace ViciOne.ServiceBus.ActiveMq;

/// <summary>
/// Provides extension methods for active mq host settings.
/// </summary>
public static class ActiveMqHostSettingsExtensions
{
    /// <summary>
    /// Performs the to description operation.
    /// </summary>
    /// <param name="settings">The settings value.</param>
    /// <returns>The result of the operation.</returns>
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
