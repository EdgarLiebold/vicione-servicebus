namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for execute transform specification.
/// </summary>
/// <typeparam name="TArguments">The t arguments type.</typeparam>
public interface IExecuteTransformSpecification<TArguments> :
    IPipeSpecification<ExecuteContext<TArguments>>
    where TArguments : class
{
}
