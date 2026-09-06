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

/// <summary>
/// Provides an indexed saga dictionary implementation.
/// </summary>
/// <typeparam name="TSaga">The t saga type.</typeparam>
public class IndexedSagaDictionary<TSaga>
    where TSaga : class, ISaga
{
    readonly IIndexedSagaProperty<TSaga> _indexById;
    readonly Dictionary<string, IIndexedSagaProperty<TSaga>> _indices;
    readonly SemaphoreSlim _inUse = new SemaphoreSlim(1);
    readonly object _lock = new object();

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public IndexedSagaDictionary()
    {
        _indices = new Dictionary<string, IIndexedSagaProperty<TSaga>>();

        BuildIndices();

        _indexById = _indices["CorrelationId"];
    }

    /// <summary>
    /// Gets or sets the value at the specified index.
    /// </summary>
    /// <param name="sagaId">The saga id value.</param>
    public SagaInstance<TSaga>? this[Guid sagaId]
    {
        get
        {
            lock (_lock)
                return _indexById[sagaId];
        }
    }

    /// <summary>
    /// Gets the count value.
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
                return _indexById.Count;
        }
    }

    /// <summary>
    /// Performs the mark in use operation.
    /// </summary>
    /// <param name="cancellationToken">The token used to cancel the operation.</param>
    /// <returns>The result of the operation.</returns>
    public Task MarkInUseAsync(CancellationToken cancellationToken)
    {
        return _inUse.WaitAsync(cancellationToken);
    }

    /// <summary>
    /// Performs the release operation.
    /// </summary>
    public void Release()
    {
        _inUse.Release();
    }

    /// <summary>
    /// Performs the add operation.
    /// </summary>
    /// <param name="instance">The instance value.</param>
    public void Add(SagaInstance<TSaga> instance)
    {
        lock (_lock)
        {
            foreach (IIndexedSagaProperty<TSaga> index in _indices.Values)
                index.Add(instance);
        }
    }

    /// <summary>
    /// Performs the remove operation.
    /// </summary>
    /// <param name="item">The item value.</param>
    public void Remove(SagaInstance<TSaga> item)
    {
        lock (_lock)
        {
            foreach (IIndexedSagaProperty<TSaga> index in _indices.Values)
                index.Remove(item);
        }
    }

    /// <summary>
    /// Performs the where operation.
    /// </summary>
    /// <param name="query">The query value.</param>
    /// <returns>The result of the operation.</returns>
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

    /// <summary>
    /// Performs the select operation.
    /// </summary>
    /// <typeparam name="TResult">The t result type.</typeparam>
    /// <param name="transformer">The transformer value.</param>
    /// <returns>The result of the operation.</returns>
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
