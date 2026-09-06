
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Verifies that the configured EF provider/session gives Durable Sender admissions a synchronous durable-commit
/// boundary. Providers not covered by the built-in validator must register a provider-certified implementation.
/// </summary>
public interface IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>
    where TBus : class, IBus
{
    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <param name="dbContext">The db context value.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    Task ValidateAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default);
}
