using System.Data;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>Contains validated transaction and locking settings for one EF Core outbox DbContext.</summary>
/// <typeparam name="TDbContext">The db context type.</typeparam>
public sealed class EntityFrameworkOutboxOptions<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>Gets or sets the isolation level used for inbox and outbox transactions.</summary>
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;
    /// <summary>Gets or sets the provider-specific SQL used to acquire inbox and outbox locks.</summary>
    public ILockStatementProvider LockStatementProvider { get; set; } = null!;
}
