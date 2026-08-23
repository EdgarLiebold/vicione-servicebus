namespace ViciOne.ServiceBus.Caching.Internals
{
    public interface IBucketNode<TValue> :
        INode<TValue>
        where TValue : class
    {
        /// <summary>
        /// The node's bucket
        /// </summary>
        Bucket<TValue> Bucket { get; }

        /// <summary>
        /// Returns the next node in the bucket
        /// </summary>
        IBucketNode<TValue> Next { get; }

        /// <summary>
        /// Puts the node's bucket, once the value is resolved, so that the node
        /// can be tracked.
        /// </summary>
        /// <param name="bucket"></param>
        /// <param name="next"></param>
        void SetBucket(Bucket<TValue> bucket, IBucketNode<TValue> next);

        /// <summary>
        /// Assigns the node to a new bucket, but doesn't change the next node
        /// until it's cleaned up
        /// </summary>
        /// <param name="bucket"></param>
        void AssignToBucket(Bucket<TValue> bucket);

        /// <summary>
        /// Atomically evicts the node and returns the stored value to the caller that won the eviction.
        /// </summary>
        /// <param name="value">The stored value when eviction succeeds; otherwise <see langword="null"/>.</param>
        /// <returns><see langword="true"/> only for the caller that evicted the node.</returns>
        bool TryEvict(out TValue value);

        /// <summary>
        /// Remove the node from the bucket, and return the next node
        /// </summary>
        /// <returns></returns>
        IBucketNode<TValue> Pop();
    }
}
