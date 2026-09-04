using System;
using System.Threading;
using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers.Variables;

public class IdVariable :
    IInitializerVariable<Guid>
{
    readonly Guid _id;

    public IdVariable()
    {
        _id = NewId.NextGuid();
    }

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
