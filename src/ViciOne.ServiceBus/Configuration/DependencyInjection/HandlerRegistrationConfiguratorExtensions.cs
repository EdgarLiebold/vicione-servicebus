using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ViciOne.ServiceBus.DependencyInjection;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Registers asynchronous delegates as dependency-injected message consumers.</summary>
public static class HandlerRegistrationConfiguratorExtensions
{
    /// <summary>Registers a consume-context delegate as a message handler.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T>(this IRegistrationConfigurator configurator, Func<ConsumeContext<T>, Task> handler)
        where T : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new MessageHandlerMethod<T>(handler));

        return configurator.AddConsumer<MessageHandlerConsumer<T>, MessageHandlerConsumerDefinition<MessageHandlerConsumer<T>, T>>();
    }

    /// <summary>Registers a message delegate as a message handler.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T>(this IRegistrationConfigurator configurator, Func<T, Task> handler)
        where T : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new MessageHandlerMethod<T>(handler));

        return configurator.AddConsumer<MessageHandlerConsumer<T>, MessageHandlerConsumerDefinition<MessageHandlerConsumer<T>, T>>();
    }

    /// <summary>Registers a consume-context delegate that returns a response.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <typeparam name="TResponse">The response message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, TResponse>(this IRegistrationConfigurator configurator,
        Func<ConsumeContext<T>, Task<TResponse>> handler)
        where T : class
        where TResponse : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new RequestHandlerMethod<T, TResponse>(handler));

        return configurator.AddConsumer<RequestHandlerConsumer<T, TResponse>, MessageHandlerConsumerDefinition<RequestHandlerConsumer<T, TResponse>, T>>();
    }

    /// <summary>Registers a message delegate that returns a response.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <typeparam name="TResponse">The response message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, TResponse>(this IRegistrationConfigurator configurator, Func<T, Task<TResponse>> handler)
        where T : class
        where TResponse : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new RequestHandlerMethod<T, TResponse>(handler));

        return configurator.AddConsumer<RequestHandlerConsumer<T, TResponse>, MessageHandlerConsumerDefinition<RequestHandlerConsumer<T, TResponse>, T>>();
    }

    /// <summary>Registers a consume-context delegate with one resolved dependency.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <typeparam name="T1">The dependency resolved for each message.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1>(this IRegistrationConfigurator configurator,
        Func<ConsumeContext<T>, T1, Task> handler)
        where T : class
        where T1 : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new MessageHandlerMethod<T, T1>(handler));

        return configurator.AddConsumer<MessageHandlerConsumer<T, T1>, MessageHandlerConsumerDefinition<MessageHandlerConsumer<T, T1>, T>>();
    }

    /// <summary>Registers a consume-context delegate with one resolved dependency and a response.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <typeparam name="T1">The dependency resolved for each request.</typeparam>
    /// <typeparam name="TResponse">The response message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, TResponse>(this IRegistrationConfigurator configurator,
        Func<ConsumeContext<T>, T1, Task<TResponse>> handler)
        where T : class
        where T1 : class
        where TResponse : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new RequestHandlerMethod<T, T1, TResponse>(handler));

        return configurator.AddConsumer<RequestHandlerConsumer<T, T1, TResponse>,
            MessageHandlerConsumerDefinition<RequestHandlerConsumer<T, T1, TResponse>, T>>();
    }

    /// <summary>Registers a message delegate with one resolved dependency.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <typeparam name="T1">The dependency resolved for each message.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1>(this IRegistrationConfigurator configurator, Func<T, T1, Task> handler)
        where T : class
        where T1 : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new MessageHandlerMethod<T, T1>(handler));

        return configurator.AddConsumer<MessageHandlerConsumer<T, T1>, MessageHandlerConsumerDefinition<MessageHandlerConsumer<T, T1>, T>>();
    }

    /// <summary>Registers a message delegate with one resolved dependency and a response.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <typeparam name="T1">The dependency resolved for each request.</typeparam>
    /// <typeparam name="TResponse">The response message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, TResponse>(this IRegistrationConfigurator configurator,
        Func<T, T1, Task<TResponse>> handler)
        where T : class
        where T1 : class
        where TResponse : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new RequestHandlerMethod<T, T1, TResponse>(handler));

        return configurator.AddConsumer<RequestHandlerConsumer<T, T1, TResponse>,
            MessageHandlerConsumerDefinition<RequestHandlerConsumer<T, T1, TResponse>, T>>();
    }

    /// <summary>Registers a consume-context delegate with two resolved dependencies.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <typeparam name="T1">The first dependency resolved for each message.</typeparam>
    /// <typeparam name="T2">The second dependency resolved for each message.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, T2>(this IRegistrationConfigurator configurator,
        Func<ConsumeContext<T>, T1, T2, Task> handler)
        where T : class
        where T1 : class
        where T2 : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new MessageHandlerMethod<T, T1, T2>(handler));

        return configurator.AddConsumer<MessageHandlerConsumer<T, T1, T2>, MessageHandlerConsumerDefinition<MessageHandlerConsumer<T, T1, T2>, T>>();
    }

    /// <summary>Registers a consume-context delegate with two resolved dependencies and a response.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <typeparam name="T1">The first dependency resolved for each request.</typeparam>
    /// <typeparam name="T2">The second dependency resolved for each request.</typeparam>
    /// <typeparam name="TResponse">The response message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, T2, TResponse>(this IRegistrationConfigurator configurator,
        Func<ConsumeContext<T>, T1, T2, Task<TResponse>> handler)
        where T : class
        where T1 : class
        where TResponse : class
        where T2 : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new RequestHandlerMethod<T, T1, T2, TResponse>(handler));

        return configurator.AddConsumer<RequestHandlerConsumer<T, T1, T2, TResponse>,
            MessageHandlerConsumerDefinition<RequestHandlerConsumer<T, T1, T2, TResponse>, T>>();
    }

    /// <summary>Registers a message delegate with two resolved dependencies.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <typeparam name="T1">The first dependency resolved for each message.</typeparam>
    /// <typeparam name="T2">The second dependency resolved for each message.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, T2>(this IRegistrationConfigurator configurator, Func<T, T1, T2, Task> handler)
        where T : class
        where T1 : class
        where T2 : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new MessageHandlerMethod<T, T1, T2>(handler));

        return configurator.AddConsumer<MessageHandlerConsumer<T, T1, T2>, MessageHandlerConsumerDefinition<MessageHandlerConsumer<T, T1, T2>, T>>();
    }

    /// <summary>Registers a message delegate with two resolved dependencies and a response.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <typeparam name="T1">The first dependency resolved for each request.</typeparam>
    /// <typeparam name="T2">The second dependency resolved for each request.</typeparam>
    /// <typeparam name="TResponse">The response message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, T2, TResponse>(this IRegistrationConfigurator configurator,
        Func<T, T1, T2, Task<TResponse>> handler)
        where T : class
        where T1 : class
        where TResponse : class
        where T2 : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new RequestHandlerMethod<T, T1, T2, TResponse>(handler));

        return configurator.AddConsumer<RequestHandlerConsumer<T, T1, T2, TResponse>,
            MessageHandlerConsumerDefinition<RequestHandlerConsumer<T, T1, T2, TResponse>, T>>();
    }

    /// <summary>Registers a consume-context delegate with three resolved dependencies.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <typeparam name="T1">The first dependency resolved for each message.</typeparam>
    /// <typeparam name="T2">The second dependency resolved for each message.</typeparam>
    /// <typeparam name="T3">The third dependency resolved for each message.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, T2, T3>(this IRegistrationConfigurator configurator,
        Func<ConsumeContext<T>, T1, T2, T3, Task> handler)
        where T : class
        where T1 : class
        where T2 : class
        where T3 : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new MessageHandlerMethod<T, T1, T2, T3>(handler));

        return configurator.AddConsumer<MessageHandlerConsumer<T, T1, T2, T3>, MessageHandlerConsumerDefinition<MessageHandlerConsumer<T, T1, T2, T3>,
            T>>();
    }

    /// <summary>Registers a consume-context delegate with three resolved dependencies and a response.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <typeparam name="T1">The first dependency resolved for each request.</typeparam>
    /// <typeparam name="T2">The second dependency resolved for each request.</typeparam>
    /// <typeparam name="T3">The third dependency resolved for each request.</typeparam>
    /// <typeparam name="TResponse">The response message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, T2, T3, TResponse>(this IRegistrationConfigurator configurator,
        Func<ConsumeContext<T>, T1, T2, T3, Task<TResponse>> handler)
        where T : class
        where T1 : class
        where T2 : class
        where T3 : class
        where TResponse : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new RequestHandlerMethod<T, T1, T2, T3, TResponse>(handler));

        return configurator.AddConsumer<RequestHandlerConsumer<T, T1, T2, T3, TResponse>,
            MessageHandlerConsumerDefinition<RequestHandlerConsumer<T, T1, T2, T3, TResponse>, T>>();
    }

    /// <summary>Registers a message delegate with three resolved dependencies.</summary>
    /// <typeparam name="T">The message contract.</typeparam>
    /// <typeparam name="T1">The first dependency resolved for each message.</typeparam>
    /// <typeparam name="T2">The second dependency resolved for each message.</typeparam>
    /// <typeparam name="T3">The third dependency resolved for each message.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, T2, T3>(this IRegistrationConfigurator configurator, Func<T, T1, T2, T3, Task>
        handler)
        where T : class
        where T1 : class
        where T2 : class
        where T3 : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new MessageHandlerMethod<T, T1, T2, T3>(handler));

        return configurator.AddConsumer<MessageHandlerConsumer<T, T1, T2, T3>,
            MessageHandlerConsumerDefinition<MessageHandlerConsumer<T, T1, T2, T3>, T>>();
    }

    /// <summary>Registers a message delegate with three resolved dependencies and a response.</summary>
    /// <typeparam name="T">The request message contract.</typeparam>
    /// <typeparam name="T1">The first dependency resolved for each request.</typeparam>
    /// <typeparam name="T2">The second dependency resolved for each request.</typeparam>
    /// <typeparam name="T3">The third dependency resolved for each request.</typeparam>
    /// <typeparam name="TResponse">The response message contract.</typeparam>
    /// <param name="configurator">The registration owner.</param>
    /// <param name="handler">An asynchronous method to handle the message.</param>
    /// <returns>The consumer registration created for the delegate.</returns>
    public static IConsumerRegistrationConfigurator AddHandler<T, T1, T2, T3, TResponse>(this IRegistrationConfigurator configurator,
        Func<T, T1, T2, T3, Task<TResponse>> handler)
        where T : class
        where T1 : class
        where T2 : class
        where T3 : class
        where TResponse : class
    {
        Validate<T>(configurator, handler);

        configurator.Services.TryAddSingleton(new RequestHandlerMethod<T, T1, T2, T3, TResponse>(handler));

        return configurator.AddConsumer<RequestHandlerConsumer<T, T1, T2, T3, TResponse>,
            MessageHandlerConsumerDefinition<RequestHandlerConsumer<T, T1, T2, T3, TResponse>, T>>();
    }

    static void Validate<T>(IRegistrationConfigurator configurator, Delegate handler)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(handler);

        if (!MessageTypeCache<T>.IsValidMessageType)
            throw new ArgumentException(MessageTypeCache<T>.InvalidMessageTypeReason, nameof(T));
    }
}
