namespace ViciOne.ServiceBus.Mediator;

/// <summary>Dispatches in-process messages while resolving handlers from the current dependency-injection scope.</summary>
public interface IScopedMediator :
    IMediator
{
}
