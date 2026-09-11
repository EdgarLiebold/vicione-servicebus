using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Consumers.Metadata;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

[Collection(ConsumerConventionStateCollection.Name)]
public sealed class ConsumerMetadataCacheImmutabilityTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-METADATA-IMMUTABILITY", "read-only-stable-cache")]
    public void ConsumerTypes_ExposeAReadOnlyStableCollection()
    {
        PropertyInfo property = typeof(ConsumerMetadataCache<MultiMessageConsumer>)
            .GetProperty(nameof(ConsumerMetadataCache<MultiMessageConsumer>.ConsumerTypes))!;
        Assert.Equal(typeof(IReadOnlyList<IMessageInterfaceType>), property.PropertyType);

        object first = ConsumerMetadataCache<MultiMessageConsumer>.ConsumerTypes;
        object second = ConsumerMetadataCache<MultiMessageConsumer>.ConsumerTypes;
        Assert.IsNotType<IMessageInterfaceType[]>(first);
        Assert.Same(first, second);

        IList<IMessageInterfaceType> mutable = Assert.IsAssignableFrom<IList<IMessageInterfaceType>>(first);
        Assert.Throws<NotSupportedException>(() => mutable.Clear());
        IReadOnlyList<IMessageInterfaceType> current = ConsumerMetadataCache<MultiMessageConsumer>.ConsumerTypes;
        Assert.Equal(2, current.Count);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONSUMER-CONVENTION-STATE", "same-version-concurrent-reference-identity")]
    public void SameConventionVersion_ReturnsOneSnapshotToEveryConcurrentCaller()
    {
        IReadOnlyList<IMessageInterfaceType>[] snapshots = Enumerable.Range(0, 64)
            .AsParallel()
            .WithDegreeOfParallelism(8)
            .Select(_ => ConsumerMetadataCache<MultiMessageConsumer>.ConsumerTypes)
            .ToArray();

        Assert.All(snapshots, snapshot => Assert.Same(snapshots[0], snapshot));
    }

    public sealed record FirstMessage;

    public sealed record SecondMessage;

    public sealed class MultiMessageConsumer :
        IConsumer<FirstMessage>,
        IConsumer<SecondMessage>
    {
        public Task ConsumeAsync(ConsumeContext<FirstMessage> context) => Task.CompletedTask;

        public Task ConsumeAsync(ConsumeContext<SecondMessage> context) => Task.CompletedTask;
    }
}
