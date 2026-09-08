using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using MessagePack;
using MessagePack.Formatters;
using ViciOne.ServiceBus.Advanced.Serialization;
using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.MessagePack.Serialization.Formatters;
using ViciOne.ServiceBus.Metadata;
using ViciOne.ServiceBus.Scheduling;

namespace ViciOne.ServiceBus.MessagePack.Serialization;

internal sealed class ServiceBusMessagePackFormatterResolver :
    IFormatterResolver
{
    public static ServiceBusMessagePackFormatterResolver Instance { get; } = new();

    readonly Dictionary<Type, Type> _mappedNonGenericTypes;
    readonly Dictionary<Type, Type> _mappedGenericTypes;
    readonly ConcurrentDictionary<Type, Lazy<IMessagePackFormatter>> _cachedFormatters;

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

        _cachedFormatters = new ConcurrentDictionary<Type, Lazy<IMessagePackFormatter>>();
    }

    public IMessagePackFormatter<T>? GetFormatter<T>()
    {
        var contractType = typeof(T);

        if (TryGetMappedType(contractType, out Type? formatterType))
        {
            Lazy<IMessagePackFormatter> mapped = _cachedFormatters.GetOrAdd(
                contractType,
                static (_, type) => new Lazy<IMessagePackFormatter>(
                    () => CreateMappedFormatter(type),
                    LazyThreadSafetyMode.ExecutionAndPublication),
                formatterType);
            return (IMessagePackFormatter<T>)mapped.Value;
        }

        if (!contractType.IsInterface)
            return null;

        Lazy<IMessagePackFormatter> concrete = _cachedFormatters.GetOrAdd(
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

    bool TryGetMappedType(Type originType, [NotNullWhen(true)] out Type? mappedTargetType)
    {
        return !originType.IsGenericType || originType.IsGenericTypeDefinition
            ? TryGetNonGenericMappedType(originType, out mappedTargetType)
            : TryGetOpenGenericMappedType(originType, out mappedTargetType);
    }

    bool TryGetNonGenericMappedType(Type originType, out Type? mappedTargetType)
    {
        return _mappedNonGenericTypes.TryGetValue(originType, out mappedTargetType);
    }

    bool TryGetOpenGenericMappedType(Type originType, out Type? mappedTargetType)
    {
        var genericTypeDefinition = originType.GetGenericTypeDefinition();
        if (!_mappedGenericTypes.TryGetValue(genericTypeDefinition, out Type? openGenericMappedType))
        {
            mappedTargetType = null;
            return false;
        }

        mappedTargetType = openGenericMappedType.MakeGenericType(originType.GenericTypeArguments);
        return true;
    }
}
