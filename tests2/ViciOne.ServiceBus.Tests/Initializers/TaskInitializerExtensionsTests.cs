using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class TaskInitializerExtensionsTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-PROJECTION", "reference")]
    public async Task SelectAsync_ProjectsAReferenceValue()
    {
        Task<Subject> source = Task.FromResult(new Subject { Name = "Frank" });

        string? result = await source.SelectAsync(subject => subject.Name);

        Assert.Equal("Frank", result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-PROJECTION", "value")]
    public async Task SelectAsync_ProjectsAValueType()
    {
        Task<Subject> source = Task.FromResult(new Subject { Id = 27 });

        int result = await source.SelectAsync(subject => subject.Id);

        Assert.Equal(27, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-PROJECTION", "nullable-value")]
    public async Task SelectAsync_ProjectsAPresentNullableValue()
    {
        Task<Subject> source = Task.FromResult(new Subject { MemberId = 27 });

        int? result = await source.SelectAsync(subject => subject.MemberId);

        Assert.Equal(27, result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-PROJECTION", "nullable-empty")]
    public async Task SelectAsync_PreservesAnEmptyNullableValue()
    {
        Task<Subject> source = Task.FromResult(new Subject());

        int? result = await source.SelectAsync(subject => subject.MemberId);

        Assert.Null(result);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK-VALUE", "reference")]
    public async Task SelectOrFallbackAsync_UsesAReferenceFallbackForAMissingSelection()
    {
        Task<Subject> missing = Task.FromResult(new Subject());
        Task<Subject> present = Task.FromResult(new Subject { Name = "Jane" });

        string fallback = await missing.SelectOrFallbackAsync(subject => subject.Name, "Frank");
        string selected = await present.SelectOrFallbackAsync(subject => subject.Name, "Frank");

        Assert.Equal("Frank", fallback);
        Assert.Equal("Jane", selected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK-VALUE", "nullable-value")]
    public async Task SelectOrFallbackAsync_UsesAValueFallbackForAnEmptyNullableSelection()
    {
        Task<Subject> missing = Task.FromResult(new Subject());
        Task<Subject> present = Task.FromResult(new Subject { MemberId = 19 });

        int fallback = await missing.SelectOrFallbackAsync(subject => subject.MemberId, 27);
        int selected = await present.SelectOrFallbackAsync(subject => subject.MemberId, 27);

        Assert.Equal(27, fallback);
        Assert.Equal(19, selected);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK-FACTORY", "reference")]
    public async Task SelectOrFallbackAsync_InvokesAReferenceFallbackFactoryWhenNeeded()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var invocations = 0;

        string result = await source.SelectOrFallbackAsync(
            subject => subject.Name,
            () =>
            {
                invocations++;
                return "Frank";
            });

        Assert.Equal("Frank", result);
        Assert.Equal(1, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK-FACTORY", "nullable-value")]
    public async Task SelectOrFallbackAsync_InvokesAValueFallbackFactoryWhenNeeded()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var invocations = 0;

        int result = await source.SelectOrFallbackAsync(
            subject => subject.MemberId,
            () =>
            {
                invocations++;
                return 27;
            });

        Assert.Equal(27, result);
        Assert.Equal(1, invocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-ASYNC-FALLBACK", "reference")]
    public async Task SelectOrFallbackAsync_AwaitsAReferenceFallbackTask()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var fallbackStarted = NewCompletionSource();
        var releaseFallback = NewCompletionSource<string>();

        Task<string> result = source.SelectOrFallbackAsync(
            subject => subject.Name,
            () =>
            {
                fallbackStarted.TrySetResult();
                return releaseFallback.Task;
            });

        await fallbackStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        try
        {
            Assert.False(result.IsCompleted);
        }
        finally
        {
            releaseFallback.TrySetResult("Frank");
        }

        Assert.Equal("Frank", await result.WaitAsync(OperationTimeout, TestCancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-ASYNC-FALLBACK", "nullable-value")]
    public async Task SelectOrFallbackAsync_AwaitsAValueFallbackTask()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var fallbackStarted = NewCompletionSource();
        var releaseFallback = NewCompletionSource<int>();

        Task<int> result = source.SelectOrFallbackAsync(
            subject => subject.MemberId,
            () =>
            {
                fallbackStarted.TrySetResult();
                return releaseFallback.Task;
            });

        await fallbackStarted.Task.WaitAsync(OperationTimeout, TestCancellationToken);
        try
        {
            Assert.False(result.IsCompleted);
        }
        finally
        {
            releaseFallback.TrySetResult(27);
        }

        Assert.Equal(27, await result.WaitAsync(OperationTimeout, TestCancellationToken));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-FALLBACK", "lazy")]
    public async Task SelectOrFallbackAsync_DoesNotInvokeFactoriesForAPresentSelection()
    {
        Task<Subject> source = Task.FromResult(new Subject { MemberId = 19, Name = "Jane" });
        var referenceSynchronousInvocations = 0;
        var referenceAsynchronousInvocations = 0;
        var valueSynchronousInvocations = 0;
        var valueAsynchronousInvocations = 0;

        string referenceSynchronous = await source.SelectOrFallbackAsync(
            subject => subject.Name,
            () =>
            {
                referenceSynchronousInvocations++;
                return "fallback";
            });
        string referenceAsynchronous = await source.SelectOrFallbackAsync(
            subject => subject.Name,
            () =>
            {
                referenceAsynchronousInvocations++;
                return Task.FromResult("fallback");
            });
        int valueSynchronous = await source.SelectOrFallbackAsync(
            subject => subject.MemberId,
            () =>
            {
                valueSynchronousInvocations++;
                return 27;
            });
        int valueAsynchronous = await source.SelectOrFallbackAsync(
            subject => subject.MemberId,
            () =>
            {
                valueAsynchronousInvocations++;
                return Task.FromResult(27);
            });

        Assert.Equal("Jane", referenceSynchronous);
        Assert.Equal("Jane", referenceAsynchronous);
        Assert.Equal(19, valueSynchronous);
        Assert.Equal(19, valueAsynchronous);
        Assert.Equal(0, referenceSynchronousInvocations);
        Assert.Equal(0, referenceAsynchronousInvocations);
        Assert.Equal(0, valueSynchronousInvocations);
        Assert.Equal(0, valueAsynchronousInvocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-SOURCE", "null-value")]
    public async Task NullSourceValue_BypassesTheSelectorAndUsesTheDeclaredFallback()
    {
        Task<Subject?> source = Task.FromResult<Subject?>(null);
        var selectorInvocations = 0;

        string? projected = await source.SelectAsync(subject =>
        {
            selectorInvocations++;
            return subject?.Name;
        });
        string fallback = await source.SelectOrFallbackAsync(
            subject =>
            {
                selectorInvocations++;
                return subject?.Name;
            },
            "Frank");

        Assert.Null(projected);
        Assert.Equal("Frank", fallback);
        Assert.Equal(0, selectorInvocations);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-SOURCE", "fault-and-cancellation")]
    public async Task SourceTaskState_IsPropagatedWithoutWrappingOrTokenSubstitution()
    {
        var expected = new ExpectedInitializerException("expected fault");
        Task<Subject> faulted = Task.FromException<Subject>(expected);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Task<Subject> canceled = Task.FromCanceled<Subject>(cancellation.Token);

        ExpectedInitializerException actual = await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
            faulted.SelectAsync(subject => subject.Name));
        OperationCanceledException canceledException =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                canceled.SelectAsync(subject => subject.Name));

        Assert.Same(expected, actual);
        Assert.Equal(cancellation.Token, canceledException.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-DELEGATE-STATE", "fault-and-cancellation")]
    public async Task SelectorAndFallbackState_IsPropagatedWithoutWrappingOrTokenSubstitution()
    {
        Task<Subject> source = Task.FromResult(new Subject());
        var selectorException = new ExpectedInitializerException("selector fault");
        var synchronousFallbackException = new ExpectedInitializerException("synchronous fallback fault");
        var asynchronousFallbackException = new ExpectedInitializerException("asynchronous fallback fault");
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        ExpectedInitializerException actualSelectorException =
            await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
                source.SelectAsync<Subject, string>(_ => throw selectorException));
        ExpectedInitializerException actualSynchronousFallbackException =
            await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
                source.SelectOrFallbackAsync(
                    subject => subject.Name,
                    (Func<string>)(() => throw synchronousFallbackException)));
        ExpectedInitializerException actualAsynchronousFallbackException =
            await Assert.ThrowsAsync<ExpectedInitializerException>(() =>
                source.SelectOrFallbackAsync(
                    subject => subject.Name,
                    () => Task.FromException<string>(asynchronousFallbackException)));
        OperationCanceledException actualCancellation =
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                source.SelectOrFallbackAsync(
                    subject => subject.Name,
                    () => Task.FromCanceled<string>(cancellation.Token)));

        Assert.Same(selectorException, actualSelectorException);
        Assert.Same(synchronousFallbackException, actualSynchronousFallbackException);
        Assert.Same(asynchronousFallbackException, actualAsynchronousFallbackException);
        Assert.Equal(cancellation.Token, actualCancellation.CancellationToken);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-TASK-INITIALIZER-VALIDATION", "invalid-inputs")]
    public async Task PublicMethods_RejectInvalidInputsAtTheirBoundary()
    {
        Task<Subject> source = Task.FromResult(new Subject());

        ArgumentNullException nullSource = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            TaskInitializerExtensions.SelectAsync<Subject, string>(null!, subject => subject.Name));
        ArgumentNullException nullSelector = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectAsync<Subject, string>(null!));
        ArgumentNullException nullFallback = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, (string)null!));
        ArgumentNullException nullSyncFactory = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, (Func<string>)null!));
        ArgumentNullException nullValueSyncFactory = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.MemberId, (Func<int>)null!));
        ArgumentNullException nullAsyncFactory = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, (Func<Task<string>>)null!));
        ArgumentNullException nullValueAsyncFactory = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            source.SelectOrFallbackAsync(subject => subject.MemberId, (Func<Task<int>>)null!));
        InvalidOperationException nullFallbackValue = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, () => (string)null!));
        InvalidOperationException nullFallbackTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, () => (Task<string>)null!));
        InvalidOperationException nullValueFallbackTask = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.SelectOrFallbackAsync(subject => subject.MemberId, () => (Task<int>)null!));
        InvalidOperationException nullFallbackTaskResult = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            source.SelectOrFallbackAsync(subject => subject.Name, () => Task.FromResult<string>(null!)));

        Assert.Equal("source", nullSource.ParamName);
        Assert.Equal("selector", nullSelector.ParamName);
        Assert.Equal("fallback", nullFallback.ParamName);
        Assert.Equal("fallbackFactory", nullSyncFactory.ParamName);
        Assert.Equal("fallbackFactory", nullValueSyncFactory.ParamName);
        Assert.Equal("fallbackFactory", nullAsyncFactory.ParamName);
        Assert.Equal("fallbackFactory", nullValueAsyncFactory.ParamName);
        Assert.Equal("The fallbackFactory must return a value.", nullFallbackValue.Message);
        Assert.Equal("The fallbackFactory must return a Task.", nullFallbackTask.Message);
        Assert.Equal("The fallbackFactory must return a Task.", nullValueFallbackTask.Message);
        Assert.Equal("The fallbackFactory task must produce a value.", nullFallbackTaskResult.Message);
    }

    private static CancellationToken TestCancellationToken => TestContext.Current.CancellationToken;

    private static TimeSpan OperationTimeout => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    private static TaskCompletionSource NewCompletionSource() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private static TaskCompletionSource<T> NewCompletionSource<T>() =>
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    private sealed class Subject
    {
        public int Id { get; init; }

        public int? MemberId { get; init; }

        public string? Name { get; init; }
    }

    private sealed class ExpectedInitializerException(string message) : Exception(message);
}
