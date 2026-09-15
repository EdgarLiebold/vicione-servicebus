using System.Linq.Expressions;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga;

public sealed class SagaQueryTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "query-construction-requires-expression")]
    public void MissingExpression_IsRejectedWithTheExactParameterName()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SagaQuery<QueryState>(null!));

        Assert.Equal("filterExpression", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "query-retains-expression-and-lazily-caches-real-state-predicate")]
    public void Filter_RetainsTheExpressionAndCachesItsCompiledPredicate()
    {
        Expression<Func<QueryState, bool>> expression = state => state.Value == 7;
        var query = new SagaQuery<QueryState>(expression);
        Func<QueryState, bool> filter = query.GetFilter();
        var state = new QueryState { Value = 3 };

        Assert.Same(expression, query.FilterExpression);
        Assert.Same(filter, query.GetFilter());
        Assert.False(filter(state));
        state.Value = 7;
        Assert.True(filter(state));
    }

    private sealed class QueryState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public int Value { get; set; }
    }
}
