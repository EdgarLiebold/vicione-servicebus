using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using FastExpressionCompiler;
using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Serialization.MessagePackFormatters;
/// <summary>
/// Everything one concrete type needs, compiled once: how to obtain its formatter from a resolver, and
/// how to call that formatter without reflection. The formatter itself is a parameter rather than a
/// captured constant, so one compiled entry serves every resolver and option set.
/// </summary>
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
/// The compiled invokers of one closed interface formatter, one entry per concrete type that has
/// travelled through it.
/// <para>
/// The operational bound is the admitted contract set: one entry per concrete message type this
/// process actually sends through this interface, which is finite because the deployed model is finite,
/// and reset by process restart. That is why no numeric capacity is invented here — a capacity would
/// only decide which live contract to recompile next.
/// </para>
/// <para>
/// The entries are additionally held against weak keys, so this table cannot be the thing that keeps a
/// type alive. That is a statement about this table and nothing more: it is not a promise that a module
/// becomes collectible, because asking a resolver for a type's formatter and compiling any delegate
/// over it both root that type before this cache stores anything. The architecture does not promise
/// in-process unload either; activation changes take effect through controlled restart.
/// </para>
/// <para>
/// Compilation happens exactly once per type even when many threads arrive together. The table may run
/// the creation callback on more than one thread, but only one returned <see cref="Lazy{T}" /> is
/// installed, and the expensive build runs on the installed instance, so a losing thread waits for that
/// one compilation instead of starting a second.
/// </para>
/// <para>
/// The cache is an instance rather than a static member of the formatter, so each owner controls its
/// own lifetime and observes a genuinely cold first use.
/// </para>
/// </summary>
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
    /// The build step is injectable so the table's ownership can be observed independently of
    /// compilation. Compiled delegates and resolved formatters may root generated types in runtime
    /// tables outside this cache.
    /// </summary>
    internal ConcreteFormatterCache(Func<Type, ConcreteFormatterAccess<TInterface>> build)
    {
        _build = build;

        // Held once instead of built per call, so neither the hit nor the miss path allocates a closure.
        _createEntry = CreateEntry;
    }

    /// <summary>
    /// How many entries this cache has actually compiled. Counting entries instead would prove nothing:
    /// a version that rebuilt on every call and replaced the same entry would leave the count at one.
    /// </summary>
    internal int CompiledCount => Volatile.Read(ref _compiled);

    public ConcreteFormatterAccess<TInterface> Get(Type concreteType)
    {
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
        Interlocked.Increment(ref _compiled);

        return _build(concreteType);
    }

    static ConcreteFormatterAccess<TInterface> BuildAccess(Type concreteType)
    {
        var formatterType = typeof(IMessagePackFormatter<>).MakeGenericType(concreteType);

        return new ConcreteFormatterAccess<TInterface>(BuildGetFormatter(concreteType),
            BuildSerialize(concreteType, formatterType), BuildDeserialize(concreteType, formatterType));
    }

    /// <summary>
    /// Replaces a <see cref="System.Reflection.MethodInfo" /> invocation on every single serialize and
    /// deserialize call. The generic method is closed once here and called through a delegate after
    /// that.
    /// </summary>
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
    /// The value parameter is the interface and the cast to the concrete type happens inside the
    /// compiled body. Compiling the delegate over the concrete type and reinterpreting it with
    /// <c>Unsafe.As</c> asserts a conversion the runtime never checks, and it points the wrong way along
    /// the variance of the delegate.
    /// </summary>
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
