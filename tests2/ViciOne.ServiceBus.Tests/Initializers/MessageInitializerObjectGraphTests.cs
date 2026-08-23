using ViciOne.ServiceBus.Events;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class MessageInitializerObjectGraphTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "covariant-fault")]
    public async Task FaultWithDerivedMessage_IsProjectedIntoItsCovariantBaseContract()
    {
        var messageId = Guid.Parse("913a05a0-f19a-4298-8fcc-5a4b5cde8278");
        var host = new FixedHostInfo();
        InitializeContext<Top> message = await MessageInitializerCache<Top>.Initialize(
            new { Text = "Hello" },
            TestContext.Current.CancellationToken);
        var sourceFault = new FaultEvent<Top>(
            message.Message,
            messageId,
            host,
            new InvalidOperationException("Expected failure"),
            ["urn:message:ViciOne.ServiceBus.Tests:Top"]);

        InitializeContext<Report> context = await MessageInitializerCache<Report>.Initialize(
            new { Fault = sourceFault },
            TestContext.Current.CancellationToken);

        Fault<Bottom> actual = context.Message.Fault;
        Assert.NotNull(actual);
        Assert.NotSame(sourceFault, actual);
        Assert.Equal(sourceFault.FaultId, actual.FaultId);
        Assert.Equal(messageId, actual.FaultedMessageId);
        Assert.Equal(sourceFault.Timestamp, actual.Timestamp);
        Assert.Equal(["urn:message:ViciOne.ServiceBus.Tests:Top"], actual.FaultMessageTypes);
        Assert.Single(actual.Exceptions);
        Assert.Equal(typeof(InvalidOperationException).FullName, actual.Exceptions[0].ExceptionType);
        Assert.Equal("Expected failure", actual.Exceptions[0].Message);
        Assert.Equal("Hello", actual.Message.Text);
        AssertHost(host, actual.Host);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "nested-private-setters")]
    public async Task NestedAnonymousInput_InitializesPrivateSetterProperties()
    {
        InitializeContext<Member> context = await MessageInitializerCache<Member>.Initialize(
            new
            {
                Name = "Frank",
                Address = new
                {
                    Street = "123 American Way",
                    City = "Dallas",
                },
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("Frank", context.Message.Name);
        Assert.NotNull(context.Message.Address);
        Assert.Equal("123 American Way", context.Message.Address.Street);
        Assert.Equal("Dallas", context.Message.Address.City);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "interface-anonymous-readonly")]
    public async Task InterfaceTarget_FromAnonymousInput_InitializesTheSuppliedWritableProperty()
    {
        InitializeContext<IReadWriteReadOnly> context = await MessageInitializerCache<IReadWriteReadOnly>.Initialize(
            new { ReadWrite = "Some Property Value" },
            TestContext.Current.CancellationToken);

        Assert.Equal("Some Property Value", context.Message.ReadWrite);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "interface-concrete-readonly")]
    public async Task InterfaceTarget_FromConcreteInput_PreservesTheComputedReadOnlyProperty()
    {
        var source = new ReadWriteReadOnly { ReadWrite = "Some Property Value" };

        InitializeContext<IReadWriteReadOnly> context = await MessageInitializerCache<IReadWriteReadOnly>.Initialize(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal("Some Property Value", context.Message.ReadWrite);
        Assert.Equal("Some Property Value", context.Message.ReadOnly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "class-anonymous-readonly")]
    public async Task ConcreteTarget_FromAnonymousInput_PreservesItsComputedReadOnlyProperty()
    {
        InitializeContext<ReadWriteReadOnly> context = await MessageInitializerCache<ReadWriteReadOnly>.Initialize(
            new { ReadWrite = "Some Property Value" },
            TestContext.Current.CancellationToken);

        Assert.Equal("Some Property Value", context.Message.ReadWrite);
        Assert.Equal("Some Property Value", context.Message.ReadOnly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "class-concrete-readonly")]
    public async Task ConcreteTarget_FromSameTypeInput_CreatesAnInitializedCopyWithComputedReadOnlyProperty()
    {
        var source = new ReadWriteReadOnly { ReadWrite = "Some Property Value" };

        InitializeContext<ReadWriteReadOnly> context = await MessageInitializerCache<ReadWriteReadOnly>.Initialize(
            source,
            TestContext.Current.CancellationToken);

        Assert.NotSame(source, context.Message);
        Assert.Equal("Some Property Value", context.Message.ReadWrite);
        Assert.Equal("Some Property Value", context.Message.ReadOnly);
    }

    private static void AssertHost(HostInfo expected, HostInfo actual)
    {
        Assert.Equal(expected.MachineName, actual.MachineName);
        Assert.Equal(expected.ProcessName, actual.ProcessName);
        Assert.Equal(expected.ProcessId, actual.ProcessId);
        Assert.Equal(expected.Assembly, actual.Assembly);
        Assert.Equal(expected.AssemblyVersion, actual.AssemblyVersion);
        Assert.Equal(expected.FrameworkVersion, actual.FrameworkVersion);
        Assert.Equal(expected.ViciOneServiceBusVersion, actual.ViciOneServiceBusVersion);
        Assert.Equal(expected.OperatingSystemVersion, actual.OperatingSystemVersion);
    }

    public sealed class ReadWriteReadOnly : IReadWriteReadOnly
    {
        public required string ReadWrite { get; set; }

        public string ReadOnly => ReadWrite;
    }

    public interface IReadWriteReadOnly
    {
        string ReadWrite { get; set; }

        string ReadOnly { get; }
    }

    public interface Report
    {
        Fault<Bottom> Fault { get; }
    }

    public interface Top : Bottom
    {
    }

    public interface Bottom
    {
        string Text { get; }
    }

    public sealed class Member
    {
        public string? Name { get; set; }

        public Address? Address { get; private set; }
    }

    public sealed class Address
    {
        public string? Street { get; private set; }

        public string? City { get; set; }
    }

    private sealed class FixedHostInfo : HostInfo
    {
        public string MachineName => "test-machine";

        public string ProcessName => "test-process";

        public int ProcessId => 42;

        public string Assembly => "ViciOne.ServiceBus.Tests";

        public string AssemblyVersion => "1.2.3.4";

        public string FrameworkVersion => ".NET 10.0";

        public string ViciOneServiceBusVersion => "10.0.0";

        public string OperatingSystemVersion => "test-os";
    }
}
