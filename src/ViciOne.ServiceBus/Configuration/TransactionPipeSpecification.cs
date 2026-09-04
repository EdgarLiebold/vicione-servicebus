using System;
using System.Collections.Generic;
using System.Transactions;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public class TransactionPipeSpecification<T> :
    ITransactionConfigurator,
    IPipeSpecification<T>
    where T : class, PipeContext
{
    IsolationLevel _isolationLevel;
    TimeSpan _timeout;

    public TransactionPipeSpecification()
    {
        _isolationLevel = IsolationLevel.ReadCommitted;
        _timeout = TimeSpan.FromSeconds(30);
    }

    public void Apply(IPipeBuilder<T> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        builder.AddFilter(new TransactionFilter<T>(_isolationLevel, _timeout));
    }

    public IEnumerable<ValidationResult> Validate()
    {
        if (_timeout <= TimeSpan.Zero)
            yield return this.Failure("Timeout", "Must be > 0");
    }

    public TimeSpan Timeout
    {
        set => _timeout = value;
    }

    public IsolationLevel IsolationLevel
    {
        set => _isolationLevel = value;
    }
}
