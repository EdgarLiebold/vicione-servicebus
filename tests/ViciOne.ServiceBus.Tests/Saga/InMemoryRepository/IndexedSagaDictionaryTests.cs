using System.Linq.Expressions;
using ViciOne.ServiceBus.Saga;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Saga.InMemoryRepository;

public sealed class IndexedSagaDictionaryTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "bare-boolean-query-evaluates-real-state")]
    public void BareBooleanQuery_MatchesOnlyActualReferencedState(bool enabled)
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var matching = new SagaInstance<IndexedState>(new IndexedState { Enabled = enabled });
        var other = new SagaInstance<IndexedState>(new IndexedState { Enabled = !enabled });
        dictionary.Add(matching);
        dictionary.Add(other);
        ISagaQuery<IndexedState> query = enabled
            ? new SagaQuery<IndexedState>(state => state.Enabled)
            : new SagaQuery<IndexedState>(state => !state.Enabled);

        Assert.Same(matching, Assert.Single(dictionary.Where(query)));
        Assert.Equal(2, dictionary.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "query-observes-current-mutable-secondary-state")]
    public void EqualityQuery_ObservesCurrentMutableKeysAndNonIndexedState()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState { Group = "before", Value = 3 });
        dictionary.Add(instance);
        instance.Instance.Group = "after";
        instance.Instance.Value = 7;

        Assert.Empty(dictionary.Where(new SagaQuery<IndexedState>(state => state.Group == "before")));
        Assert.Same(instance, Assert.Single(dictionary.Where(new SagaQuery<IndexedState>(state => state.Group == "after" && state.Value == 7))));
        Assert.Same(instance.Instance, Assert.Single(dictionary.Select(state => state)));
    }

    [Theory]
    [InlineData("Add", "instance")]
    [InlineData("Remove", "instance")]
    [InlineData("Where", "query")]
    [InlineData("Select", "transformer")]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "dictionary-required-inputs-fail-before-effects")]
    public void RequiredInput_IsRejectedBeforeChangingMembership(string operation, string parameter)
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        dictionary.Add(instance);

        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
        {
            switch (operation)
            {
                case "Add": dictionary.Add(null!); break;
                case "Remove": dictionary.Remove(null!); break;
                case "Where": _ = dictionary.Where(null!); break;
                case "Select": _ = dictionary.Select<IndexedState>(null!); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.Equal(parameter, exception.ParamName);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        Assert.False(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "duplicate-id-rejected-without-replacing-state")]
    public void DuplicateCorrelationId_IsRejectedWithoutPublishingAnotherWrapper()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var first = new SagaInstance<IndexedState>(new IndexedState());
        var duplicate = new SagaInstance<IndexedState>(new IndexedState { CorrelationId = first.Instance.CorrelationId, Group = "different" });
        dictionary.Add(first);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(duplicate));

        Assert.Contains(first.Instance.CorrelationId.ToString(), exception.Message, StringComparison.Ordinal);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(first, dictionary[first.Instance.CorrelationId]);
        Assert.False(first.IsRemoved);
        Assert.False(duplicate.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "same-wrapper-registration-does-not-repeat-getters")]
    public void RepeatedRegistration_DoesNotRepeatIndexedGetters()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        int reads = 0;
        instance.Instance.OnReadGroup = () => reads++;
        dictionary.Add(instance);
        int initialReads = reads;
        instance.Instance.OnReadGroup = () => throw new InvalidOperationException("repeated getter");

        dictionary.Add(instance);

        Assert.Equal(1, initialReads);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "later-getter-fault-cannot-partially-publish-membership")]
    public void LaterIndexedGetterFault_PreservesTheExactFaultWithoutPartialPublication()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var retained = new SagaInstance<IndexedState>(new IndexedState());
        dictionary.Add(retained);
        var expected = new InvalidOperationException("indexed getter failed");
        var rejected = new SagaInstance<IndexedState>(new IndexedState { OnReadGroup = () => throw expected });

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(rejected));

        Assert.Same(expected, exception);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(retained, dictionary[retained.Instance.CorrelationId]);
        Assert.Null(dictionary[rejected.Instance.CorrelationId]);
        Assert.False(rejected.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "initial-getter-fault-publishes-no-identifier-and-leaves-registration-reusable")]
    public void InitialIndexedGetterFault_PublishesNoIdentifierAndAllowsRetry()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var expected = new InvalidOperationException("initial indexed getter failed");
        var state = new IndexedState { OnReadGroup = () => throw expected };
        Guid correlationId = state.CorrelationId;
        var instance = new SagaInstance<IndexedState>(state);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Same(expected, exception);
        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[correlationId]);
        Assert.False(instance.IsRemoved);
        state.OnReadGroup = null;
        dictionary.Add(instance);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[correlationId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "correlation-changed-during-key-capture-is-diagnosed-before-publication")]
    public void GetterCorrelationMutation_IsDiagnosedDuringAdmissionWithoutPublishingEitherIdentifier()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var state = new IndexedState();
        Guid capturedId = state.CorrelationId;
        Guid changedId = NewId.NextGuid();
        state.OnReadGroup = () => state.CorrelationId = changedId;
        var instance = new SagaInstance<IndexedState>(state);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Equal($"Saga {capturedId} changed its correlation identifier during registration.", exception.Message);
        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[capturedId]);
        Assert.Null(dictionary[changedId]);
        Assert.Equal(changedId, state.CorrelationId);
        Assert.False(instance.IsRemoved);
        state.OnReadGroup = null;
        dictionary.Add(instance);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[changedId]);
        Assert.Null(dictionary[capturedId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "nullable-secondary-state-registers-and-removes-cleanly")]
    public void NullSecondaryKey_IsQueryableAndRemovableWithoutPartialPublication()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState { Group = null });
        dictionary.Add(instance);

        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        Assert.Same(instance, Assert.Single(dictionary.Where(new SagaQuery<IndexedState>(state => state.Group == null))));
        dictionary.Remove(instance);
        Assert.Equal(0, dictionary.Count);
        Assert.True(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "mutable-state-hash-does-not-strand-removal")]
    public void MutableStateHash_DoesNotPreventCompleteDictionaryRemoval()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState { Value = 11 });
        dictionary.Add(instance);
        instance.Instance.Value = 29;
        dictionary.Remove(instance);

        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[instance.Instance.CorrelationId]);
        Assert.Empty(dictionary.Select(state => state));
        Assert.True(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "equal-unregistered-wrapper-cannot-remove-or-invalidate-retained-state")]
    public void EqualUnregisteredWrapper_CannotRemoveOrInvalidateTheRegisteredWrapper()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var retained = new SagaInstance<IndexedState>(new IndexedState());
        var unrelated = new SagaInstance<IndexedState>(new IndexedState { CorrelationId = retained.Instance.CorrelationId });
        dictionary.Add(retained);
        Assert.Equal(retained, unrelated);
        dictionary.Remove(unrelated);

        Assert.Equal(1, dictionary.Count);
        Assert.Same(retained, dictionary[retained.Instance.CorrelationId]);
        Assert.False(retained.IsRemoved);
        Assert.False(unrelated.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "stale-wrapper-removal-does-not-touch-replacement")]
    public void StaleWrapperRemoval_DoesNotTouchAReplacementWithTheSameIdentifier()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var stale = new SagaInstance<IndexedState>(new IndexedState());
        dictionary.Add(stale);
        dictionary.Remove(stale);
        var replacement = new SagaInstance<IndexedState>(new IndexedState { CorrelationId = stale.Instance.CorrelationId });
        dictionary.Add(replacement);
        dictionary.Remove(stale);

        Assert.Equal(1, dictionary.Count);
        Assert.Same(replacement, dictionary[replacement.Instance.CorrelationId]);
        Assert.True(stale.IsRemoved);
        Assert.False(replacement.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "captured-correlation-key-allows-cleanup-after-state-mutation")]
    public void CorrelationMutation_DoesNotPreventCleanupOfTheRegisteredIdentifier()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        Guid registeredId = instance.Instance.CorrelationId;
        dictionary.Add(instance);
        instance.Instance.CorrelationId = NewId.NextGuid();
        dictionary.Remove(instance);

        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[registeredId]);
        Assert.Null(dictionary[instance.Instance.CorrelationId]);
        Assert.True(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "select-materializes-original-membership-and-values")]
    public void Select_MaterializesOriginalMembershipAndTransformedValues()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var first = new SagaInstance<IndexedState>(new IndexedState { Value = 5 });
        dictionary.Add(first);
        IEnumerable<int> selected = dictionary.Select(state => state.Value);
        first.Instance.Value = 13;
        dictionary.Add(new SagaInstance<IndexedState>(new IndexedState { Value = 17 }));

        Assert.Equal(5, Assert.Single(selected));
        Assert.Equal(2, dictionary.Count);
    }

    [Theory]
    [InlineData("Lookup")]
    [InlineData("Where")]
    [InlineData("Select")]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "correlation-mutation-is-rejected-before-lookup-or-query-results")]
    public void CorrelationMutation_IsRejectedBeforeResultsWithoutPreventingReferenceCleanup(string operation)
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        Guid registeredId = instance.Instance.CorrelationId;
        dictionary.Add(instance);
        instance.Instance.CorrelationId = NewId.NextGuid();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        {
            switch (operation)
            {
                case "Lookup": _ = dictionary[registeredId]; break;
                case "Where": _ = dictionary.Where(new SagaQuery<IndexedState>(state => true)); break;
                case "Select": _ = dictionary.Select(state => state); break;
                default: throw new ArgumentOutOfRangeException(nameof(operation));
            }
        });

        Assert.Equal($"Saga {registeredId} changed its correlation identifier while registered.", exception.Message);
        Assert.Equal(1, dictionary.Count);
        Assert.False(instance.IsRemoved);
        dictionary.Remove(instance);
        Assert.Equal(0, dictionary.Count);
        Assert.True(instance.IsRemoved);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "callback-correlation-mutation-is-rejected-before-result-publication")]
    public void CallbackCorrelationMutation_IsRejectedBeforePublishingResults(bool transform)
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        Guid registeredId = instance.Instance.CorrelationId;
        dictionary.Add(instance);
        Func<IndexedState, bool> filter = state => { state.CorrelationId = NewId.NextGuid(); return true; };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
        {
            if (transform)
                _ = dictionary.Select(state => { state.CorrelationId = NewId.NextGuid(); return state; });
            else
                _ = dictionary.Where(new SagaQuery<IndexedState>(state => filter(state)));
        });

        Assert.Equal($"Saga {registeredId} changed its correlation identifier while registered.", exception.Message);
        Assert.Equal(1, dictionary.Count);
        dictionary.Remove(instance);
        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "where-callback-mutation-cannot-change-original-membership")]
    public void WhereCallback_CanAddStateWithoutCorruptingItsOriginalMembershipSnapshot()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var first = new SagaInstance<IndexedState>(new IndexedState());
        var added = new SagaInstance<IndexedState>(new IndexedState());
        dictionary.Add(first);
        int calls = 0;
        Func<IndexedState, bool> filter = state => { calls++; dictionary.Add(added); return true; };
        IEnumerable<SagaInstance<IndexedState>> selected = dictionary.Where(new SagaQuery<IndexedState>(state => filter(state)));

        Assert.Same(first, Assert.Single(selected));
        Assert.Equal(1, calls);
        Assert.Equal(2, dictionary.Count);
        Assert.Same(added, dictionary[added.Instance.CorrelationId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "select-callback-mutation-cannot-change-original-membership")]
    public void SelectCallback_CanAddStateWithoutCorruptingItsOriginalMembershipSnapshot()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var first = new SagaInstance<IndexedState>(new IndexedState());
        var added = new SagaInstance<IndexedState>(new IndexedState());
        dictionary.Add(first);
        int calls = 0;
        IEnumerable<IndexedState> selected = dictionary.Select(state => { calls++; dictionary.Add(added); return state; });

        Assert.Same(first.Instance, Assert.Single(selected));
        Assert.Equal(1, calls);
        Assert.Equal(2, dictionary.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "canonical-correlation-uses-explicit-saga-interface")]
    public void ExplicitSagaCorrelationId_IsUsedWithoutAConcretePublicProperty()
    {
        var dictionary = new IndexedSagaDictionary<ExplicitState>();
        var state = new ExplicitState();
        Guid id = ((ISaga)state).CorrelationId;
        var instance = new SagaInstance<ExplicitState>(state);
        dictionary.Add(instance);

        Assert.Same(instance, dictionary[id]);
        Assert.Same(state, Assert.Single(dictionary.Select(value => value)));
        dictionary.Remove(instance);
        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[id]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "query-null-filter-boundary-rejected-without-effects")]
    public void NullQueryFilter_IsRejectedBeforeChangingMembership()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        dictionary.Add(instance);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Where(new NullFilterQuery()));

        Assert.Equal("The saga query returned a null filter.", exception.Message);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "user-callbacks-run-outside-dictionary-lock")]
    public async Task UserCallback_CanObserveDictionaryFromAnotherThreadAsync(bool transform)
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        dictionary.Add(instance);
        using var cancellation = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cancellation.CancelAfter(TimeSpan.FromSeconds(5));
        Task<int>? observer = null;
        int Observe(IndexedState state)
        {
            observer = Task.Run(() => dictionary.Count, TestContext.Current.CancellationToken);
            return observer.WaitAsync(cancellation.Token).GetAwaiter().GetResult();
        }
        Func<IndexedState, int> observe = Observe;

        Exception? callbackError;
        try
        {
            callbackError = Record.Exception(() =>
            {
                if (transform)
                    Assert.Equal(1, Assert.Single(dictionary.Select(Observe)));
                else
                    Assert.Same(instance, Assert.Single(dictionary.Where(new SagaQuery<IndexedState>(state => observe(state) == 1))));
            });
        }
        finally
        {
            cancellation.Cancel();
            if (observer != null)
                Assert.Equal(1, await observer);
        }

        Assert.Null(callbackError);
        Assert.NotNull(observer);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "dictionary-unowned-or-repeat-release-cannot-increase-lease-capacity")]
    public async Task UnownedOrRepeatedRelease_CannotIncreaseDictionaryLeaseCapacityAsync(bool acquired)
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        if (acquired)
        {
            await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
            dictionary.Release();
        }

        Assert.Throws<SemaphoreFullException>(dictionary.Release);
        await dictionary.MarkInUseAsync(TestContext.Current.CancellationToken);
        Assert.Null(Record.Exception(dictionary.Release));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "invalidated-wrapper-cannot-be-registered-as-new-state")]
    public void InvalidatedWrapper_CannotBeRegisteredAsNewState()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        instance.Remove();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Equal("An invalidated or removing saga wrapper cannot be registered.", exception.Message);
        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[instance.Instance.CorrelationId]);
        Assert.True(instance.IsRemoved);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "reentrant-dictionary-registration-fails-before-repeating-getter-and-is-reusable")]
    public void ReentrantRegistration_IsRejectedBeforeRepeatingItsGetterAndCanBeRetried()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var state = new IndexedState();
        var instance = new SagaInstance<IndexedState>(state);
        int reads = 0;
        state.OnReadGroup = () =>
        {
            if (++reads > 1)
                throw new InvalidOperationException("fixture prevented recursive getter overflow");
            dictionary.Add(instance);
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Equal("A saga registration is already in progress for this wrapper.", exception.Message);
        Assert.Equal(1, reads);
        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[instance.Instance.CorrelationId]);
        state.OnReadGroup = null;
        dictionary.Add(instance);
        Assert.Same(instance, dictionary[instance.Instance.CorrelationId]);
        Assert.Equal(1, dictionary.Count);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "retired-callback-state-may-change-id-without-corrupting-original-snapshot")]
    public void CallbackRemoval_CanChangeRetiredCorrelationWithoutInvalidatingItsSnapshot(bool transform)
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        Guid registeredId = instance.Instance.CorrelationId;
        Guid retiredId = NewId.NextGuid();
        dictionary.Add(instance);
        Func<IndexedState, bool> retire = state =>
        {
            dictionary.Remove(instance);
            state.CorrelationId = retiredId;
            return true;
        };

        if (transform)
            Assert.Same(instance.Instance, Assert.Single(dictionary.Select(state => { retire(state); return state; })));
        else
            Assert.Same(instance, Assert.Single(dictionary.Where(new SagaQuery<IndexedState>(state => retire(state)))));

        Assert.Equal(0, dictionary.Count);
        Assert.True(instance.IsRemoved);
        Assert.Null(dictionary[registeredId]);
        Assert.Null(dictionary[retiredId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "inherited-interface-indexed-getter-is-staged-before-publication")]
    public void InheritedInterfaceIndexedGetter_IsReadExactlyOnceBeforePublication()
    {
        var dictionary = new IndexedSagaDictionary<IDerivedIndexedState>();
        var expected = new InvalidOperationException("inherited interface getter failed");
        var state = new InterfaceIndexedState { OnReadGroup = () => throw expected };
        var instance = new SagaInstance<IDerivedIndexedState>(state);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Same(expected, exception);
        Assert.Equal(1, state.GroupReads);
        Assert.Equal(0, dictionary.Count);
        Assert.False(instance.IsRemoved);
        state.OnReadGroup = null;
        dictionary.Add(instance);
        Assert.Equal(2, state.GroupReads);
        Assert.Same(instance, dictionary[state.CorrelationId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "explicit-interface-indexed-getter-is-staged-before-publication")]
    public void ExplicitInterfaceIndexedGetter_IsReadExactlyOnceBeforePublication()
    {
        var dictionary = new IndexedSagaDictionary<ExplicitIndexedState>();
        var expected = new InvalidOperationException("explicit interface getter failed");
        var state = new ExplicitIndexedState { OnReadGroup = () => throw expected };
        var instance = new SagaInstance<ExplicitIndexedState>(state);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Same(expected, exception);
        Assert.Equal(1, state.GroupReads);
        Assert.Equal(0, dictionary.Count);
        state.OnReadGroup = null;
        dictionary.Add(instance);
        Assert.Equal(2, state.GroupReads);
        Assert.Same(instance, dictionary[state.CorrelationId]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "interface-and-concrete-metadata-share-one-implemented-getter")]
    public void InterfaceAndConcreteIndexedMetadata_DoesNotRepeatTheSameGetter()
    {
        var dictionary = new IndexedSagaDictionary<InterfaceIndexedState>();
        var state = new InterfaceIndexedState();
        var instance = new SagaInstance<InterfaceIndexedState>(state);
        dictionary.Add(instance);

        Assert.Equal(1, state.GroupReads);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[state.CorrelationId]);
        dictionary.Remove(instance);
        Assert.Equal(1, state.GroupReads);
        Assert.Equal(0, dictionary.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "attributed-correlation-metadata-does-not-duplicate-the-canonical-getter")]
    public void CanonicalCorrelationIndex_DoesNotRepeatAnAttributedIdentifierGetter()
    {
        Guid registeredId = NewId.NextGuid();
        var dictionary = new IndexedSagaDictionary<AttributedCorrelationState>();
        var state = new AttributedCorrelationState { CorrelationId = registeredId };
        var instance = new SagaInstance<AttributedCorrelationState>(state);
        dictionary.Add(instance);

        Assert.Equal(2, state.CorrelationReads);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[registeredId]);
        Assert.Equal(3, state.CorrelationReads);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "inherited-indexed-override-getter-is-staged-before-publication")]
    public void InheritedIndexedOverride_PublishesOnlyAfterItsGetterSucceeds()
    {
        var dictionary = new IndexedSagaDictionary<OverrideIndexedState>();
        var expected = new InvalidOperationException("inherited override getter failed");
        var state = new OverrideIndexedState { OnReadGroup = () => throw expected };
        var instance = new SagaInstance<OverrideIndexedState>(state);

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Same(expected, exception);
        Assert.Equal(1, state.GroupReads);
        Assert.Equal(0, dictionary.Count);
        state.OnReadGroup = null;
        dictionary.Add(instance);
        Assert.Equal(2, state.GroupReads);
        Assert.Same(instance, dictionary[state.CorrelationId]);
    }

    [Theory]
    [InlineData("static")]
    [InlineData("indexer")]
    [InlineData("write-only")]
    [InlineData("ref-return")]
    [InlineData("ref-like")]
    [InlineData("pointer")]
    [InlineData("function-pointer")]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "unsupported-attributed-member-has-property-specific-construction-diagnostic")]
    public void UnsupportedAttributedMetadata_IsRejectedWithPropertySpecificDiagnosis(string kind)
    {
        (Type StateType, string PropertyName, Action Construct) metadata = kind switch
        {
            "static" => (typeof(StaticIndexedState), "Value", () => { _ = new IndexedSagaDictionary<StaticIndexedState>(); }),
            "indexer" => (typeof(IndexerIndexedState), "Item", () => { _ = new IndexedSagaDictionary<IndexerIndexedState>(); }),
            "write-only" => (typeof(WriteOnlyIndexedState), "Value", () => { _ = new IndexedSagaDictionary<WriteOnlyIndexedState>(); }),
            "ref-return" => (typeof(RefIndexedState), "Value", () => { _ = new IndexedSagaDictionary<RefIndexedState>(); }),
            "ref-like" => (typeof(RefLikeIndexedState), "Value", () => { _ = new IndexedSagaDictionary<RefLikeIndexedState>(); }),
            "pointer" => (typeof(PointerIndexedState), "Value", () => { _ = new IndexedSagaDictionary<PointerIndexedState>(); }),
            "function-pointer" => (typeof(FunctionPointerIndexedState), "Value", () => { _ = new IndexedSagaDictionary<FunctionPointerIndexedState>(); }),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(metadata.Construct);

        Assert.Equal($"Indexed saga property '{metadata.StateType.FullName}.{metadata.PropertyName}' must be a readable instance non-indexer property with a supported key type.", exception.Message);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "wrapper-invalidation-without-dictionary-removal-does-not-relax-the-registered-id-invariant")]
    public void DirectWrapperInvalidation_DoesNotRelaxTheRegisteredCorrelationInvariant()
    {
        var dictionary = new IndexedSagaDictionary<IndexedState>();
        var instance = new SagaInstance<IndexedState>(new IndexedState());
        Guid registeredId = instance.Instance.CorrelationId;
        dictionary.Add(instance);
        instance.Remove();
        instance.Instance.CorrelationId = NewId.NextGuid();

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Where(new SagaQuery<IndexedState>(state => true)));

        Assert.Equal($"Saga {registeredId} changed its correlation identifier while registered.", exception.Message);
        Assert.Equal(1, dictionary.Count);
        Assert.True(instance.IsRemoved);
        dictionary.Remove(instance);
        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[registeredId]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "closed-generic-interface-getters-with-shared-metadata-tokens-remain-independent")]
    public void ClosedGenericInterfaceGetters_RetainEachImplementedMemberAndStageItsFault(bool textFault)
    {
        var dictionary = new IndexedSagaDictionary<IGenericIndexedState>();
        var state = new GenericIndexedState();
        var instance = new SagaInstance<IGenericIndexedState>(state);
        var expected = new InvalidOperationException("closed generic interface getter failed");
        if (textFault)
            state.OnReadText = () => throw expected;
        else
            state.OnReadNumber = () => throw expected;

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));

        Assert.Same(expected, exception);
        Assert.Equal(1, textFault ? state.TextReads : state.NumberReads);
        Assert.Equal(0, dictionary.Count);
        Assert.Null(dictionary[state.CorrelationId]);
        int previousNumbers = state.NumberReads;
        int previousTexts = state.TextReads;
        state.OnReadNumber = null;
        state.OnReadText = null;
        dictionary.Add(instance);
        Assert.Equal(previousNumbers + 1, state.NumberReads);
        Assert.Equal(previousTexts + 1, state.TextReads);
        Assert.Equal(1, dictionary.Count);
        Assert.Same(instance, dictionary[state.CorrelationId]);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequirementCoverage("REQ-VSB-SAGA-INDEX-INTEGRITY", "inherited-default-interface-getter-is-captured-once-with-exact-fault-and-retry")]
    public void DefaultInterfaceGetter_StagesItsExactFaultAndAllowsRetryForBothDeclaredSagaShapes(bool concreteSaga)
    {
        var state = new DefaultIndexedState();
        var expected = new InvalidOperationException("default interface getter failed");
        int reads = 0;
        state.OnReadGroup = () => { reads++; throw expected; };

        void Verify<TState>(IndexedSagaDictionary<TState> dictionary, TState reference)
            where TState : class, ISaga
        {
            var instance = new SagaInstance<TState>(reference);
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => dictionary.Add(instance));
            Assert.Same(expected, exception);
            Assert.Equal(1, reads);
            Assert.Equal(0, dictionary.Count);
            Assert.Null(dictionary[state.CorrelationId]);
            state.OnReadGroup = () => reads++;
            dictionary.Add(instance);
            Assert.Equal(2, reads);
            Assert.Equal(1, dictionary.Count);
            Assert.Same(instance, dictionary[state.CorrelationId]);
        }

        if (concreteSaga)
            Verify(new IndexedSagaDictionary<DefaultIndexedState>(), state);
        else
            Verify(new IndexedSagaDictionary<IDerivedDefaultIndexedState>(), (IDerivedDefaultIndexedState)state);
    }

    private interface IGenericIndex<T>
    {
        [Indexed] T Key { get; }
    }

    private interface IGenericIndexedState : ISaga, IGenericIndex<int>, IGenericIndex<string>;

    private sealed class GenericIndexedState : IGenericIndexedState
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public Action? OnReadNumber { get; set; }
        public Action? OnReadText { get; set; }
        public int NumberReads { get; private set; }
        public int TextReads { get; private set; }
        int IGenericIndex<int>.Key
        {
            get { NumberReads++; OnReadNumber?.Invoke(); return 7; }
        }
        string IGenericIndex<string>.Key
        {
            get { TextReads++; OnReadText?.Invoke(); return "text"; }
        }
    }

    private interface IBaseDefaultIndexedState : ISaga
    {
        Action? OnReadGroup { get; }
        [Indexed]
        string Group
        {
            get { OnReadGroup?.Invoke(); return "group"; }
        }
    }

    private interface IDerivedDefaultIndexedState : IBaseDefaultIndexedState;

    private sealed class DefaultIndexedState : IDerivedDefaultIndexedState
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public Action? OnReadGroup { get; set; }
    }

    private unsafe sealed class PointerIndexedState : ISaga
    {
        public Guid CorrelationId { get; set; }
        [Indexed] public int* Value => null;
    }

    private unsafe sealed class FunctionPointerIndexedState : ISaga
    {
        public Guid CorrelationId { get; set; }
        [Indexed] public delegate*<int> Value => null;
    }

    private interface IBaseIndexedState : ISaga
    {
        [Indexed] string? Group { get; }
    }

    private interface IDerivedIndexedState : IBaseIndexedState;

    private sealed class InterfaceIndexedState : IDerivedIndexedState
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public Action? OnReadGroup { get; set; }
        public int GroupReads { get; private set; }
        [Indexed]
        public string? Group
        {
            get
            {
                GroupReads++;
                OnReadGroup?.Invoke();
                return "group";
            }
        }
    }

    private sealed class ExplicitIndexedState : IBaseIndexedState
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        public Action? OnReadGroup { get; set; }
        public int GroupReads { get; private set; }
        string? IBaseIndexedState.Group
        {
            get
            {
                GroupReads++;
                OnReadGroup?.Invoke();
                return "group";
            }
        }
    }

    private class BaseIndexedState : ISaga
    {
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        [Indexed] public virtual string? Group => "group";
    }

    private sealed class OverrideIndexedState : BaseIndexedState
    {
        public Action? OnReadGroup { get; set; }
        public int GroupReads { get; private set; }
        public override string? Group
        {
            get
            {
                GroupReads++;
                OnReadGroup?.Invoke();
                return base.Group;
            }
        }
    }

    private sealed class AttributedCorrelationState : ISaga
    {
        private Guid _correlationId;
        public int CorrelationReads { get; private set; }
        [Indexed]
        public Guid CorrelationId
        {
            get { CorrelationReads++; return _correlationId; }
            set => _correlationId = value;
        }
    }

    private sealed class StaticIndexedState : ISaga
    {
        public Guid CorrelationId { get; set; }
        [Indexed] public static int Value => 7;
    }

    private sealed class IndexerIndexedState : ISaga
    {
        public Guid CorrelationId { get; set; }
        [Indexed] public int this[int index] => index;
    }

    private sealed class WriteOnlyIndexedState : ISaga
    {
        private int _value;
        public Guid CorrelationId { get; set; }
        [Indexed] public int Value { set => _value = value; }
        public int ReadValue() => _value;
    }

    private sealed class RefIndexedState : ISaga
    {
        private int _value;
        public Guid CorrelationId { get; set; }
        [Indexed] public ref int Value => ref _value;
    }

    private sealed class RefLikeIndexedState : ISaga
    {
        public Guid CorrelationId { get; set; }
        [Indexed] public ReadOnlySpan<int> Value => ReadOnlySpan<int>.Empty;
    }

    private sealed class NullFilterQuery : ISagaQuery<IndexedState>
    {
        public Expression<Func<IndexedState, bool>> FilterExpression => state => true;
        public Func<IndexedState, bool> GetFilter() => null!;
    }

    private sealed class ExplicitState : ISaga
    {
        Guid ISaga.CorrelationId { get; set; } = NewId.NextGuid();
    }

    private sealed class IndexedState : ISaga
    {
        private string? _group = "group";
        public Guid CorrelationId { get; set; } = NewId.NextGuid();
        [Indexed] public bool Enabled { get; set; }
        [Indexed] public string? Group { get { OnReadGroup?.Invoke(); return _group; } set => _group = value; }
        public int Value { get; set; }
        public Action? OnReadGroup { get; set; }
        public override bool Equals(object? other) => other is IndexedState state && state.Value == Value;
        public override int GetHashCode() => Value;
    }
}
