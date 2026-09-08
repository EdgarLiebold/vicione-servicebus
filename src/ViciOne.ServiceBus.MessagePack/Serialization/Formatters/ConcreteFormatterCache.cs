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
/// Holds compiled delegates that resolve and invoke the formatter for one concrete implementation type.
/// </summary>
/// <typeparam name="TInterface">The interface type.</typeparam>
sealed class ConcreteFormatterAccess<TInterface>
{
    public ConcreteFormatterAccess(Func<IFormatterResolver, object> getFormatter,
        SerializeDelegate<TInterface> serialize, DeserializeDelegate<TInterface> deserialize)
    {
        GetFormatter = getFormatter;
        Serialize = serialize;
        Deserialize = deserialize;
    }

    public Func<IFormatterResolver, object> GetFormatter { get; }
    public SerializeDelegate<TInterface> Serialize { get; }
    public DeserializeDelegate<TInterface> Deserialize { get; }
}


/// <summary>
/// Caches lazily compiled formatter delegates by concrete implementation type for one closed interface
/// formatter. Weak keys prevent this table from retaining a concrete type on their own, and
/// <see cref="Lazy{T}" /> ensures the installed entry is compiled once under concurrent access.
/// </summary>
/// <typeparam name="TInterface">The interface type.</typeparam>
sealed class ConcreteFormatterCache<TInterface>
{
    readonly Func<Type, ConcreteFormatterAccess<TInterface>> _build;
    readonly ConditionalWeakTable<Type, Lazy<ConcreteFormatterAccess<TInterface>>>.CreateValueCallback _createEntry;
    readonly ConditionalWeakTable<Type, Lazy<ConcreteFormatterAccess<TInterface>>> _entries = new();
    int _compiled;

    public ConcreteFormatterCache()
        : this(BuildAccess)
    {
    }

    /// <summary>
    /// Initializes the cache with the delegate factory used for each concrete type.
    /// </summary>
    /// <param name="build">The factory that compiles formatter access for a concrete type.</param>
    internal ConcreteFormatterCache(Func<Type, ConcreteFormatterAccess<TInterface>> build)
    {
        _build = build;

        // Reusing the callback avoids allocating a closure on cache hits and misses.
        _createEntry = CreateEntry;
    }

    /// <summary>
    /// Gets the number of concrete formatter entries compiled by this cache instance.
    /// </summary>
    internal int CompiledCount => Volatile.Read(ref _compiled);

    public ConcreteFormatterAccess<TInterface> Get(Type concreteType)
    {
        ArgumentNullException.ThrowIfNull(concreteType);
        return _entries.TryGetValue(concreteType, out var entry)
            ? entry.Value
            : _entries.GetValue(concreteType, _createEntry).Value;
    }

    Lazy<ConcreteFormatterAccess<TInterface>> CreateEntry(Type concreteType)
    {
        return new Lazy<ConcreteFormatterAccess<TInterface>>(() => Build(concreteType),
            LazyThreadSafetyMode.ExecutionAndPublication);
    }

    ConcreteFormatterAccess<TInterface> Build(Type concreteType)
    {
        ConcreteFormatterAccess<TInterface> access = _build(concreteType);
        Interlocked.Increment(ref _compiled);

        return access;
    }

    static ConcreteFormatterAccess<TInterface> BuildAccess(Type concreteType)
    {
        var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(concreteType);

        return new ConcreteFormatterAccess<TInterface>(BuildGetFormatter(concreteType),
            BuildSerialize(concreteType, formatterType), BuildDeserialize(concreteType, formatterType));
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
    static SerializeDelegate<TInterface> BuildSerialize(Type concreteType, Type formatterType)
    {
        var serialize = formatterType.GetMethod(nameof(IMessagePackFormatter<object>.Serialize))!;

        var formatter = Expression.Parameter(typeof(object), "formatter");
        var writer = Expression.Parameter(typeof(MessagePackWriter).MakeByRefType(), "writer");
        var value = Expression.Parameter(typeof(TInterface), "value");
        var options = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

        var call = Expression.Call(Expression.Convert(formatter, formatterType), serialize, writer,
            Expression.Convert(value, concreteType), options);

        return Expression
            .Lambda<SerializeDelegate<TInterface>>(call, formatter, writer, value, options)
            .CompileFast();
    }

    static DeserializeDelegate<TInterface> BuildDeserialize(Type concreteType, Type formatterType)
    {
        var deserialize = formatterType.GetMethod(nameof(IMessagePackFormatter<object>.Deserialize))!;

        var formatter = Expression.Parameter(typeof(object), "formatter");
        var reader = Expression.Parameter(typeof(MessagePackReader).MakeByRefType(), "reader");
        var options = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

        var call = Expression.Call(Expression.Convert(formatter, formatterType), deserialize, reader, options);

        return Expression
            .Lambda<DeserializeDelegate<TInterface>>(Expression.Convert(call, typeof(TInterface)), formatter,
                reader, options)
            .CompileFast();
    }
}
