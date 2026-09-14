using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Provides cached access to the built-in type converters.</summary>
public static class TypeConverterCache
{
    static readonly List<object> _converters;
    static readonly object _lock = new();
    static readonly ConcurrentDictionary<Type, object> _typeConverters;

    static TypeConverterCache()
    {
        _typeConverters = new ConcurrentDictionary<Type, object>();
        _converters = new List<object>();

        AddSupportedTypes(typeof(BooleanTypeConverter));
        AddSupportedTypes(typeof(ByteTypeConverter));
        AddSupportedTypes(typeof(DateTimeOffsetTypeConverter));
        AddSupportedTypes(typeof(DateTimeTypeConverter));
        AddSupportedTypes(typeof(DecimalTypeConverter));
        AddSupportedTypes(typeof(DoubleTypeConverter));
        AddSupportedTypes(typeof(ExceptionTypeConverter));
        AddSupportedTypes(typeof(GuidTypeConverter));
        AddSupportedTypes(typeof(IntTypeConverter));
        AddSupportedTypes(typeof(LongTypeConverter));
        AddSupportedTypes(typeof(ShortTypeConverter));
        AddSupportedTypes(typeof(StringTypeConverter));
        AddSupportedTypes(typeof(TimeSpanTypeConverter));
        AddSupportedTypes(typeof(UriTypeConverter));
        AddSupportedTypes(typeof(VersionTypeConverter));
    }

    static bool TryGetTypeConverterCore<TProperty, TInput>([NotNullWhen(true)] out ITypeConverter<TProperty, TInput>? typeConverter)
    {
        var neededType = typeof(ITypeConverter<TProperty, TInput>);

        if (TryGetCachedConverter(neededType, out typeConverter))
            return true;

        lock (_lock)
        {
            if (TryGetCachedConverter(neededType, out typeConverter))
                return true;

            AddConverterIfSupported(neededType, typeof(TProperty), typeof(TInput));

            return TryGetCachedConverter(neededType, out typeConverter);
        }
    }

    static bool TryGetCachedConverter<TProperty, TInput>(Type neededType,
        [NotNullWhen(true)] out ITypeConverter<TProperty, TInput>? typeConverter)
    {
        if (_typeConverters.TryGetValue(neededType, out object? converter))
        {
            typeConverter = converter as ITypeConverter<TProperty, TInput>;
            return typeConverter != null;
        }

        typeConverter = null;
        return false;
    }

    static void AddConverterIfSupported(Type neededType, Type propertyType, Type inputType)
    {
        if (propertyType == typeof(string) && typeof(INamedInitializerValue).IsAssignableFrom(inputType))
        {
            var namedValueConverterType = typeof(NamedInitializerValueTypeConverter<>).MakeGenericType(inputType);
            AddSupportedTypes(namedValueConverterType);

            if (_typeConverters.ContainsKey(neededType))
                return;
        }

        object? matched = _converters.FirstOrDefault(x => x.GetType().ImplementsInterface(neededType));
        if (matched != null)
        {
            _typeConverters.GetOrAdd(neededType, matched);
            return;
        }

        if (propertyType.IsEnum)
        {
            var enumConverterType = typeof(EnumTypeConverter<>).MakeGenericType(propertyType);
            if (enumConverterType.ImplementsInterface(neededType))
                AddSupportedTypes(enumConverterType);

            return;
        }

        if (propertyType.IsNullable(out Type? resultType))
        {
            AddNullableResultConverterIfSupported(resultType, inputType);
            return;
        }

        if (inputType.IsNullable(out Type? sourceType))
            AddNullableSourceConverterIfSupported(propertyType, sourceType);
    }

    static void AddNullableResultConverterIfSupported(Type resultType, Type inputType)
    {
        if (resultType == inputType)
        {
            var nullableType = typeof(ToNullableTypeConverter<>).MakeGenericType(resultType);
            AddSupportedTypes(nullableType);
            return;
        }

        var converterType = typeof(ITypeConverter<,>).MakeGenericType(resultType, inputType);
        AddEnumConverterIfSupported(resultType, converterType);
        if (_typeConverters.TryGetValue(converterType, out object? converter))
        {
            var nullableType = typeof(ToNullableTypeConverter<,>).MakeGenericType(resultType, inputType);
            AddSupportedTypes(nullableType, converter);
        }
    }

    static void AddNullableSourceConverterIfSupported(Type propertyType, Type sourceType)
    {
        if (sourceType == propertyType)
        {
            var nullableType = typeof(FromNullableTypeConverter<>).MakeGenericType(sourceType);
            AddSupportedTypes(nullableType);
            return;
        }

        var converterType = typeof(ITypeConverter<,>).MakeGenericType(propertyType, sourceType);
        if (_typeConverters.TryGetValue(converterType, out object? converter))
        {
            var nullableType = typeof(FromNullableTypeConverter<,>).MakeGenericType(propertyType, sourceType);
            AddSupportedTypes(nullableType, converter);
        }
    }

    static void AddSupportedTypes(Type converterType, params object[] args)
    {
        Type[] interfaceTypes = converterType.GetInterfaces();

        Type[] types = interfaceTypes.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITypeConverter<,>)).ToArray();
        if (types.Length > 0)
        {
            var converter = Activator.CreateInstance(converterType, args)
                ?? throw new InvalidOperationException($"The converter type '{converterType}' could not be activated.");

            _converters.Add(converter);

            foreach (var type in types)
                _typeConverters[type] = converter;
        }
    }

    static void AddEnumConverterIfSupported(Type resultType, Type converterContract)
    {
        if (_typeConverters.ContainsKey(converterContract) || !resultType.IsEnum)
            return;

        Type enumConverterType = typeof(EnumTypeConverter<>).MakeGenericType(resultType);
        if (enumConverterType.ImplementsInterface(converterContract))
            AddSupportedTypes(enumConverterType);
    }

    /// <summary>Attempts to resolve the shared converter for a source and result type pair.</summary>
    /// <typeparam name="TProperty">The converted result type.</typeparam>
    /// <typeparam name="TInputProperty">The source value type.</typeparam>
    /// <param name="typeConverter">The shared converter when the pair is supported.</param>
    /// <returns><see langword="true" /> when a converter is available; otherwise, <see langword="false" />.</returns>
    public static bool TryGetTypeConverter<TProperty, TInputProperty>(
        [NotNullWhen(true)] out ITypeConverter<TProperty, TInputProperty>? typeConverter)
    {
        return TryGetTypeConverterCore(out typeConverter);
    }
}
