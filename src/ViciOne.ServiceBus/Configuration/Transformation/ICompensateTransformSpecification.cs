namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Defines the contract for compensate transform specification.
/// </summary>
/// <typeparam name="TLog">The t log type.</typeparam>
public interface ICompensateTransformSpecification<TLog> :
    IPipeSpecification<CompensateContext<TLog>>
    where TLog : class
{
}
