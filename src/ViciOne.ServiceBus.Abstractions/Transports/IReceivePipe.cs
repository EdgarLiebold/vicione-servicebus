using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by receive pipe.</summary>
public interface IReceivePipe :
    IPipe<ReceiveContext>,
    IConsumePipeConnector,
    IRequestPipeConnector,
    IConsumeMessageObserverConnector,
    IConsumeObserverConnector
{
    /// <summary>Task is completed once a connection has been made to the consume pipe (any type of consumer, response handler, etc.</summary>
    Task Connected { get; }
}
