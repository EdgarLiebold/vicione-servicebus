namespace ViciOne.ServiceBus.Caching.Internals
{
    using System;
    using System.Diagnostics;
    using System.Threading;


    public class Bucket<TValue>
        where TValue : class
    {
        const long ActiveTimestamp = long.MinValue;
        readonly INodeTracker<TValue> _tracker;
        int _count;
        IBucketNode<TValue> _head;
        long _startTimestamp;
        long _stopTimestamp = ActiveTimestamp;

        public Bucket(INodeTracker<TValue> tracker)
        {
            _tracker = tracker;
        }

        public IBucketNode<TValue> Head => Volatile.Read(ref _head);

        public int Count => Volatile.Read(ref _count);

        public bool HasExpired(DateTime expirationTime)
        {
            return Volatile.Read(ref _startTimestamp) < expirationTime.Ticks;
        }

        public bool IsOldEnough(DateTime agedTime)
        {
            long stopTimestamp = Volatile.Read(ref _stopTimestamp);

            return stopTimestamp != ActiveTimestamp && stopTimestamp < agedTime.Ticks;
        }

        /// <summary>
        /// Clear the bucket, no node cleanup is performed
        /// </summary>
        public void Clear()
        {
            Volatile.Write(ref _head, null);
            Volatile.Write(ref _count, 0);
        }

        public void Stop(DateTime now)
        {
            Volatile.Write(ref _stopTimestamp, now.Ticks);
        }

        public void Start(DateTime now)
        {
            Volatile.Write(ref _head, null);
            Volatile.Write(ref _count, 0);
            Volatile.Write(ref _startTimestamp, now.Ticks);
            Volatile.Write(ref _stopTimestamp, ActiveTimestamp);
        }

        /// <summary>
        /// Push a node to the front of the bucket, and set the node's bucket to this bucket
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public void Push(IBucketNode<TValue> node)
        {
            Debug.Assert(Volatile.Read(ref _stopTimestamp) == ActiveTimestamp, "Bucket is stopped");

            IBucketNode<TValue> next;
            do
            {
                next = Volatile.Read(ref _head);

                node.SetBucket(this, next);
            }
            while (next != Interlocked.CompareExchange(ref _head, node, next));

            Interlocked.Increment(ref _count);
        }

        /// <summary>
        /// When a node is used, check and rebucket if necessary to keep it in the cache
        /// </summary>
        /// <param name="node"></param>
        public void Used(IBucketNode<TValue> node)
        {
            // a stopped bucket is no longer the current bucket, so give the node back to the manager
            if (Volatile.Read(ref _stopTimestamp) == ActiveTimestamp)
                return;

            _tracker.Rebucket(node);
        }

        internal void ValueUsed()
        {
            Interlocked.Decrement(ref _count);
        }
    }
}
