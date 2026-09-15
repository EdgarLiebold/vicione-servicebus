using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Builds a rescue filter from a context projection and the configured rescue branches.</summary>
/// <typeparam name="TContext">The original pipeline context type.</typeparam>
/// <typeparam name="TRescue">The context type supplied to the rescue branch.</typeparam>
internal sealed class RescuePipeSpecification<TContext, TRescue> :
    ExceptionSpecification,
    IPipeSpecification<TContext>,
    IRescueConfigurator<TContext, TRescue>
    where TContext : class, PipeContext
    where TRescue : class, TContext
{
    readonly IPipeConfigurator<TContext> _contextPipeConfigurator;
    readonly IBuildPipeConfigurator<TRescue> _pipeConfigurator;
    readonly RescueContextFactory<TContext, TRescue> _rescueContextFactory;

    /// <summary>Creates a rescue specification for the supplied context projection.</summary>
    /// <param name="rescueContextFactory">Creates the rescue context after a selected failure.</param>
    public RescuePipeSpecification(RescueContextFactory<TContext, TRescue> rescueContextFactory)
    {
        _rescueContextFactory = rescueContextFactory ?? throw new ArgumentNullException(nameof(rescueContextFactory));

        _pipeConfigurator = new PipeConfigurator<TRescue>();
        _contextPipeConfigurator = new ContextPipeConfigurator(_pipeConfigurator);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<TContext> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        IPipe<TRescue> rescuePipe = _pipeConfigurator.Build();

        builder.AddFilter(new RescueFilter<TContext, TRescue>(rescuePipe, Filter, _rescueContextFactory));
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in _pipeConfigurator.Validate())
            yield return result;
    }

    IPipeConfigurator<TContext> IRescueConfigurator<TContext, TRescue>.ContextPipe => _contextPipeConfigurator;

    void IPipeConfigurator<TRescue>.AddPipeSpecification(IPipeSpecification<TRescue> specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        _pipeConfigurator.AddPipeSpecification(specification);
    }


    sealed class ContextPipeConfigurator :
        IPipeConfigurator<TContext>
    {
        readonly IPipeConfigurator<TRescue> _configurator;

        public ContextPipeConfigurator(IPipeConfigurator<TRescue> configurator)
        {
            _configurator = configurator ?? throw new ArgumentNullException(nameof(configurator));
        }

        public void AddPipeSpecification(IPipeSpecification<TContext> specification)
        {
            ArgumentNullException.ThrowIfNull(specification);
            _configurator.AddPipeSpecification(new PipeConfigurator<TRescue>.SplitFilterPipeSpecification<TContext>(specification, InputContext, Context));
        }

        static TRescue Context(TRescue context)
        {
            return context;
        }

        static TRescue InputContext(TRescue input, TContext context)
        {
            return input;
        }
    }
}
