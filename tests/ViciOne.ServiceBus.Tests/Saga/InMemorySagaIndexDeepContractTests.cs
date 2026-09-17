using System.Reflection;
using System.Runtime.ExceptionServices;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga;

public sealed class InMemorySagaIndexDeepContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-staged-capture-apply-rollback-freezes-key")]
    public void StagedCapture_FreezesTheKeyWithoutPublishingUntilApplyAndRollback()
    {
        var index = CreateStableIndex();
        var state = new IndexSaga { StableKey = "captured" };
        var instance = new SagaInstance<IndexSaga>(state);

        RegistrationProbe registration = Capture(index, instance);
        state.StableKey = "mutated";

        Assert.Equal("captured", registration.Key);
        Assert.Equal(1, state.StableReads);
        Assert.Equal(0, index.Count);
        Assert.Null(index["captured"]);

        registration.Apply();

        Assert.Equal(1, state.StableReads);
        Assert.Equal(1, index.Count);
        Assert.Same(instance, index["captured"]);
        Assert.Null(index["mutated"]);

        registration.Rollback();

        Assert.Equal(1, state.StableReads);
        Assert.Equal(0, index.Count);
        Assert.Null(index["captured"]);
        Assert.False(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-property-direct-add-is-reference-idempotent")]
    public void IndexedSagaProperty_DirectAddOfTheSameWrapperIsIdempotentWithoutRepeatingTheGetter()
    {
        var index = CreateStableIndex();
        var state = new IndexSaga { StableKey = "stable" };
        var instance = new SagaInstance<IndexSaga>(state);
        index.Add(instance);
        state.OnReadStable = () => throw new InvalidOperationException("getter must not be repeated");

        index.Add(instance);

        Assert.Equal(1, state.StableReads);
        Assert.Equal(1, index.Count);
        Assert.Same(instance, index["stable"]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-property-second-captured-add-does-not-own-registration")]
    public void IndexedSagaProperty_ApplyingTwoCapturesOfTheSameWrapperMakesTheSecondRegistrationNonOwning()
    {
        var index = CreateStableIndex();
        var state = new IndexSaga { StableKey = "shared" };
        var instance = new SagaInstance<IndexSaga>(state);
        RegistrationProbe first = Capture(index, instance);
        RegistrationProbe second = Capture(index, instance);

        first.Apply();
        second.Apply();
        second.Rollback();

        Assert.Equal(2, state.StableReads);
        Assert.Equal(1, index.Count);
        Assert.Same(instance, index["shared"]);

        first.Rollback();

        Assert.Equal(0, index.Count);
        Assert.Null(index["shared"]);
    }

    [Theory]
    [InlineData("getter", "getProperty")]
    [InlineData("capture", "instance")]
    [InlineData("apply", "apply")]
    [InlineData("rollback", "rollback")]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-staged-registration-null-boundary-matrix")]
    public void StagedRegistration_RejectsEveryNullCollaborator(string boundary, string parameterName)
    {
        ArgumentNullException exception = boundary switch
        {
            "getter" => Assert.Throws<ArgumentNullException>(() =>
                CreatePropertyIndex(null!)),
            "capture" => Assert.Throws<ArgumentNullException>(() =>
                Capture(CreateStableIndex(), null!)),
            "apply" => Assert.Throws<ArgumentNullException>(() =>
                CreateRegistration("key", null!, () => { })),
            _ => Assert.Throws<ArgumentNullException>(() =>
                CreateRegistration("key", () => true, null!)),
        };

        Assert.Equal(parameterName, exception.ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-registration-double-apply-preserves-rollback-ownership")]
    public void SagaIndexRegistration_RepeatedApplyCannotEraseRollbackOwnership()
    {
        int applyCalls = 0;
        int rollbackCalls = 0;
        RegistrationProbe registration = CreateRegistration(
            "key",
            () => { applyCalls++; return true; },
            () => rollbackCalls++);

        registration.Apply();
        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(registration.Apply);
        registration.Rollback();
        registration.Rollback();

        Assert.Contains("already attempted", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, applyCalls);
        Assert.Equal(1, rollbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-registration-concurrent-apply-is-single-flight")]
    public async Task SagaIndexRegistration_ConcurrentApplyInvokesThePublisherExactlyOnceAsync()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int applyCalls = 0;
        int rollbackCalls = 0;
        RegistrationProbe registration = CreateRegistration(
            "key",
            () =>
            {
                Interlocked.Increment(ref applyCalls);
                entered.TrySetResult(true);
                release.Wait(TestContext.Current.CancellationToken);
                return true;
            },
            () => rollbackCalls++);
        Task first = Task.Run(registration.Apply, TestContext.Current.CancellationToken);

        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(registration.Apply);
            Assert.Contains("already attempted", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            release.Set();
            await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        registration.Rollback();
        Assert.Equal(1, applyCalls);
        Assert.Equal(1, rollbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-registration-concurrent-rollback-is-single-flight")]
    public async Task SagaIndexRegistration_ConcurrentRollbackInvokesTheCallbackExactlyOnceAsync()
    {
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int rollbackCalls = 0;
        RegistrationProbe registration = CreateRegistration(
            "key",
            () => true,
            () =>
            {
                Interlocked.Increment(ref rollbackCalls);
                entered.TrySetResult(true);
                release.Wait(TestContext.Current.CancellationToken);
            });
        registration.Apply();
        Task first = Task.Run(registration.Rollback, TestContext.Current.CancellationToken);
        Task? competing = null;

        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            competing = Task.Run(registration.Rollback, TestContext.Current.CancellationToken);
            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => competing.WaitAsync(TimeSpan.FromSeconds(1), TestContext.Current.CancellationToken));
            Assert.Contains("transition in progress", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            release.Set();
            await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            if (competing is not null)
            {
                try
                {
                    await competing.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
                }
                catch (InvalidOperationException)
                {
                }
            }
        }

        registration.Rollback();
        Assert.Equal(1, rollbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-registration-reentrant-apply-is-terminal")]
    public void SagaIndexRegistration_ReentrantApplyIsRejectedWithoutRollbackOwnership()
    {
        RegistrationProbe? registration = null;
        int applyCalls = 0;
        int rollbackCalls = 0;
        registration = CreateRegistration(
            "key",
            () =>
            {
                applyCalls++;
                registration!.Apply();
                return true;
            },
            () => rollbackCalls++);

        InvalidOperationException first = Assert.Throws<InvalidOperationException>(registration.Apply);
        InvalidOperationException retry = Assert.Throws<InvalidOperationException>(registration.Apply);
        registration.Rollback();

        Assert.Contains("already attempted", first.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("already attempted", retry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, applyCalls);
        Assert.Equal(0, rollbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-registration-reentrant-rollback-rejected-during-apply")]
    public void SagaIndexRegistration_ReentrantRollbackDuringApplyIsRejectedWithoutLosingOwnership()
    {
        RegistrationProbe? registration = null;
        InvalidOperationException? transitionFailure = null;
        int rollbackCalls = 0;
        registration = CreateRegistration(
            "key",
            () =>
            {
                transitionFailure = Assert.Throws<InvalidOperationException>(registration!.Rollback);
                return true;
            },
            () => rollbackCalls++);

        registration.Apply();

        Assert.NotNull(transitionFailure);
        Assert.Contains("transition in progress", transitionFailure.Message, StringComparison.OrdinalIgnoreCase);
        registration.Rollback();
        Assert.Equal(1, rollbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-registration-failed-rollback-remains-retryable")]
    public void SagaIndexRegistration_FailedRollbackCanBeRetriedAndSuccessIsIdempotent()
    {
        var expected = new InvalidOperationException("rollback failed");
        int rollbackCalls = 0;
        RegistrationProbe registration = CreateRegistration(
            "key",
            () => true,
            () =>
            {
                rollbackCalls++;
                if (rollbackCalls == 1)
                    throw expected;
            });
        registration.Apply();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(registration.Rollback);
        registration.Rollback();
        registration.Rollback();

        Assert.Same(expected, exception);
        Assert.Equal(2, rollbackCalls);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-registration-rolls-back-only-owned-additions")]
    public void SagaIndexRegistration_RollbackRunsOnlyForAnOwnedAddition(bool added)
    {
        int rollbackCalls = 0;
        RegistrationProbe registration = CreateRegistration("key", () => added, () => rollbackCalls++);

        registration.Rollback();
        registration.Apply();
        registration.Rollback();
        registration.Rollback();

        Assert.Equal(added ? 1 : 0, rollbackCalls);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-concurrent-same-wrapper-registration-is-single-flight")]
    public async Task Dictionary_ConcurrentRegistrationOfTheSameWrapperRejectsThePendingAttemptAndPublishesOnceAsync()
    {
        var dictionary = new IndexedSagaDictionary<IndexSaga>();
        using var release = new ManualResetEventSlim();
        var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int reads = 0;
        var state = new IndexSaga
        {
            OnReadStable = () =>
            {
                Interlocked.Increment(ref reads);
                entered.TrySetResult(true);
                release.Wait(TestContext.Current.CancellationToken);
            },
        };
        var instance = new SagaInstance<IndexSaga>(state);
        Task first = Task.Run(() => dictionary.Add(instance), TestContext.Current.CancellationToken);

        try
        {
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));
            Assert.Equal("A saga registration is already in progress for this wrapper.", exception.Message);
        }
        finally
        {
            release.Set();
            await first.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        Assert.Equal(1, reads);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[state.CorrelationId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "deep-dictionary-late-invalidation-before-publication")]
    public void Dictionary_InvalidationAfterCaptureRejectsTheWrapperBeforePublication()
    {
        var dictionary = new IndexedSagaDictionary<LateInvalidationSaga>();
        Guid correlationId = NewId.NextGuid();
        var state = new LateInvalidationSaga { CorrelationId = correlationId };
        var instance = new SagaInstance<LateInvalidationSaga>(state);
        state.OnReadCorrelation = read =>
        {
            if (read == 2)
                instance.Remove();
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Equal("An invalidated or removing saga wrapper cannot be registered.", exception.Message);
        Assert.Equal(2, state.CorrelationReads);
        Assert.True(instance.IsRemoved);
        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[correlationId]);
    }

    private static IndexedSagaProperty<IndexSaga, string> CreateStableIndex() =>
        new(typeof(IndexSaga).GetProperty(nameof(IndexSaga.StableKey))!);

    private static IndexedSagaProperty<IndexSaga, string> CreatePropertyIndex(Func<IndexSaga, string?> getProperty)
    {
        ConstructorInfo constructor = typeof(IndexedSagaProperty<IndexSaga, string>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            [typeof(Func<IndexSaga, string?>)],
            modifiers: null)!;

        return (IndexedSagaProperty<IndexSaga, string>)Invoke(constructor, [getProperty]);
    }

    private static RegistrationProbe Capture(
        IndexedSagaProperty<IndexSaga, string> index,
        SagaInstance<IndexSaga> instance)
    {
        MethodInfo method = typeof(IndexedSagaProperty<IndexSaga, string>)
            .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
            .Single(candidate => candidate.Name.EndsWith(".Capture", StringComparison.Ordinal));

        return new RegistrationProbe(Invoke(method, index, [instance])!);
    }

    private static RegistrationProbe CreateRegistration(object? key, Func<bool> apply, Action rollback)
    {
        Type type = typeof(IndexedSagaDictionary<>).Assembly.GetType(
            "ViciOne.ServiceBus.Saga.SagaIndexRegistration",
            throwOnError: true)!;
        ConstructorInfo constructor = type.GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            [typeof(object), typeof(Func<bool>), typeof(Action)],
            modifiers: null)!;

        return new RegistrationProbe(Invoke(constructor, [key, apply, rollback]));
    }

    private static object Invoke(ConstructorInfo constructor, object?[] arguments)
    {
        try
        {
            return constructor.Invoke(arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private static object? Invoke(MethodInfo method, object target, object?[]? arguments = null)
    {
        try
        {
            return method.Invoke(target, arguments);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
            throw;
        }
    }

    private sealed class RegistrationProbe
    {
        readonly MethodInfo _apply;
        readonly PropertyInfo _key;
        readonly object _registration;
        readonly MethodInfo _rollback;

        public RegistrationProbe(object registration)
        {
            _registration = registration;
            Type type = registration.GetType();
            _apply = type.GetMethod("Apply", BindingFlags.Instance | BindingFlags.Public)!;
            _key = type.GetProperty("Key", BindingFlags.Instance | BindingFlags.Public)!;
            _rollback = type.GetMethod("Rollback", BindingFlags.Instance | BindingFlags.Public)!;
        }

        public object? Key => _key.GetValue(_registration);

        public void Apply() => Invoke(_apply, _registration);

        public void Rollback() => Invoke(_rollback, _registration);
    }

    private sealed class IndexSaga : ISaga
    {
        string? _stableKey = "stable";

        public Guid CorrelationId { get; set; } = NewId.NextGuid();

        public Action? OnReadStable { get; set; }

        public int StableReads { get; private set; }

        [Indexed]
        public string? StableKey
        {
            get
            {
                StableReads++;
                OnReadStable?.Invoke();
                return _stableKey;
            }
            set => _stableKey = value;
        }
    }

    private sealed class LateInvalidationSaga : ISaga
    {
        Guid _correlationId;

        public int CorrelationReads { get; private set; }

        public Action<int>? OnReadCorrelation { get; set; }

        public Guid CorrelationId
        {
            get
            {
                int read = ++CorrelationReads;
                OnReadCorrelation?.Invoke(read);
                return _correlationId;
            }
            set => _correlationId = value;
        }
    }
}
