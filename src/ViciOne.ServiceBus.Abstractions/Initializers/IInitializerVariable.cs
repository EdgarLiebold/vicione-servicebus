using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Initializers;

public interface IInitializerVariable<T>
{
    Task<T> GetValue<TMessage>(InitializeContext<TMessage> context)
        where TMessage : class;
}
