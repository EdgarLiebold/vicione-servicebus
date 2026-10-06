using System;
using System.Collections.Generic;

namespace ViciOne.ServiceBus.Configuration;

/// <summary>Accumulates pipe specifications and builds them in registration order.</summary>
/// <typeparam name="TContext">The context contract handled by every registered specification.</typeparam>
public partial class PipeConfigurator<TContext> :
    IBuildPipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    readonly List<IPipeSpecification<TContext>> _specifications;

    /// <summary>Creates an empty configuration with no registered pipe specifications.</summary>
    public PipeConfigurator()
    {
        _specifications = new List<IPipeSpecification<TContext>>(16);
    }

    /// <summary>Enumerates the validation results of the registered specifications in registration order.</summary>
    /// <returns>Each result yielded by a registered specification, or an empty sequence when none are registered.</returns>
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

    /// <summary>Appends a required specification to the configured pipeline.</summary>
    /// <param name="specification">The specification whose filters and validation results are included in the pipeline.</param>
    public void AddPipeSpecification(IPipeSpecification<TContext> specification)
    {
        if (specification == null)
            throw new ArgumentNullException(nameof(specification));

        _specifications.Add(specification);
    }

    internal Func<IPipe<TContext>, IPipe<TContext>> BuildWithContinuation()
    {
        var builder = new SpecificationPipeBuilder();
        for (var index = 0; index < _specifications.Count; index++)
            _specifications[index].Apply(builder);

        return builder.Build;
    }

    /// <summary>Builds the registered specifications in registration order, or returns an empty pipeline when none are registered.</summary>
    /// <returns>The pipeline produced by the registered specifications, or an empty pipeline.</returns>
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
