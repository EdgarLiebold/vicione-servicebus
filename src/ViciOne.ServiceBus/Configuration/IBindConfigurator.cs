using System;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures bind.</summary>
/// <typeparam name="TLeft">The left type.</typeparam>
public interface IBindConfigurator<TLeft>
    where TLeft : class, PipeContext
{
    /// <summary>Specifies a pipe context source which is used to create the PipeContext bound to the BindContext.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="source">The source value.</param>
    /// <param name="configureTarget">The configure target.</param>
    void Source<T>(IPipeContextSource<T, TLeft> source, Action<IBindConfigurator<TLeft, T>> configureTarget)
        where T : class, PipeContext;
}


/// <summary>Configures a binding using the specified pipe context source.</summary>
/// <typeparam name="TLeft">The left type.</typeparam>
/// <typeparam name="TRight">The right type.</typeparam>
public interface IBindConfigurator<TLeft, TRight> :
    IPipeConfigurator<BindContext<TLeft, TRight>>
    where TRight : class, PipeContext
    where TLeft : class, PipeContext
{
    /// <summary>Configure a filter on the context pipe, versus the bound pipe.</summary>
    IPipeConfigurator<TLeft> ContextPipe { get; }
}
