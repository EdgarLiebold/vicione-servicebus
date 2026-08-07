// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.Caching
{
    using System.Threading.Tasks;


    public interface IValueTracker<TValue, TCacheValue>
        where TValue : class
        where TCacheValue : ICacheValue<TValue>
    {
        int Capacity { get; }

        Task Add(TCacheValue value);

        Task ReBucket(IBucket<TValue, TCacheValue> source, TCacheValue value);

        Task Clear();
    }
}
