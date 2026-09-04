using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus.Sagas;

/// <summary>
/// Provides a vici one service bus state machine implementation.
/// </summary>
public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    /// <summary>
    /// Provides a state machine request implementation.
    /// </summary>
    /// <typeparam name="TRequest">The t request type.</typeparam>
    /// <typeparam name="TResponse">The t response type.</typeparam>
    public class StateMachineRequest<TRequest, TResponse> :
        Request<TInstance, TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        readonly List<string> _accept;
        readonly IReadProperty<TInstance, Guid?> _read = null!;
        readonly IWriteProperty<TInstance, Guid?> _write = null!;

        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="name">The name value.</param>
        /// <param name="settings">The settings value.</param>
        /// <param name="requestIdExpression">The request id expression value.</param>
        public StateMachineRequest(string name, RequestSettings<TInstance, TRequest, TResponse> settings,
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

        /// <summary>
        /// Gets the name value.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// Gets the settings value.
        /// </summary>
        public RequestSettings<TInstance, TRequest, TResponse> Settings { get; }
        /// <summary>
        /// Gets or sets the completed value.
        /// </summary>
        public Event<TResponse> Completed { get; set; } = null!;
        /// <summary>
        /// Gets or sets the faulted value.
        /// </summary>
        public Event<Fault<TRequest>> Faulted { get; set; } = null!;
        /// <summary>
        /// Gets or sets the timeout expired value.
        /// </summary>
        public Event<RequestTimeoutExpired<TRequest>> TimeoutExpired { get; set; } = null!;
        /// <summary>
        /// Gets or sets the pending value.
        /// </summary>
        public State Pending { get; set; } = null!;
        /// <summary>
        /// Sets request id.
        /// </summary>
        /// <param name="instance">The instance value.</param>
        /// <param name="requestId">The request id value.</param>
        public void SetRequestId(TInstance instance, Guid? requestId)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _write?.Set(instance, requestId);
        }

        /// <summary>
        /// Gets request id.
        /// </summary>
        /// <param name="instance">The instance value.</param>
        /// <returns>The result of the operation.</returns>
        public Guid? GetRequestId(TInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return _read != null
                ? _read.Get(instance)
                : instance.CorrelationId;
        }

        /// <summary>
        /// Performs the generate request id operation.
        /// </summary>
        /// <param name="instance">The instance value.</param>
        /// <returns>The result of the operation.</returns>
        public Guid GenerateRequestId(TInstance instance)
        {
            return _read != null
                ? NewId.NextGuid()
                : instance.CorrelationId;
        }

        /// <summary>
        /// Sets send context headers.
        /// </summary>
        /// <param name="context">The operation context.</param>
        public void SetSendContextHeaders(SendContext<TRequest> context)
        {
            if (Settings.TimeToLive.HasValue && Settings.TimeToLive.Value > TimeSpan.Zero)
                context.TimeToLive = Settings.TimeToLive.Value;

            context.Headers.Set(MessageHeaders.Request.Accept, _accept);
        }

        /// <summary>
        /// Performs the event filter operation.
        /// </summary>
        /// <param name="context">The operation context.</param>
        /// <returns><see langword="true" /> when the condition is satisfied; otherwise, <see langword="false" />.</returns>
        public bool EventFilter(BehaviorContext<TInstance, RequestTimeoutExpired<TRequest>> context)
        {
            if (!context.RequestId.HasValue)
                return false;

            Guid? requestId = GetRequestId(context.Saga);

            return requestId.HasValue && requestId.Value == context.RequestId.Value;
        }

        /// <summary>
        /// Performs the accept response operation.
        /// </summary>
        /// <typeparam name="T">The t type.</typeparam>
        protected void AcceptResponse<T>()
            where T : class
        {
            _accept.Add(MessageUrn.ForTypeString<T>());
        }
    }


    /// <summary>
    /// Provides a state machine request implementation.
    /// </summary>
    /// <typeparam name="TRequest">The t request type.</typeparam>
    /// <typeparam name="TResponse">The t response type.</typeparam>
    /// <typeparam name="TResponse2">The t response2 type.</typeparam>
    public class StateMachineRequest<TRequest, TResponse, TResponse2> :
        StateMachineRequest<TRequest, TResponse>,
        Request<TInstance, TRequest, TResponse, TResponse2>
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="name">The name value.</param>
        /// <param name="settings">The settings value.</param>
        /// <param name="requestIdExpression">The request id expression value.</param>
        public StateMachineRequest(string name, RequestSettings<TInstance, TRequest, TResponse, TResponse2> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
            : base(name, settings, requestIdExpression)
        {
            Settings = settings;

            AcceptResponse<TResponse2>();
        }

        /// <summary>
        /// Gets the settings value.
        /// </summary>
        public new RequestSettings<TInstance, TRequest, TResponse, TResponse2> Settings { get; }

        /// <summary>
        /// Gets or sets the completed2 value.
        /// </summary>
        public Event<TResponse2> Completed2 { get; set; } = null!;
    }


    /// <summary>
    /// Provides a state machine request implementation.
    /// </summary>
    /// <typeparam name="TRequest">The t request type.</typeparam>
    /// <typeparam name="TResponse">The t response type.</typeparam>
    /// <typeparam name="TResponse2">The t response2 type.</typeparam>
    /// <typeparam name="TResponse3">The t response3 type.</typeparam>
    public class StateMachineRequest<TRequest, TResponse, TResponse2, TResponse3> :
        StateMachineRequest<TRequest, TResponse, TResponse2>,
        Request<TInstance, TRequest, TResponse, TResponse2, TResponse3>
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
        where TResponse3 : class
    {
        /// <summary>
        /// Initializes a new instance of the containing type.
        /// </summary>
        /// <param name="name">The name value.</param>
        /// <param name="settings">The settings value.</param>
        /// <param name="requestIdExpression">The request id expression value.</param>
        public StateMachineRequest(string name, RequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
            : base(name, settings, requestIdExpression)
        {
            Settings = settings;

            AcceptResponse<TResponse3>();
        }

        /// <summary>
        /// Gets the settings value.
        /// </summary>
        public new RequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> Settings { get; }

        /// <summary>
        /// Gets or sets the completed3 value.
        /// </summary>
        public Event<TResponse3> Completed3 { get; set; } = null!;
    }
}
