using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Internals;
using ViciOne.ServiceBus.Internals.Reflection;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Describes a saga request, its accepted response and its correlation storage.</summary>
    /// <typeparam name="TRequest">The message contract sent as the request.</typeparam>
    /// <typeparam name="TResponse">The accepted response message contract.</typeparam>
    public class StateMachineRequest<TRequest, TResponse> :
        IRequest<TInstance, TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        readonly List<string> _accept;
        readonly IReadProperty<TInstance, Guid?> _read = null!;
        readonly IWriteProperty<TInstance, Guid?> _write = null!;

        /// <summary>Configures a request and registers its accepted response message URN.</summary>
        /// <param name="name">The request's state-machine name.</param>
        /// <param name="settings">The request's destination, timing and response settings.</param>
        /// <param name="requestIdExpression">The writable request-ID property, or <see langword="null" /> to use the saga's correlation ID.</param>
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

        /// <summary>Gets the request's state-machine name.</summary>
        public string Name { get; }
        /// <summary>Gets the request's destination, timing and response settings.</summary>
        public IRequestSettings<TInstance, TRequest, TResponse> Settings { get; }
        /// <summary>Gets or sets the response event assigned during request configuration.</summary>
        public IEvent<TResponse> Completed { get; set; } = null!;
        /// <summary>Gets or sets the request-fault event assigned during request configuration.</summary>
        public IEvent<Fault<TRequest>> Faulted { get; set; } = null!;
        /// <summary>Gets or sets the timeout event assigned during request configuration.</summary>
        public IEvent<IRequestTimeoutExpired<TRequest>> TimeoutExpired { get; set; } = null!;
        /// <summary>Gets or sets the state used while the request awaits a response.</summary>
        public IState Pending { get; set; } = null!;
        /// <summary>Writes the configured request-ID property; does nothing when correlation uses the saga ID.</summary>
        /// <param name="instance">The saga whose request-ID property is written.</param>
        /// <param name="requestId">The request ID to store, or <see langword="null" /> to clear that property.</param>
        public void SetRequestId(TInstance instance, Guid? requestId)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _write?.Set(instance, requestId);
        }

        /// <summary>Reads the configured request-ID property or falls back to the saga's correlation ID.</summary>
        /// <param name="instance">The saga whose current request correlation is read.</param>
        /// <returns>The stored nullable request ID, or the saga's correlation ID when no property is configured.</returns>
        public Guid? GetRequestId(TInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return _read != null
                ? _read.Get(instance)
                : instance.CorrelationId;
        }

        /// <summary>Creates a new request ID for explicit storage or reuses the saga's correlation ID.</summary>
        /// <param name="instance">The saga supplying the fallback correlation ID.</param>
        /// <returns>A new ID when a request-ID property is configured; otherwise, the saga's correlation ID.</returns>
        public Guid GenerateRequestId(TInstance instance)
        {
            return _read != null
                ? NewId.NextGuid()
                : instance.CorrelationId;
        }

        /// <summary>Sets a positive configured time to live and the accepted-response URNs on the outgoing request.</summary>
        /// <param name="context">The request's outgoing send context.</param>
        public void SetSendContextHeaders(SendContext<TRequest> context)
        {
            if (Settings.TimeToLive.HasValue && Settings.TimeToLive.Value > TimeSpan.Zero)
                context.TimeToLive = Settings.TimeToLive.Value;

            context.Headers.Set(MessageHeaders.Request.Accept, _accept);
        }

        /// <summary>Matches a timeout event's request ID against the saga's current request correlation.</summary>
        /// <param name="context">The timeout event and saga being correlated.</param>
        /// <returns><see langword="true" /> only when both request IDs are present and equal.</returns>
        public bool EventFilter(IBehaviorContext<TInstance, IRequestTimeoutExpired<TRequest>> context)
        {
            if (!context.RequestId.HasValue)
                return false;

            Guid? requestId = GetRequestId(context.Saga);

            return requestId.HasValue && requestId.Value == context.RequestId.Value;
        }

        /// <summary>Adds a response message URN to the request's accepted-response list.</summary>
        /// <typeparam name="T">The accepted response message contract.</typeparam>
        protected void AcceptResponse<T>()
            where T : class
        {
            _accept.Add(MessageUrn.ForTypeString<T>());
        }
    }


    /// <summary>Describes a saga request that accepts either of two response message contracts.</summary>
    /// <typeparam name="TRequest">The outgoing request message contract.</typeparam>
    /// <typeparam name="TResponse">The first accepted response message contract.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response message contract.</typeparam>
    public class StateMachineRequest<TRequest, TResponse, TResponse2> :
        StateMachineRequest<TRequest, TResponse>,
        IRequest<TInstance, TRequest, TResponse, TResponse2>
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        /// <summary>Configures the request and registers both accepted response message URNs.</summary>
        /// <param name="name">The request's state-machine name.</param>
        /// <param name="settings">The request's destination, timing and two-response settings.</param>
        /// <param name="requestIdExpression">The writable request-ID property, or <see langword="null" /> to use saga-ID correlation.</param>
        public StateMachineRequest(string name, IRequestSettings<TInstance, TRequest, TResponse, TResponse2> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
            : base(name, settings, requestIdExpression)
        {
            Settings = settings;

            AcceptResponse<TResponse2>();
        }

        /// <summary>Gets the request settings for both accepted response contracts.</summary>
        public new IRequestSettings<TInstance, TRequest, TResponse, TResponse2> Settings { get; }

        /// <summary>Gets or sets the second response event assigned during request configuration.</summary>
        public IEvent<TResponse2> Completed2 { get; set; } = null!;
    }


    /// <summary>Describes a saga request that accepts any of three response message contracts.</summary>
    /// <typeparam name="TRequest">The outgoing request message contract.</typeparam>
    /// <typeparam name="TResponse">The first accepted response message contract.</typeparam>
    /// <typeparam name="TResponse2">The second accepted response message contract.</typeparam>
    /// <typeparam name="TResponse3">The third accepted response message contract.</typeparam>
    public class StateMachineRequest<TRequest, TResponse, TResponse2, TResponse3> :
        StateMachineRequest<TRequest, TResponse, TResponse2>,
        IRequest<TInstance, TRequest, TResponse, TResponse2, TResponse3>
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
        where TResponse3 : class
    {
        /// <summary>Configures the request and registers all three accepted response message URNs.</summary>
        /// <param name="name">The request's state-machine name.</param>
        /// <param name="settings">The request's destination, timing and three-response settings.</param>
        /// <param name="requestIdExpression">The writable request-ID property, or <see langword="null" /> to use saga-ID correlation.</param>
        public StateMachineRequest(string name, IRequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
            : base(name, settings, requestIdExpression)
        {
            Settings = settings;

            AcceptResponse<TResponse3>();
        }

        /// <summary>Gets the request settings for all three accepted response contracts.</summary>
        public new IRequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> Settings { get; }

        /// <summary>Gets or sets the third response event assigned during request configuration.</summary>
        public IEvent<TResponse3> Completed3 { get; set; } = null!;
    }
}
