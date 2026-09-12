using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Initializers.Conventions;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Initializers.Factories;

/// <summary>Builds the initializer pipeline for one message and input contract pair.</summary>
internal sealed class MessageInitializerFactory<TMessage, TInput> :
    IMessageInitializerFactory<TMessage>
    where TMessage : class
    where TInput : class
{
    readonly IInitializerConvention[] _conventions;
    readonly IMessageFactory<TMessage>? _messageFactory;

    public MessageInitializerFactory(IInitializerConvention[] conventions)
    {
        _conventions = SnapshotConventions(conventions);
    }

    public MessageInitializerFactory(IMessageFactory<TMessage>? messageFactory, IInitializerConvention[] conventions)
    {
        _messageFactory = messageFactory;
        _conventions = SnapshotConventions(conventions);
    }

    public IMessageInitializer<TMessage> CreateMessageInitializer()
    {
        var builder = new MessageInitializerBuilder<TMessage, TInput>(_messageFactory);

        foreach (IPropertyInitializerInspector<TMessage, TInput> inspector in CreatePropertyInspectors())
        {
            foreach (var convention in _conventions)
            {
                if (inspector.Apply(builder, convention))
                    break;
            }
        }

        foreach (IHeaderInitializerInspector<TMessage, TInput> inspector in CreateHeaderInspectors())
        {
            foreach (var convention in _conventions)
            {
                if (inspector.Apply(builder, convention))
                    break;
            }
        }

        foreach (IHeaderInitializerInspector<TMessage, TInput> inspector in CreateInputHeaderInspectors())
        {
            foreach (var convention in _conventions)
            {
                if (inspector.Apply(builder, convention))
                    break;
            }
        }

        return builder.Build();
    }

    static IEnumerable<IPropertyInitializerInspector<TMessage, TInput>> CreatePropertyInspectors()
    {
        return MessageTypeCache<TMessage>.Properties.Where(x => x.CanRead)
            .Select(x => (IPropertyInitializerInspector<TMessage, TInput>)(Activator.CreateInstance(
                typeof(PropertyInitializerInspector<,,>).MakeGenericType(typeof(TMessage), typeof(TInput), x.PropertyType), x)
                ?? throw new InvalidOperationException($"Could not create a property initializer inspector for '{x.Name}'.")));
    }

    static IEnumerable<IHeaderInitializerInspector<TMessage, TInput>> CreateInputHeaderInspectors()
    {
        return MessageTypeCache<TInput>.Properties.Where(x => x.CanRead)
            .Select(x => (IHeaderInitializerInspector<TMessage, TInput>)(Activator.CreateInstance(
                typeof(InputHeaderInitializerInspector<,,>).MakeGenericType(typeof(TMessage), typeof(TInput), x.PropertyType), x)
                ?? throw new InvalidOperationException($"Could not create an input header initializer inspector for '{x.Name}'.")));
    }

    static IEnumerable<IHeaderInitializerInspector<TMessage, TInput>> CreateHeaderInspectors()
    {
        yield return CreateHeaderInspector(x => x.SourceAddress);
        yield return CreateHeaderInspector(x => x.DestinationAddress);
        yield return CreateHeaderInspector(x => x.ResponseAddress);
        yield return CreateHeaderInspector(x => x.FaultAddress);

        yield return CreateHeaderInspector(x => x.RequestId);
        yield return CreateHeaderInspector(x => x.MessageId);
        yield return CreateHeaderInspector(x => x.CorrelationId);

        yield return CreateHeaderInspector(x => x.ConversationId);
        yield return CreateHeaderInspector(x => x.InitiatorId);

        yield return CreateHeaderInspector(x => x.ScheduledMessageId);

        yield return CreateHeaderInspector(x => x.TimeToLive);

        yield return CreateHeaderInspector(x => x.Durable);
    }

    static IHeaderInitializerInspector<TMessage, TInput> CreateHeaderInspector(Expression<Func<SendContext, Guid?>> expression)
    {
        return new HeaderInitializerInspector<TMessage, TInput, Guid?>(expression.GetPropertyInfo());
    }

    static IHeaderInitializerInspector<TMessage, TInput> CreateHeaderInspector(Expression<Func<SendContext, TimeSpan?>> expression)
    {
        return new HeaderInitializerInspector<TMessage, TInput, TimeSpan?>(expression.GetPropertyInfo());
    }

    static IHeaderInitializerInspector<TMessage, TInput> CreateHeaderInspector(Expression<Func<SendContext, Uri?>> expression)
    {
        return new HeaderInitializerInspector<TMessage, TInput, Uri?>(expression.GetPropertyInfo());
    }

    static IHeaderInitializerInspector<TMessage, TInput> CreateHeaderInspector(Expression<Func<SendContext, bool>> expression)
    {
        return new HeaderInitializerInspector<TMessage, TInput, bool>(expression.GetPropertyInfo());
    }

    static IInitializerConvention[] SnapshotConventions(IInitializerConvention[] conventions)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        IInitializerConvention[] snapshot = [.. conventions];
        if (Array.Exists(snapshot, static convention => convention is null))
            throw new ArgumentException("The convention collection cannot contain null elements.", nameof(conventions));

        return snapshot;
    }
}
