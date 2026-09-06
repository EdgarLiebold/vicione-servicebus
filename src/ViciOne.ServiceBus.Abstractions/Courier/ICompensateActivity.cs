using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Defines the operations required by compensate activity.</summary>
/// <typeparam name="TLog">The log type.</typeparam>
public interface ICompensateActivity<in TLog> :
    ICompensateActivity
    where TLog : class
{
    /// <summary>Compensate the activity and return the remaining compensation items.</summary>
    /// <param name="context">The compensation information for the activity.</param>
    /// <returns>A task that produces the compensate outcome.</returns>
    Task<CompensationResult> CompensateAsync(CompensateContext<TLog> context);
}


/// <summary>
/// Marker interface used to assist identification in IoC containers.
/// Not to be used directly as it does not contain the message type of the
/// consumer.
/// </summary>
/// <remarks>
/// Not to be used directly by application code, for internal reflection only
/// </remarks>
public interface ICompensateActivity
{
}
