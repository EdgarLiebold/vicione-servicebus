using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Describes requirements for dispatch pipe.</summary>
/// <typeparam name="TInput">The input type.</typeparam>
public class DispatchPipeSpecification<TInput> :
    IPipeSpecification<TInput>,
    IDispatchConfigurator<TInput>
    where TInput : class, PipeContext
{
    readonly IPipeContextConverterFactory<TInput> _pipeContextConverterFactory;
    readonly List<IPipeConnectorSpecification> _specifications;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="pipeContextConverterFactory">The pipe context converter factory.</param>
    public DispatchPipeSpecification(IPipeContextConverterFactory<TInput> pipeContextConverterFactory)
    {
        _pipeContextConverterFactory = pipeContextConverterFactory
            ?? throw new ArgumentNullException(nameof(pipeContextConverterFactory));

        _specifications = new List<IPipeConnectorSpecification>();
    }

    /// <summary>Builds the configured pipeline.</summary>
    /// <typeparam name="T">The value type.</typeparam>
    /// <param name="configurePipe">The configure pipe.</param>
    public void Pipe<T>(Action<IPipeConfigurator<T>> configurePipe)
        where T : class, PipeContext
    {
        var specification = new ConfiguratorPipeConnectorSpecification<T>();

        configurePipe?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>Applies this specification to the target builder.</summary>
    /// <param name="builder">The builder that receives the configuration.</param>
    public void Apply(IPipeBuilder<TInput> builder)
    {
        var dynamicFilter = new DynamicFilter<TInput>(_pipeContextConverterFactory);

        var count = _specifications.Count;
        for (var index = 0; index < count; index++)
            _specifications[index].Connect(dynamicFilter);

        builder.AddFilter(dynamicFilter);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in _specifications.SelectMany(x => x.Validate()))
            yield return result.WithParentKey("Dispatch");
    }
}
