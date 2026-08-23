using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class AsyncPropertyProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-ASYNC-SOURCE", "scalar")]
    public async Task TaskValuedScalar_IsAwaitedBeforeExposure()
    {
        var input = new TaskScalarInput(Task.FromResult(27));
        var reader = PropertyProviderTestContext.For(input);

        int value = await reader.ReadAsync<int>(nameof(TaskScalarInput.Value));

        Assert.Equal(27, value);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-ASYNC-SOURCE", "array-elements")]
    public async Task ArrayOfTasks_AwaitsEveryElementAndDefaultsNullElements()
    {
        var input = new TaskElementArrayInput(
            [Task.FromResult(1), Task.FromResult(2), null, Task.FromResult(3)]);
        var reader = PropertyProviderTestContext.For(input);

        long[] values = await reader.ReadAsync<long[]>(nameof(TaskElementArrayInput.Values));

        Assert.Equal([1L, 2L, 0L, 3L], values);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-ASYNC-SOURCE", "sequence-result-shapes")]
    public async Task TaskValuedSequence_SupportsExactConvertedAndListResultShapes()
    {
        var input = new TaskSequenceInput(Task.FromResult(new[] { 1, 2, 3 }));
        var reader = PropertyProviderTestContext.For(input);

        int[] exactArray = await reader.ReadAsync<int[]>(nameof(TaskSequenceInput.Values));
        long[] convertedArray = await reader.ReadAsync<long[]>(nameof(TaskSequenceInput.Values));
        IEnumerable<int> enumerable = await reader.ReadAsync<IEnumerable<int>>(nameof(TaskSequenceInput.Values));
        List<int> list = await reader.ReadAsync<List<int>>(nameof(TaskSequenceInput.Values));
        IReadOnlyList<int> readOnlyList = await reader.ReadAsync<IReadOnlyList<int>>(nameof(TaskSequenceInput.Values));

        Assert.Equal([1, 2, 3], exactArray);
        Assert.Equal([1L, 2L, 3L], convertedArray);
        Assert.Equal([1, 2, 3], enumerable);
        Assert.Equal([1, 2, 3], list);
        Assert.Equal([1, 2, 3], readOnlyList);
    }

    private sealed record TaskScalarInput(Task<int> Value);

    private sealed record TaskElementArrayInput(Task<int>?[] Values);

    private sealed record TaskSequenceInput(Task<int[]> Values);
}
