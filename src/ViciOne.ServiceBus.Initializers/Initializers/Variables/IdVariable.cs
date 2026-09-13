using System;
using System.Threading;
using System.Threading.Tasks;
using ViciOne.ServiceBus;
using ViciOne.ServiceBus.Advanced;

namespace ViciOne.ServiceBus.Initializers.Variables;

/// <summary>
/// Supplies one identifier to every matching property within an initialization context.
/// A variable can therefore initialize related identifier properties with the same value.
/// </summary>
public sealed class IdVariable :
    IInitializerVariable<Guid>
{
    readonly Guid _id;

    /// <summary>Creates a variable with a newly generated sequential identifier.</summary>
    public IdVariable()
    {
        _id = NewId.NextGuid();
    }

    /// <summary>Creates a variable that supplies the specified identifier.</summary>
    /// <param name="id">The identifier supplied during initialization.</param>
    public IdVariable(Guid id)
    {
        _id = id;
    }

    Task<Guid> IInitializerVariable<Guid>.GetValueAsync<TMessage>(InitializeContext<TMessage> context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();
        var idContext = context.GetOrAddPayload<IdContext>(() => new IdContext(_id));

        return Task.FromResult(idContext.Id);
    }

    /// <summary>Returns the identifier captured by the variable.</summary>
    /// <param name="variable">The identifier variable.</param>
    /// <returns>The captured identifier.</returns>
    public static implicit operator Guid(IdVariable variable)
    {
        ArgumentNullException.ThrowIfNull(variable);
        return variable._id;
    }

    sealed class IdContext(Guid id)
    {
        public Guid Id { get; } = id;
    }
}
