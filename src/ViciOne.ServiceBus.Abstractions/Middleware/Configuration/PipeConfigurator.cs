using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Configures pipe.</summary>
/// <typeparam name="TContext">The pipeline context carried by the member.</typeparam>
public partial class PipeConfigurator<TContext> :
    IBuildPipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    readonly List<IPipeSpecification<TContext>> _specifications;

    /// <summary>Initializes a new instance.</summary>
    public PipeConfigurator()
    {
        _specifications = new List<IPipeSpecification<TContext>>(16);
    }

    /// <summary>Validates the current configuration.</summary>
    /// <returns>The validation failures.</returns>
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

    /// <summary>Adds pipe specification to the configuration.</summary>
    /// <param name="specification">The specification.</param>
    public void AddPipeSpecification(IPipeSpecification<TContext> specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    /// <summary>Builds the configured component.</summary>
    /// <returns>The configured component.</returns>
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
