using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.AmazonSqs;

/// <summary>
/// Defines the contract for batcher.
/// </summary>
/// <typeparam name="TEntry">The t entry type.</typeparam>
public interface IBatcher<in TEntry> :
    IAsyncDisposable
{
    /// <summary>
    /// Performs the execute operation.
    /// </summary>
    /// <param name="entry">The entry value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ExecuteAsync(TEntry entry, CancellationToken cancellationToken = default);
}
