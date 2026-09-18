using System;
using System.Reflection;
using ViciOne.ServiceBus.Internals.Reflection;

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
    /// <exception cref="ArgumentNullException"><paramref name="propertyInfo" /> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="propertyInfo" /> is not a readable, writable instance <see cref="CompositeEventStatus" /> property owned by <typeparamref name="TSaga" />.</exception>
    public StructCompositeEventStatusAccessor(PropertyInfo propertyInfo)
    {
        ArgumentNullException.ThrowIfNull(propertyInfo);
        _read = new ReadProperty<TSaga, CompositeEventStatus>(propertyInfo);
        _write = new WriteProperty<TSaga, CompositeEventStatus>(typeof(TSaga), propertyInfo);
        _name = propertyInfo.Name;
    }

    /// <summary>Retrieves the requested value.</summary>
    /// <param name="instance">The instance.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="instance" /> is null.</exception>
    public CompositeEventStatus Get(TSaga instance)
    {
        ArgumentNullException.ThrowIfNull(instance);
        return _read.Get(instance);
    }

    /// <summary>Updates the target with the supplied value.</summary>
    /// <param name="instance">The instance.</param>
    /// <param name="status">The status.</param>
    /// <exception cref="ArgumentNullException"><paramref name="instance" /> is null.</exception>
    public void Set(TSaga instance, CompositeEventStatus status)
    {
        ArgumentNullException.ThrowIfNull(instance);
        _write.Set(instance, status);
    }

    /// <summary>Writes diagnostic information to the probe context.</summary>
    /// <param name="context">The context associated with the operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is null.</exception>
    public void Probe(ProbeContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        context.Add("property", _name);
        context.Add("type", nameof(CompositeEventStatus));
    }
}
