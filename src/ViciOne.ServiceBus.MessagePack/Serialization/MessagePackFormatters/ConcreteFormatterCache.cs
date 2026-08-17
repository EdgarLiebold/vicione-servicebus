namespace ViciOne.ServiceBus.Serialization.MessagePackFormatters;

using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Threading;
using Internals;
using MessagePack;
using MessagePack.Formatters;


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
/// The previous version was a static <c>ConcurrentDictionary&lt;Type, Delegate&gt;</c> justified by the
/// claim that concrete types are a closed set. That is not a bound on this platform: modules can
/// introduce contract types at runtime, and a strong key would then hold every such type, and the
/// assembly behind it, alive for the life of the process. The entries are held against weak keys
/// instead, so an entry can never outlive the type it describes. That is a lifecycle bound rather than
/// a number, which is why no capacity is invented here: a capacity would only decide which live type to
/// recompile next.
/// </para>
/// <para>
/// This bounds what the cache itself holds; it does not make a runtime generated contract type
/// collectible again. Asking a resolver for that type's formatter already roots it, and so does
/// compiling any delegate over it, both before this cache stores anything. That is measured in the
/// specs and reported, not claimed away.
/// </para>
/// <para>
/// Compilation happens exactly once per type even when many threads arrive together. The entry is a
/// <see cref="Lazy{T}" /> published under the table's own lock and executed on the instance that won,
/// so a losing thread waits for that one compilation rather than starting a second.
/// </para>
/// <para>
/// The cache is an instance rather than a static member of the formatter, so a first use can be a first
/// use: the concurrency spec builds its own and is genuinely cold, instead of measuring state that an
/// earlier test in the same run had already warmed.
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
    /// The build step is a parameter so that what this table holds can be observed without compiling
    /// for it. Compiling a delegate over a runtime generated type roots that type in the runtime's own
    /// tables, and so does asking a resolver for its formatter, so a test that did either could never
    /// see whether this table itself lets go of a key.
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
    /// compiled body. The previous version compiled the delegate over the concrete type and then
    /// reinterpreted it with <c>Unsafe.As</c>, which asserted a conversion the runtime never checked and
    /// which pointed the wrong way along the variance of the delegate.
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
