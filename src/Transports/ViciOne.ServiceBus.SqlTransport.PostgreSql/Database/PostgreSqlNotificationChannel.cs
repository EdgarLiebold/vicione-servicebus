using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace ViciOne.ServiceBus.SqlTransport.PostgreSql;

/// <summary>Creates deterministic, schema-scoped PostgreSQL notification channel names.</summary>
internal static class PostgreSqlNotificationChannel
{
    const int NamespaceDiscriminatorBytes = 17;
    const string ChannelPrefix = "vsb_";
    const string DefaultSchemaName = "transport";
    const string QueueSeparator = "_msg_";

    /// <summary>Creates the fixed channel prefix for a transport schema.</summary>
    /// <param name="schemaName">The schema whose notifications the channel carries.</param>
    /// <returns>An ASCII prefix containing a stable 136-bit discriminator for the schema.</returns>
    public static string CreatePrefix(string? schemaName)
    {
        string effectiveSchemaName = string.IsNullOrWhiteSpace(schemaName)
            ? DefaultSchemaName
            : schemaName;
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(effectiveSchemaName));
        string discriminator = Convert.ToHexString(hash, 0, NamespaceDiscriminatorBytes).ToLowerInvariant();

        return string.Concat(ChannelPrefix, discriminator, QueueSeparator);
    }

    /// <summary>Creates the notification channel for a queue in a transport schema.</summary>
    /// <param name="schemaName">The schema whose notifications the channel carries.</param>
    /// <param name="queueId">The database identifier of the queue.</param>
    /// <returns>An ASCII channel name no longer than PostgreSQL's 63-byte identifier limit.</returns>
    public static string CreateName(string? schemaName, long queueId)
    {
        return string.Concat(CreatePrefix(schemaName), queueId.ToString(CultureInfo.InvariantCulture));
    }
}
