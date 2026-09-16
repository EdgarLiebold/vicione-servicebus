using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.TypeConverters;

/// <summary>Provides cached access to built-in converters and weakly owns converters composed for runtime types.</summary>
public static class TypeConverterCache
{
    static readonly List<object> _builtInConverters;
    static readonly object _lock = new();
    static readonly ConditionalWeakTable<Type, CachedConverter> _typeConverters;

    static TypeConverterCache()
    {
        _typeConverters = new ConditionalWeakTable<Type, CachedConverter>();
        _builtInConverters = new List<object>();

        AddBuiltInSupportedTypes(typeof(BooleanTypeConverter));
        AddBuiltInSupportedTypes(typeof(ByteTypeConverter));
        AddBuiltInSupportedTypes(typeof(DateTimeOffsetTypeConverter));
        AddBuiltInSupportedTypes(typeof(DateTimeTypeConverter));
        AddBuiltInSupportedTypes(typeof(DecimalTypeConverter));
        AddBuiltInSupportedTypes(typeof(DoubleTypeConverter));
        AddBuiltInSupportedTypes(typeof(ExceptionTypeConverter));
        AddBuiltInSupportedTypes(typeof(GuidTypeConverter));
        AddBuiltInSupportedTypes(typeof(IntTypeConverter));
        AddBuiltInSupportedTypes(typeof(LongTypeConverter));
        AddBuiltInSupportedTypes(typeof(ShortTypeConverter));
        AddBuiltInSupportedTypes(typeof(StringTypeConverter));
        AddBuiltInSupportedTypes(typeof(TimeSpanTypeConverter));
        AddBuiltInSupportedTypes(typeof(UriTypeConverter));
        AddBuiltInSupportedTypes(typeof(VersionTypeConverter));
    }

    static bool TryGetTypeConverterCore<TProperty, TInput>([NotNullWhen(true)] out ITypeConverter<TProperty, TInput>? typeConverter)
    {
        typeConverter = GetOrCreateConverter(typeof(TProperty), typeof(TInput)) as ITypeConverter<TProperty, TInput>;
        return typeConverter != null;
    }

    static object? GetOrCreateConverter(Type propertyType, Type inputType)
    {
        Type neededType = typeof(ITypeConverter<,>).MakeGenericType(propertyType, inputType);
        if (_typeConverters.TryGetValue(neededType, out CachedConverter? cached))
            return cached.Value;

        lock (_lock)
        {
            if (_typeConverters.TryGetValue(neededType, out cached))
                return cached.Value;

            object? converter = CreateConverterIfSupported(neededType, propertyType, inputType);
            if (converter != null)
                CacheDeclaredContracts(converter);
            CacheConverter(neededType, converter);
            return converter;
        }
    }

    static object? CreateConverterIfSupported(Type neededType, Type propertyType, Type inputType)
    {
        if (propertyType == typeof(string) && typeof(INamedInitializerValue).IsAssignableFrom(inputType))
        {
            var namedValueConverterType = typeof(NamedInitializerValueTypeConverter<>).MakeGenericType(inputType);
            if (namedValueConverterType.ImplementsInterface(neededType))
                return Activator.CreateInstance(namedValueConverterType)
                    ?? throw new InvalidOperationException($"The converter type '{namedValueConverterType}' could not be activated.");
        }

        object? matched = _builtInConverters.FirstOrDefault(x => x.GetType().ImplementsInterface(neededType));
        if (matched != null)
            return matched;

        if (propertyType.IsEnum)
        {
            var enumConverterType = typeof(EnumTypeConverter<>).MakeGenericType(propertyType);
            if (enumConverterType.ImplementsInterface(neededType))
                return Activator.CreateInstance(enumConverterType)
                    ?? throw new InvalidOperationException($"The converter type '{enumConverterType}' could not be activated.");

            return null;
        }

        if (propertyType.IsNullable(out Type? resultType))
            return CreateNullableResultConverter(resultType, inputType);

        if (inputType.IsNullable(out Type? sourceType))
            return CreateNullableSourceConverter(propertyType, sourceType);

        return null;
    }

    static object? CreateNullableResultConverter(Type resultType, Type inputType)
    {
        if (resultType == inputType)
        {
            var directNullableType = typeof(ToNullableTypeConverter<>).MakeGenericType(resultType);
            return Activator.CreateInstance(directNullableType)
                ?? throw new InvalidOperationException($"The converter type '{directNullableType}' could not be activated.");
        }

        object? converter = GetOrCreateConverter(resultType, inputType);
        if (converter == null)
            return null;

        var convertedNullableType = typeof(ToNullableTypeConverter<,>).MakeGenericType(resultType, inputType);
        return Activator.CreateInstance(convertedNullableType, converter)
            ?? throw new InvalidOperationException($"The converter type '{convertedNullableType}' could not be activated.");
    }

    static object? CreateNullableSourceConverter(Type propertyType, Type sourceType)
    {
        if (sourceType == propertyType)
        {
            var directNullableType = typeof(FromNullableTypeConverter<>).MakeGenericType(sourceType);
            return Activator.CreateInstance(directNullableType)
                ?? throw new InvalidOperationException($"The converter type '{directNullableType}' could not be activated.");
        }

        object? converter = GetOrCreateConverter(propertyType, sourceType);
        if (converter == null)
            return null;

        var convertedNullableType = typeof(FromNullableTypeConverter<,>).MakeGenericType(propertyType, sourceType);
        return Activator.CreateInstance(convertedNullableType, converter)
            ?? throw new InvalidOperationException($"The converter type '{convertedNullableType}' could not be activated.");
    }

    static void AddBuiltInSupportedTypes(Type converterType, params object[] args)
    {
        Type[] interfaceTypes = converterType.GetInterfaces();

        Type[] types = interfaceTypes.Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITypeConverter<,>)).ToArray();
        if (types.Length > 0)
        {
            var converter = Activator.CreateInstance(converterType, args)
                ?? throw new InvalidOperationException($"The converter type '{converterType}' could not be activated.");

            _builtInConverters.Add(converter);

            foreach (Type type in types)
                CacheConverter(type, converter);
        }
    }

    static void CacheDeclaredContracts(object converter)
    {
        Type[] types = converter.GetType().GetInterfaces()
            .Where(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(ITypeConverter<,>))
            .ToArray();

        foreach (Type type in types)
            CacheConverter(type, converter);
    }

    static void CacheConverter(Type contractType, object? converter)
    {
        if (!_typeConverters.TryGetValue(contractType, out _))
            _typeConverters.Add(contractType, new CachedConverter(converter));
    }


    /// <summary>Stores a supported converter or a negative lookup behind its weak contract key.</summary>
    sealed class CachedConverter(object? value)
    {
        public object? Value { get; } = value;
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
