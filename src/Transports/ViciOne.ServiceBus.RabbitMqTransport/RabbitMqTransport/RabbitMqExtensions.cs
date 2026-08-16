namespace ViciOne.ServiceBus.RabbitMqTransport
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Configuration;
    using RabbitMQ.Client;


    public static class RabbitMqExtensions
    {
        /// <summary>
        /// Close and dispose of a RabbitMQ channel without throwing any exceptions
        /// </summary>
        /// <param name="channel">The channel (can be null)</param>
        /// <param name="replyCode"></param>
        /// <param name="message">Message for channel closure</param>
        /// <param name="cancellationToken"></param>
        public static async Task Cleanup(this IChannel channel, ushort replyCode = 200, string message = "Unknown",
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
                    // Deliberately not thrown — but an empty catch is not "diagnostically visible", and
                    // record 0049 is right that it was one. It is recorded at error level with the reply
                    // code and message it was closing with, and never replaces the primary failure.
                    LogContext.Error?.Log(exception, "Closing the channel faulted, the primary failure is unaffected: {ReplyCode} {Message}",
                        replyCode, message);
                }

                // Inside the guard, not beside it. Disposing a channel the broker has just closed
                // throws, and this method promises in its own summary not to — so the promise was
                // broken exactly when it mattered, on the failure path.
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

        /// <summary>
        /// Close and dispose of a RabbitMQ connection without throwing any exceptions
        /// </summary>
        /// <param name="connection">The channel (can be null)</param>
        /// <param name="replyCode"></param>
        /// <param name="message">Message for channel closure</param>
        /// <param name="cancellationToken"></param>
        public static async Task Cleanup(this IConnection connection, ushort replyCode = 200, string message = "Unknown",
            CancellationToken cancellationToken = default)
        {
            if (connection != null)
            {
                try
                {
                    if (connection.IsOpen)
                        await connection.CloseAsync(replyCode, message, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                }
            }

            await connection.DisposeAsync().ConfigureAwait(false);
        }

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
}
