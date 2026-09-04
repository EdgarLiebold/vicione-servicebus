using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus.Context;

namespace ViciOne.ServiceBus;

/// <summary>
/// Sends messages to a specific destination using application-level options.
/// </summary>
public interface ISendEndpoint :
    ISendObserverConnector
{
    /// <summary>Sends a message to the endpoint.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync<T>(T message, CancellationToken cancellationToken = default)
        where T : class;

    /// <summary>Sends a message to the endpoint with application-level metadata.</summary>
    /// <param name="message">The message processed by the operation.</param>
    /// <param name="options">The options used by the operation.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    Task SendAsync<T>(T message, SendOptions options, CancellationToken cancellationToken = default)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(options);

        return this is Advanced.IAdvancedSendEndpoint advanced
            ? advanced.SendAsync(message, new SendOptionsPipe<T>(options), cancellationToken)
            : throw new NotSupportedException($"The send endpoint '{GetType().FullName}' does not support send options.");
    }
}
