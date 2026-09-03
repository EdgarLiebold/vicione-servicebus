namespace ViciOne.ServiceBus;

using System.Threading;
using System.Threading.Tasks;


/// <summary>
/// Administrative RabbitMQ queue operations for the default bus instance. Authorization, approval,
/// audit, and user interface policy belong to the host application.
/// </summary>
public interface IRabbitMqQueueOperations
{
    Task<RabbitMqFaultRedriveResult> RedriveFaultedMessages(
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken = default);
}


/// <summary>
/// Administrative RabbitMQ queue operations bound to one explicit bus instance.
/// </summary>
public interface IRabbitMqQueueOperations<TBus>
    where TBus : class, IBus
{
    Task<RabbitMqFaultRedriveResult> RedriveFaultedMessages(
        RabbitMqFaultRedriveRequest request,
        CancellationToken cancellationToken = default);
}
