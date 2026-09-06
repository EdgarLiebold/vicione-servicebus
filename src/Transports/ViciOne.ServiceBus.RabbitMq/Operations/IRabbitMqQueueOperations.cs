using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMq;
/// <summary>
/// Administrative RabbitMQ queue operations for the default bus instance. Authorization, approval,
/// audit, and user interface policy belong to the host application.
/// </summary>
public interface IRabbitMqQueueOperations
{
    /// <summary>Moves faulted messages back to the delivery queue.</summary>
    /// <param name="request">The bounded scan, selection, and destination settings.</param>
    /// <param name="cancellationToken">Cancellation for broker topology checks and message transfer.</param>
    /// <returns>The counts and termination condition of the redrive attempt.</returns>
    Task<RabbitMqFaultRedriveResult> RedriveFaultedMessagesAsync(
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken = default);
}


/// <summary>Administrative RabbitMQ queue operations bound to one explicit bus instance.</summary>
/// <typeparam name="TBus">The bus instance whose RabbitMQ endpoint owns the queues.</typeparam>
public interface IRabbitMqQueueOperations<TBus>
    where TBus : class, IBus
{
    /// <summary>Moves faulted messages back to the delivery queue.</summary>
    /// <param name="request">The bounded scan, selection, and destination settings.</param>
    /// <param name="cancellationToken">Cancellation for broker topology checks and message transfer.</param>
    /// <returns>The counts and termination condition of the redrive attempt.</returns>
    Task<RabbitMqFaultRedriveResult> RedriveFaultedMessagesAsync(
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken = default);
}
