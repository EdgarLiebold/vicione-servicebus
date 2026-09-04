using System;
using System.Linq;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>
/// Provides a pipe context converter factory implementation.
/// </summary>
public class PipeContextConverterFactory :
    IPipeContextConverterFactory<PipeContext>
{
    /// <summary>
    /// Gets converter.
    /// </summary>
    /// <typeparam name="TOutput">The t output type.</typeparam>
    /// <returns>The result of the operation.</returns>
    public IPipeContextConverter<PipeContext, TOutput> GetConverter<TOutput>()
        where TOutput : class, PipeContext
    {
        if (typeof(TOutput).ImplementsInterface<CommandContext>())
        {
            var innerType = typeof(TOutput).GetSingleClosedGenericArguments(typeof(CommandContext<>)).Single();

            return (IPipeContextConverter<PipeContext, TOutput>)(Activator.CreateInstance(typeof(CommandContextConverter<>).MakeGenericType(innerType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
        }

        if (typeof(TOutput).ImplementsInterface<EventContext>())
        {
            var innerType = typeof(TOutput).GetSingleClosedGenericArguments(typeof(EventContext<>)).Single();

            return (IPipeContextConverter<PipeContext, TOutput>)(Activator.CreateInstance(typeof(EventContextConverter<>).MakeGenericType(innerType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
        }

        throw new ArgumentException($"The output type is not supported: {TypeCache<TOutput>.ShortName}", nameof(TOutput));
    }


    class CommandContextConverter<T> :
        IPipeContextConverter<PipeContext, CommandContext<T>>
        where T : class
    {
        bool IPipeContextConverter<PipeContext, CommandContext<T>>.TryConvert(PipeContext input,
            [NotNullWhen(true)] out CommandContext<T>? output)
        {
            if (input is CommandContext<T> commandContext)
            {
                output = commandContext;
                return true;
            }

            output = null;
            return false;
        }
    }


    class EventContextConverter<T> :
        IPipeContextConverter<PipeContext, EventContext<T>>
        where T : class
    {
        bool IPipeContextConverter<PipeContext, EventContext<T>>.TryConvert(PipeContext input,
            [NotNullWhen(true)] out EventContext<T>? output)
        {
            if (input is EventContext<T> eventContext)
            {
                output = eventContext;
                return true;
            }

            output = null;
            return false;
        }
    }
}
