namespace ViciOne.ServiceBus.Tests.SagaStateMachineTests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Internals;
    using TestFramework;


    [TestFixture]
    public class When_a_message_is_not_correlated :
        InMemoryTestFixture
    {
        [Test]
        public async Task Should_retry_the_status_message()
        {
            // Its own service name. The saga repository belongs to the fixture, so a name another case also starts
            // would leave a running instance behind and turn that case into a saga fault instead of a start.
            Task<Response<Status>> statusTask =
                Bus.Request<CheckStatus, Status>(InputQueueAddress, new CheckStatus("C"), TestCancellationToken);

            // The status message has to arrive before the saga exists, otherwise the case never reaches the retry it
            // is named after. Waiting for the first retry states that ordering as an event of the product instead of
            // hoping for it or sleeping past it.
            await _retries.FirstRetry.OrTimeout(s: 30);

            await InputQueueSendEndpoint.Send(new Start("C", Guid.NewGuid()));

            Response<Status> status = await statusTask;

            Assert.Multiple(() =>
            {
                Assert.That(status.Message.ServiceName, Is.EqualTo("C"));
                Assert.That(_retries.RetryCount, Is.GreaterThan(0),
                    "The status message must have been retried, not answered on its first delivery");
            });
        }

        [Test]
        public async Task Should_start_and_handle_the_status_request()
        {
            Response<StartupComplete> startupComplete =
                await Bus.Request<Start, StartupComplete>(InputQueueAddress, new Start("A", Guid.NewGuid()), TestCancellationToken);

            Response<Status> status = await Bus.Request<CheckStatus, Status>(InputQueueAddress, new CheckStatus("A"), TestCancellationToken);

            Assert.That(status.Message.ServiceName, Is.EqualTo("A"));
        }

        [Test]
        public async Task Should_start_and_handle_the_status_request_awaited()
        {
            Response<StartupComplete> startupComplete =
                await Bus.Request<Start, StartupComplete>(InputQueueAddress, new Start("B", Guid.NewGuid()), TestCancellationToken);

            Response<Status> status = await Bus.Request<CheckStatus, Status>(InputQueueAddress, new CheckStatus("B"), TestCancellationToken);

            Assert.That(status.Message.ServiceName, Is.EqualTo("B"));
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _machine = new TestStateMachine();
            _repository = new InMemorySagaRepository<Instance>();

            configurator.UseMessageRetry(r =>
            {
                r.Interval(20, TimeSpan.FromMilliseconds(100));
                r.ConnectRetryObserver(_retries);
            });

            configurator.StateMachineSaga(_machine, _repository);
        }

        TestStateMachine _machine;
        InMemorySagaRepository<Instance> _repository;
        readonly RetryRecordingObserver _retries = new RetryRecordingObserver();


        /// <summary>
        /// Signals the first retry of a status message that found no saga instance. That retry is exactly what this
        /// case is named after, so it is both the ordering signal and an assertion.
        /// </summary>
        class RetryRecordingObserver :
            IRetryObserver
        {
            readonly TaskCompletionSource<bool> _first = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            int _retries;

            public Task FirstRetry => _first.Task;
            public int RetryCount => Volatile.Read(ref _retries);

            public Task PostCreate<T>(RetryPolicyContext<T> context)
                where T : class, PipeContext
            {
                return Task.CompletedTask;
            }

            public Task PostFault<T>(RetryContext<T> context)
                where T : class, PipeContext
            {
                return Task.CompletedTask;
            }

            public Task PreRetry<T>(RetryContext<T> context)
                where T : class, PipeContext
            {
                Interlocked.Increment(ref _retries);
                _first.TrySetResult(true);

                return Task.CompletedTask;
            }

            public Task RetryFault<T>(RetryContext<T> context)
                where T : class, PipeContext
            {
                return Task.CompletedTask;
            }

            public Task RetryComplete<T>(RetryContext<T> context)
                where T : class, PipeContext
            {
                return Task.CompletedTask;
            }
        }


        class Instance :
            SagaStateMachineInstance
        {
            public Instance(Guid correlationId)
            {
                CorrelationId = correlationId;
            }

            protected Instance()
            {
            }

            public State CurrentState { get; set; }
            public string ServiceName { get; set; }
            public Guid CorrelationId { get; set; }
        }


        class TestStateMachine :
            ViciOneServiceBusStateMachine<Instance>
        {
            public TestStateMachine()
            {
                InstanceState(x => x.CurrentState);

                Event(() => Started, x => x
                    .CorrelateBy(instance => instance.ServiceName, context => context.Message.ServiceName)
                    .SelectId(context => context.Message.ServiceId));

                Event(() => CheckStatus, x => x
                    .CorrelateBy(instance => instance.ServiceName, context => context.Message.ServiceName)
                    .OnMissingInstance(m => m.Fault()));

                Initially(
                    When(Started)
                        .Then(context => context.Instance.ServiceName = context.Data.ServiceName)
                        .Respond(context => new StartupComplete
                        {
                            ServiceId = context.Instance.CorrelationId,
                            ServiceName = context.Instance.ServiceName
                        })
                        .Then(context => Console.WriteLine("Started: {0} - {1}", context.Instance.CorrelationId, context.Instance.ServiceName))
                        .TransitionTo(Running));

                During(Running,
                    When(CheckStatus)
                        .Then(context => Console.WriteLine("Status check!"))
                        .Respond(context => new Status("Running", context.Instance.ServiceName)));
            }

            public State Running { get; private set; }
            public Event<Start> Started { get; private set; }
            public Event<CheckStatus> CheckStatus { get; private set; }
        }


        class Status
        {
            public Status()
            {
            }

            public Status(string status, string serviceName)
            {
                StatusDescription = status;
                ServiceName = serviceName;
            }

            public string ServiceName { get; set; }
            public string StatusDescription { get; set; }
        }


        class CheckStatus
        {
            public CheckStatus(string serviceName)
            {
                ServiceName = serviceName;
            }

            public CheckStatus()
            {
            }

            public string ServiceName { get; set; }
        }


        class Start
        {
            public Start(string serviceName, Guid serviceId)
            {
                ServiceName = serviceName;
                ServiceId = serviceId;
            }

            public Start()
            {
            }

            public string ServiceName { get; set; }
            public Guid ServiceId { get; set; }
        }


        class StartupComplete
        {
            public Guid ServiceId { get; set; }
            public string ServiceName { get; set; }
        }
    }
}
