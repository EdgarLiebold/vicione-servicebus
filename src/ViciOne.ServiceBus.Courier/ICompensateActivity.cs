using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Courier;

/// <summary>Compensates one previously completed routing-slip activity.</summary>
/// <typeparam name="TLog">The activity log contract needed for compensation.</typeparam>
[ActivityContract]
public interface ICompensateActivity<in TLog>
    where TLog : class
{
    /// <summary>Compensates the completed activity.</summary>
    /// <param name="context">The compensation information for the activity.</param>
    /// <returns>A task containing the routing-slip compensation outcome.</returns>
    Task<CompensationResult> CompensateAsync(CompensateContext<TLog> context);
}
