using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using FastExpressionCompiler;
using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.MessagePack.Serialization.Formatters;

/// <summary>
/// Caches lazily compiled formatter delegates by concrete implementation type for one closed interface
/// formatter. Weak keys prevent this table from retaining a concrete type on its own, and
/// <see cref="Lazy{T}" /> ensures the installed entry is compiled once under concurrent access.
/// </summary>
/// <typeparam name="TContract">The interface contract.</typeparam>
sealed class ConcreteFormatterInvokerCache<TContract>
{
    readonly Func<Type, ConcreteFormatterInvoker<TContract>> _build;
    readonly ConditionalWeakTable<Type, Lazy<ConcreteFormatterInvoker<TContract>>>.CreateValueCallback _createEntry;
    readonly ConditionalWeakTable<Type, Lazy<ConcreteFormatterInvoker<TContract>>> _entries = [];

    public ConcreteFormatterInvokerCache()
        : this(BuildAccess)
    {
    }

    /// <summary>
    /// Initializes the cache with the delegate factory used for each concrete type.
    /// </summary>
    /// <param name="build">The factory that compiles formatter access for a concrete type.</param>
    internal ConcreteFormatterInvokerCache(Func<Type, ConcreteFormatterInvoker<TContract>> build)
    {
        _build = build;

        // Reusing the callback avoids allocating a closure on cache hits and misses.
        _createEntry = CreateEntry;
    }

    public ConcreteFormatterInvoker<TContract> Get(Type concreteType)
    {
        ArgumentNullException.ThrowIfNull(concreteType);
        return _entries.TryGetValue(concreteType, out var entry)
            ? entry.Value
            : _entries.GetValue(concreteType, _createEntry).Value;
    }

    Lazy<ConcreteFormatterInvoker<TContract>> CreateEntry(Type concreteType)
    {
        return new Lazy<ConcreteFormatterInvoker<TContract>>(() => _build(concreteType),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    static ConcreteFormatterInvoker<TContract> BuildAccess(Type concreteType)
    {
        var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(concreteType);

        return new ConcreteFormatterInvoker<TContract>(BuildGetFormatter(concreteType),
            BuildSerialize(concreteType, formatterType), BuildDeserialize(formatterType));
    }

    /// <summary>
    /// Compiles a delegate that obtains the concrete formatter from an arbitrary resolver.
    /// </summary>
    /// <param name="concreteType">The concrete message implementation type.</param>
    /// <returns>A delegate that resolves that type's formatter.</returns>
    static Func<IFormatterResolver, object> BuildGetFormatter(Type concreteType)
    {
        var getFormatter = typeof(IFormatterResolver)
            .GetMethod(nameof(IFormatterResolver.GetFormatter))!
            .MakeGenericMethod(concreteType);

        var resolver = Expression.Parameter(typeof(IFormatterResolver), "resolver");

        return Expression
            .Lambda<Func<IFormatterResolver, object>>(
                Expression.Convert(Expression.Call(resolver, getFormatter), typeof(object)), resolver)
            .CompileFast();
    }

    /// <summary>
    /// Compiles a serializer delegate that accepts the interface value and casts it to the concrete
    /// implementation before invoking the concrete formatter.
    /// </summary>
    /// <param name="concreteType">The concrete message implementation type.</param>
    /// <param name="formatterType">The closed MessagePack formatter type.</param>
    /// <returns>The compiled serialization delegate.</returns>
    static ConcreteFormatterInvoker<TContract>.SerializeValue BuildSerialize(Type concreteType, Type formatterType)
    {
        var serialize = formatterType.GetMethod(nameof(IMessagePackFormatter<>.Serialize))!;

        var formatter = Expression.Parameter(typeof(object), "formatter");
        var writer = Expression.Parameter(typeof(MessagePackWriter).MakeByRefType(), "writer");
        var value = Expression.Parameter(typeof(TContract), "value");
        var options = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

        var call = Expression.Call(Expression.Convert(formatter, formatterType), serialize, writer,
            Expression.Convert(value, concreteType), options);

        return Expression
            .Lambda<ConcreteFormatterInvoker<TContract>.SerializeValue>(call, formatter, writer, value, options)
            .CompileFast();
    }

    static ConcreteFormatterInvoker<TContract>.DeserializeValue BuildDeserialize(Type formatterType)
    {
        var deserialize = formatterType.GetMethod(nameof(IMessagePackFormatter<>.Deserialize))!;

        var formatter = Expression.Parameter(typeof(object), "formatter");
        var reader = Expression.Parameter(typeof(MessagePackReader).MakeByRefType(), "reader");
        var options = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

        var call = Expression.Call(Expression.Convert(formatter, formatterType), deserialize, reader, options);

        return Expression
            .Lambda<ConcreteFormatterInvoker<TContract>.DeserializeValue>(Expression.Convert(call, typeof(TContract)), formatter,
                reader, options)
            .CompileFast();
    }
}
