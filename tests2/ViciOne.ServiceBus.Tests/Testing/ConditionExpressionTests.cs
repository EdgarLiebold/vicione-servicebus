using ViciOne.ServiceBus.Testing.Implementations;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Testing;

public sealed class ConditionExpressionTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONDITION", "or-of-and-blocks")]
    public async Task ConditionBlocks_ApplyAndWithinABlockAndOrBetweenBlocks()
    {
        var signal = new CountingSignalResource();
        var first = new TestCondition();
        var second = new TestCondition();
        var alternative = new TestCondition();
        using var expression = new ConditionExpression(signal);
        expression.AddConditionBlock(first, second);
        expression.AddConditionBlock(alternative);

        await first.Set(true);
        Assert.False(expression.CheckCondition());
        Assert.Equal(0, signal.Count);

        await second.Set(true);
        Assert.True(expression.CheckCondition());
        Assert.Equal(1, signal.Count);

        await first.Set(false);
        await alternative.Set(true);
        Assert.True(expression.CheckCondition());
        Assert.Equal(2, signal.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONDITION", "clear-disconnects-observers")]
    public async Task ClearAllConditions_DisconnectsEveryObserverAndLeavesAnExplicitEmptyState()
    {
        var signal = new CountingSignalResource();
        var removedCondition = new TestCondition();
        using var expression = new ConditionExpression(signal);
        expression.AddConditionBlock(removedCondition);

        expression.ClearAllConditions();
        await removedCondition.Set(true);

        Assert.Equal(0, signal.Count);
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => expression.CheckCondition());
        Assert.Equal("Cannot check an empty condition.", exception.Message);

        var replacementCondition = new TestCondition(initialValue: true);
        expression.AddConditionBlock(replacementCondition);
        await removedCondition.Set(false);

        Assert.Equal(0, signal.Count);
        Assert.True(expression.CheckCondition());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TEST-HARNESS-CONDITION", "invalid-blocks-rejected")]
    public void InvalidConditionBlocks_AreRejectedBeforeAnyObserverIsConnected()
    {
        var signal = new CountingSignalResource();
        using var expression = new ConditionExpression(signal);

        ArgumentNullException nullArray = Assert.Throws<ArgumentNullException>(() => expression.AddConditionBlock(null!));
        ArgumentException empty = Assert.Throws<ArgumentException>(() => expression.AddConditionBlock());
        ArgumentException nullElement = Assert.Throws<ArgumentException>(() => expression.AddConditionBlock([new TestCondition(), null!]));

        Assert.Equal("conditions", nullArray.ParamName);
        Assert.Equal("Must add at least 1 condition to a condition block.", empty.Message);
        Assert.Equal("conditions", nullElement.ParamName);
    }

    private sealed class TestCondition(bool initialValue = false) : BaseBusActivityIndicatorConnectable
    {
        private bool _isMet = initialValue;

        public override bool IsMet => _isMet;

        public Task Set(bool value)
        {
            _isMet = value;
            return ConditionUpdated();
        }
    }

    private sealed class CountingSignalResource : ISignalResource
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Signal() => Interlocked.Increment(ref _count);
    }
}
