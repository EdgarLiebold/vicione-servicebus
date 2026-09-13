using ViciOne.ServiceBus.Testing;
using ViciOne.ServiceBus.Tests.Infrastructure.Configuration;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.SagaStateMachine;

public sealed class StateMachineRequestIntegrationTests
{
    [Theory]
    [InlineData(CompositeOutcome.Success, "Frank", "Castle", "Success", "Frank", "Castle")]
    [InlineData(CompositeOutcome.NameRejected, "Clark", "Kent", "Invalid Name", "REJECTED!", "Kent")]
    [InlineData(CompositeOutcome.SurnameRejected, "Peter", "Parker", "Invalid Surname", "Peter", "REJECTED!")]
    [InlineData(CompositeOutcome.BothRejected, "Bruce", "Wayne", "Invalid ID", "REJECTED!", "REJECTED!")]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "parallel-request-composite-outcome-matrix")]
    public async Task ParallelRequests_ProduceTheExactCompositeOutcomeAsync(
        CompositeOutcome outcome,
        string name,
        string surname,
        string expectedResult,
        string expectedName,
        string expectedSurname)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("parallel-requests", timeout);
        string serviceEndpointName = $"validation-{NewId.NextGuid():N}";
        Uri serviceAddress = new(harness.BaseAddress, serviceEndpointName);
        var machine = new CompositeRequestMachine(serviceAddress);
        harness.InMemoryReceiveEndpointConfiguring += endpoint => endpoint.UseVolatileOutbox();
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint(
            serviceEndpointName,
            endpoint =>
            {
                endpoint.Handler<ValidateName>(context => HandleValidateNameAsync(context, outcome));
                endpoint.Handler<ValidateSurname>(context => HandleValidateSurnameAsync(context, outcome));
            });
        ISagaStateMachineTestHarness<CompositeRequestMachine, CompositeRequestState> sagaHarness =
            harness.AddSagaStateMachine<CompositeRequestMachine, CompositeRequestState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid memberId = NewId.NextGuid();
            Task<IPublishedMessage<MemberRegistrationResult>> published = harness.Published
                .SelectAsync<MemberRegistrationResult>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IConsumedMessage<ValidateName>> nameRequest = harness.Consumed
                .SelectAsync<ValidateName>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IConsumedMessage<ValidateSurname>> surnameRequest = harness.Consumed
                .SelectAsync<ValidateSurname>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IConsumedMessage<RegisterMember>> start = sagaHarness.Consumed
                .SelectAsync<RegisterMember>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.InputQueueSendEndpoint.SendAsync(
                new RegisterMember(memberId, name, surname),
                cancellationToken);

            Assert.Null((await start.WaitAsync(timeout, cancellationToken)).Exception);
            IConsumedMessage<ValidateName> nameDelivery = await nameRequest.WaitAsync(timeout, cancellationToken);
            IConsumedMessage<ValidateSurname> surnameDelivery = await surnameRequest.WaitAsync(timeout, cancellationToken);
            AssertValidationDelivery(
                nameDelivery.Exception,
                outcome is CompositeOutcome.NameRejected or CompositeOutcome.BothRejected,
                $"name:{name}");
            AssertValidationDelivery(
                surnameDelivery.Exception,
                outcome is CompositeOutcome.SurnameRejected or CompositeOutcome.BothRejected,
                $"surname:{surname}");
            MemberRegistrationResult result = (await published.WaitAsync(timeout, cancellationToken)).Context.Message;
            State expectedState = outcome == CompositeOutcome.Success ? machine.Registered : machine.Rejected;
            Guid? located = await sagaHarness.WaitForSagaInStateAsync(memberId, expectedState, timeout, TestContext.Current.CancellationToken);
            CompositeRequestState? instance = sagaHarness.Sagas.FindById(memberId);

            Assert.Equal(memberId, located);
            Assert.NotNull(instance);
            Assert.Equal(memberId, result.CorrelationId);
            Assert.Equal(expectedResult, result.Result);
            Assert.Equal(expectedName, result.Name);
            Assert.Equal(expectedSurname, result.Surname);
            Assert.Equal(expectedResult, instance.Result);
            Assert.Equal(expectedName, instance.Name);
            Assert.Equal(expectedSurname, instance.Surname);
            Assert.Equal(expectedState.Name, instance.CurrentState);
            Assert.Equal(1, instance.TerminalOutcomeCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Snapshot<MemberRegistrationResult>());
    }

    [Theory]
    [InlineData(MultiResponseKind.Second, "invalid")]
    [InlineData(MultiResponseKind.Third, "duplicate")]
    [RequirementCoverage("REQ-VSB-STATE-MACHINE-REQUEST", "second-and-third-response-routes")]
    public async Task MultiResponseRequest_RoutesTheSecondAndThirdAcceptedTypesExactlyAsync(
        MultiResponseKind responseKind,
        string expectedReason)
    {
        TimeSpan timeout = OperationTimeout();
        CancellationToken cancellationToken = TestContext.Current.CancellationToken;
        using var harness = CreateHarness("multi-response", timeout);
        string serviceEndpointName = $"multi-service-{NewId.NextGuid():N}";
        Uri serviceAddress = new(harness.BaseAddress, serviceEndpointName);
        var machine = new MultiResponseMachine(serviceAddress);
        harness.InMemoryBusConfiguring += configurator => configurator.ReceiveEndpoint(
            serviceEndpointName,
            endpoint => endpoint.Handler<ValidateMember>(context => responseKind switch
            {
                MultiResponseKind.Second => context.RespondAsync(new MemberInvalid(context.Message.CorrelationId, "invalid")),
                MultiResponseKind.Third => context.RespondAsync(new MemberDuplicate(context.Message.CorrelationId, "duplicate")),
                _ => context.RespondAsync(new MemberValid(context.Message.CorrelationId, "valid")),
            }));
        ISagaStateMachineTestHarness<MultiResponseMachine, MultiResponseState> sagaHarness =
            harness.AddSagaStateMachine<MultiResponseMachine, MultiResponseState>(machine);

        await harness.StartAsync(cancellationToken).WaitAsync(timeout, cancellationToken);
        try
        {
            Guid correlationId = NewId.NextGuid();
            Task<IPublishedMessage<MemberRejected>> published = harness.Published
                .SelectAsync<MemberRejected>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IConsumedMessage<ValidateMember>> request = harness.Consumed
                .SelectAsync<ValidateMember>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);
            Task<IConsumedMessage<BeginMemberValidation>> start = sagaHarness.Consumed
                .SelectAsync<BeginMemberValidation>(cancellationToken)
                .FirstObservedAsync(cancellationToken: TestContext.Current.CancellationToken);

            await harness.InputQueueSendEndpoint.SendAsync(new BeginMemberValidation(correlationId), cancellationToken);

            Assert.Null((await start.WaitAsync(timeout, cancellationToken)).Exception);
            ISentMessage<ValidateMember> sentRequest = Assert.Single(
                harness.Sent.Snapshot<ValidateMember>());
            Assert.Equal(serviceAddress, sentRequest.Context.DestinationAddress);
            Assert.True(sentRequest.Context.Headers.TryGetHeader(MessageHeaders.Request.Accept, out object? acceptHeader));
            IList<string> acceptedTypes = Assert.IsAssignableFrom<IList<string>>(acceptHeader);
            Assert.Equal(
                new[]
                {
                    MessageUrn.ForTypeString<MemberValid>(),
                    MessageUrn.ForTypeString<MemberInvalid>(),
                    MessageUrn.ForTypeString<MemberDuplicate>(),
                },
                acceptedTypes);
            await request.WaitAsync(timeout, cancellationToken);
            MemberRejected rejected = (await published.WaitAsync(timeout, cancellationToken)).Context.Message;
            Guid? registered = await sagaHarness.WaitForSagaInStateAsync(correlationId, machine.Registered, timeout, TestContext.Current.CancellationToken);
            MultiResponseState? instance = sagaHarness.Sagas.FindById(correlationId);

            Assert.Equal(correlationId, registered);
            Assert.NotNull(instance);
            Assert.Equal(responseKind, instance.ResponseKind);
            Assert.Equal(expectedReason, instance.Reason);
            Assert.Equal(new MemberRejected(correlationId, responseKind, expectedReason), rejected);
            Assert.Equal(1, instance.ResponseCount);
        }
        finally
        {
            await harness.StopAsync(TestContext.Current.CancellationToken).WaitAsync(timeout, CancellationToken.None);
        }

        Assert.Single(harness.Published.Snapshot<MemberRejected>());
    }

    private static Task HandleValidateNameAsync(ConsumeContext<ValidateName> context, CompositeOutcome outcome) =>
        outcome is CompositeOutcome.NameRejected or CompositeOutcome.BothRejected
            ? Task.FromException(new ExpectedValidationException($"name:{context.Message.Name}"))
            : context.RespondAsync(new NameValidated(context.Message.CorrelationId, context.Message.Name));

    private static Task HandleValidateSurnameAsync(ConsumeContext<ValidateSurname> context, CompositeOutcome outcome) =>
        outcome is CompositeOutcome.SurnameRejected or CompositeOutcome.BothRejected
            ? Task.FromException(new ExpectedValidationException($"surname:{context.Message.Surname}"))
            : context.RespondAsync(new SurnameValidated(context.Message.CorrelationId, context.Message.Surname));

    private static void AssertValidationDelivery(Exception? exception, bool expectedFailure, string expectedMessage)
    {
        if (!expectedFailure)
        {
            Assert.Null(exception);
            return;
        }

        ExpectedValidationException validationException = Assert.IsType<ExpectedValidationException>(exception);
        Assert.Equal(expectedMessage, validationException.Message);
    }

    private static InMemoryTestHarness CreateHarness(string prefix, TimeSpan timeout) =>
        new($"{prefix}-{NewId.NextGuid():N}")
        {
            TestTimeout = timeout,
            TestInactivityTimeout = timeout,
        };


    private static TimeSpan OperationTimeout() => TestConfigurationProvider.ForCurrentTestRun()
        .GetValidatedOptions()
        .OperationTimeout!.Value;

    public enum CompositeOutcome
    {
        Success,
        NameRejected,
        SurnameRejected,
        BothRejected,
    }

    public enum MultiResponseKind
    {
        First,
        Second,
        Third,
    }

    public sealed record RegisterMember(Guid CorrelationId, string Name, string Surname) : CorrelatedBy<Guid>;

    public sealed class ValidateName : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    public sealed class ValidateSurname : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }

        public string Surname { get; set; } = string.Empty;
    }

    public sealed record NameValidated(Guid CorrelationId, string Name) : CorrelatedBy<Guid>;

    public sealed record SurnameValidated(Guid CorrelationId, string Surname) : CorrelatedBy<Guid>;

    public sealed record MemberRegistrationResult(
        Guid CorrelationId,
        string Result,
        string Name,
        string Surname) : CorrelatedBy<Guid>;

    public sealed class CompositeRequestState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Surname { get; set; } = string.Empty;

        public string Result { get; set; } = string.Empty;

        public int CompositeStatus { get; set; }

        public int BothFaultedStatus { get; set; }

        public int NameFaultedStatus { get; set; }

        public int SurnameFaultedStatus { get; set; }

        public int TerminalOutcomeCount { get; set; }

        public Guid? NameRequestId { get; set; }

        public Guid? SurnameRequestId { get; set; }
    }

    public sealed class CompositeRequestMachine : ViciOneServiceBusStateMachine<CompositeRequestState>
    {
        public CompositeRequestMachine(Uri serviceAddress)
        {
            ArgumentNullException.ThrowIfNull(serviceAddress);
            InstanceState(instance => instance.CurrentState);
            Event(() => Register, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });
            Request(() => NameRequest, instance => instance.NameRequestId, configuration =>
            {
                configuration.ServiceAddress = serviceAddress;
                configuration.Timeout = TimeSpan.Zero;
            });
            Request(() => SurnameRequest, instance => instance.SurnameRequestId, configuration =>
            {
                configuration.ServiceAddress = serviceAddress;
                configuration.Timeout = TimeSpan.Zero;
            });

            CompositeEvent(() => BothCompleted, instance => instance.CompositeStatus,
                NameRequest.Completed, SurnameRequest.Completed);
            CompositeEvent(() => BothFaulted, instance => instance.BothFaultedStatus,
                NameRequest.Faulted, SurnameRequest.Faulted);
            CompositeEvent(() => NameFaulted, instance => instance.NameFaultedStatus,
                NameRequest.Faulted, SurnameRequest.Completed);
            CompositeEvent(() => SurnameFaulted, instance => instance.SurnameFaultedStatus,
                NameRequest.Completed, SurnameRequest.Faulted);

            Initially(
                When(Register)
                    .Then(context =>
                    {
                        context.Saga.Name = context.Message.Name;
                        context.Saga.Surname = context.Message.Surname;
                    })
                    .Request(NameRequest, context => context.InitAsync<ValidateName>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.Name,
                    }))
                    .Request(SurnameRequest, context => context.InitAsync<ValidateSurname>(new
                    {
                        context.Saga.CorrelationId,
                        context.Saga.Surname,
                    }))
                    .TransitionTo(Registering));

            DuringAny(
                When(NameRequest.Completed).Then(context => context.Saga.Name = context.Message.Name),
                When(SurnameRequest.Completed).Then(context => context.Saga.Surname = context.Message.Surname),
                When(NameRequest.Faulted).Then(context => context.Saga.Name = "REJECTED!"),
                When(SurnameRequest.Faulted).Then(context => context.Saga.Surname = "REJECTED!"));

            DuringAny(
                When(BothCompleted).Then(context => context.Saga.Result = "Success").Then(RecordOutcome).Publish(Outcome).TransitionTo(Registered),
                When(BothFaulted).Then(context =>
                {
                    context.Saga.Result = "Invalid ID";
                    context.Saga.Name = "REJECTED!";
                    context.Saga.Surname = "REJECTED!";
                }).Then(RecordOutcome).Publish(Outcome).TransitionTo(Rejected),
                When(NameFaulted).Then(context =>
                {
                    context.Saga.Result = "Invalid Name";
                    context.Saga.Name = "REJECTED!";
                }).Then(RecordOutcome).Publish(Outcome).TransitionTo(Rejected),
                When(SurnameFaulted).Then(context =>
                {
                    context.Saga.Result = "Invalid Surname";
                    context.Saga.Surname = "REJECTED!";
                }).Then(RecordOutcome).Publish(Outcome).TransitionTo(Rejected));
        }

        private static void RecordOutcome(BehaviorContext<CompositeRequestState> context) =>
            context.Saga.TerminalOutcomeCount++;

        private static MemberRegistrationResult Outcome(BehaviorContext<CompositeRequestState> context) =>
            new(context.Saga.CorrelationId, context.Saga.Result, context.Saga.Name, context.Saga.Surname);

        public State Registering { get; } = null!;

        public State Registered { get; } = null!;

        public State Rejected { get; } = null!;

        public Event<RegisterMember> Register { get; } = null!;

        public Request<CompositeRequestState, ValidateName, NameValidated> NameRequest { get; } = null!;

        public Request<CompositeRequestState, ValidateSurname, SurnameValidated> SurnameRequest { get; } = null!;

        public Event BothCompleted { get; } = null!;

        public Event BothFaulted { get; } = null!;

        public Event NameFaulted { get; } = null!;

        public Event SurnameFaulted { get; } = null!;
    }

    public sealed record BeginMemberValidation(Guid CorrelationId) : CorrelatedBy<Guid>;

    public sealed class ValidateMember : CorrelatedBy<Guid>
    {
        public Guid CorrelationId { get; set; }
    }

    public sealed record MemberValid(Guid CorrelationId, string Reason) : CorrelatedBy<Guid>;

    public sealed record MemberInvalid(Guid CorrelationId, string Reason) : CorrelatedBy<Guid>;

    public sealed record MemberDuplicate(Guid CorrelationId, string Reason) : CorrelatedBy<Guid>;

    public sealed record MemberRejected(Guid CorrelationId, MultiResponseKind ResponseKind, string Reason)
        : CorrelatedBy<Guid>;

    public sealed class MultiResponseState : SagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }

        public string CurrentState { get; set; } = string.Empty;

        public Guid? RequestId { get; set; }

        public MultiResponseKind ResponseKind { get; set; }

        public string Reason { get; set; } = string.Empty;

        public int ResponseCount { get; set; }
    }

    public sealed class MultiResponseMachine : ViciOneServiceBusStateMachine<MultiResponseState>
    {
        public MultiResponseMachine(Uri serviceAddress)
        {
            ArgumentNullException.ThrowIfNull(serviceAddress);
            InstanceState(instance => instance.CurrentState);
            Event(() => Begin, configuration =>
            {
                configuration.CorrelateById(context => context.Message.CorrelationId);
                configuration.SelectId(context => context.Message.CorrelationId);
                configuration.InsertOnInitial = true;
            });
            Request(() => Validation, instance => instance.RequestId, configuration =>
            {
                configuration.ServiceAddress = serviceAddress;
                configuration.Timeout = TimeSpan.Zero;
            });

            Initially(
                When(Begin)
                    .Request(Validation, context => context.InitAsync<ValidateMember>(new
                    {
                        context.Saga.CorrelationId,
                    }))
                    .TransitionTo(Validation.Pending));
            During(
                Validation.Pending,
                When(Validation.Completed2)
                    .Then(context =>
                    {
                        context.Saga.ResponseKind = MultiResponseKind.Second;
                        context.Saga.Reason = context.Message.Reason;
                        context.Saga.ResponseCount++;
                    })
                    .Publish(context => new MemberRejected(
                        context.Saga.CorrelationId,
                        context.Saga.ResponseKind,
                        context.Saga.Reason))
                    .TransitionTo(Registered),
                When(Validation.Completed3)
                    .Then(context =>
                    {
                        context.Saga.ResponseKind = MultiResponseKind.Third;
                        context.Saga.Reason = context.Message.Reason;
                        context.Saga.ResponseCount++;
                    })
                    .Publish(context => new MemberRejected(
                        context.Saga.CorrelationId,
                        context.Saga.ResponseKind,
                        context.Saga.Reason))
                    .TransitionTo(Registered));
        }

        public State Registered { get; } = null!;

        public Event<BeginMemberValidation> Begin { get; } = null!;

        public Request<MultiResponseState, ValidateMember, MemberValid, MemberInvalid, MemberDuplicate> Validation { get; }
            = null!;
    }

    private sealed class ExpectedValidationException(string message) : Exception(message);
}
