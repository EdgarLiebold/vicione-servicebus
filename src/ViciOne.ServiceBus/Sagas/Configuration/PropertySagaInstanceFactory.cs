using System;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Creates a saga instance through one cached compiled constructor-and-property delegate.
/// </summary>
/// <typeparam name="TSaga"></typeparam>
internal sealed class PropertySagaInstanceFactory<TSaga>
    where TSaga : class, ISaga
{
    public PropertySagaInstanceFactory()
    {
        var constructorInfo = typeof(TSaga).GetConstructor(Type.EmptyTypes);
        if (constructorInfo == null)
            throw new ArgumentException($"The saga {TypeCache<TSaga>.ShortName} does not have a default public constructor");

        if (!TypeCache<TSaga>.ReadWritePropertyCache.TryGetValue(nameof(ISaga.CorrelationId), out ReadWriteProperty<TSaga> property))
            throw new ArgumentException($"The saga {TypeCache<TSaga>.ShortName} does not have a writable CorrelationId property");

        var correlationId = Expression.Parameter(typeof(Guid), "correlationId");
        MemberInitExpression newSaga = Expression.MemberInit(
            Expression.New(constructorInfo),
            Expression.Bind(property.Property, correlationId));

        FactoryMethod = Expression.Lambda<SagaInstanceFactoryMethod<TSaga>>(newSaga, correlationId).CompileFast();
    }

    public SagaInstanceFactoryMethod<TSaga> FactoryMethod { get; }
}
