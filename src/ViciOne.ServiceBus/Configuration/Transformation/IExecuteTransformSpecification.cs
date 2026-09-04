namespace ViciOne.ServiceBus.Configuration;

public interface IExecuteTransformSpecification<TArguments> :
    IPipeSpecification<ExecuteContext<TArguments>>
    where TArguments : class
{
}
