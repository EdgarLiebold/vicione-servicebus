namespace ViciOne.ServiceBus.Transports;

/// <summary>Defines the operations required by send pipe.</summary>
public interface ISendPipe :
    ISendContextPipe,
    IProbeSite
{
}
