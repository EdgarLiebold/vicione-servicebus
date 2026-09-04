using System.Data;
using Microsoft.EntityFrameworkCore;
using ViciOne.ServiceBus.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines configuration options for entity framework outbox.
/// </summary>
/// <typeparam name="TDbContext">The t db context type.</typeparam>
public class EntityFrameworkOutboxOptions<TDbContext>
    where TDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel { get; set; } = IsolationLevel.RepeatableRead;
    /// <summary>
    /// Gets or sets the lock statement provider value.
    /// </summary>
    public ILockStatementProvider LockStatementProvider { get; set; } = null!;
}
