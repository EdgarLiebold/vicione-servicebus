namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for execute transform.</summary>
/// <typeparam name="TArguments">The arguments type.</typeparam>
public interface IExecuteTransformSpecification<TArguments> :
    IPipeSpecification<ExecuteContext<TArguments>>
    where TArguments : class
{
}
