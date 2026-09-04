using System;
using Microsoft.EntityFrameworkCore;

namespace ViciOne.ServiceBus.EntityFrameworkCore;

/// <summary>
/// Defines the contract for saga class map.
/// </summary>
public interface ISagaClassMap
{
    /// <summary>
    /// Gets the saga type value.
    /// </summary>
    Type SagaType { get; }
    /// <summary>
    /// Performs the configure operation.
    /// </summary>
    /// <param name="model">The model value.</param>
    void Configure(ModelBuilder model);
}


/// <summary>
/// Defines the contract for saga class map.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public interface ISagaClassMap<TSaga> :
    ISagaClassMap
    where TSaga : class, ISaga
{
}
