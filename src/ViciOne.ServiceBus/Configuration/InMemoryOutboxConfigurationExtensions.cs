using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.DependencyInjection;
using ViciOne.ServiceBus.Middleware;
using ViciOne.ServiceBus.Middleware.Outbox;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides extension methods for in memory outbox configuration.
/// </summary>
public static class InMemoryOutboxConfigurationExtensions
{
    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="context"></param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
        where T : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var specification = new InMemoryOutboxSpecification<T>(context);

        configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, Action<IOutboxConfigurator>? configure = default)
        where T : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var specification = new InMemoryOutboxSpecification<T>((ISetScopedConsumeContext?)null);

        configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="context"></param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox(this IConsumePipeConfigurator configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new InMemoryOutboxConfigurationObserver(context, configurator, configure);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator">The pipe configurator</param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox(this IConsumePipeConfigurator configurator, Action<IOutboxConfigurator>? configure = default)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new InMemoryOutboxConfigurationObserver((ISetScopedConsumeContext?)null, configurator, configure);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="context"></param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
        where TConsumer : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new InMemoryOutboxConsumerConfigurationObserver<TConsumer>(context, configurator, configure);
        configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox<TConsumer>(this IConsumerConfigurator<TConsumer> configurator, Action<IOutboxConfigurator>? configure = default)
        where TConsumer : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new InMemoryOutboxConsumerConfigurationObserver<TConsumer>((ISetScopedConsumeContext?)null, configurator, configure);
        configurator.ConnectConsumerConfigurationObserver(observer);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="context"></param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox<TSaga>(this ISagaConfigurator<TSaga> configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
        where TSaga : class, ISaga
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new InMemoryOutboxSagaConfigurationObserver<TSaga>(context, configurator, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox<TSaga>(this ISagaConfigurator<TSaga> configurator, Action<IOutboxConfigurator>? configure = default)
        where TSaga : class, ISaga
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new InMemoryOutboxSagaConfigurationObserver<TSaga>((ISetScopedConsumeContext?)null, configurator, configure);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="context"></param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox<TMessage>(this IHandlerConfigurator<TMessage> configurator, IRegistrationContext context,
        Action<IOutboxConfigurator>? configure = default)
        where TMessage : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new InMemoryOutboxHandlerConfigurationObserver(context, configure);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>
    /// Includes an outbox in the consume filter path, which delays outgoing messages until the return path
    /// of the pipeline returns to the outbox filter. At this point, the message execution pipeline should be
    /// nearly complete with only the ack remaining. If an exception is thrown, the messages are not sent/published.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="configure">Configure the outbox</param>
    public static void UseVolatileOutbox<TMessage>(this IHandlerConfigurator<TMessage> configurator, Action<IOutboxConfigurator>? configure = default)
        where TMessage : class
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));

        var observer = new InMemoryOutboxHandlerConfigurationObserver((ISetScopedConsumeContext?)null, configure);
        configurator.ConnectHandlerConfigurationObserver(observer);
    }

    /// <summary>
    /// Adds the required components to support the in-memory version of the InboxOutbox, which is intended for
    /// testing purposes only.
    /// </summary>
    /// <param name="collection"></param>
    /// <returns></returns>
    public static IServiceCollection AddInMemoryInboxOutbox(this IServiceCollection collection)
    {
        collection.TryAddSingleton<InMemoryOutboxMessageRepository>();
        collection.TryAddScoped<IOutboxContextFactory<InMemoryOutboxMessageRepository>, InMemoryOutboxContextFactory>();

        return collection;
    }

    /// <summary>
    /// Includes a combination inbox/outbox in the consume pipeline, which stores outgoing messages in memory until
    /// the message consumer completes.
    /// </summary>
    /// <param name="configurator"></param>
    /// <param name="context">Configuration service provider</param>
    public static void UseInMemoryInboxOutbox(this IReceiveEndpointConfigurator configurator, IRegistrationContext context)
    {
        if (configurator == null)
            throw new ArgumentNullException(nameof(configurator));
        if (context == null)
            throw new ArgumentNullException(nameof(context));

        var observer = new OutboxConsumePipeSpecificationObserver<InMemoryOutboxMessageRepository>(configurator, context);

        configurator.ConnectConsumerConfigurationObserver(observer);
        configurator.ConnectSagaConfigurationObserver(observer);
    }

}
