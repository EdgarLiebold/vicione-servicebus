using System;
using System.Linq;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Middleware;

/// <summary>Creates consume context converter instances.</summary>
public class ConsumeContextConverterFactory :
    IPipeContextConverterFactory<ConsumeContext>
{
    /// <summary>Gets converter.</summary>
    /// <typeparam name="TOutput">The output type.</typeparam>
    /// <returns>The converter.</returns>
    public IPipeContextConverter<ConsumeContext, TOutput> GetConverter<TOutput>()
        where TOutput : class, PipeContext
    {
        var innerType = typeof(TOutput).GetSingleClosedGenericArguments(typeof(ConsumeContext<>)).Single();

        return (IPipeContextConverter<ConsumeContext, TOutput>)(Activator.CreateInstance(typeof(Converter<>).MakeGenericType(innerType)) ?? throw new System.InvalidOperationException("The requested runtime type could not be activated."));
    }


    class Converter<T> :
        IPipeContextConverter<ConsumeContext, ConsumeContext<T>>
        where T : class
    {
        public bool TryConvert(ConsumeContext input, [NotNullWhen(true)] out ConsumeContext<T>? output)
        {
            return input.TryGetMessage(out output);
        }
    }
}
