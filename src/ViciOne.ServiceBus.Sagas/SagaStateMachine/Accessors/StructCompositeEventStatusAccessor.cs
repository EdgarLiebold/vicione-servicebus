using System.Reflection;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.SagaStateMachine;

/// <summary>Provides access to struct composite event status.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class StructCompositeEventStatusAccessor<TSaga> :
    ICompositeEventStatusAccessor<TSaga>
    where TSaga : class
{
    readonly string _name;
    readonly IReadProperty<TSaga, CompositeEventStatus> _read;
    readonly IWriteProperty<TSaga, CompositeEventStatus> _write;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="propertyInfo">The property info.</param>
    public StructCompositeEventStatusAccessor(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        _read = ReadPropertyCache<TSaga>.GetProperty<CompositeEventStatus>(propertyInfo);
        _write = WritePropertyCache<TSaga>.GetProperty<CompositeEventStatus>(propertyInfo);
        _name = propertyInfo.Name;
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="instance">The instance.</param>
    /// <returns>The requested value.</returns>
    public CompositeEventStatus Get(TSaga instance)
    {
        return _read.Get(instance);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="status">The status.</param>
    public void Set(TSaga instance, CompositeEventStatus status)
    {
        _write.Set(instance, status);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    public void Probe(ProbeContext context)
    {
        context.Add("property", _name);
        context.Add("type", nameof(CompositeEventStatus));
    }
}
