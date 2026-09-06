namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for compensate transform.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateTransformSpecification<TLog> :
    IPipeSpecification<CompensateContext<TLog>>
    where TLog : class
{
}
