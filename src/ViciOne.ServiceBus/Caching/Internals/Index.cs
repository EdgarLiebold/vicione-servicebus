namespace ViciOne.ServiceBus.Caching.Internals
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;


    public class Index<TKey, TValue> :
        ICacheIndex<TValue>,
        ICacheValueObserver<TValue>,
        IIndex<TKey, TValue>
        where TValue : class
    {
        readonly KeyProvider<TKey, TValue> _keyProvider;
        readonly object _lock = new object();
        readonly INodeTracker<TValue> _nodeTracker;
        Dictionary<TKey, INode<TValue>> _index;

        public Index(INodeTracker<TValue> nodeTracker, KeyProvider<TKey, TValue> keyProvider)
        {
            _nodeTracker = nodeTracker;
            _keyProvider = keyProvider;

            _index = new Dictionary<TKey, INode<TValue>>();

            _nodeTracker.Connect(this);
        }

        Type ICacheIndex<TValue>.KeyType => typeof(TKey);

        public void Clear()
        {
            lock (_lock)
                _index.Clear();
        }

        public async Task<bool> Add(INode<TValue> node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            var value = await node.Value.ConfigureAwait(false);

            var key = _keyProvider(value);

            lock (_lock)
            {
                if (!_index.TryGetValue(key, out INode<TValue> existingNode)
                    || !existingNode.IsValid)
                {
                    _index[key] = node;
                    return true;
                }

                return false;
            }
        }

        public bool TryGetExistingNode(TValue value, out INode<TValue> node)
        {
            var key = _keyProvider(value);

            lock (_lock)
                return TryGetExistingNode(key, out node);
        }

        public void ValueAdded(INode<TValue> node, TValue value)
        {
            var key = _keyProvider(value);

            lock (_lock)
            {
                // Add notifications are published after tracker state changes. A delayed
                // notification for an already-evicted node must not replace a newer live value.
                if (node.IsValid)
                    _index[key] = node;
            }
        }

        public void ValueRemoved(INode<TValue> node, TValue value)
        {
            var key = _keyProvider(value);

            lock (_lock)
            {
                // A removal may be published after another value with the same key was added.
                // Remove only the exact generation that produced this notification.
                if (_index.TryGetValue(key, out INode<TValue> current) && ReferenceEquals(current, node))
                    _index.Remove(key);
            }
        }

        public void CacheCleared()
        {
            Rebuild();
        }

        public Task<TValue> Get(TKey key, MissingValueFactory<TKey, TValue> missingValueFactory)
        {
            INode<TValue> valueNode = null;

            lock (_lock)
            {
                if (TryGetExistingNode(key, out INode<TValue> existingNode))
                {
                    if (existingNode.HasValue)
                    {
                        _nodeTracker.Statistics.Hit();
                        valueNode = existingNode;
                    }
                    else
                    {
                        var pending = new PendingValue<TKey, TValue>(key, missingValueFactory);

                        return existingNode.GetValue(pending);
                    }
                }
                else if (missingValueFactory == null)
                {
                    _nodeTracker.Statistics.Miss();
                    throw new KeyNotFoundException($"Key not found: {key}");
                }
                else
                {
                    var pendingValue = new PendingValue<TKey, TValue>(key, missingValueFactory);

                    var nodeValueFactory = new NodeValueFactory<TValue>(pendingValue, 0);

                    var node = new FactoryNode<TValue>(nodeValueFactory);

                    _index[key] = node;

                    _nodeTracker.Add(nodeValueFactory);

                    return pendingValue.Value;
                }
            }

            // Reading a bucket node records usage and may rebucket it. Never do that while the
            // index lock is held: cleanup publishes index notifications after releasing the
            // tracker lock, and the two independent locks must not be acquired in opposite order.
            return valueNode.Value;
        }

        public bool Remove(TKey key)
        {
            lock (_lock)
            {
                if (!TryGetExistingNode(key, out INode<TValue> existingNode))
                    return false;

                // A factory node is only a placeholder. Removing it would report success while
                // the value creation continues and can publish the value back into every index.
                if (existingNode is not IBucketNode<TValue> storedNode)
                    return false;

                _index.Remove(key);
                _nodeTracker.Remove(storedNode);

                return true;
            }
        }

        bool TryGetExistingNode(TKey key, out INode<TValue> existingNode)
        {
            if (_index.TryGetValue(key, out existingNode))
            {
                if (existingNode.IsValid)
                    return true;

                _index.Remove(key);
            }

            existingNode = null;
            return false;
        }

        void Rebuild()
        {
            lock (_lock)
            {
                // this will throw an exception if there is a duplicate key, but until we support multi-value indices, that's okay
                Dictionary<TKey, INode<TValue>> updatedIndex = _nodeTracker.GetAll()
                    .ToDictionary(node => _keyProvider(node.Value.Result));

                _index = updatedIndex;
            }
        }
    }
}
