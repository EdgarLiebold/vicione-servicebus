using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>
/// Provides a pipe configurator implementation.
/// </summary>
public partial class PipeConfigurator<TContext> :
    IBuildPipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    readonly List<IPipeSpecification<TContext>> _specifications;

    /// <summary>
    /// Initializes a new instance of the containing type.
    /// </summary>
    public PipeConfigurator()
    {
        _specifications = new List<IPipeSpecification<TContext>>(16);
    }

    /// <summary>
    /// Validates the current configuration.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IEnumerable<ValidationResult> Validate()
    {
        if (_specifications.Count == 0)
            yield break;

        for (var i = 0; i < _specifications.Count; i++)
        {
            foreach (var result in _specifications[i].Validate())
                yield return result;
        }
    }

    /// <summary>
    /// Adds pipe specification to the configuration.
    /// </summary>
    /// <param name="specification">The specification value.</param>
    public void AddPipeSpecification(IPipeSpecification<TContext> specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    /// <summary>
    /// Performs the build operation.
    /// </summary>
    /// <returns>The result of the operation.</returns>
    public IPipe<TContext> Build()
    {
        if (_specifications.Count == 0)
            return Cache.EmptyPipe;

        var builder = new PipeBuilder(_specifications.Count);

        var count = _specifications.Count;
        for (var index = 0; index < count; index++)
            _specifications[index].Apply(builder);

        return builder.Build();
    }
}
