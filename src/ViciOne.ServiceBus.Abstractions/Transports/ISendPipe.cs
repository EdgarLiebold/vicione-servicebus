namespace ViciOne.ServiceBus.Transports;

/// <summary>Configures typed send contexts through an endpoint-level pipeline with diagnostic probing.</summary>
public interface ISendPipe :
    ISendContextPipe,
    IProbeSite
{
}
