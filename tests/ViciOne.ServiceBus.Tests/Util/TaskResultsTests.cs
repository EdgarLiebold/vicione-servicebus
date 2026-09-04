using System.Reflection;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using ViciOne.ServiceBus.Util;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Util;

public sealed class TaskResultsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-RESULTS-CACHED", "completed-values")]
    public async Task CachedTasks_ExposeTheirDeclaredCompletedValuesAndStableIdentityAsync()
    {
        Assert.Same(Task.CompletedTask, TaskResults.Completed);
        Assert.Same(TaskResults.True, TaskResults.True);
        Assert.Same(TaskResults.False, TaskResults.False);
        Assert.Same(
            TaskResults.DefaultAsync<string>(TestContext.Current.CancellationToken),
            TaskResults.DefaultAsync<string>(TestContext.Current.CancellationToken));
        Assert.Same(
            TaskResults.CanceledAsync<int>(TestContext.Current.CancellationToken),
            TaskResults.CanceledAsync<int>(TestContext.Current.CancellationToken));
        Assert.True(TaskResults.Completed.IsCompletedSuccessfully);
        Assert.True(await TaskResults.True);
        Assert.False(await TaskResults.False);
        Assert.Null(await TaskResults.DefaultAsync<string>(TestContext.Current.CancellationToken));
        Assert.Equal(0, await TaskResults.DefaultAsync<int>(TestContext.Current.CancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-RESULTS-FAULT", "exact-exception")]
    public async Task Faulted_PreservesTheExactExceptionAsync()
    {
        var expected = new ExpectedTaskResultException("expected fault");

        Task<int> task = TaskResults.FaultedAsync<int>(expected, TestContext.Current.CancellationToken);
        ExpectedTaskResultException actual =
            await Assert.ThrowsAsync<ExpectedTaskResultException>(() => task);

        Assert.Same(expected, actual);
        Assert.True(task.IsFaulted);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-RESULTS-CANCELLATION", "cached-task")]
    public async Task Canceled_ExposesACanceledTaskWithACanceledTokenAsync()
    {
        Task<int> task = TaskResults.CanceledAsync<int>(TestContext.Current.CancellationToken);

        OperationCanceledException exception =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);

        Assert.True(task.IsCanceled);
        Assert.True(exception.CancellationToken.IsCancellationRequested);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-RESULTS-VALIDATION", "null-exception")]
    public void Faulted_RejectsANullException()
    {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = TaskResults.FaultedAsync<int>(null!, TestContext.Current.CancellationToken);
        });

        Assert.Equal("exception", exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-PRIMITIVES-PLATFORM", "no-desktop-windows-reference")]
    public void ProductAssembly_DoesNotReferenceDesktopWindowsFrameworks()
    {
        string[] prohibited =
        [
            "PresentationCore",
            "PresentationFramework",
            "System.Windows.Forms",
            "WindowsBase",
        ];
        string[] references = typeof(TaskResults).Assembly
            .GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .OfType<string>()
            .ToArray();

        Assert.DoesNotContain(references, reference =>
            prohibited.Contains(reference, StringComparer.OrdinalIgnoreCase));
    }

    private sealed class ExpectedTaskResultException(string message) : Exception(message);
}
