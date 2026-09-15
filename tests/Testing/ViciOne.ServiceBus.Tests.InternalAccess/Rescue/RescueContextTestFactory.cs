using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Consumers.Contexts;
using ViciOne.ServiceBus.Middleware.Rescue;
using ViciOne.ServiceBus.Transports;

namespace ViciOne.ServiceBus.Tests.InternalAccess.Rescue;

/// <summary>Exposes Core rescue projections to source-mirrored behavioral tests.</summary>
public static class RescueContextTestFactory
{
    /// <summary>Creates a typed consume-failure projection.</summary>
    public static ExceptionConsumeContext<TMessage> Create<TMessage>(
        ConsumeContext<TMessage> context,
        Exception exception)
        where TMessage : class =>
        new RescueExceptionConsumeContext<TMessage>(context, exception);

    /// <summary>Creates an untyped consume-failure projection.</summary>
    public static ExceptionConsumeContext Create(ConsumeContext context, Exception exception) =>
        new RescueExceptionConsumeContext(context, exception);

    /// <summary>Creates a consumer scope backed by a typed consume context.</summary>
    public static ConsumerConsumeContext<TConsumer> CreateConsumerContext<TConsumer, TMessage>(
        ConsumeContext<TMessage> context,
        TConsumer consumer)
        where TConsumer : class
        where TMessage : class =>
        new ConsumerConsumeContextScope<TConsumer, TMessage>(context, consumer);

    /// <summary>Creates a consumer-failure projection.</summary>
    public static ExceptionConsumerConsumeContext<TConsumer> Create<TConsumer>(
        ConsumerConsumeContext<TConsumer> context,
        Exception exception)
        where TConsumer : class =>
        new RescueExceptionConsumerConsumeContext<TConsumer>(context, exception);

    /// <summary>Creates a receive-failure projection.</summary>
    public static ExceptionReceiveContext Create(ReceiveContext context, Exception exception) =>
        new RescueExceptionReceiveContext(context, exception);
}
