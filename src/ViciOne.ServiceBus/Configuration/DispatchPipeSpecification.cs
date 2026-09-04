using System;
using System.Collections.Generic;
using System.Linq;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a dispatch pipe specification implementation.
/// </summary>
/// <typeparam name="TInput">The t input type.</typeparam>
public class DispatchPipeSpecification<TInput> :
    IPipeSpecification<TInput>,
    IDispatchConfigurator<TInput>
    where TInput : class, PipeContext
{
    readonly IPipeContextConverterFactory<TInput> _pipeContextConverterFactory;
    readonly List<IPipeConnectorSpecification> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    /// <param name="pipeContextConverterFactory">The pipe context converter factory value.</param>
    public DispatchPipeSpecification(IPipeContextConverterFactory<TInput> pipeContextConverterFactory)
    {
        _pipeContextConverterFactory = pipeContextConverterFactory
            ?? throw new ArgumentNullException(nameof(pipeContextConverterFactory));

        _specifications = new List<IPipeConnectorSpecification>();
    }

    /// <summary>
    /// Performs the pipe operation.
    /// </summary>
    /// <typeparam name="T">The t type.</typeparam>
    /// <param name="configurePipe">The configure pipe value.</param>
    public void Pipe<T>(Action<IPipeConfigurator<T>> configurePipe)
        where T : class, PipeContext
    {
        var specification = new ConfiguratorPipeConnectorSpecification<T>();

        configurePipe?.Invoke(specification);

        _specifications.Add(specification);
    }

    /// <summary>
    /// Applies this specification to the target builder.
    /// </summary>
    /// <param name="builder">The builder value.</param>
    public void Apply(IPipeBuilder<TInput> builder)
    {
        var dynamicFilter = new DynamicFilter<TInput>(_pipeContextConverterFactory);

        var count = _specifications.Count;
        for (var index = 0; index < count; index++)
            _specifications[index].Connect(dynamicFilter);

        builder.AddFilter(dynamicFilter);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        foreach (var result in _specifications.SelectMany(x => x.Validate()))
            yield return result.WithParentKey("Dispatch");
    }
}
