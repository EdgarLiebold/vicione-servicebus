namespace ViciOne.ServiceBus.Mediator;

/// <summary>Identifies a mediator request and its expected response contract.</summary>
/// <typeparam name="TResponse">The response message contract.</typeparam>
[ExcludeFromTopology]
[ExcludeFromImplementedTypes]
public interface IRequest<out TResponse>
    where TResponse : class
{
}
