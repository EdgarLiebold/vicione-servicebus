using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using ViciOne.ServiceBus.Contracts;
using ViciOne.ServiceBus.Internals;

namespace ViciOne.ServiceBus;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, SagaStateMachineInstance
{
    public class StateMachineRequest<TRequest, TResponse> :
        Request<TInstance, TRequest, TResponse>
        where TRequest : class
        where TResponse : class
    {
        readonly List<string> _accept;
        readonly IReadProperty<TInstance, Guid?> _read = null!;
        readonly IWriteProperty<TInstance, Guid?> _write = null!;

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

        public string Name { get; }
        public RequestSettings<TInstance, TRequest, TResponse> Settings { get; }
        public Event<TResponse> Completed { get; set; } = null!;
        public Event<Fault<TRequest>> Faulted { get; set; } = null!;
        public Event<RequestTimeoutExpired<TRequest>> TimeoutExpired { get; set; } = null!;
        public State Pending { get; set; } = null!;
        public void SetRequestId(TInstance instance, Guid? requestId)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            _write?.Set(instance, requestId);
        }

        public Guid? GetRequestId(TInstance instance)
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            return _read != null
                ? _read.Get(instance)
                : instance.CorrelationId;
        }

        public Guid GenerateRequestId(TInstance instance)
        {
            return _read != null
                ? NewId.NextGuid()
                : instance.CorrelationId;
        }

        public void SetSendContextHeaders(SendContext<TRequest> context)
        {
            if (Settings.TimeToLive.HasValue && Settings.TimeToLive.Value > TimeSpan.Zero)
                context.TimeToLive = Settings.TimeToLive.Value;

            context.Headers.Set(MessageHeaders.Request.Accept, _accept);
        }

        public bool EventFilter(BehaviorContext<TInstance, RequestTimeoutExpired<TRequest>> context)
        {
            if (!context.RequestId.HasValue)
                return false;

            Guid? requestId = GetRequestId(context.Saga);

            return requestId.HasValue && requestId.Value == context.RequestId.Value;
        }

        protected void AcceptResponse<T>()
            where T : class
        {
            _accept.Add(MessageUrn.ForTypeString<T>());
        }
    }


    public class StateMachineRequest<TRequest, TResponse, TResponse2> :
        StateMachineRequest<TRequest, TResponse>,
        Request<TInstance, TRequest, TResponse, TResponse2>
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
    {
        public StateMachineRequest(string name, RequestSettings<TInstance, TRequest, TResponse, TResponse2> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
            : base(name, settings, requestIdExpression)
        {
            Settings = settings;

            AcceptResponse<TResponse2>();
        }

        public new RequestSettings<TInstance, TRequest, TResponse, TResponse2> Settings { get; }

        public Event<TResponse2> Completed2 { get; set; } = null!;
    }


    public class StateMachineRequest<TRequest, TResponse, TResponse2, TResponse3> :
        StateMachineRequest<TRequest, TResponse, TResponse2>,
        Request<TInstance, TRequest, TResponse, TResponse2, TResponse3>
        where TRequest : class
        where TResponse : class
        where TResponse2 : class
        where TResponse3 : class
    {
        public StateMachineRequest(string name, RequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> settings,
            Expression<Func<TInstance, Guid?>>? requestIdExpression = default)
            : base(name, settings, requestIdExpression)
        {
            Settings = settings;

            AcceptResponse<TResponse3>();
        }

        public new RequestSettings<TInstance, TRequest, TResponse, TResponse2, TResponse3> Settings { get; }

        public Event<TResponse3> Completed3 { get; set; } = null!;
    }
}
