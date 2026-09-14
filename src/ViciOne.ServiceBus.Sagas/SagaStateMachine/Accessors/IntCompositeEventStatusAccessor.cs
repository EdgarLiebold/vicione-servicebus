using System;
using System.Reflection;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Provides access to int composite event status.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class IntCompositeEventStatusAccessor<TSaga> :
    ICompositeEventStatusAccessor<TSaga>
    where TSaga : class
{
    readonly string _name;
    readonly IReadProperty<TSaga, int> _read;
    readonly IWriteProperty<TSaga, int> _write;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyInfo">The property info.</param>
    public IntCompositeEventStatusAccessor(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        _read = ReadPropertyCache<TSaga>.GetProperty<int>(propertyInfo);
        _write = WritePropertyCache<TSaga>.GetProperty<int>(propertyInfo);
        _name = propertyInfo.Name;
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="instance">The instance.</param>
    /// <returns>The requested value.</returns>
    public CompositeEventStatus Get(TSaga instance)
    {
        return new CompositeEventStatus(_read.Get(instance));
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="status">The status.</param>
    public void Set(TSaga instance, CompositeEventStatus status)
    {
        _write.Set(instance, status.Bits);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("property", _name);
        context.Add("type", nameof(Int32));
    }
}
