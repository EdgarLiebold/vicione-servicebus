namespace ViciOne.ServiceBus.Logging
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Diagnostics.Metrics;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using Courier.Contracts;
    using Metadata;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Middleware;
    using Monitoring;
    using Transports;


    public static class LogContextInstrumentationExtensions
    {
        static readonly ConcurrentDictionary<string, string> _labelCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);
        static readonly ConditionalWeakTable<ILogContext, LogContextInstrumentationState> _logContextStates =
            new ConditionalWeakTable<ILogContext, LogContextInstrumentationState>();
        // IMeterFactory is scoped to one DI service graph. Keying by the factory lets all buses in that graph
        // share their instruments without coupling independent hosts or keeping disposed providers alive.
        static readonly ConditionalWeakTable<IMeterFactory, Lazy<LogContextInstrumentationState>> _meterFactoryStates =
            new ConditionalWeakTable<IMeterFactory, Lazy<LogContextInstrumentationState>>();
        static readonly object _bindingLock = new object();
        static readonly object _fallbackLock = new object();

        static readonly char[] _delimiters = { '<', '>' };

        // The fallback exists only for the explicit non-DI UseInstrumentation API.
        static LogContextInstrumentationState _fallbackState;

        public static StartedInstrument? StartReceiveInstrument(this ILogContext logContext, ReceiveContext context)
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.ReceiveTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _receiveTotal = instrumentation.ReceiveTotal;
            var _receiveFaultTotal = instrumentation.ReceiveFaultTotal;
            var _receiveInProgress = instrumentation.ReceiveInProgress;
            var _receiveDuration = instrumentation.ReceiveDuration;

            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.InputAddress) }
            };

            AddCustomTags(ref tagList, context);

            _receiveTotal.Add(1, tagList);
            _receiveInProgress.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _receiveFaultTotal.Add(1, tagList);
            }, () =>
            {
                _receiveInProgress.Add(-1, tagList);
                _receiveDuration.Record(context.ElapsedTime.TotalMilliseconds, tagList);
            });
        }

        public static StartedInstrument? StartHandlerInstrument<TMessage>(this ILogContext logContext, ConsumeContext<TMessage> context,
            Stopwatch stopwatch)
            where TMessage : class
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.HandlerTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _handlerTotal = instrumentation.HandlerTotal;
            var _handlerFaultTotal = instrumentation.HandlerFaultTotal;
            var _handlerInProgress = instrumentation.HandlerInProgress;
            var _handlerDuration = instrumentation.HandlerDuration;

            var messageTypeLabel = GetMessageTypeLabel<TMessage>();
            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.ReceiveContext.InputAddress) },
                { _options.MessageTypeLabel, messageTypeLabel },
                { _options.ConsumerTypeLabel, GetConsumerTypeLabel<MessageHandler<TMessage>, TMessage>(messageTypeLabel) }
            };

            AddCustomTags(ref tagList, context);

            _handlerTotal.Add(1, tagList);
            _handlerInProgress.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _handlerFaultTotal.Add(1, tagList);
            }, () =>
            {
                _handlerInProgress.Add(-1, tagList);
                _handlerDuration.Record(stopwatch.ElapsedMilliseconds, tagList);
            });
        }

        public static StartedInstrument? StartSagaInstrument<TSaga, T>(this ILogContext logContext, SagaConsumeContext<TSaga, T> context)
            where T : class
            where TSaga : class, ISaga
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.SagaTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _sagaTotal = instrumentation.SagaTotal;
            var _sagaFaultTotal = instrumentation.SagaFaultTotal;
            var _sagaInProgress = instrumentation.SagaInProgress;
            var _sagaDuration = instrumentation.SagaDuration;

            var messageTypeLabel = GetMessageTypeLabel<T>();
            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.ReceiveContext.InputAddress) },
                { _options.MessageTypeLabel, messageTypeLabel },
                { _options.ConsumerTypeLabel, GetConsumerTypeLabel<TSaga, T>(messageTypeLabel) }
            };

            AddCustomTags(ref tagList, context);

            _sagaTotal.Add(1, tagList);
            _sagaInProgress.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _sagaFaultTotal.Add(1, tagList);
            }, () =>
            {
                _sagaInProgress.Add(-1, tagList);
                _sagaDuration.Record(context.ReceiveContext.ElapsedTime.TotalMilliseconds, tagList);
            });
        }

        public static StartedInstrument? StartSagaStateMachineInstrument<TSaga, T>(this ILogContext logContext, BehaviorContext<TSaga, T> context)
            where T : class
            where TSaga : class, SagaStateMachineInstance
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.SagaTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _sagaTotal = instrumentation.SagaTotal;
            var _sagaFaultTotal = instrumentation.SagaFaultTotal;
            var _sagaInProgress = instrumentation.SagaInProgress;
            var _sagaDuration = instrumentation.SagaDuration;

            var messageTypeLabel = GetMessageTypeLabel<T>();
            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.ReceiveContext.InputAddress) },
                { _options.MessageTypeLabel, messageTypeLabel },
                { _options.ConsumerTypeLabel, GetConsumerTypeLabel<TSaga, T>(messageTypeLabel) }
            };

            AddCustomTags(ref tagList, context);

            _sagaTotal.Add(1, tagList);
            _sagaInProgress.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _sagaFaultTotal.Add(1, tagList);
            }, () =>
            {
                _sagaInProgress.Add(-1, tagList);
                _sagaDuration.Record(context.ReceiveContext.ElapsedTime.TotalMilliseconds, tagList);
            });
        }

        public static StartedInstrument? StartConsumeInstrument<TConsumer, T>(this ILogContext logContext, ConsumeContext<T> context, Stopwatch timer)
            where T : class
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.ConsumeTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _consumeTotal = instrumentation.ConsumeTotal;
            var _consumeFaultTotal = instrumentation.ConsumeFaultTotal;
            var _consumeRetryTotal = instrumentation.ConsumeRetryTotal;
            var _consumerInProgress = instrumentation.ConsumerInProgress;
            var _consumeDuration = instrumentation.ConsumeDuration;
            var _deliveryDuration = instrumentation.DeliveryDuration;

            var messageTypeLabel = GetMessageTypeLabel<T>();
            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.ReceiveContext.InputAddress) },
                { _options.MessageTypeLabel, messageTypeLabel },
                { _options.ConsumerTypeLabel, GetConsumerTypeLabel<TConsumer, T>(messageTypeLabel) }
            };

            AddCustomTags(ref tagList, context);

            _consumeTotal.Add(1, tagList);
            _consumerInProgress.Add(1, tagList);

            var retryAttempt = context.GetRetryAttempt();
            if (retryAttempt > 0)
                _consumeRetryTotal.Add(1, tagList);

            if (context.SentTime.HasValue)
            {
                var deliveryDuration = DateTime.UtcNow - context.SentTime.Value;
                if (deliveryDuration < TimeSpan.Zero)
                    deliveryDuration = TimeSpan.Zero;

                _deliveryDuration.Record(deliveryDuration.TotalMilliseconds, tagList);
            }

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _consumeFaultTotal.Add(1, tagList);
            }, () =>
            {
                _consumerInProgress.Add(-1, tagList);
                _consumeDuration.Record(timer.ElapsedMilliseconds, tagList);
            });
        }

        public static StartedInstrument? StartActivityExecuteInstrument<TActivity, TArguments>(this ILogContext logContext,
            ConsumeContext<RoutingSlip> context, Stopwatch timer)
            where TActivity : class, IExecuteActivity<TArguments>
            where TArguments : class
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.ExecuteTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _executeTotal = instrumentation.ExecuteTotal;
            var _executeFaultTotal = instrumentation.ExecuteFaultTotal;
            var _executeInProgress = instrumentation.ExecuteInProgress;
            var _executeDuration = instrumentation.ExecuteDuration;

            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.ReceiveContext.InputAddress) },
                { _options.ActivityNameLabel, GetActivityTypeLabel<TActivity>() },
                { _options.ArgumentTypeLabel, GetArgumentTypeLabel<TArguments>() }
            };

            AddCustomTags(ref tagList, context);

            _executeTotal.Add(1, tagList);
            _executeInProgress.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _executeFaultTotal.Add(1, tagList);
            }, () =>
            {
                _executeInProgress.Add(-1, tagList);
                _executeDuration.Record(timer.ElapsedMilliseconds, tagList);
            });
        }

        public static StartedInstrument? StartActivityCompensateInstrument<TActivity, TLog>(this ILogContext logContext,
            ConsumeContext<RoutingSlip> context, Stopwatch timer)
            where TActivity : class, ICompensateActivity<TLog>
            where TLog : class
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.CompensateTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _compensateTotal = instrumentation.CompensateTotal;
            var _compensateFaultTotal = instrumentation.CompensateFaultTotal;
            var _compensateInProgress = instrumentation.CompensateInProgress;
            var _compensateDuration = instrumentation.CompensateDuration;

            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.ReceiveContext.InputAddress) },
                { _options.ActivityNameLabel, GetActivityTypeLabel<TActivity>() },
                { _options.LogTypeLabel, GetLogTypeLabel<TLog>() }
            };

            AddCustomTags(ref tagList, context);

            _compensateTotal.Add(1, tagList);
            _compensateInProgress.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _compensateFaultTotal.Add(1, tagList);
            }, () =>
            {
                _compensateInProgress.Add(-1, tagList);
                _compensateDuration.Record(timer.ElapsedMilliseconds, tagList);
            });
        }

        public static StartedInstrument? StartSendInstrument<T>(this ILogContext logContext, SendTransportContext transportContext, SendContext<T> context)
            where T : class
        {
            var instrumentation = GetInstrumentation(transportContext.LogContext);
            if (instrumentation == null || !instrumentation.SendTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _sendTotal = instrumentation.SendTotal;
            var _sendFaultTotal = instrumentation.SendFaultTotal;

            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.DestinationAddress) },
                { _options.MessageTypeLabel, GetMessageTypeLabel<T>() }
            };

            AddCustomTags(ref tagList, context);

            _sendTotal.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _sendFaultTotal.Add(1, tagList);
            });
        }

        public static StartedInstrument? StartOutboxSendInstrument<T>(this ILogContext logContext, SendContext<T> context)
            where T : class
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.OutboxSendTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _outboxSendTotal = instrumentation.OutboxSendTotal;
            var _outboxSendFaultTotal = instrumentation.OutboxSendFaultTotal;

            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.DestinationAddress) },
                { _options.MessageTypeLabel, GetMessageTypeLabel<T>() }
            };

            AddCustomTags(ref tagList, context);

            _outboxSendTotal.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _outboxSendFaultTotal.Add(1, tagList);
            });
        }

        public static StartedInstrument? StartOutboxDeliveryInstrument(this ILogContext logContext, OutboxMessageContext context)
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.OutboxDeliveryTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _outboxDeliveryTotal = instrumentation.OutboxDeliveryTotal;
            var _outboxDeliveryFaultTotal = instrumentation.OutboxDeliveryFaultTotal;

            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.DestinationAddress) }
            };

            _outboxDeliveryTotal.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _outboxDeliveryFaultTotal.Add(1, tagList);
            });
        }

        public static StartedInstrument? StartOutboxDeliveryInstrument(this ILogContext logContext,
            OutboxConsumeContext consumeContext, OutboxMessageContext context)
        {
            var instrumentation = GetInstrumentation(logContext);
            if (instrumentation == null || !instrumentation.OutboxDeliveryTotal.Enabled)
                return null;

            var _options = instrumentation.Options;
            var _outboxDeliveryTotal = instrumentation.OutboxDeliveryTotal;
            var _outboxDeliveryFaultTotal = instrumentation.OutboxDeliveryFaultTotal;

            var tagList = new TagList
            {
                { _options.ServiceNameLabel, _options.ServiceName },
                { _options.EndpointLabel, GetEndpointLabel(context.DestinationAddress) }
            };

            AddCustomTags(ref tagList, consumeContext);

            _outboxDeliveryTotal.Add(1, tagList);

            return new StartedInstrument(exception =>
            {
                tagList.Add(_options.ExceptionTypeLabel, exception.GetType().Name);
                _outboxDeliveryFaultTotal.Add(1, tagList);
            });
        }

        public static void TryConfigure(IServiceProvider provider)
        {
            var instrumentationOptions = provider.GetRequiredService<IOptions<InstrumentationOptions>>().Value;
            var meterFactory = provider.GetService<IMeterFactory>();
            if (meterFactory == null)
            {
                // The dependency injection path uses its own meter factory scope only. It never falls back to the
                // state of the explicit non dependency injection path.
                return;
            }

            var lazyState = _meterFactoryStates.GetValue(meterFactory, key =>
                new Lazy<LogContextInstrumentationState>(() =>
                {
                    var meter = key.Create(new MeterOptions(InstrumentationOptions.MeterName)
                    {
                        Version = HostMetadataCache.Host.ViciOneServiceBusVersion
                    });

                    ILoggerFactory loggerFactory = provider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;

                    return new LogContextInstrumentationState(meter, instrumentationOptions, new BusLogContext(loggerFactory));
                }, LazyThreadSafetyMode.ExecutionAndPublication));

            LogContextInstrumentationState instrumentation = lazyState.Value;

            BindInstrumentation(instrumentation.RootLogContext, instrumentation);

            // Activates the scope of this provider. The root context belongs to this scope, so no instance another
            // provider already uses is rebound here.
            LogContext.Current = instrumentation.RootLogContext;
        }

        public static void TryConfigure(InstrumentationOptions options)
        {
            if (Volatile.Read(ref _fallbackState) == null)
            {
                lock (_fallbackLock)
                {
                    if (_fallbackState == null)
                    {
                        var meter = new Meter(InstrumentationOptions.MeterName, HostMetadataCache.Host.ViciOneServiceBusVersion);

                        ILogContext root = LogContext.Current ?? new BusLogContext(NullLoggerFactory.Instance);

                        Volatile.Write(ref _fallbackState, new LogContextInstrumentationState(meter, options, root));
                    }
                }
            }

            LogContextInstrumentationState fallback = Volatile.Read(ref _fallbackState);

            // The explicit non dependency injection path binds its state to its own current log context. That is the
            // only way this state is ever reached; nothing resolves to it implicitly.
            ILogContext current = LogContext.Current;
            if (current == null)
            {
                // Nothing is current yet, so the root context of this state becomes the current one. Otherwise the
                // bus would create an unbound context of its own while building and this state would be unreachable.
                current = fallback.RootLogContext;

                LogContext.Current = current;
            }

            BindInstrumentation(current, fallback);
        }

        internal static void CopyInstrumentation(ILogContext source, ILogContext destination)
        {
            if (source != null && destination != null && _logContextStates.TryGetValue(source, out var instrumentation))
                BindInstrumentation(destination, instrumentation);
        }

        static void BindInstrumentation(ILogContext logContext, LogContextInstrumentationState instrumentation)
        {
            if (logContext == null || instrumentation == null)
                return;

            lock (_bindingLock)
            {
                _logContextStates.Remove(logContext);
                _logContextStates.Add(logContext, instrumentation);
            }
        }

        /// <summary>
        /// Only the state bound to the requested context. A context that carries no binding records nothing; it does
        /// not implicitly reach the state of the explicit non dependency injection path.
        /// </summary>
        static LogContextInstrumentationState GetInstrumentation(ILogContext logContext)
        {
            if (logContext != null && _logContextStates.TryGetValue(logContext, out var instrumentation))
                return instrumentation;

            return null;
        }

        static void AddCustomTags(ref TagList tags, PipeContext pipeContext)
        {
            if (pipeContext.TryGetPayload<MetricsContext>(out var metricsContext))
                metricsContext.Populate(ref tags);
        }

        static string GetConsumerTypeLabel<TConsumer, TMessage>(string messageLabel)
        {
            return _labelCache.GetOrAdd(TypeCache<TConsumer>.ShortName, type =>
            {
                if (type.StartsWith("ViciOne.ServiceBus.MessageHandler<"))
                    return "Handler";

                var genericMessageType = "<" + TypeCache<TMessage>.ShortName + ">";
                if (type.IndexOf(genericMessageType, StringComparison.Ordinal) >= 0)
                    type = type.Replace(genericMessageType, "_" + messageLabel);

                return CleanupLabel(type);
            });
        }

        static string CleanupLabel(string label)
        {
            string SimpleClean(string text)
            {
                return text.Split('.', '+').Last();
            }

            var indexOf = label.IndexOfAny(_delimiters);
            if (indexOf >= 0)
            {
                if (label[indexOf] == '<')
                    return SimpleClean(label.Substring(0, indexOf)) + "_" + CleanupLabel(label.Substring(indexOf + 1));

                if (label[indexOf] == '>')
                    return SimpleClean(label.Substring(0, indexOf)) + CleanupLabel(label.Substring(indexOf + 1));

                return SimpleClean(label);
            }

            return SimpleClean(label);
        }

        static string GetArgumentTypeLabel<TArguments>()
        {
            return _labelCache.GetOrAdd(TypeCache<TArguments>.ShortName, type => FormatTypeName(new StringBuilder(), typeof(TArguments))
                .Replace("Arguments", ""));
        }

        static string GetLogTypeLabel<TLog>()
        {
            return _labelCache.GetOrAdd(TypeCache<TLog>.ShortName, type => FormatTypeName(new StringBuilder(), typeof(TLog)).Replace("Log", ""));
        }

        static string GetActivityTypeLabel<TActivity>()
        {
            return _labelCache.GetOrAdd(TypeCache<TActivity>.ShortName, type => FormatTypeName(new StringBuilder(), typeof(TActivity)).Replace("Activity", ""));
        }

        static string GetEndpointLabel(Uri inputAddress)
        {
            return inputAddress?.AbsolutePath.Split('/').LastOrDefault()?.Replace(".", "_").Replace("/", "_");
        }

        static string GetMessageTypeLabel<TMessage>()
        {
            return _labelCache.GetOrAdd(TypeCache<TMessage>.ShortName, type => FormatTypeName(new StringBuilder(), typeof(TMessage)));
        }

        static string FormatTypeName(StringBuilder sb, Type type)
        {
            if (type.IsGenericParameter)
                return "";

            if (type.IsGenericType)
            {
                var name = type.GetGenericTypeDefinition().Name;

                //remove `1
                var index = name.IndexOf('`');
                if (index > 0)
                    name = name.Remove(index);

                sb.Append(name);
                sb.Append('_');
                Type[] arguments = type.GenericTypeArguments;
                for (var i = 0; i < arguments.Length; i++)
                {
                    if (i > 0)
                        sb.Append('_');

                    FormatTypeName(sb, arguments[i]);
                }
            }
            else
                sb.Append(type.Name);

            return sb.ToString();
        }
    }
}
