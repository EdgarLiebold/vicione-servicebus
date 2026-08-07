// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.Caching
{
    using System.Threading.Tasks;


    public interface IBucket<TValue, in TCacheValue>
        where TValue : class
        where TCacheValue : ICacheValue<TValue>
    {
        Task Add(TCacheValue value);
    }
}
