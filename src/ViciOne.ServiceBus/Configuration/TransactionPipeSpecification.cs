using System;
using System.Collections.Generic;
using System.Transactions;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for transaction pipe.</summary>
/// <typeparam name="T">The value type.</typeparam>
public class TransactionPipeSpecification<T> :
    ITransactionConfigurator,
    IPipeSpecification<T>
    where T : class, PipeContext
{
    IsolationLevel _isolationLevel;
    TimeSpan _timeout;

    /// <summary>Initializes a new instance.</summary>
    public TransactionPipeSpecification()
    {
        _isolationLevel = IsolationLevel.ReadCommitted;
        _timeout = TimeSpan.FromSeconds(30);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new TransactionFilter<T>(_isolationLevel, _timeout));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_timeout <= TimeSpan.Zero)
            yield return this.Failure("Timeout", "Must be > 0");
    }

    /// <summary>Gets or sets the timeout.</summary>
    public TimeSpan Timeout
    {
        set => _timeout = value;
    }

    /// <summary>Gets or sets the isolation level.</summary>
    public IsolationLevel IsolationLevel
    {
        set => _isolationLevel = value;
    }
}
