// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers
{
    using System.Threading.Tasks;


    public interface IInitializerVariable<T>
    {
        Task<T> GetValue<TMessage>(InitializeContext<TMessage> context)
            where TMessage : class;
    }
}
