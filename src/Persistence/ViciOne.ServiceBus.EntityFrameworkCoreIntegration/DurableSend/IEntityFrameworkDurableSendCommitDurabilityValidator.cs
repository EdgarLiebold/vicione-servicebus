#nullable enable

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCoreIntegration;

/// <summary>
/// Verifies that the configured EF provider/session gives Durable Sender admissions a synchronous durable-commit
/// boundary. Providers not covered by the built-in validator must register a provider-certified implementation.
/// </summary>
public interface IEntityFrameworkDurableSendCommitDurabilityValidator<TBus>
    where TBus : class, IBus
{
    Task ValidateAsync(
        DbContext dbContext,
        CancellationToken cancellationToken = default);
}
