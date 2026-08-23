using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers.PropertyConverters;

public sealed class TaskPropertyConverterTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-NESTED-TASK", "task-of-task-to-value")]
    public async Task NestedTaskInput_IsFullyAwaitedAndConvertedToTheTargetValue()
    {
        Task<Task<int>> nestedValue = Task.FromResult(Task.FromResult(37));

        InitializeContext<TaskMessage> context = await MessageInitializerCache<TaskMessage>.Initialize(
            new { Value = nestedValue },
            TestContext.Current.CancellationToken);

        Assert.Equal(37L, context.Message.Value);
    }

    public interface TaskMessage
    {
        long Value { get; }
    }
}
