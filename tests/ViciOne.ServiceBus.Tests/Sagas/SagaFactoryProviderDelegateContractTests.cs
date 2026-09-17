using System.Reflection;
using ViciOne.ServiceBus.Sagas;
using ViciOne.ServiceBus.Tests.Infrastructure.Requirements;
using Xunit;

namespace ViciOne.ServiceBus.Tests.Sagas;

public sealed class SagaFactoryProviderDelegateContractTests
{
    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "message-factories-preserve-context-and-sync-async-results")]
    public async Task MessageFactories_PreserveContextIdentityAndSyncAsyncResultsAsync()
    {
        IBehaviorContext<DelegateSaga> stateContext = CreateContext<IBehaviorContext<DelegateSaga>>();
        IBehaviorContext<DelegateSaga, InputMessage> messageContext = CreateContext<IBehaviorContext<DelegateSaga, InputMessage>>();
        var stateResult = new OutputMessage("state-sync");
        var messageResult = new OutputMessage("message-sync");
        var asyncStateResult = new OutputMessage("state-async");
        var asyncMessageResult = new OutputMessage("message-async");
        Task<OutputMessage> stateTask = Task.FromResult(asyncStateResult);
        Task<OutputMessage> messageTask = Task.FromResult(asyncMessageResult);
        IBehaviorContext<DelegateSaga>? observedStateContext = null;
        IBehaviorContext<DelegateSaga, InputMessage>? observedMessageContext = null;
        IBehaviorContext<DelegateSaga>? observedAsyncStateContext = null;
        IBehaviorContext<DelegateSaga, InputMessage>? observedAsyncMessageContext = null;

        EventMessageFactory<DelegateSaga, OutputMessage> stateFactory = context =>
        {
            observedStateContext = context;
            return stateResult;
        };
        EventMessageFactory<DelegateSaga, InputMessage, OutputMessage> messageFactory = context =>
        {
            observedMessageContext = context;
            return messageResult;
        };
        AsyncEventMessageFactory<DelegateSaga, OutputMessage> asyncStateFactory = context =>
        {
            observedAsyncStateContext = context;
            return stateTask;
        };
        AsyncEventMessageFactory<DelegateSaga, InputMessage, OutputMessage> asyncMessageFactory = context =>
        {
            observedAsyncMessageContext = context;
            return messageTask;
        };

        Assert.Same(stateResult, stateFactory(stateContext));
        Assert.Same(stateContext, observedStateContext);
        Assert.Same(messageResult, messageFactory(messageContext));
        Assert.Same(messageContext, observedMessageContext);

        Task<OutputMessage> returnedStateTask = asyncStateFactory(stateContext);
        Assert.Same(stateTask, returnedStateTask);
        Assert.Same(asyncStateResult, await returnedStateTask);
        Assert.Same(stateContext, observedAsyncStateContext);

        Task<OutputMessage> returnedMessageTask = asyncMessageFactory(messageContext);
        Assert.Same(messageTask, returnedMessageTask);
        Assert.Same(asyncMessageResult, await returnedMessageTask);
        Assert.Same(messageContext, observedAsyncMessageContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "exception-factories-preserve-context-shape-and-results")]
    public async Task ExceptionFactories_PreserveContextIdentityAndSyncAsyncResultsAsync()
    {
        IBehaviorExceptionContext<DelegateSaga, InvalidOperationException> stateContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException> messageContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException>>();
        var stateResult = new OutputMessage("state-sync");
        var messageResult = new OutputMessage("message-sync");
        var asyncStateResult = new OutputMessage("state-async");
        var asyncMessageResult = new OutputMessage("message-async");
        Task<OutputMessage> stateTask = Task.FromResult(asyncStateResult);
        Task<OutputMessage> messageTask = Task.FromResult(asyncMessageResult);
        IBehaviorExceptionContext<DelegateSaga, InvalidOperationException>? observedStateContext = null;
        IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException>? observedMessageContext = null;
        IBehaviorExceptionContext<DelegateSaga, InvalidOperationException>? observedAsyncStateContext = null;
        IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException>? observedAsyncMessageContext = null;

        EventExceptionMessageFactory<DelegateSaga, InvalidOperationException, OutputMessage> stateFactory = context =>
        {
            observedStateContext = context;
            return stateResult;
        };
        EventExceptionMessageFactory<DelegateSaga, InputMessage, InvalidOperationException, OutputMessage> messageFactory = context =>
        {
            observedMessageContext = context;
            return messageResult;
        };
        AsyncEventExceptionMessageFactory<DelegateSaga, InvalidOperationException, OutputMessage> asyncStateFactory = context =>
        {
            observedAsyncStateContext = context;
            return stateTask;
        };
        AsyncEventExceptionMessageFactory<DelegateSaga, InputMessage, InvalidOperationException, OutputMessage> asyncMessageFactory = context =>
        {
            observedAsyncMessageContext = context;
            return messageTask;
        };

        Assert.Same(stateResult, stateFactory(stateContext));
        Assert.Same(stateContext, observedStateContext);
        Assert.Same(messageResult, messageFactory(messageContext));
        Assert.Same(messageContext, observedMessageContext);

        Task<OutputMessage> returnedStateTask = asyncStateFactory(stateContext);
        Assert.Same(stateTask, returnedStateTask);
        Assert.Same(asyncStateResult, await returnedStateTask);
        Assert.Same(stateContext, observedAsyncStateContext);

        Task<OutputMessage> returnedMessageTask = asyncMessageFactory(messageContext);
        Assert.Same(messageTask, returnedMessageTask);
        Assert.Same(asyncMessageResult, await returnedMessageTask);
        Assert.Same(messageContext, observedAsyncMessageContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "address-providers-preserve-context-and-addresses")]
    public void AddressProviders_PreserveContextIdentityAndSelectedAddresses()
    {
        IBehaviorContext<DelegateSaga> stateContext = CreateContext<IBehaviorContext<DelegateSaga>>();
        IBehaviorContext<DelegateSaga, InputMessage> messageContext = CreateContext<IBehaviorContext<DelegateSaga, InputMessage>>();
        var stateDestination = new Uri("loopback://state-destination");
        var messageDestination = new Uri("loopback://message-destination");
        var stateService = new Uri("loopback://state-service");
        var messageService = new Uri("loopback://message-service");
        IBehaviorContext<DelegateSaga>? observedStateDestinationContext = null;
        IBehaviorContext<DelegateSaga, InputMessage>? observedMessageDestinationContext = null;
        IBehaviorContext<DelegateSaga>? observedStateServiceContext = null;
        IBehaviorContext<DelegateSaga, InputMessage>? observedMessageServiceContext = null;

        DestinationAddressProvider<DelegateSaga> stateDestinationProvider = context =>
        {
            observedStateDestinationContext = context;
            return stateDestination;
        };
        DestinationAddressProvider<DelegateSaga, InputMessage> messageDestinationProvider = context =>
        {
            observedMessageDestinationContext = context;
            return messageDestination;
        };
        ServiceAddressProvider<DelegateSaga> stateServiceProvider = context =>
        {
            observedStateServiceContext = context;
            return stateService;
        };
        ServiceAddressProvider<DelegateSaga, InputMessage> messageServiceProvider = context =>
        {
            observedMessageServiceContext = context;
            return messageService;
        };

        Assert.Same(stateDestination, stateDestinationProvider(stateContext));
        Assert.Same(stateContext, observedStateDestinationContext);
        Assert.Same(messageDestination, messageDestinationProvider(messageContext));
        Assert.Same(messageContext, observedMessageDestinationContext);
        Assert.Same(stateService, stateServiceProvider(stateContext));
        Assert.Same(stateContext, observedStateServiceContext);
        Assert.Same(messageService, messageServiceProvider(messageContext));
        Assert.Same(messageContext, observedMessageServiceContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "schedule-providers-preserve-context-and-temporal-values")]
    public void ScheduleProviders_PreserveContextIdentityAndTemporalValues()
    {
        IBehaviorContext<DelegateSaga> stateContext = CreateContext<IBehaviorContext<DelegateSaga>>();
        IBehaviorContext<DelegateSaga, InputMessage> messageContext = CreateContext<IBehaviorContext<DelegateSaga, InputMessage>>();
        IBehaviorExceptionContext<DelegateSaga, InvalidOperationException> stateExceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InvalidOperationException>>();
        IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionContext =
            CreateContext<IBehaviorExceptionContext<DelegateSaga, InputMessage, InvalidOperationException>>();
        TimeSpan stateDelay = TimeSpan.FromSeconds(1);
        TimeSpan messageDelay = TimeSpan.FromSeconds(2);
        TimeSpan stateExceptionDelay = TimeSpan.FromSeconds(3);
        TimeSpan messageExceptionDelay = TimeSpan.FromSeconds(4);
        DateTimeOffset stateTime = new(2031, 1, 2, 3, 4, 5, TimeSpan.Zero);
        DateTimeOffset messageTime = stateTime.AddMinutes(1);
        DateTimeOffset stateExceptionTime = stateTime.AddMinutes(2);
        DateTimeOffset messageExceptionTime = stateTime.AddMinutes(3);
        object? observedStateDelayContext = null;
        object? observedMessageDelayContext = null;
        object? observedStateExceptionDelayContext = null;
        object? observedMessageExceptionDelayContext = null;
        object? observedStateTimeContext = null;
        object? observedMessageTimeContext = null;
        object? observedStateExceptionTimeContext = null;
        object? observedMessageExceptionTimeContext = null;

        ScheduleDelayProvider<DelegateSaga> stateDelayProvider = context =>
        {
            observedStateDelayContext = context;
            return stateDelay;
        };
        ScheduleDelayProvider<DelegateSaga, InputMessage> messageDelayProvider = context =>
        {
            observedMessageDelayContext = context;
            return messageDelay;
        };
        ScheduleDelayExceptionProvider<DelegateSaga, InvalidOperationException> stateExceptionDelayProvider = context =>
        {
            observedStateExceptionDelayContext = context;
            return stateExceptionDelay;
        };
        ScheduleDelayExceptionProvider<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionDelayProvider = context =>
        {
            observedMessageExceptionDelayContext = context;
            return messageExceptionDelay;
        };
        ScheduleTimeProvider<DelegateSaga> stateTimeProvider = context =>
        {
            observedStateTimeContext = context;
            return stateTime;
        };
        ScheduleTimeProvider<DelegateSaga, InputMessage> messageTimeProvider = context =>
        {
            observedMessageTimeContext = context;
            return messageTime;
        };
        ScheduleTimeExceptionProvider<DelegateSaga, InvalidOperationException> stateExceptionTimeProvider = context =>
        {
            observedStateExceptionTimeContext = context;
            return stateExceptionTime;
        };
        ScheduleTimeExceptionProvider<DelegateSaga, InputMessage, InvalidOperationException> messageExceptionTimeProvider = context =>
        {
            observedMessageExceptionTimeContext = context;
            return messageExceptionTime;
        };

        Assert.Equal(stateDelay, stateDelayProvider(stateContext));
        Assert.Same(stateContext, observedStateDelayContext);
        Assert.Equal(messageDelay, messageDelayProvider(messageContext));
        Assert.Same(messageContext, observedMessageDelayContext);
        Assert.Equal(stateExceptionDelay, stateExceptionDelayProvider(stateExceptionContext));
        Assert.Same(stateExceptionContext, observedStateExceptionDelayContext);
        Assert.Equal(messageExceptionDelay, messageExceptionDelayProvider(messageExceptionContext));
        Assert.Same(messageExceptionContext, observedMessageExceptionDelayContext);
        Assert.Equal(stateTime, stateTimeProvider(stateContext));
        Assert.Same(stateContext, observedStateTimeContext);
        Assert.Equal(messageTime, messageTimeProvider(messageContext));
        Assert.Same(messageContext, observedMessageTimeContext);
        Assert.Equal(stateExceptionTime, stateExceptionTimeProvider(stateExceptionContext));
        Assert.Same(stateExceptionContext, observedStateExceptionTimeContext);
        Assert.Equal(messageExceptionTime, messageExceptionTimeProvider(messageExceptionContext));
        Assert.Same(messageExceptionContext, observedMessageExceptionTimeContext);
    }

    [Fact]
    [RequirementCoverage("REQ-VSB-SAGA-DELEGATE-CONTRACTS", "generic-variance-and-class-constraints-remain-compatible")]
    public void DelegateDefinitions_DeclareExpectedVarianceAndEveryGenericConstraint()
    {
        DelegateContract[] contracts =
        [
            Contract(typeof(AsyncEventExceptionMessageFactory<,,>), Saga(), ExceptionInput(), AsyncOutput()),
            Contract(typeof(AsyncEventExceptionMessageFactory<,,,>), Saga(), MessageInput(), ExceptionInput(), AsyncOutput()),
            Contract(typeof(AsyncEventMessageFactory<,>), Saga(), AsyncOutput()),
            Contract(typeof(AsyncEventMessageFactory<,,>), Saga(), MessageInput(), AsyncOutput()),
            Contract(typeof(EventExceptionMessageFactory<,,>), Saga(), ExceptionInput(), SyncOutput()),
            Contract(typeof(EventExceptionMessageFactory<,,,>), Saga(), MessageInput(), ExceptionInput(), SyncOutput()),
            Contract(typeof(EventMessageFactory<,>), Saga(), SyncOutput()),
            Contract(typeof(EventMessageFactory<,,>), Saga(), MessageInput(), SyncOutput()),
            Contract(typeof(DestinationAddressProvider<>), Saga()),
            Contract(typeof(DestinationAddressProvider<,>), Saga(), MessageInput()),
            Contract(typeof(ServiceAddressProvider<>), Saga()),
            Contract(typeof(ServiceAddressProvider<,>), Saga(), MessageInput()),
            Contract(typeof(ScheduleDelayExceptionProvider<,>), Saga(), ExceptionInput()),
            Contract(typeof(ScheduleDelayExceptionProvider<,,>), Saga(), MessageInput(), ExceptionInput()),
            Contract(typeof(ScheduleDelayProvider<>), Saga()),
            Contract(typeof(ScheduleDelayProvider<,>), Saga(), MessageInput()),
            Contract(typeof(ScheduleTimeExceptionProvider<,>), Saga(), ExceptionInput()),
            Contract(typeof(ScheduleTimeExceptionProvider<,,>), Saga(), MessageInput(), ExceptionInput()),
            Contract(typeof(ScheduleTimeProvider<>), Saga()),
            Contract(typeof(ScheduleTimeProvider<,>), Saga(), MessageInput()),
        ];

        Assert.Equal(20, contracts.Length);
        foreach (DelegateContract contract in contracts)
        {
            Assert.True(contract.Definition.IsGenericTypeDefinition, contract.Definition.FullName);
            Assert.Equal(typeof(MulticastDelegate), contract.Definition.BaseType);
            Type[] parameters = contract.Definition.GetGenericArguments();
            Assert.Equal(contract.Parameters.Length, parameters.Length);

            for (var index = 0; index < parameters.Length; index++)
            {
                GenericParameterContract expected = contract.Parameters[index];
                Type parameter = parameters[index];
                Assert.True(parameter.IsGenericParameter, $"{contract.Definition}.{parameter.Name}");
                Assert.Equal(expected.Attributes, parameter.GenericParameterAttributes);
                Assert.Equal(expected.TypeConstraints, parameter.GetGenericParameterConstraints());
            }
        }
    }

    private static TContext CreateContext<TContext>()
        where TContext : class => DispatchProxy.Create<TContext, ContextProxy>();

    private static DelegateContract Contract(Type definition, params GenericParameterContract[] parameters) =>
        new(definition, parameters);

    private static GenericParameterContract Saga() =>
        new(GenericParameterAttributes.ReferenceTypeConstraint, [typeof(ISagaStateMachineInstance)]);

    private static GenericParameterContract MessageInput() =>
        new(GenericParameterAttributes.Contravariant | GenericParameterAttributes.ReferenceTypeConstraint, []);

    private static GenericParameterContract ExceptionInput() =>
        new(GenericParameterAttributes.Contravariant, [typeof(Exception)]);

    private static GenericParameterContract AsyncOutput() =>
        new(GenericParameterAttributes.ReferenceTypeConstraint, []);

    private static GenericParameterContract SyncOutput() =>
        new(GenericParameterAttributes.Covariant | GenericParameterAttributes.ReferenceTypeConstraint, []);

    private sealed record DelegateContract(Type Definition, GenericParameterContract[] Parameters);

    private sealed record GenericParameterContract(GenericParameterAttributes Attributes, Type[] TypeConstraints);

    private class ContextProxy : DispatchProxy
    {
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            throw new NotSupportedException($"The context member '{targetMethod?.Name}' is not used by these delegate contracts.");
    }

    private sealed class DelegateSaga : ISagaStateMachineInstance
    {
        public Guid CorrelationId { get; set; }
        public string CurrentState { get; set; } = "Initial";
    }

    private sealed record InputMessage(string Value = "input");

    private sealed record OutputMessage(string Value);
}
