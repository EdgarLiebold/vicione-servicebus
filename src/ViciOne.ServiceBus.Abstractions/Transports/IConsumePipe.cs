using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by consume pipe.</summary>
public interface IConsumePipe :
    IPipe<ConsumeContext>,
    IConsumePipeConnector,
    IRequestPipeConnector,
    IConsumeMessageObserverConnector,
    IConsumeObserverConnector
{
    /// <summary>Task is completed once a connection has been made to the consume pipe (any type of consumer, response handler, etc.</summary>
    Task Connected { get; }
}
