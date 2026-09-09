using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Batching;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.Serialization.JsonConverters;

/// <summary>Creates System.Text.Json converters for message contracts and supported dictionary shapes.</summary>
public sealed class SystemTextJsonConverterFactory :
    JsonConverterFactory
{
    static SystemTextJsonConverterFactory()
    {
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(Fault<>), typeof(FaultEvent<>));
        JsonMessageTypeMappingRegistry.RegisterOpenGeneric(typeof(Batch<>), typeof(MessageBatch<>));
        JsonMessageTypeMappingRegistry.Register<Fault, FaultEvent>();
        JsonMessageTypeMappingRegistry.Register<ReceiveFault, ReceiveFaultEvent>();
        JsonMessageTypeMappingRegistry.Register<ExceptionInfo, FaultExceptionInfo>();
        JsonMessageTypeMappingRegistry.Register<HostInfo, BusHostInfo>();
        JsonMessageTypeMappingRegistry.Register<ScheduleMessage, ScheduleMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<ScheduleRecurringMessage, ScheduleRecurringMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<CancelScheduledMessage, CancelScheduledMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<CancelScheduledRecurringMessage, CancelScheduledRecurringMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<PauseScheduledRecurringMessage, PauseScheduledRecurringMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<ResumeScheduledRecurringMessage, ResumeScheduledRecurringMessageCommand>();
        JsonMessageTypeMappingRegistry.Register<MessageEnvelope, JsonMessageEnvelope>();
    }

    /// <summary>Determines whether this factory can convert a type.</summary>
    /// <param name="typeToConvert">The candidate type.</param>
    /// <returns><see langword="true" /> when the factory supports the type; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="typeToConvert" /> is <see langword="null" />.</exception>
    public override bool CanConvert(Type typeToConvert)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);

        if (typeToConvert.IsGenericType)
        {
            if (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IDictionary<,>), out Type[] elementTypes)
                || typeToConvert.TryGetSingleClosedGenericArguments(typeof(IReadOnlyDictionary<,>), out elementTypes)
                || typeToConvert.TryGetSingleClosedGenericArguments(typeof(Dictionary<,>), out elementTypes)
                || (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IEnumerable<>), out Type[] enumerableType)
                    && enumerableType[0].TryGetSingleClosedGenericArguments(typeof(KeyValuePair<,>), out elementTypes)
                    && elementTypes[1] == typeof(object)
                    && !typeToConvert.ClosesGenericType(typeof(IReadOnlyList<>))))
            {
                var keyType = elementTypes[0];

                if (keyType != typeof(string) && keyType != typeof(Uri))
                    return false;

                if (typeToConvert.IsFSharpType())
                    return false;

                return true;
            }
        }

        if (!typeToConvert.IsInterface)
            return false;

        if (JsonMessageTypeMappingRegistry.Contains(typeToConvert))
            return true;

        return IsConvertibleInterfaceType(typeToConvert);
    }

    /// <summary>Creates the converter for a supported type.</summary>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">The active serializer options.</param>
    /// <returns>The converter for <paramref name="typeToConvert" />.</returns>
    /// <exception cref="ArgumentNullException">Either argument is <see langword="null" />.</exception>
    /// <exception cref="ViciOneServiceBusException"><paramref name="typeToConvert" /> is not supported.</exception>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(typeToConvert);
        ArgumentNullException.ThrowIfNull(options);

        if (JsonMessageTypeMappingRegistry.TryCreateConverter(typeToConvert, out JsonConverter? mappedConverter))
            return mappedConverter;

        if (typeToConvert.IsGenericType)
        {
            if (!typeToConvert.IsFSharpType())
            {
                if (typeToConvert == typeof(IDictionary<string, object>))
                    return new CaseInsensitiveDictionaryStringObjectJsonConverter<IDictionary<string, object>>();
                if (typeToConvert == typeof(Dictionary<string, object>))
                    return new CaseInsensitiveDictionaryStringObjectJsonConverter<Dictionary<string, object>>();
                if (typeToConvert == typeof(IReadOnlyDictionary<string, object>))
                    return new CaseInsensitiveDictionaryStringObjectJsonConverter<IReadOnlyDictionary<string, object>>();
                if (typeToConvert == typeof(IEnumerable<KeyValuePair<string, object>>))
                    return new CaseInsensitiveDictionaryStringObjectJsonConverter<IEnumerable<KeyValuePair<string, object>>>();

                if (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IDictionary<,>), out Type[] elementTypes)
                    || typeToConvert.TryGetSingleClosedGenericArguments(typeof(IReadOnlyDictionary<,>), out elementTypes)
                    || typeToConvert.TryGetSingleClosedGenericArguments(typeof(Dictionary<,>), out elementTypes)
                    || (typeToConvert.TryGetSingleClosedGenericArguments(typeof(IEnumerable<>), out Type[] enumerableTypes)
                        && enumerableTypes[0].TryGetSingleClosedGenericArguments(typeof(KeyValuePair<,>), out elementTypes)
                        && elementTypes[1] == typeof(object)))
                {
                    if (elementTypes[0] == typeof(string))
                    {
                        return (JsonConverter)(Activator.CreateInstance(typeof(CaseInsensitiveDictionaryJsonConverter<,>)
                            .MakeGenericType(typeToConvert, elementTypes[1])) ?? throw new InvalidOperationException("The requested runtime type could not be activated."));
                    }

                    if (elementTypes[0] == typeof(Uri))
                    {
                        return (JsonConverter)(Activator.CreateInstance(typeof(UriDictionarySystemTextJsonConverter<,>)
                            .MakeGenericType(typeToConvert, elementTypes[1])) ?? throw new InvalidOperationException("The requested runtime type could not be activated."));
                    }
                }
            }
        }

        if (IsConvertibleInterfaceType(typeToConvert))
        {
            return (JsonConverter)(Activator.CreateInstance(
                typeof(InterfaceJsonConverter<,>).MakeGenericType(typeToConvert, TypeMetadataCache.GetImplementationType(typeToConvert))) ?? throw new InvalidOperationException("The requested runtime type could not be activated."));
        }

        throw new ViciOneServiceBusException($"Unsupported type for json serialization {TypeCache.GetShortName(typeToConvert)}");
    }

    static bool IsConvertibleInterfaceType(Type typeToConvert)
    {
        if (!typeToConvert.IsInterfaceOrConcreteClass())
            return false;

        if (!MessageTypeCache.IsValidMessageType(typeToConvert))
            return false;

        if (typeToConvert.IsValueTypeOrObject())
            return false;

        foreach (var attribute in typeToConvert.GetCustomAttributes())
        {
            switch (attribute.GetType().Name)
            {
                case "JsonDerivedTypeAttribute":
                case "JsonPolymorphicAttribute":
                    return false;
            }
        }

        return true;
    }
}
