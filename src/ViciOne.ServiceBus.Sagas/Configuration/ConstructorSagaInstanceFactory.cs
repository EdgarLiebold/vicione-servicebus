using System;
using System.Linq.Expressions;
using FastExpressionCompiler;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Saga;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Creates a saga instance through one cached compiled constructor delegate.</summary>
/// <typeparam name="TSaga">The saga state managed by the member.</typeparam>
internal sealed class ConstructorSagaInstanceFactory<TSaga>
    where TSaga : class, ISaga
{
    public ConstructorSagaInstanceFactory()
    {
        if (!typeof(TSaga).IsClass || typeof(TSaga).IsAbstract)
            throw new ArgumentException($"The saga must be a concrete class: {TypeCache<TSaga>.ShortName}");

        var constructorInfo = typeof(TSaga).GetConstructor([typeof(Guid)]);
        if (constructorInfo == null)
        {
            throw new ArgumentException("The saga does not have a public constructor with a single Guid correlationId parameter: "
                + TypeCache<TSaga>.ShortName);
        }

        var correlationId = Expression.Parameter(typeof(Guid), "correlationId");
        var @new = Expression.New(constructorInfo, correlationId);

        FactoryMethod = Expression.Lambda<SagaInstanceFactoryMethod<TSaga>>(@new, correlationId).CompileFast();
    }

    public SagaInstanceFactoryMethod<TSaga> FactoryMethod { get; }
}
