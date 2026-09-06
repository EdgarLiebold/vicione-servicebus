using System.Reflection;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Consumers;

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
