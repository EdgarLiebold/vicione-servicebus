using System.Linq.Expressions;
using System.Reflection;
using ViciOne.ServiceBus.Advanced;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Exceptions;

public sealed class CapabilityExceptionOwnershipTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "saga-context-is-preserved-and-retry-classification-is-explicit")]
    public void SagaFailures_PreserveTheirDiagnosticContextAndRetryClassification()
    {
        Guid correlationId = Guid.Parse("be13eacf-01bf-4675-8778-c85e42f5addc");
        var saga = new SagaException("Correlation was unavailable.", typeof(TestSaga), typeof(TestMessage));
        var concurrency = new ConcurrencyException("Concurrent update.", typeof(TestSaga), correlationId);

        Assert.Null(saga.CorrelationId);
        Assert.Equal(typeof(TestSaga), saga.SagaType);
        Assert.Equal(typeof(TestMessage), saga.MessageType);
        Assert.Equal(correlationId, concurrency.CorrelationId);
        Assert.Equal(
            RetryFailureKind.Transient,
            Assert.IsAssignableFrom<IRetryFailureClassification>(concurrency).RetryFailureKind);
        Assert.DoesNotContain(
            typeof(SagaException).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            constructor => constructor.GetParameters().Any(parameter => typeof(Expression).IsAssignableFrom(parameter.ParameterType)));
        Assert.Equal(
            "sagaType",
            Assert.Throws<ArgumentNullException>(() =>
                new SagaException("Correlation was unavailable.", null!, typeof(TestMessage))).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "state-machine-and-future-context-is-preserved")]
    public void StateMachineAndFutureFailures_PreserveTheirDiagnosticContext()
    {
        Guid futureId = Guid.Parse("3efbe874-7fc0-4286-a41d-e136ad95cd91");

        var unknownEvent = new UnknownEventException("OrderStateMachine", "OrderAccepted");
        var unknownState = new UnknownStateException("OrderStateMachine", "Archived");
        var unhandledEvent = new UnhandledEventException("OrderStateMachine", "Cancel", "Completed");
        var future = new FutureNotFoundException(typeof(TestMessage), futureId);

        Assert.Equal("OrderStateMachine", unknownEvent.MachineName);
        Assert.Equal("OrderAccepted", unknownEvent.EventName);
        Assert.Equal("OrderStateMachine", unknownState.MachineName);
        Assert.Equal("Archived", unknownState.StateName);
        Assert.Equal("OrderStateMachine", unhandledEvent.MachineName);
        Assert.Equal("Cancel", unhandledEvent.EventName);
        Assert.Equal("Completed", unhandledEvent.StateName);
        Assert.All(
            new Exception[] { unknownEvent, unknownState, unhandledEvent },
            exception => Assert.Equal(
                RetryFailureKind.NonRetryable,
                Assert.IsAssignableFrom<IRetryFailureClassification>(exception).RetryFailureKind));
        Assert.Equal(typeof(TestMessage), future.FutureType);
        Assert.Equal(futureId, future.FutureId);
        Assert.Equal(
            "machineName",
            Assert.Throws<ArgumentException>(() => new UnknownEventException(" ", "OrderAccepted")).ParamName);
        Assert.Equal(
            "type",
            Assert.Throws<ArgumentNullException>(() => new FutureNotFoundException(null!, futureId)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "compensation-address-is-typed-and-courier-failures-are-terminal")]
    public void CompensationFailures_PreserveTheirAddressAndTerminalClassification()
    {
        var address = new Uri("loopback://localhost/compensate");

        var compensation = new InvalidCompensationAddressException(address);

        Assert.Same(address, compensation.Address);
        Assert.IsAssignableFrom<ActivityExecutionException>(compensation);
        Assert.Equal(
            RetryFailureKind.NonRetryable,
            Assert.IsAssignableFrom<IRetryFailureClassification>(compensation).RetryFailureKind);
    }

    private sealed class TestSaga;

    private sealed class TestMessage;
}
