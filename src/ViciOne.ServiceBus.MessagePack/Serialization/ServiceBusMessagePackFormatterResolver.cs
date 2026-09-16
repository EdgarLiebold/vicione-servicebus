using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Threading;
using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.MessagePack.Serialization.Formatters;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

/// <summary>Resolves ViciOne interface contracts to their MessagePack wire formatters.</summary>
internal sealed class ServiceBusMessagePackFormatterResolver :
    IFormatterResolver
{
    /// <summary>Gets the shared stateless resolver.</summary>
    public static ServiceBusMessagePackFormatterResolver Instance { get; } = new();

    readonly Dictionary<Type, Type> _mappedNonGenericTypes;
    readonly Dictionary<Type, Type> _mappedGenericTypes;
    readonly ConditionalWeakTable<Type, Lazy<IMessagePackFormatter>> _cachedFormatters;

    ServiceBusMessagePackFormatterResolver()
    {
        _mappedGenericTypes = new Dictionary<Type, Type>
        {
            // This table maps open generic contracts to open generic formatters.
            { typeof(MessageData<>), typeof(MessageDataFormatter<>) },
        };

        _mappedNonGenericTypes = new Dictionary<Type, Type>
        {
            // These contracts require stable concrete wire representations.
            { typeof(Fault), typeof(InterfaceConcreteMapFormatter<Fault, FaultEvent>) },
            { typeof(ReceiveFault), typeof(InterfaceConcreteMapFormatter<ReceiveFault, ReceiveFaultEvent>) },
            { typeof(ExceptionInfo), typeof(InterfaceConcreteMapFormatter<ExceptionInfo, FaultExceptionInfo>) },
            { typeof(HostInfo), typeof(InterfaceConcreteMapFormatter<HostInfo, BusHostInfo>) },
            { typeof(ScheduleMessage), typeof(InterfaceConcreteMapFormatter<ScheduleMessage, ScheduleMessageCommand>) },
            { typeof(ScheduleRecurringMessage), typeof(InterfaceConcreteMapFormatter<ScheduleRecurringMessage, ScheduleRecurringMessageCommand>) },
            { typeof(CancelScheduledMessage), typeof(InterfaceConcreteMapFormatter<CancelScheduledMessage, CancelScheduledMessageCommand>) },
            {
                typeof(CancelScheduledRecurringMessage),
                typeof(InterfaceConcreteMapFormatter<CancelScheduledRecurringMessage, CancelScheduledRecurringMessageCommand>)
            },
            {
                typeof(PauseScheduledRecurringMessage),
                typeof(InterfaceConcreteMapFormatter<PauseScheduledRecurringMessage, PauseScheduledRecurringMessageCommand>)
            },
            {
                typeof(ResumeScheduledRecurringMessage),
                typeof(InterfaceConcreteMapFormatter<ResumeScheduledRecurringMessage, ResumeScheduledRecurringMessageCommand>)
            },
        };

        _cachedFormatters = [];
    }

    /// <summary>Gets the owned formatter for a contract, or defers unsupported concrete types to the next resolver.</summary>
    /// <typeparam name="T">The requested contract.</typeparam>
    /// <returns>The mapped or interface formatter, or <see langword="null" /> when this resolver does not own the type.</returns>
    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        var contractType = typeof(T);

        if (TryGetMappedType(contractType, out Type? formatterType))
        {
            Lazy<IMessagePackFormatter> mapped = _cachedFormatters.GetValue(
                contractType,
                _ => new Lazy<IMessagePackFormatter>(
                    () => CreateMappedFormatter(formatterType),
                    LazyThreadSafetyMode.ExecutionAndPublication));
            return (IMessagePackFormatter<T>)mapped.Value;
        }

        if (!contractType.IsInterface)
            return null;

        Lazy<IMessagePackFormatter> concrete = _cachedFormatters.GetValue(
            contractType,
            static _ => new Lazy<IMessagePackFormatter>(
                static () => new InterfaceMessagePackFormatter<T>(),
                LazyThreadSafetyMode.ExecutionAndPublication));
        return (IMessagePackFormatter<T>)concrete.Value;
    }

    static IMessagePackFormatter CreateMappedFormatter(Type formatterType)
    {
        return Activator.CreateInstance(formatterType) as IMessagePackFormatter
            ?? throw new InvalidOperationException($"Failed to create an instance of '{formatterType}'.");
    }

    bool TryGetMappedType(Type contractType, [NotNullWhen(true)] out Type? formatterType)
    {
        return !contractType.IsGenericType || contractType.IsGenericTypeDefinition
            ? TryGetNonGenericMappedType(contractType, out formatterType)
            : TryGetOpenGenericMappedType(contractType, out formatterType);
    }

    bool TryGetNonGenericMappedType(Type contractType, out Type? formatterType)
    {
        return _mappedNonGenericTypes.TryGetValue(contractType, out formatterType);
    }

    bool TryGetOpenGenericMappedType(Type contractType, out Type? formatterType)
    {
        var genericTypeDefinition = contractType.GetGenericTypeDefinition();
        if (!_mappedGenericTypes.TryGetValue(genericTypeDefinition, out Type? openGenericMappedType))
        {
            formatterType = null;
            return false;
        }

        formatterType = openGenericMappedType.MakeGenericType(contractType.GenericTypeArguments);
        return true;
    }
}
