using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.Variables;

/// <summary>
/// Provides an id variable implementation.
/// </summary>
public class IdVariable :
    IInitializerVariable<Guid>
{
    readonly Guid _id;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public IdVariable()
    {
        _id = NewId.NextGuid();
    }

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="id">The id value.</param>
    public IdVariable(Guid id)
    {
        _id = id;
    }

    Task<Guid> IInitializerVariable<Guid>.GetValueAsync<TMessage>(InitializeContext<TMessage> context, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var timestampContext = context.GetOrAddPayload<IdContext>(() => new Context(_id));

        return Task.FromResult(timestampContext.Id);
    }

    /// <summary>
    /// Converts a value to <see cref="Guid" />.
    /// </summary>
    /// <param name="variable">The variable value.</param>
    /// <returns>The result of the operation.</returns>
    public static implicit operator Guid(IdVariable variable)
    {
        return variable._id;
    }


    interface IdContext
    {
        Guid Id { get; }
    }


    class Context :
        IdContext
    {
        public Context(Guid id)
        {
            Id = id;
        }

        public Guid Id { get; }
    }
}
