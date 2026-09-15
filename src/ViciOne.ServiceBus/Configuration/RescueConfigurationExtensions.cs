using System;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Adds exception rescue branches to message pipelines.</summary>
public static class RescueConfigurationExtensions
{
    /// <summary>Routes selected receive failures through an alternate pipe.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="rescuePipe">The rescue pipe.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseRescue(this IPipeConfigurator<ReceiveContext> configurator, IPipe<ExceptionReceiveContext> rescuePipe,
        Action<IExceptionConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(rescuePipe);

        var rescueConfigurator = new ReceiveContextRescuePipeSpecification(rescuePipe);

        configure?.Invoke(rescueConfigurator);

        configurator.AddPipeSpecification(rescueConfigurator);
    }

    /// <summary>Routes selected consume failures through an alternate pipe.</summary>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="rescuePipe">The rescue pipe.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseRescue(this IPipeConfigurator<ConsumeContext> configurator, IPipe<ExceptionConsumeContext> rescuePipe,
        Action<IExceptionConfigurator>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(rescuePipe);

        var rescueConfigurator = new ConsumeContextRescuePipeSpecification(rescuePipe);

        configure?.Invoke(rescueConfigurator);

        configurator.AddPipeSpecification(rescueConfigurator);
    }

    /// <summary>Routes selected typed-message consume failures through an alternate pipe.</summary>
    /// <typeparam name="T">The consumed message type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="rescuePipe">The rescue pipe.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseRescue<T>(this IPipeConfigurator<ConsumeContext<T>> configurator, IPipe<ExceptionConsumeContext<T>> rescuePipe,
        Action<IExceptionConfigurator>? configure = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(rescuePipe);

        var rescueConfigurator = new ConsumeContextRescuePipeSpecification<T>(rescuePipe);

        configure?.Invoke(rescueConfigurator);

        configurator.AddPipeSpecification(rescueConfigurator);
    }

    /// <summary>Routes selected consumer failures through an alternate pipe.</summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="rescuePipe">The rescue pipe.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseRescue<T>(this IPipeConfigurator<ConsumerConsumeContext<T>> configurator, IPipe<ExceptionConsumerConsumeContext<T>> rescuePipe,
        Action<IExceptionConfigurator>? configure = null)
        where T : class
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(rescuePipe);

        var rescueConfigurator = new ConsumerConsumeContextRescuePipeSpecification<T>(rescuePipe);

        configure?.Invoke(rescueConfigurator);

        configurator.AddPipeSpecification(rescueConfigurator);
    }

    /// <summary>Routes selected failures through a caller-supplied rescue pipe and context projection.</summary>
    /// <typeparam name="TContext">The original pipeline context type.</typeparam>
    /// <typeparam name="TRescue">The context type supplied to the rescue pipe.</typeparam>
    /// <param name="configurator">The configurator to update.</param>
    /// <param name="rescuePipe">The rescue pipe.</param>
    /// <param name="rescueContextFactory">Factory method to convert the pipe context to the rescue pipe context.</param>
    /// <param name="configure">The callback used to configure the component.</param>
    public static void UseRescue<TContext, TRescue>(this IPipeConfigurator<TContext> configurator, IPipe<TRescue> rescuePipe,
        RescueContextFactory<TContext, TRescue> rescueContextFactory, Action<IRescueConfigurator<TContext, TRescue>>? configure = null)
        where TContext : class, PipeContext
        where TRescue : class, TContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(rescuePipe);
        ArgumentNullException.ThrowIfNull(rescueContextFactory);

        UseRescue(configurator, rescueContextFactory, x =>
        {
            configure?.Invoke(x);

            x.UseFork(rescuePipe);
        });
    }

    /// <summary>Builds a rescue branch for a projected context type.</summary>
    /// <typeparam name="TContext">The original pipeline context type.</typeparam>
    /// <typeparam name="TRescue">The context type supplied to the rescue branch.</typeparam>
    /// <param name="configurator">The pipeline configurator to update.</param>
    /// <param name="rescueContextFactory">Creates the rescue context after a selected failure.</param>
    /// <param name="configure">Configures exception selection and the rescue branches.</param>
    public static void UseRescue<TContext, TRescue>(this IPipeConfigurator<TContext> configurator,
        RescueContextFactory<TContext, TRescue> rescueContextFactory, Action<IRescueConfigurator<TContext, TRescue>>? configure = null)
        where TContext : class, PipeContext
        where TRescue : class, TContext
    {
        ArgumentNullException.ThrowIfNull(configurator);
        ArgumentNullException.ThrowIfNull(rescueContextFactory);

        var specification = new RescuePipeSpecification<TContext, TRescue>(rescueContextFactory);

        configure?.Invoke(specification);

        configurator.AddPipeSpecification(specification);
    }
}
