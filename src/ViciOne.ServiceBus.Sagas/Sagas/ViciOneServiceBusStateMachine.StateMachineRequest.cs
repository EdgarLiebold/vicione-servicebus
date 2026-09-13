using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Carries the request for state machine.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    public class StateMachineRequest<TRequest, TResponse> :
        IRequest<TInstance, TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        readonly List<string> _accept;
        readonly IReadProperty<TInstance, Guid?> _read = null!;
        readonly IWriteProperty<TInstance, Guid?> _write = null!;

        /// <summary>Initializes a new instance.</summary>
        /// <param name="name">The name.</param>
        /// <param name="settings">The settings that control the operation.</param>
        /// <param name="requestIdExpression">The request id expression.</param>
        public StateMachineRequest(string name, IRequestSettings<TInstance, TRequest, TResponse> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
        {
            Name = name;
            Settings = settings;

            _accept = new List<string>();

            AcceptResponse<TResponse>();

            if (requestIdExpression != null)
            {
                var propertyInfo = requestIdExpression.GetPropertyInfo();

                _read = ReadPropertyCache<TInstance>.GetProperty<Guid?>(propertyInfo);
                _write = WritePropertyCache<TInstance>.GetProperty<Guid?>(propertyInfo);
            }
        }

        /// <summary>Gets the name.</summary>
        public string Name { get; }
        /// <summary>Gets the settings.</summary>
        public IRequestSettings<TInstance, TRequest, TResponse> Settings { get; }
        /// <summary>Gets or sets the completed.</summary>
        public IEvent<TResponse> Completed { get; set; } = null!;
        /// <summary>Gets or sets the faulted.</summary>
        public IEvent<Fault<TRequest>> Faulted { get; set; } = null!;
        /// <summary>Gets or sets the timeout expired.</summary>
        public IEvent<IRequestTimeoutExpired<TRequest>> TimeoutExpired { get; set; } = null!;
        /// <summary>Gets or sets the pending.</summary>
        public IState Pending { get; set; } = null!;
        /// <summary>Sets request id.</summary>
        /// <param name="instance">The instance.</param>
        /// <param name="requestId">The request id.</param>
        public void SetRequestId(TInstance instance, Guid? requestId)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _write?.Set(instance, requestId);
        }

        /// <summary>Gets request id.</summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The request id.</returns>
        public Guid? GetRequestId(TInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return _read != null
                ? _read.Get(instance)
                : instance.CorrelationId;
        }

        /// <summary>Generates request id.</summary>
        /// <param name="instance">The instance.</param>
        /// <returns>The guid produced by the operation.</returns>
        public Guid GenerateRequestId(TInstance instance)
        {
            return _read != null
                ? NewId.NextGuid()
                : instance.CorrelationId;
        }

        /// <summary>Sets send context headers.</summary>
        /// <param name="context">The context associated with the operation.</param>
        public void SetSendContextHeaders(SendContext<TRequest> context)
        {
            if (Settings.TimeToLive.HasValue && Settings.TimeToLive.Value > TimeSpan.Zero)
                context.TimeToLive = Settings.TimeToLive.Value;

            context.Headers.Set(MessageHeaders.Request.Accept, _accept);
        }

        /// <summary>Filters events using the supplied predicate.</summary>
        /// <param name="context">The context associated with the operation.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool EventFilter(IBehaviorContext<TInstance, IRequestTimeoutExpired<TRequest>> context)
        {
            if (!context.RequestId.HasValue)
                return false;

            Guid? requestId = GetRequestId(context.Saga);

            return requestId.HasValue && requestId.Value == context.RequestId.Value;
        }

        /// <summary>Accepts response.</summary>
        /// <typeparam name="T">The value type.</typeparam>
        protected void AcceptResponse<T>()
            where T : class
        {
            _accept.Add(MessageUrn.ForTypeString<T>());
        }
    }


    /// <summary>Carries the request for state machine.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The response2 type.</typeparam>
    public class StateMachineRequest<TRequest, TResponse, TResponse2> :
        StateMachineRequest<TRequest, TResponse>,
        IRequest<TInstance, TRequest, TResponse, TResponse2>
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        /// <summary>Initializes a new instance.</summary>
        /// <param name="name">The name.</param>
        /// <param name="settings">The settings that control the operation.</param>
        /// <param name="requestIdExpression">The request id expression.</param>
        public StateMachineRequest(string name, IRequestSettings<TInstance, TRequest, TResponse, TResponse2> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
            : base(name, settings, requestIdExpression)
        {
            Settings = settings;

            AcceptResponse<TResponse2>();
        }

        /// <summary>Gets the settings.</summary>
        public new IRequestSettings<TInstance, TRequest, TResponse, TResponse2> Settings { get; }

        /// <summary>Gets or sets the completed2.</summary>
        public IEvent<TResponse2> Completed2 { get; set; } = null!;
    }


    /// <summary>Carries the request for state machine.</summary>
    /// <typeparam name="TRequest">The request type.</typeparam>
    /// <typeparam name="TResponse">The response type.</typeparam>
    /// <typeparam name="TResponse2">The response2 type.</typeparam>
    /// <typeparam name="TResponse3">The response3 type.</typeparam>
    public class StateMachineRequest<TRequest, TResponse, TResponse2, TResponse3> :
        StateMachineRequest<TRequest, TResponse, TResponse2>,
        IRequest<TInstance, TRequest, TResponse, TResponse2, TResponse3>
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
        where TResponse3 : class
    {
        /// <summary>Initializes a new instance.</summary>
        /// <param name="name">The name.</param>
        /// <param name="settings">The settings that control the operation.</param>
        /// <param name="requestIdExpression">The request id expression.</param>
        public StateMachineRequest(string name, IRequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
            : base(name, settings, requestIdExpression)
        {
            Settings = settings;

            AcceptResponse<TResponse3>();
        }

        /// <summary>Gets the settings.</summary>
        public new IRequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> Settings { get; }

        /// <summary>Gets or sets the completed3.</summary>
        public IEvent<TResponse3> Completed3 { get; set; } = null!;
    }
}
