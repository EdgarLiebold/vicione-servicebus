using System;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Azure.Table.Saga;

/// <summary>Scopes one configured Azure Table key formatter to its saga type in dependency injection.</summary>
/// <typeparam name="TSaga">The saga state whose persistence keys are formatted.</typeparam>
internal sealed class AzureTableSagaKeyFormatterProvider<TSaga>
    where TSaga : class, ISaga
{
    /// <summary>Creates a saga-type-specific formatter binding.</summary>
    /// <param name="formatter">The formatter bound to <typeparamref name="TSaga"/>.</param>
    internal AzureTableSagaKeyFormatterProvider(IAzureTableSagaKeyFormatter formatter)
    {
        ArgumentNullException.ThrowIfNull(formatter);
        Formatter = formatter;
    }

    /// <summary>Gets the formatter bound to the saga type.</summary>
    internal IAzureTableSagaKeyFormatter Formatter { get; }
}
