using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.RabbitMq;
/// <summary>
/// Administrative RabbitMQ queue operations for the default bus instance. Authorization, approval,
/// audit, and user interface policy belong to the host application.
/// </summary>
public interface IRabbitMqQueueOperations
{
    /// <summary>
    /// Performs the redrive faulted messages operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<RabbitMqFaultRedriveResult> RedriveFaultedMessagesAsync(
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken = default);
}


/// <summary>
/// Administrative RabbitMQ queue operations bound to one explicit bus instance.
/// </summary>
public interface IRabbitMqQueueOperations<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Performs the redrive faulted messages operation.
    /// </summary>
    /// <param name="request">The request value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task<RabbitMqFaultRedriveResult> RedriveFaultedMessagesAsync(
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken = default);
}
