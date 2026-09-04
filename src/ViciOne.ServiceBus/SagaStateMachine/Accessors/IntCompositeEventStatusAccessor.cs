using System;
using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>
/// Provides an int composite event status accessor implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class IntCompositeEventStatusAccessor<TSaga> :
    ICompositeEventStatusAccessor<TSaga>
    where TSaga : class
{
    readonly string _name;
    readonly IReadProperty<TSaga, int> _read;
    readonly IWriteProperty<TSaga, int> _write;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="propertyInfo">The property info value.</param>
    public IntCompositeEventStatusAccessor(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        _read = ReadPropertyCache<TSaga>.GetProperty<int>(propertyInfo);
        _write = WritePropertyCache<TSaga>.GetProperty<int>(propertyInfo);
        _name = propertyInfo.Name;
    }

    /// <summary>
    /// Performs the get operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <returns>The result of the operation.</returns>
    public CompositeEventStatus Get(TSaga instance)
    {
        return new CompositeEventStatus(_read.Get(instance));
    }

    /// <summary>
    /// Performs the set operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    /// <param name="status">The status value.</param>
    public void Set(TSaga instance, CompositeEventStatus status)
    {
        _write.Set(instance, status.Bits);
    }

    /// <summary>
    /// Performs the probe operation.
    /// </summary>
    /// <param name="context">The operation context.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("property", _name);
        context.Add("type", nameof(Int32));
    }
}
