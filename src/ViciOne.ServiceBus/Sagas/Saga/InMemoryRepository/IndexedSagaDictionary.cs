using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FastExpressionCompiler;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Saga;

/// <summary>Stores indexed saga values by key.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
public class IndexedSagaDictionary<TSaga>
    where TSaga : class, ISaga
{
    readonly IIndexedSagaProperty<TSaga> _indexById;
    readonly Dictionary<string, IIndexedSagaProperty<TSaga>> _indices;
    readonly SemaphoreSlim _inUse = new SemaphoreSlim(1);
    readonly object _lock = new object();

    /// <summary>Initializes a new instance.</summary>
    public IndexedSagaDictionary()
    {
        _indices = new Dictionary<string, IIndexedSagaProperty<TSaga>>();

        BuildIndices();

        _indexById = _indices["CorrelationId"];
    }

    /// <summary>Gets or sets the value at the specified index.</summary>
    /// <param name="sagaId">The saga id.</param>
    public SagaInstance<TSaga>? this[Guid sagaId]
    {
        get
        {
            lock (_lock)
                return _indexById[sagaId];
        }
    }

    /// <summary>Gets the count.</summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _indexById.Count;
        }
    }

    /// <summary>Marks in use.</summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        return _inUse.WaitAsync(cancellationToken);
    }

    /// <summary>Releases the owned resource.</summary>
    public void Release()
    {
        _inUse.Release();
    }

    /// <summary>Adds the supplied value to the current collection.</summary>
    /// <param name="instance">The instance.</param>
    public void Add(SagaInstance<TSaga> instance)
    {
        lock (_lock)
        {
            foreach (IIndexedSagaProperty<TSaga> index in _indices.Values)
                index.Add(instance);
        }
    }

    /// <summary>Removes the selected value.</summary>
    /// <param name="item">The item.</param>
    public void Remove(SagaInstance<TSaga> item)
    {
        lock (_lock)
        {
            foreach (IIndexedSagaProperty<TSaga> index in _indices.Values)
                index.Remove(item);
        }
    }

    /// <summary>Filters values using the supplied predicate.</summary>
    /// <param name="query">The query.</param>
    /// <returns>The enumerable produced by the operation.</returns>
    public IEnumerable<SagaInstance<TSaga>> Where(ISagaQuery<TSaga> query)
    {
        lock (_lock)
        {
            IIndexedSagaProperty<TSaga>? index = HasIndexFor(query.FilterExpression);
            if (index == null)
                return _indexById.Where(query.GetFilter()).ToList();

            var rightValue = GetRightValue(query.FilterExpression);
            if (rightValue == null)
                return _indexById.Where(query.GetFilter()).ToList();

            return index.Where(rightValue, query.GetFilter()).ToList();
        }
    }

    /// <summary>Selects the matching value.</summary>
    /// <typeparam name="TResult">The result produced by the operation.</typeparam>
    /// <param name="transformer">The transformer.</param>
    /// <returns>The selected value.</returns>
    public IEnumerable<TResult> Select<TResult>(Func<TSaga, TResult> transformer)
    {
        lock (_lock)
            return _indexById.Select(transformer);
    }

    IIndexedSagaProperty<TSaga>? HasIndexFor(Expression<Func<TSaga, bool>> expression)
    {
        if (expression.Body.NodeType == ExpressionType.MemberAccess)
        {
            var propertyInfo = ((MemberExpression)expression.Body).Member as PropertyInfo;

            if (propertyInfo == null)
                return null;

            if (_indices.TryGetValue(propertyInfo.Name, out IIndexedSagaProperty<TSaga>? result))
                return result;
        }

        return null;
    }

    void BuildIndices()
    {
        IEnumerable<PropertyInfo> indexProperties = typeof(TSaga).GetProperties()
            .Where(x => x.HasAttribute<IndexedAttribute>() || x.Name.Equals("CorrelationId"));

        foreach (var property in indexProperties)
        {
            var propertyType = typeof(IndexedSagaProperty<,>).MakeGenericType(typeof(TSaga), property.PropertyType);

            var index = Activator.CreateInstance(propertyType, property) as IIndexedSagaProperty<TSaga>
                ?? throw new InvalidOperationException($"Could not create a saga index for '{property.Name}'.");
            _indices.Add(property.Name, index);
        }
    }

    static object? GetRightValue(Expression<Func<TSaga, bool>> right)
    {
        switch (right.Body.NodeType)
        {
            case ExpressionType.Constant:
                return ((ConstantExpression)right.Body).Value;

            default:
                return right.CompileFast().DynamicInvoke(null);
        }
    }
}
