using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyProviders;

public sealed class TaskPropertyProviderTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PROPERTY-PROVIDER-TASK-RESULT", "scalar-and-converted-array")]
    public async Task ScalarAndArrayValues_CanBeReturnedAsTasks()
    {
        var scalarReader = PropertyProviderTestContext.For(new ScalarInput(27));
        var arrayReader = PropertyProviderTestContext.For(new ArrayInput([1, 2, 3]));

        Task<int> scalarTask = await scalarReader.ReadAsync<Task<int>>(nameof(ScalarInput.Value));
        Task<long[]> arrayTask = await arrayReader.ReadAsync<Task<long[]>>(nameof(ArrayInput.Values));
        int scalar = await scalarTask;
        long[] array = await arrayTask;

        Assert.Equal(27, scalar);
        Assert.Equal([1L, 2L, 3L], array);
    }

    private sealed record ScalarInput(int Value);

    private sealed record ArrayInput(int[] Values);
}
