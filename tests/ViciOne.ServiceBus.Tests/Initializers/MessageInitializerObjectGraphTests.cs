using System.Text.Json.Nodes;
using ViciOne.ServiceBus.Events.Faults;
using ViciOne.ServiceBus.Initializers;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Initializers;

public sealed class MessageInitializerObjectGraphTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "covariant-fault")]
    public async Task FaultWithDerivedMessage_IsProjectedIntoItsCovariantBaseContractAsync()
    {
        var messageId = Guid.Parse("913a05a0-f19a-4298-8fcc-5a4b5cde8278");
        var host = new FixedHostInfo();
        InitializeContext<Top> message = await MessageInitializerCache<Top>.InitializeAsync(
            new { Text = "Hello" },
            TestContext.Current.CancellationToken);
        var sourceFault = new FaultEvent<Top>(
            message.Message,
            messageId,
            host,
            new InvalidOperationException("Expected failure"),
            ["urn:message:ViciOne.ServiceBus.Tests:Top"]);

        InitializeContext<Report> context = await MessageInitializerCache<Report>.InitializeAsync(
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
    public async Task NestedAnonymousInput_InitializesPrivateSetterPropertiesAsync()
    {
        InitializeContext<Member> context = await MessageInitializerCache<Member>.InitializeAsync(
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
    public async Task InterfaceTarget_FromAnonymousInput_InitializesTheSuppliedWritablePropertyAsync()
    {
        InitializeContext<IReadWriteReadOnly> context = await MessageInitializerCache<IReadWriteReadOnly>.InitializeAsync(
            new { ReadWrite = "Some Property Value" },
            TestContext.Current.CancellationToken);

        Assert.Equal("Some Property Value", context.Message.ReadWrite);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "interface-concrete-readonly")]
    public async Task InterfaceTarget_FromConcreteInput_PreservesTheComputedReadOnlyPropertyAsync()
    {
        var source = new ReadWriteReadOnly { ReadWrite = "Some Property Value" };

        InitializeContext<IReadWriteReadOnly> context = await MessageInitializerCache<IReadWriteReadOnly>.InitializeAsync(
            source,
            TestContext.Current.CancellationToken);

        Assert.Equal("Some Property Value", context.Message.ReadWrite);
        Assert.Equal("Some Property Value", context.Message.ReadOnly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "class-anonymous-readonly")]
    public async Task ConcreteTarget_FromAnonymousInput_PreservesItsComputedReadOnlyPropertyAsync()
    {
        InitializeContext<ReadWriteReadOnly> context = await MessageInitializerCache<ReadWriteReadOnly>.InitializeAsync(
            new { ReadWrite = "Some Property Value" },
            TestContext.Current.CancellationToken);

        Assert.Equal("Some Property Value", context.Message.ReadWrite);
        Assert.Equal("Some Property Value", context.Message.ReadOnly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "class-concrete-readonly")]
    public async Task ConcreteTarget_FromSameTypeInput_CreatesAnInitializedCopyWithComputedReadOnlyPropertyAsync()
    {
        var source = new ReadWriteReadOnly { ReadWrite = "Some Property Value" };

        InitializeContext<ReadWriteReadOnly> context = await MessageInitializerCache<ReadWriteReadOnly>.InitializeAsync(
            source,
            TestContext.Current.CancellationToken);

        Assert.NotSame(source, context.Message);
        Assert.Equal("Some Property Value", context.Message.ReadWrite);
        Assert.Equal("Some Property Value", context.Message.ReadOnly);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "complex-concrete-dto")]
    public async Task DynamicInterfaceTarget_PreservesAnExactConcreteNestedDtoAsync()
    {
        var correlationId = Guid.Parse("582bcb34-d08c-4b95-ad1a-ccff34e41419");
        var timestamp = new DateTime(2024, 5, 6, 7, 8, 9, DateTimeKind.Utc);
        var order = new OrderDto
        {
            Amount = 123.45m,
            Id = 27,
            CustomerId = "FRANK01",
            ItemType = "Crayon",
            OrderState = new OrderState { Status = OrderStatus.Validated },
            TokenizedCreditCard = new TokenizedCreditCardDto
            {
                ExpirationMonth = "12",
                ExpirationYear = "2031",
                PublicKey = new JsonObject { ["key"] = "12345" },
                Token = new JsonObject { ["value"] = "Token123" },
            },
        };

        InitializeContext<PaymentGatewaySubmitted> context = await MessageInitializerCache<PaymentGatewaySubmitted>.InitializeAsync(
            new
            {
                Order = order,
                CorrelationId = correlationId,
                TimeStamp = timestamp,
                ConsumerProcessed = true,
            },
            TestContext.Current.CancellationToken);

        Assert.Equal(correlationId, context.Message.CorrelationId);
        Assert.Equal(timestamp, context.Message.TimeStamp);
        Assert.True(context.Message.ConsumerProcessed);
        Assert.Same(order, context.Message.Order);
        Assert.Equal(27, context.Message.Order.Id);
        Assert.Equal(123.45m, context.Message.Order.Amount);
        Assert.Equal("FRANK01", context.Message.Order.CustomerId);
        Assert.Equal("Crayon", context.Message.Order.ItemType);
        Assert.Equal(OrderStatus.Validated, context.Message.Order.OrderState.Status);
        Assert.Equal("12", context.Message.Order.TokenizedCreditCard.ExpirationMonth);
        Assert.Equal("2031", context.Message.Order.TokenizedCreditCard.ExpirationYear);
        Assert.Equal("12345", context.Message.Order.TokenizedCreditCard.PublicKey["key"]?.GetValue<string>());
        Assert.Equal("Token123", context.Message.Order.TokenizedCreditCard.Token["value"]?.GetValue<string>());
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "partial-exception-info")]
    public async Task ExceptionInfoTarget_InitializesSuppliedPropertiesAndLeavesMissingPropertiesDefaultAsync()
    {
        InitializeContext<ExceptionInfo> context = await MessageInitializerCache<ExceptionInfo>.InitializeAsync(
            new
            {
                Message = "Hello",
                ExceptionType = TypeCache<ArgumentException>.ShortName,
            },
            TestContext.Current.CancellationToken);

        Assert.Equal("Hello", context.Message.Message);
        Assert.Equal(TypeCache<ArgumentException>.ShortName, context.Message.ExceptionType);
        Assert.Null(context.Message.InnerException);
        Assert.Null(context.Message.StackTrace);
        Assert.Null(context.Message.Source);
        Assert.Null(context.Message.Data);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "most-derived-duplicate-property")]
    public async Task NestedConcreteInput_UsesTheMostDerivedDuplicatePropertyAsync()
    {
        var nested = new DerivedProperty { NewProperty = "Derived value" };
        ((BaseProperty)nested).NewProperty = "Base value";

        InitializeContext<DuplicatePropertyMessage> context = await MessageInitializerCache<DuplicatePropertyMessage>.InitializeAsync(
            new { Value = nested },
            TestContext.Current.CancellationToken);

        Assert.NotNull(context.Message.Value);
        Assert.Equal("Derived value", context.Message.Value.NewProperty);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-INITIALIZER-OBJECT-GRAPH", "nested-interface")]
    public async Task NestedAnonymousInput_InitializesAnInterfacePropertyAsync()
    {
        InitializeContext<NestedInterfaceMessage> context = await MessageInitializerCache<NestedInterfaceMessage>.InitializeAsync(
            new { Value = new { Text = "Mary" } },
            TestContext.Current.CancellationToken);

        Assert.NotNull(context.Message.Value);
        Assert.Equal("Mary", context.Message.Value.Text);
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

    public interface PaymentGatewaySubmitted : ICorrelatedBy<Guid>
    {
        DateTime TimeStamp { get; }

        OrderDto Order { get; }

        bool ConsumerProcessed { get; }
    }

    public sealed class OrderDto
    {
        public int Id { get; init; }

        public decimal Amount { get; init; }

        public required string ItemType { get; init; }

        public required OrderState OrderState { get; init; }

        public required TokenizedCreditCardDto TokenizedCreditCard { get; init; }

        public required string CustomerId { get; init; }
    }

    public sealed class OrderState
    {
        public OrderStatus Status { get; init; }
    }

    public enum OrderStatus
    {
        ClientSubmitted,
        Validated,
    }

    public sealed class TokenizedCreditCardDto
    {
        public required string ExpirationMonth { get; init; }

        public required string ExpirationYear { get; init; }

        public required JsonObject PublicKey { get; init; }

        public required JsonObject Token { get; init; }
    }

    public interface DuplicatePropertyMessage
    {
        NamedProperty Value { get; }
    }

    public interface NamedProperty
    {
        string NewProperty { get; }
    }

    public class BaseProperty
    {
        public string? NewProperty { get; set; }
    }

    public sealed class DerivedProperty : BaseProperty
    {
        public new required string NewProperty { get; set; }
    }

    public interface NestedInterfaceMessage
    {
        TextValue Value { get; }
    }

    public interface TextValue
    {
        string Text { get; }
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
