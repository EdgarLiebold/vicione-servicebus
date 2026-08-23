namespace ViciOne.ServiceBus.Caching.Internals
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Util;


    /// <summary>
    /// A bucket node has been stored in a bucket, and is a fully resolved value.
    /// </summary>
    /// <typeparam name="TValue">The value type</typeparam>
    public class BucketNode<TValue> :
        IBucketNode<TValue>
        where TValue : class
    {
        Bucket<TValue> _bucket;
        IBucketNode<TValue> _next;
        Task<TValue> _value;

        public BucketNode(TValue value)
        {
            _value = Task.FromResult(value);

            if (value is INotifyValueUsed notify)
                notify.Used += Used;
        }

        public Task<TValue> Value
        {
            get
            {
                Used();
                return _value;
            }
        }

        public bool HasValue => true;
        public bool IsValid => _value.Status == TaskStatus.RanToCompletion;

        public Bucket<TValue> Bucket => _bucket;
        public IBucketNode<TValue> Next => _next;

        public Task<TValue> GetValue(IPendingValue<TValue> pendingValue)
        {
            return _value;
        }

        public void SetBucket(Bucket<TValue> bucket, IBucketNode<TValue> next)
        {
            _bucket = bucket;
            _next = next;
        }

        public void AssignToBucket(Bucket<TValue> bucket)
        {
            _bucket = bucket;
        }

        public bool TryEvict(out TValue value)
        {
            Task<TValue> storedValue = Interlocked.Exchange(ref _value, Cached.Removed);
            if (storedValue.Status != TaskStatus.RanToCompletion)
            {
                value = null;
                return false;
            }

            value = storedValue.GetAwaiter().GetResult();

            if (value is INotifyValueUsed notify)
                notify.Used -= Used;

            _bucket = null;

            // The owning bucket still needs the link to traverse past this tombstone.
            // Pop clears it when the bucket is compacted.

            return true;
        }

        public IBucketNode<TValue> Pop()
        {
            IBucketNode<TValue> next = _next;

            _next = null;

            return next;
        }

        void Used()
        {
            _bucket?.Used(this);
        }


        static class Cached
        {
            internal static readonly Task<TValue> Removed;

            static Cached()
            {
                TaskCompletionSource<TValue> source = TaskUtil.GetTask<TValue>();
                source.TrySetException(new InvalidOperationException("The cached value has been removed"));

                Removed = source.Task;
            }
        }
    }
}
