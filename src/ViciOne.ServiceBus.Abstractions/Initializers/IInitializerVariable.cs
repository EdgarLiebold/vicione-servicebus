using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

public interface IInitializerVariable<T>
{
    Task<T> GetValueAsync<TMessage>(InitializeContext<TMessage> context, CancellationToken cancellationToken = default)
        where TMessage : class;
}
