namespace ViciOne.ServiceBus.Transports;

/// <summary>
/// Defines the contract for send pipe.
/// </summary>
public interface ISendPipe :
    ISendContextPipe,
    IProbeSite
{
}
