using System;
using System.Collections.Generic;
using System.Transactions;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a transaction pipe specification implementation.
/// </summary>
/// <typeparam name="T">The t type.</typeparam>
public class TransactionPipeSpecification<T> :
    ITransactionConfigurator,
    IPipeSpecification<T>
    where T : class, PipeContext
{
    IsolationLevel _isolationLevel;
    TimeSpan _timeout;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public TransactionPipeSpecification()
    {
        _isolationLevel = IsolationLevel.ReadCommitted;
        _timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new TransactionFilter<T>(_isolationLevel, _timeout));
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_timeout <= TimeSpan.Zero)
            yield return this.Failure("Timeout", "Must be > 0");
    }

    /// <summary>
    /// Gets or sets the timeout value.
    /// </summary>
    public TimeSpan Timeout
    {
        set => _timeout = value;
    }

    /// <summary>
    /// Gets or sets the isolation level value.
    /// </summary>
    public IsolationLevel IsolationLevel
    {
        set => _isolationLevel = value;
    }
}
