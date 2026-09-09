using System.Reflection;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Abstractions.Tests;

public sealed class ServiceBusExceptionHierarchyTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-HIERARCHY", "bus-owned-admission-failures-share-the-root-type")]
    public void AdmissionFailures_DeriveFromTheServiceBusExceptionRoot()
    {
        var messageSize = new MessageTooLargeException(
            actualBytes: 65,
            maximumBytes: 64,
            inputAddress: new Uri("loopback://localhost/input"));
        var payload = new PayloadAdmissionException(
            PayloadAdmissionStage.SerializedBody,
            actualBytes: 65,
            configuredLimitBytes: 64,
            "The serialized payload is too large.");

        Assert.IsAssignableFrom<ViciOneServiceBusException>(messageSize);
        Assert.IsAssignableFrom<ViciOneServiceBusException>(payload);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "transport-uri-is-required-and-immutable")]
    public void TransportFailure_RequiresAndPreservesItsAddress()
    {
        var address = new Uri("loopback://localhost/input");

        var exception = new TransportException(address, "delivery failed");

        Assert.Same(address, exception.Uri);
        Assert.Contains(address.AbsoluteUri, exception.Message, StringComparison.Ordinal);
        Assert.False(typeof(AbstractUriException).GetProperty(nameof(AbstractUriException.Uri))!.CanWrite);
        Assert.Equal(
            "uri",
            Assert.Throws<ArgumentNullException>(() => new TransportException(null!)).ParamName);
        Assert.Equal(
            "uri",
            Assert.Throws<ArgumentNullException>(() => new TransportException(null!, "delivery failed")).ParamName);
        Assert.Equal(
            "uri",
            Assert.Throws<ArgumentNullException>(() =>
                new TransportException(null!, "delivery failed", new InvalidOperationException())).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "configuration-results-are-snapshotted")]
    public void ConfigurationFailure_SnapshotsItsValidationResults()
    {
        var results = new List<ValidationResult>
        {
            new StubValidationResult("endpoint", "The endpoint is invalid.")
        };

        var exception = new ConfigurationException(results, "Configuration failed.");
        results.Clear();

        Assert.Single(exception.Results);
        Assert.IsAssignableFrom<IReadOnlyList<ValidationResult>>(exception.Results);
        Assert.False(typeof(ConfigurationException).GetProperty(nameof(ConfigurationException.Results))!.CanWrite);
        Assert.Equal(
            "results",
            Assert.Throws<ArgumentNullException>(() =>
                new ConfigurationException(null!, "Configuration failed.")).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "request-identifiers-are-typed")]
    public void RequestFailures_PreserveTheirTypedIdentifier()
    {
        Guid requestId = Guid.Parse("12a61472-f67a-4db6-a2b9-1f585bc73346");
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        var canceled = new RequestCanceledException(requestId, cancellationSource.Token);
        var timedOut = new RequestTimeoutException(requestId);

        Assert.Equal(requestId, canceled.RequestId);
        Assert.Equal(requestId, timedOut.RequestId);
        Assert.Contains(requestId.ToString("D"), canceled.Message, StringComparison.Ordinal);
        Assert.Contains(requestId.ToString("D"), timedOut.Message, StringComparison.Ordinal);
        Assert.Equal(
            "requestId",
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new RequestCanceledException(Guid.Empty, cancellationSource.Token)).ParamName);
        Assert.Equal(
            "requestId",
            Assert.Throws<ArgumentOutOfRangeException>(() => new RequestTimeoutException(Guid.Empty)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-SURFACE", "legacy-request-response-payload-is-absent")]
    public void RequestFailure_DoesNotExposeTheUnusedLegacyResponsePayload()
    {
        Type exceptionType = typeof(RequestException);

        Assert.Null(exceptionType.GetProperty("Response", BindingFlags.Instance | BindingFlags.Public));
        Assert.DoesNotContain(
            exceptionType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            constructor => constructor.GetParameters().Any(parameter => parameter.ParameterType == typeof(object)));
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "missing-context-is-rejected-or-null")]
    public void OptionalAndRequiredExceptionContext_IsRepresentedPrecisely()
    {
        var saga = new SagaException("Correlation was unavailable.", typeof(TestSaga), typeof(TestMessage));
        var remoteData = new Dictionary<string, object> { ["attempt"] = 1 };
        var remote = new StubExceptionInfo(remoteData);
        var remoteException = new ExceptionInfoException(remote);
        remoteData["attempt"] = 2;

        Assert.Null(saga.CorrelationId);
        Assert.Equal(1, remoteException.Data["attempt"]);
        Assert.DoesNotContain(
            typeof(SagaException).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            constructor => constructor.GetParameters().Any(parameter => typeof(Expression).IsAssignableFrom(parameter.ParameterType)));
        Assert.Equal(
            "sagaType",
            Assert.Throws<ArgumentNullException>(() =>
                new SagaException("Correlation was unavailable.", null!, typeof(TestMessage))).ParamName);
        Assert.Equal(
            "exceptionInfo",
            Assert.Throws<ArgumentNullException>(() => new ExceptionInfoException(null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "message-type-and-data-address-are-required")]
    public void MessageFailures_PreserveTheirRequiredTypedContext()
    {
        var address = new Uri("https://payload.example/messages/42");

        var message = new MessageException(typeof(TestMessage), "Message conversion failed.");
        var messageData = new MessageDataNotFoundException(address);

        Assert.Equal(typeof(TestMessage), message.MessageType);
        Assert.Same(address, messageData.Address);
        Assert.False(typeof(MessageException).GetProperty(nameof(MessageException.MessageType))!.CanWrite);
        Assert.Equal(
            "messageType",
            Assert.Throws<ArgumentNullException>(() =>
                new MessageException(null!, "Message conversion failed.")).ParamName);
        Assert.Equal(
            "address",
            Assert.Throws<ArgumentNullException>(() => new MessageDataNotFoundException(null!)).ParamName);
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
    [RequirementCoverage("REQ-VSB-EXCEPTION-CONTEXT", "request-fault-and-compensation-address-are-typed")]
    public void RequestFaultAndCompensationFailures_PreserveTheirTypedContext()
    {
        var fault = new StubFault();
        var address = new Uri("loopback://localhost/compensate");

        var request = new RequestFaultException(typeof(TestMessage), fault);
        var compensation = new InvalidCompensationAddressException(address);

        Assert.Equal(typeof(TestMessage), request.RequestType);
        Assert.Same(fault, request.Fault);
        Assert.Same(address, compensation.Address);
        Assert.Equal(
            "requestType",
            Assert.Throws<ArgumentNullException>(() => new RequestFaultException(null!, fault)).ParamName);
        Assert.Equal(
            "fault",
            Assert.Throws<ArgumentNullException>(() => new RequestFaultException(typeof(TestMessage), null!)).ParamName);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-EXCEPTION-SURFACE", "connection-transience-is-explicit")]
    public void ConnectionFailure_RequiresExplicitTransientClassification()
    {
        ParameterInfo[] transientParameters = typeof(ConnectionException)
            .GetConstructors()
            .SelectMany(constructor => constructor.GetParameters())
            .Where(parameter => parameter.Name == "isTransient")
            .ToArray();

        Assert.NotEmpty(transientParameters);
        Assert.All(transientParameters, parameter => Assert.False(parameter.HasDefaultValue));
        Assert.True(new ConnectionException("Disconnected.", isTransient: true).IsTransient);
        Assert.False(new ConnectionException("Rejected.", isTransient: false).IsTransient);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-CONCURRENCY-LIMIT", "stale-command-exception-contract")]
    public void StaleConcurrencyCommandFailure_PreservesItsOrderingEvidence()
    {
        DateTimeOffset commandTimestamp = DateTimeOffset.UnixEpoch;
        DateTimeOffset lastAppliedTimestamp = commandTimestamp.AddTicks(1);

        var exception = new StaleConcurrencyLimitCommandException(commandTimestamp, lastAppliedTimestamp);

        Assert.Equal(commandTimestamp, exception.CommandTimestamp);
        Assert.Equal(lastAppliedTimestamp, exception.LastAppliedTimestamp);
        Assert.Contains(commandTimestamp.ToString("O"), exception.Message, StringComparison.Ordinal);
        Assert.Contains(lastAppliedTimestamp.ToString("O"), exception.Message, StringComparison.Ordinal);
        Assert.Equal(
            "commandTimestamp",
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StaleConcurrencyLimitCommandException(lastAppliedTimestamp, commandTimestamp)).ParamName);
        Assert.Equal(
            "commandTimestamp",
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new StaleConcurrencyLimitCommandException(commandTimestamp, commandTimestamp)).ParamName);
    }

    private sealed class StubValidationResult(string key, string message) : ValidationResult
    {
        public ValidationResultDisposition Disposition => ValidationResultDisposition.Failure;

        public string Message { get; } = message;

        public string Key { get; } = key;

        public string? Value => null;
    }

    private sealed class TestSaga;

    private sealed class TestMessage;

    private sealed class StubFault : Fault
    {
        public Guid FaultId { get; } = Guid.NewGuid();

        public Guid? FaultedMessageId => null;

        public DateTimeOffset Timestamp { get; } = DateTimeOffset.UtcNow;

        public ExceptionInfo[] Exceptions { get; } = [];

        public HostInfo Host => null!;

        public string[] FaultMessageTypes { get; } = [];
    }

    private sealed class StubExceptionInfo(IDictionary<string, object> data) : ExceptionInfo
    {
        public string ExceptionType => typeof(InvalidOperationException).FullName!;

        public ExceptionInfo? InnerException => null;

        public string StackTrace => "remote stack";

        public string Message => "Remote failure.";

        public string Source => "Remote.Service";

        public IDictionary<string, object>? Data { get; } = data;
    }
}
