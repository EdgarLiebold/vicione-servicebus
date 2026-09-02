using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests.Middleware.Configuration.Publish;

public sealed class PublishPipeSpecificationTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-PIPE-COMPOSITION", "implemented-parent-direct-and-base-order")]
    public void ConcreteMessage_ComposesEveryOwnedLayerOnceInStableOrder()
    {
        var trace = new List<string>();
        var parent = new PublishPipeSpecification();
        parent.AddPipeSpecification(new RecordingSpecification<PublishContext>(trace, "parent-base"));
        parent.GetMessageSpecification<IAlphaContract>()
            .AddPipeSpecification(new RecordingSpecification<PublishContext<IAlphaContract>>(trace, "parent-alpha"));
        parent.GetMessageSpecification<IZuluContract>()
            .AddPipeSpecification(new RecordingSpecification<PublishContext<IZuluContract>>(trace, "parent-zulu"));
        parent.GetMessageSpecification<ConcreteMessage>()
            .AddPipeSpecification(new RecordingSpecification<PublishContext<ConcreteMessage>>(trace, "parent-concrete"));

        var endpoint = new PublishPipeSpecification();
        endpoint.ConnectPublishPipeSpecificationObserver(new ParentSpecificationObserver(parent));
        endpoint.AddPipeSpecification(new RecordingSpecification<PublishContext>(trace, "endpoint-base"));
        endpoint.GetMessageSpecification<IAlphaContract>()
            .AddPipeSpecification(new RecordingSpecification<PublishContext<IAlphaContract>>(trace, "endpoint-alpha"));
        endpoint.GetMessageSpecification<IZuluContract>()
            .AddPipeSpecification(new RecordingSpecification<PublishContext<IZuluContract>>(trace, "endpoint-zulu"));
        IMessagePublishPipeSpecification<ConcreteMessage> concrete = endpoint.GetMessageSpecification<ConcreteMessage>();
        concrete.AddPipeSpecification(new RecordingSpecification<PublishContext<ConcreteMessage>>(trace, "endpoint-concrete"));

        concrete.Apply(new RecordingBuilder<PublishContext<ConcreteMessage>>());

        Assert.Equal(
            [
                "parent-zulu",
                "endpoint-zulu",
                "parent-alpha",
                "endpoint-alpha",
                "parent-concrete",
                "parent-base",
                "endpoint-concrete",
                "endpoint-base",
            ],
            trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-PIPE-VALIDATION", "owned-results-and-point-in-time-snapshot")]
    public void Validate_ReturnsAStableSnapshotAndReportsEachOwnedSpecificationOnce()
    {
        var specification = new PublishPipeSpecification();
        specification.AddPipeSpecification(new InvalidSpecification<PublishContext>("base", "base-invalid"));
        specification.GetMessageSpecification<ConcreteMessage>()
            .AddPipeSpecification(new InvalidSpecification<PublishContext<ConcreteMessage>>("message", "message-invalid"));

        IEnumerable<ValidationResult> snapshot = specification.Validate();
        specification.GetMessageSpecification<IAlphaContract>()
            .AddPipeSpecification(new InvalidSpecification<PublishContext<IAlphaContract>>("later", "later-invalid"));

        ValidationResult[] original = snapshot.ToArray();
        ValidationResult[] current = specification.Validate().ToArray();

        Assert.Equal(["base", "message"], original.Select(result => result.Key));
        Assert.Equal(["base-invalid", "message-invalid"], original.Select(result => result.Message));
        Assert.Equal(3, current.Length);
        Assert.Contains(current, result => result.Key == "later" && result.Message == "later-invalid");
        Assert.All(current, result => Assert.Equal(ValidationResultDisposition.Failure, result.Disposition));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-PIPE-CREATION", "same-message-observer-reentry")]
    public void ObserverReentryForTheSameMessage_ReturnsTheSinglePublishedSpecification()
    {
        var specification = new PublishPipeSpecification();
        var observer = new ReentrantObserver(specification);
        specification.ConnectPublishPipeSpecificationObserver(observer);

        IMessagePublishPipeSpecification<ConcreteMessage> result = specification.GetMessageSpecification<ConcreteMessage>();

        Assert.Equal(
            [typeof(ConcreteMessage), typeof(IAlphaContract), typeof(IZuluContract)],
            observer.NotificationCounts.Keys.OrderBy(type => type.Name, StringComparer.Ordinal));
        Assert.All(observer.NotificationCounts.Values, count => Assert.Equal(1, count));
        Assert.Same(result, observer.ObservedSpecifications[typeof(ConcreteMessage)]);
        Assert.Same(result, observer.ReentrantSpecifications[typeof(ConcreteMessage)]);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-PIPE-COMPOSITION", "late-root-specification-reaches-existing-message")]
    public void RootSpecificationAddedAfterMessageCreation_ReachesTheExistingMessageExactlyOnce()
    {
        var trace = new List<string>();
        var specification = new PublishPipeSpecification();
        IMessagePublishPipeSpecification<StandaloneMessage> messageSpecification =
            specification.GetMessageSpecification<StandaloneMessage>();

        specification.AddPipeSpecification(new RecordingSpecification<PublishContext>(trace, "late-root"));
        messageSpecification.Apply(new RecordingBuilder<PublishContext<StandaloneMessage>>());

        Assert.Equal(["late-root"], trace);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-PUBLISH-PIPE-CREATION", "failed-initialization-is-not-cached")]
    public void FailedObserverInitialization_IsDiscardedBeforeTheMessageCanBeRequestedAgain()
    {
        var specification = new PublishPipeSpecification();
        var observer = new ThrowOnceObserver();
        specification.ConnectPublishPipeSpecificationObserver(observer);

        Assert.Throws<ExpectedInitializationException>(() => specification.GetMessageSpecification<StandaloneMessage>());
        IMessagePublishPipeSpecification<StandaloneMessage> recovered = specification.GetMessageSpecification<StandaloneMessage>();

        Assert.Same(recovered, specification.GetMessageSpecification<StandaloneMessage>());
        Assert.Equal(2, observer.NotificationCount);
    }

    private interface IAlphaContract
    {
    }

    private interface IZuluContract
    {
    }

    private sealed class ConcreteMessage : IAlphaContract, IZuluContract
    {
    }

    private sealed class StandaloneMessage
    {
    }

    private sealed class RecordingSpecification<TContext>(List<string> trace, string marker) : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public void Apply(IPipeBuilder<TContext> builder)
        {
            ArgumentNullException.ThrowIfNull(builder);
            trace.Add(marker);
        }

        public IEnumerable<ValidationResult> Validate() => [];
    }

    private sealed class InvalidSpecification<TContext>(string key, string message) : IPipeSpecification<TContext>
        where TContext : class, PipeContext
    {
        public void Apply(IPipeBuilder<TContext> builder) => ArgumentNullException.ThrowIfNull(builder);

        public IEnumerable<ValidationResult> Validate()
        {
            yield return this.Failure(key, message);
        }
    }

    private sealed class RecordingBuilder<TContext> : ISpecificationPipeBuilder<TContext>
        where TContext : class, PipeContext
    {
        private readonly bool _isDelegated;
        private readonly bool _isImplemented;

        public RecordingBuilder(bool isDelegated = false, bool isImplemented = false)
        {
            _isDelegated = isDelegated;
            _isImplemented = isImplemented;
        }

        public bool IsDelegated => _isDelegated;

        public bool IsImplemented => _isImplemented;

        public void AddFilter(IFilter<TContext> filter) => ArgumentNullException.ThrowIfNull(filter);

        public ISpecificationPipeBuilder<TContext> CreateDelegatedBuilder() =>
            new RecordingBuilder<TContext>(isDelegated: true, isImplemented: _isImplemented);

        public ISpecificationPipeBuilder<TContext> CreateImplementedBuilder() =>
            new RecordingBuilder<TContext>(isDelegated: _isDelegated, isImplemented: true);
    }

    private sealed class ReentrantObserver(PublishPipeSpecification owner) : IPublishPipeSpecificationObserver
    {
        public Dictionary<Type, int> NotificationCounts { get; } = [];

        public Dictionary<Type, object> ObservedSpecifications { get; } = [];

        public Dictionary<Type, object> ReentrantSpecifications { get; } = [];

        public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
            where T : class
        {
            NotificationCounts[typeof(T)] = NotificationCounts.GetValueOrDefault(typeof(T)) + 1;
            ObservedSpecifications[typeof(T)] = specification;
            ReentrantSpecifications[typeof(T)] = owner.GetMessageSpecification<T>();
        }
    }

    private sealed class ParentSpecificationObserver(IPublishPipeSpecification parent) : IPublishPipeSpecificationObserver
    {
        public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
            where T : class
        {
            specification.AddParentMessageSpecification(parent.GetMessageSpecification<T>());
        }
    }

    private sealed class ThrowOnceObserver : IPublishPipeSpecificationObserver
    {
        public int NotificationCount { get; private set; }

        public void MessageSpecificationCreated<T>(IMessagePublishPipeSpecification<T> specification)
            where T : class
        {
            NotificationCount++;
            if (NotificationCount == 1)
                throw new ExpectedInitializationException();
        }
    }

    private sealed class ExpectedInitializationException : Exception
    {
    }
}
