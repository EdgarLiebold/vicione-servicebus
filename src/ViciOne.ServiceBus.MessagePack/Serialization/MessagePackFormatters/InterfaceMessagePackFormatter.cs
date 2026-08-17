namespace ViciOne.ServiceBus.Serialization.MessagePackFormatters;

using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Internals;
using MessagePack;
using MessagePack.Formatters;
using Metadata;


delegate void SerializeDelegate<in TConcrete>(object formatter, ref MessagePackWriter writer, TConcrete value,
    MessagePackSerializerOptions options);


delegate TConcrete DeserializeDelegate<out TConcrete>(object formatter, ref MessagePackReader reader,
    MessagePackSerializerOptions options);


/// <summary>
/// Serializes an interface typed message through the formatter of its concrete type.
/// <para>
/// The invoker used to be built and compiled on every single serialize and deserialize call, so every
/// message of every type paid for an expression tree and a compilation. The invokers are cached now,
/// keyed by the concrete type alone: the formatter instance is passed in as an argument rather than
/// captured as a constant, so one compiled invoker serves every resolver and the cache cannot grow
/// with the number of option sets.
/// </para>
/// </summary>
public class InterfaceMessagePackFormatter<TInterface> :
    IMessagePackFormatter<TInterface>
{
    static readonly FormatterProxyInfo _formatterProxyInfo;
    static readonly Lazy<DeserializeDelegate<TInterface>> _deserializeInvoker;

    // One entry per concrete type ever serialized through this interface. Concrete types are a closed
    // set at runtime, so this is bounded by the model rather than by traffic.
    static readonly ConcurrentDictionary<Type, Delegate> _serializeInvokers = new();

    static InterfaceMessagePackFormatter()
    {
        var proxyType = TypeMetadataCache.GetImplementationType(typeof(TInterface));
        _formatterProxyInfo = GetFormatterProxyInfoFromType(proxyType);
        _deserializeInvoker = new Lazy<DeserializeDelegate<TInterface>>(
            () => BuildDeserializeInvoker(_formatterProxyInfo), true);
    }

    static int _compiledInvokerCount;

    /// <summary>
    /// How many invokers this closed formatter has actually compiled. Counting cache entries instead
    /// would prove nothing: a version that rebuilt on every call and overwrote the same key would leave
    /// the entry count at one and keep such a test green.
    /// </summary>
    internal static int CompiledInvokerCount => Volatile.Read(ref _compiledInvokerCount);

    public void Serialize(ref MessagePackWriter writer, TInterface value, MessagePackSerializerOptions options)
    {
        var typeOfValue = value?.GetType();

        // If the value is not null and not an interface, use the formatter for the concrete type.
        var formatterProxyInfoToUse = typeOfValue != null && !typeOfValue.IsInterface
            ? GetFormatterProxyInfoFromType(typeOfValue)
            : _formatterProxyInfo;

        // IMessagePackFormatter of unknown type
        var formatter = formatterProxyInfoToUse.GetFormatterMethodInfo.Invoke(options.Resolver, BindingFlags.Default, null, null, null);

        var invoker = _serializeInvokers.GetOrAdd(formatterProxyInfoToUse.TargetType,
            _ => BuildSerializeInvoker(formatterProxyInfoToUse));

        var proxyFunc = Unsafe.As<SerializeDelegate<TInterface>>(invoker);

        proxyFunc(formatter!, ref writer, value!, options);
    }

    public TInterface Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
    {
        var formatter = _formatterProxyInfo.GetFormatterMethodInfo.Invoke(options.Resolver, BindingFlags.Default, null, null, null);

        return _deserializeInvoker.Value(formatter!, ref reader, options);
    }

    static Delegate BuildSerializeInvoker(FormatterProxyInfo proxyInfo)
    {
        Interlocked.Increment(ref _compiledInvokerCount);

        var formatterParameter = Expression.Parameter(typeof(object), "formatter");
        var writerParameter = Expression.Parameter(typeof(MessagePackWriter).MakeByRefType(), "writer");
        var valueParameter = Expression.Parameter(proxyInfo.TargetType, "value");
        var optionsParameter = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

        var call = Expression.Call(Expression.Convert(formatterParameter, proxyInfo.FormatterType),
            proxyInfo.SerializeMethodInfo, writerParameter, valueParameter, optionsParameter);

        var delegateType = typeof(SerializeDelegate<>).MakeGenericType(proxyInfo.TargetType);

        return Expression
            .Lambda(delegateType, call, formatterParameter, writerParameter, valueParameter, optionsParameter)
            .CompileFast();
    }

    static DeserializeDelegate<TInterface> BuildDeserializeInvoker(FormatterProxyInfo proxyInfo)
    {
        Interlocked.Increment(ref _compiledInvokerCount);

        var formatterParameter = Expression.Parameter(typeof(object), "formatter");
        var readerParameter = Expression.Parameter(typeof(MessagePackReader).MakeByRefType(), "reader");
        var optionsParameter = Expression.Parameter(typeof(MessagePackSerializerOptions), "options");

        var call = Expression.Call(Expression.Convert(formatterParameter, proxyInfo.FormatterType),
            proxyInfo.DeserializeMethodInfo, readerParameter, optionsParameter);

        return Expression
            .Lambda<DeserializeDelegate<TInterface>>(call, formatterParameter, readerParameter, optionsParameter)
            .CompileFast();
    }

    static FormatterProxyInfo GetFormatterProxyInfoFromType(Type targetType)
    {
        // The null-forgiving operator (!) is used because the methods are guaranteed to exist,
        // during normal operation. In case it doesn't, we likely won't even get here.

        var getFormatterMethodInfo = typeof(IFormatterResolver)
            .GetMethod(nameof(IFormatterResolver.GetFormatter))!
            .MakeGenericMethod(targetType);

        var formatterType = typeof(IMessagePackFormatter<>)
            .MakeGenericType(targetType);

        var serializeMethodInfo = formatterType
            .GetMethod(nameof(IMessagePackFormatter<object>.Serialize))!;

        var deserializeMethodInfo = formatterType
            .GetMethod(nameof(IMessagePackFormatter<object>.Deserialize))!;

        return new FormatterProxyInfo
        {
            TargetType = targetType,
            FormatterType = formatterType,
            GetFormatterMethodInfo = getFormatterMethodInfo,
            SerializeMethodInfo = serializeMethodInfo,
            DeserializeMethodInfo = deserializeMethodInfo
        };
    }


    struct FormatterProxyInfo
    {
        public Type TargetType { get; set; }
        public Type FormatterType { get; set; }
        public MethodInfo GetFormatterMethodInfo { get; set; }
        public MethodInfo SerializeMethodInfo { get; set; }
        public MethodInfo DeserializeMethodInfo { get; set; }
    }
}
