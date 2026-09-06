using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using ViciOne.ServiceBus.RabbitMq.Configuration;

namespace ViciOne.ServiceBus.RabbitMq;

/// <summary>Cleans up RabbitMQ client resources and formats sanitized connection descriptions.</summary>
public static class RabbitMqExtensions
{
    /// <summary>Closes and disposes a RabbitMQ channel, logging and suppressing cleanup failures.</summary>
    /// <param name="channel">The channel to clean up.</param>
    /// <param name="replyCode">The AMQP close reply code.</param>
    /// <param name="message">The AMQP close reason.</param>
    /// <param name="cancellationToken">Cancellation for the close handshake.</param>
    /// <returns>A task that completes after close and disposal have been attempted.</returns>
    public static async Task CleanupAsync(this IChannel channel, ushort replyCode = 200, string message = "Unknown",
        CancellationToken cancellationToken = default)
    {
        if (channel != null)
        {
            try
            {
                if (channel.IsOpen)
                    await channel.CloseAsync(replyCode, message, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                // Closing is best effort; record protocol context and continue to disposal.
                LogContext.Error?.Log(exception, "Closing the channel faulted, the primary failure is unaffected: {ReplyCode} {Message}",
                    replyCode, message);
            }

            // Disposal is also best effort because the broker may already have closed the channel.
            try
            {
                await channel.DisposeAsync().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                LogContext.Error?.Log(exception, "Disposing the channel faulted, the primary failure is unaffected: {ReplyCode} {Message}",
                    replyCode, message);
            }
        }
    }

    /// <summary>Attempts to close a RabbitMQ connection, suppresses close failure, and then disposes it.</summary>
    /// <param name="connection">The connection to clean up, or <see langword="null" />.</param>
    /// <param name="replyCode">The AMQP close reply code.</param>
    /// <param name="message">The AMQP close reason.</param>
    /// <param name="cancellationToken">Cancellation for the close handshake.</param>
    /// <returns>A task that completes after disposal; disposal failure is propagated.</returns>
    public static async Task CleanupAsync(this IConnection? connection, ushort replyCode = 200, string message = "Unknown",
        CancellationToken cancellationToken = default)
    {
        if (connection == null)
            return;

        try
        {
            if (connection.IsOpen)
                await connection.CloseAsync(replyCode, message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
        }

        await connection.DisposeAsync().ConfigureAwait(false);
    }

    /// <summary>Formats a connection description without including the password.</summary>
    /// <param name="settings">The RabbitMQ host and authentication settings.</param>
    /// <returns>The user, configured and selected host, port, and virtual host.</returns>
    public static string ToDescription(this RabbitMqHostSettings settings)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(settings.Username))
            sb.Append(settings.Username).Append('@');

        sb.Append(settings.Host);

        ClusterNode? actualHost = settings.EndpointResolver?.LastHost;
        if (actualHost != null)
            sb.Append('(').Append(actualHost).Append(')');
        if (settings.Port != -1)
            sb.Append(':').Append(settings.Port);

        if (string.IsNullOrWhiteSpace(settings.VirtualHost))
            sb.Append('/');
        else if (settings.VirtualHost.StartsWith("/"))
            sb.Append(settings.VirtualHost);
        else
            sb.Append("/").Append(settings.VirtualHost);

        return sb.ToString();
    }

    /// <summary>Formats a connection description using the credentials currently applied to a client factory.</summary>
    /// <param name="settings">The RabbitMQ host settings.</param>
    /// <param name="connectionFactory">The refreshed client factory supplying the current user name.</param>
    /// <returns>The user, configured and selected host, port, and virtual host.</returns>
    public static string ToDescription(this RabbitMqHostSettings settings, ConnectionFactory connectionFactory)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(connectionFactory.UserName))
            sb.Append(connectionFactory.UserName).Append('@');

        sb.Append(settings.Host);

        ClusterNode? actualHost = settings.EndpointResolver?.LastHost;
        if (actualHost != null)
            sb.Append('(').Append(actualHost).Append(')');
        if (settings.Port != -1)
            sb.Append(':').Append(settings.Port);

        if (string.IsNullOrWhiteSpace(settings.VirtualHost))
            sb.Append('/');
        else if (settings.VirtualHost.StartsWith("/"))
            sb.Append(settings.VirtualHost);
        else
            sb.Append("/").Append(settings.VirtualHost);

        return sb.ToString();
    }
}
