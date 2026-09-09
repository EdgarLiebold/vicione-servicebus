using System;
using System.Collections.Generic;
using System.Linq;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds typed filters to consume, send, publish, and general middleware pipelines.</summary>
public static class FilterConfigurationExtensions
{
    /// <summary>Adds a filter to the consume pipe for the specific message type.</summary>
    /// <typeparam name="TMessage">The consumed message contract.</typeparam>
    /// <param name="configurator">The consume pipeline to configure.</param>
    /// <param name="filter">The filter to add.</param>
    public static void UseFilter<TMessage>(this IConsumePipeConfigurator configurator, IFilter<ConsumeContext<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);

        var specification = new FilterPipeSpecification<ConsumeContext<TMessage>>(filter);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds a filter to the send pipe for the specific message type.</summary>
    /// <typeparam name="TMessage">The sent message contract.</typeparam>
    /// <param name="configurator">The send pipeline to configure.</param>
    /// <param name="filter">The filter to add.</param>
    public static void UseFilter<TMessage>(this ISendPipeConfigurator configurator, IFilter<SendContext<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);

        var specification = new FilterPipeSpecification<SendContext<TMessage>>(filter);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds a filter to the publish pipe for the specific message type.</summary>
    /// <typeparam name="TMessage">The published message contract.</typeparam>
    /// <param name="configurator">The publish pipeline to configure.</param>
    /// <param name="filter">The filter to add.</param>
    public static void UseFilter<TMessage>(this IPublishPipeConfigurator configurator, IFilter<PublishContext<TMessage>> filter)
        where TMessage : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);

        var specification = new FilterPipeSpecification<PublishContext<TMessage>>(filter);

        configurator.AddPipeSpecification(specification);
    }

    /// <summary>Adds a filter to the pipe.</summary>
    /// <typeparam name="TContext">The pipeline context.</typeparam>
    /// <param name="configurator">The pipeline to configure.</param>
    /// <param name="filter">The filter to add.</param>
    public static void UseFilter<TContext>(this IPipeConfigurator<TContext> configurator, IFilter<TContext> filter)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);

        var pipeBuilderConfigurator = new FilterPipeSpecification<TContext>(filter);

        configurator.AddPipeSpecification(pipeBuilderConfigurator);
    }

    /// <summary>Adds filters to the pipe.</summary>
    /// <typeparam name="TContext">The pipeline context.</typeparam>
    /// <param name="configurator">The pipeline to configure.</param>
    /// <param name="filters">The filters to add.</param>
    public static void UseFilters<TContext>(this IPipeConfigurator<TContext> configurator, IEnumerable<IFilter<TContext>> filters)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filters);

        IFilter<TContext>[] snapshot = filters.ToArray();
        foreach (IFilter<TContext> filter in snapshot)
            ArgumentNullException.ThrowIfNull(filter, nameof(filters));

        foreach (IFilter<TContext> filter in snapshot)
        {
            var pipeBuilderConfigurator = new FilterPipeSpecification<TContext>(filter);

            configurator.AddPipeSpecification(pipeBuilderConfigurator);
        }
    }

    /// <summary>Adds filters to the pipe.</summary>
    /// <typeparam name="TContext">The pipeline context.</typeparam>
    /// <param name="configurator">The pipeline to configure.</param>
    /// <param name="filters">The filters to add.</param>
    public static void UseFilters<TContext>(this IPipeConfigurator<TContext> configurator, params IFilter<TContext>[] filters)
        where TContext : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filters);

        foreach (IFilter<TContext> filter in filters)
            ArgumentNullException.ThrowIfNull(filter, nameof(filters));

        foreach (IFilter<TContext> filter in filters)
        {
            var pipeBuilderConfigurator = new FilterPipeSpecification<TContext>(filter);

            configurator.AddPipeSpecification(pipeBuilderConfigurator);
        }
    }

    /// <summary>Adds a filter to the pipe which is of a different type than the native pipe context type.</summary>
    /// <typeparam name="TContext">The native pipeline context.</typeparam>
    /// <typeparam name="TFilter">The base context accepted by the filter.</typeparam>
    /// <param name="configurator">The pipeline to configure.</param>
    /// <param name="filter">The filter to add.</param>
    /// <param name="contextProvider">The function that merges the filter context back into the native context.</param>
    /// <param name="inputContextProvider">The function that presents the native context to the filter.</param>
    public static void UseFilter<TContext, TFilter>(this IPipeConfigurator<TContext> configurator, IFilter<TFilter> filter,
        MergeFilterContextProvider<TContext, TFilter> contextProvider, FilterContextProvider<TFilter, TContext> inputContextProvider)
        where TContext : class, TFilter
        where TFilter : class, PipeContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(filter);
        ArgumentNullException.ThrowIfNull(contextProvider);
        ArgumentNullException.ThrowIfNull(inputContextProvider);

        var filterSpecification = new FilterPipeSpecification<TFilter>(filter);

        var pipeBuilderConfigurator = new PipeConfigurator<TContext>.SplitFilterPipeSpecification<TFilter>(filterSpecification,
            contextProvider,
            inputContextProvider);

        configurator.AddPipeSpecification(pipeBuilderConfigurator);
    }
}
