using System;
using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Adapts an inner pipeline specification to a different context contract.</summary>
    /// <typeparam name="TFilter">The inner pipeline context contract.</typeparam>
    public sealed class SplitFilterPipeSpecification<TFilter> :
        IPipeSpecification<TContext>
        where TFilter : class, PipeContext
    {
        readonly MergeFilterContextProvider<TContext, TFilter> _contextProvider;
        readonly FilterContextProvider<TFilter, TContext> _inputContextProvider;
        readonly IPipeSpecification<TFilter> _specification;

        /// <summary>Creates a specification with explicit projections between the two context contracts.</summary>
        /// <param name="specification">The inner specification whose filters and validation results are preserved.</param>
        /// <param name="contextProvider">Reconstructs the outer context when an inner filter continues the pipeline.</param>
        /// <param name="inputContextProvider">Projects the outer context into the inner pipeline.</param>
        public SplitFilterPipeSpecification(IPipeSpecification<TFilter> specification, MergeFilterContextProvider<TContext, TFilter> contextProvider,
            FilterContextProvider<TFilter, TContext> inputContextProvider)
        {
            _specification = specification ?? throw new ArgumentNullException(nameof(specification));
            _contextProvider = contextProvider ?? throw new ArgumentNullException(nameof(contextProvider));
            _inputContextProvider = inputContextProvider ?? throw new ArgumentNullException(nameof(inputContextProvider));
        }

        /// <summary>Applies this specification to the target builder.</summary>
        /// <param name="builder">The builder that receives the configuration.</param>
        public void Apply(IPipeBuilder<TContext> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            var splitBuilder = new Builder(builder, _contextProvider, _inputContextProvider);

            _specification.Apply(splitBuilder);
        }

        /// <summary>Returns the validation results owned by the inner specification.</summary>
        /// <returns>The complete inner validation sequence.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            return _specification.Validate()
                ?? throw new InvalidOperationException("The inner pipe specification returned null validation results.");
        }


        sealed class Builder :
            IPipeBuilder<TFilter>
        {
            readonly IPipeBuilder<TContext> _builder;
            readonly MergeFilterContextProvider<TContext, TFilter> _contextProvider;
            readonly FilterContextProvider<TFilter, TContext> _inputContextProvider;

            public Builder(IPipeBuilder<TContext> builder, MergeFilterContextProvider<TContext, TFilter> contextProvider,
                FilterContextProvider<TFilter, TContext> inputContextProvider)
            {
                _builder = builder;
                _contextProvider = contextProvider;
                _inputContextProvider = inputContextProvider;
            }

            public void AddFilter(IFilter<TFilter> filter)
            {
                ArgumentNullException.ThrowIfNull(filter);
                var splitFilter = new SplitFilter<TContext, TFilter>(filter, _contextProvider, _inputContextProvider);

                _builder.AddFilter(splitFilter);
            }
        }
    }
}
