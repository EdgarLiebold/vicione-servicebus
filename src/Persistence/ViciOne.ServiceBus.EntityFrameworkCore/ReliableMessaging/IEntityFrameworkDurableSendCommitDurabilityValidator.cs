using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Verifies that the configured EF provider/session gives Durable Sender admissions a synchronous durable-commit
/// boundary. Providers not covered by the built-in validator must register a provider-certified implementation.
/// </summary>
/// <typeparam name="TBus">The bus type.</typeparam>
public interface IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>
    where TBus : class, IBus
{
    /// <summary>Rejects provider settings that cannot guarantee a durable commit before admission returns.</summary>
    /// <param name="dbContext">A context configured for the reliable-messaging database.</param>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that completes after the provider settings have been accepted.</returns>
    Task ValidateAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default);
}
