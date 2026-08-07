// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Internals.Caching
{
    using System.Threading;


    public struct CacheMetrics
    {
        long _hits;
        long _misses;

        public double HitRatio => Total == 0 ? 0 : (double)_hits / Total;

        long Total => _hits + _misses;

        public void Miss()
        {
            Interlocked.Increment(ref _misses);
        }

        public void Hit()
        {
            Interlocked.Increment(ref _hits);
        }
    }
}
