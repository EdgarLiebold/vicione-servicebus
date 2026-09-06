using System.Collections.Generic;
using ViciOne.ServiceBus.Middleware;

namespace ViciOne.ServiceBus.Configuration;

public partial class PipeConfigurator<TContext>
    where TContext : class, PipeContext
{
    /// <summary>Adds an arbitrary filter to the pipe.</summary>
    /// <typeparam name="TFilter">The filter type.</typeparam>
    public class SplitFilterPipeSpecification<TFilter> :
        IPipeSpecification<TContext>
        where TFilter : class, PipeContext
    {
        readonly MergeFilterContextProvider<TContext, TFilter> _contextProvider;
        readonly FilterContextProvider<TFilter, TContext> _inputContextProvider;
        readonly IPipeSpecification<TFilter> _specification;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="specification">The specification.</param>
        /// <param name="contextProvider">The context provider.</param>
        /// <param name="inputContextProvider">The input context provider.</param>
        public SplitFilterPipeSpecification(IPipeSpecification<TFilter> specification, MergeFilterContextProvider<TContext, TFilter> contextProvider,
            FilterContextProvider<TFilter, TContext> inputContextProvider)
        {
            _specification = specification;
            _contextProvider = contextProvider;
            _inputContextProvider = inputContextProvider;
        }

        /// <summary>Applies this specification to the target builder.</summary>
        /// <param name="builder">The builder that receives the configuration.</param>
        public void Apply(IPipeBuilder<TContext> builder)
        {
            var splitBuilder = new Builder(builder, _contextProvider, _inputContextProvider);

            _specification.Apply(splitBuilder);
        }

        /// <summary>Validates the current configuration.</summary>
        /// <returns>The validation failures.</returns>
        public IEnumerable<ValidationResult> Validate()
        {
            if (_specification == null)
                yield return this.Failure("Specification", "must not be null");
            if (_contextProvider == null)
                yield return this.Failure("ContextProvider", "must not be null");
        }


        class Builder :
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
                var splitFilter = new SplitFilter<TContext, TFilter>(filter, _contextProvider, _inputContextProvider);

                _builder.AddFilter(splitFilter);
            }
        }
    }
}
