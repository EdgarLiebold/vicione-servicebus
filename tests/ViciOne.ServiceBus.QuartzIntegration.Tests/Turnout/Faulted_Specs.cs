// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.QuartzIntegration.Tests.Turnout
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts.JobService;
    using NUnit.Framework;
    using Scheduling;
    using TestFramework;


    public interface GrindTheGears
    {
        TimeSpan Duration { get; }
    }


    [TestFixture]
    [Category("Flaky")]
    public class Submitting_a_job_to_turnout_that_faults :
        QuartzInMemoryTestFixture
    {
        [Test]
        [Order(1)]
        public async Task Should_get_the_job_accepted()
        {
            IRequestClient<SubmitJob<GrindTheGears>> requestClient = Bus.CreateRequestClient<SubmitJob<GrindTheGears>>();

            Response<JobSubmissionAccepted> response = await requestClient.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = _jobId,
                Job = new { Duration = TimeSpan.FromSeconds(1) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(_jobId));

            // just to capture all the test output in a single window
            ConsumeContext<JobFaulted> faulted = await _faulted;
        }

        [Test]
        [Order(4)]
        public async Task Should_have_published_the_job_faulted_event()
        {
            ConsumeContext<JobFaulted> faulted = await _faulted;
        }

        [Test]
        [Order(3)]
        public async Task Should_have_published_the_job_started_event()
        {
            ConsumeContext<JobStarted> started = await _started;
        }

        [Test]
        [Order(2)]
        public async Task Should_have_published_the_job_submitted_event()
        {
            ConsumeContext<JobSubmitted> submitted = await _submitted;
        }

        Guid _jobId;
        Task<ConsumeContext<JobFaulted>> _faulted;
        Task<ConsumeContext<JobSubmitted>> _submitted;
        Task<ConsumeContext<JobStarted>> _started;

        [OneTimeSetUp]
        public async Task Arrange()
        {
            _jobId = NewId.NextGuid();
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            base.ConfigureInMemoryBus(configurator);

            var options = new ServiceInstanceOptions()
                .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);

            configurator.ServiceInstance(options, instance =>
            {
                instance.ConfigureJobServiceEndpoints();

                instance.ReceiveEndpoint(instance.EndpointNameFormatter.Message<GrindTheGears>(), e =>
                {
                    e.Consumer(() => new GrindTheGearsConsumer(), cfg =>
                    {
                        cfg.Options<JobOptions<GrindTheGears>>(jobOptions => jobOptions.SetJobTimeout(TimeSpan.FromSeconds(90)));
                    });
                });
            });
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _submitted = Handled<JobSubmitted>(configurator, context => context.Message.JobId == _jobId);
            _started = Handled<JobStarted>(configurator, context => context.Message.JobId == _jobId);
            _faulted = Handled<JobFaulted>(configurator, context => context.Message.JobId == _jobId);
        }


        class GrindTheGearsConsumer :
            IJobConsumer<GrindTheGears>
        {
            public async Task Run(JobContext<GrindTheGears> context)
            {
                await Task.Delay(context.Job.Duration);

                throw new IntentionalTestException("Grinding gears, dropped the transmission");
            }
        }
    }


    [TestFixture]
    [Category("Flaky")]
    public class Submitting_a_job_to_turnout_that_faults_with_retry :
        QuartzInMemoryTestFixture
    {
        [Test]
        [Order(1)]
        public async Task Should_get_the_job_accepted()
        {
            IRequestClient<SubmitJob<GrindTheGears>> requestClient = Bus.CreateRequestClient<SubmitJob<GrindTheGears>>();

            Response<JobSubmissionAccepted> response = await requestClient.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = _jobId,
                Job = new { Duration = TimeSpan.FromSeconds(1) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(_jobId));

            ConsumeContext<JobCompleted> completed = await _completed;
        }

        [Test]
        [Order(4)]
        public async Task Should_have_published_the_job_completed_event()
        {
            ConsumeContext<JobCompleted> completed = await _completed;
        }

        [Test]
        [Order(3)]
        public async Task Should_have_published_the_job_started_event()
        {
            ConsumeContext<JobStarted> started = await _started;
        }

        [Test]
        [Order(2)]
        public async Task Should_have_published_the_job_submitted_event()
        {
            ConsumeContext<JobSubmitted> submitted = await _submitted;
        }

        [Test]
        [Order(5)]
        public async Task Should_not_have_published_the_job_faulted_event()
        {
            Assert.That(_faulted.Status, Is.EqualTo(TaskStatus.WaitingForActivation));
        }

        Guid _jobId;
        Task<ConsumeContext<JobFaulted>> _faulted;
        Task<ConsumeContext<JobSubmitted>> _submitted;
        Task<ConsumeContext<JobStarted>> _started;
        Task<ConsumeContext<JobCompleted>> _completed;

        [OneTimeSetUp]
        public async Task Arrange()
        {
            _jobId = NewId.NextGuid();
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            base.ConfigureInMemoryBus(configurator);

            var options = new ServiceInstanceOptions()
                .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);

            configurator.ServiceInstance(options, instance =>
            {
                instance.ConfigureJobServiceEndpoints();

                instance.ReceiveEndpoint(instance.EndpointNameFormatter.Message<GrindTheGears>(), e =>
                {
                    e.Consumer(() => new GrindTheGearsConsumer(), cfg =>
                    {
                        cfg.Options<JobOptions<GrindTheGears>>(jobOptions => jobOptions
                            .SetJobTimeout(TimeSpan.FromSeconds(90))
                            .SetRetry(r => r.Interval(1, 2000)));
                    });
                });
            });
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _submitted = Handled<JobSubmitted>(configurator, context => context.Message.JobId == _jobId);
            _started = Handled<JobStarted>(configurator, context => context.Message.JobId == _jobId);
            _completed = Handled<JobCompleted>(configurator, context => context.Message.JobId == _jobId);
            _faulted = Handled<JobFaulted>(configurator, context => context.Message.JobId == _jobId);
        }


        class GrindTheGearsConsumer :
            IJobConsumer<GrindTheGears>
        {
            public async Task Run(JobContext<GrindTheGears> context)
            {
                await Task.Delay(context.Job.Duration);

                if (context.RetryAttempt == 0)
                    throw new IntentionalTestException("Grinding gears, dropped the transmission");
            }
        }
    }


    [TestFixture]
    [Category("Flaky")]
    public class Submitting_a_job_to_turnout_that_is_abandoned :
        QuartzInMemoryTestFixture
    {
        [Test]
        [Order(1)]
        public async Task Should_get_the_job_accepted()
        {
            IRequestClient<SubmitJob<GrindTheGears>> requestClient = Bus.CreateRequestClient<SubmitJob<GrindTheGears>>();

            Response<JobSubmissionAccepted> response = await requestClient.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = _jobId,
                Job = new { Duration = TimeSpan.FromSeconds(1) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(_jobId));

            await InMemoryTestHarness.Consumed.Any<ScheduleMessage>();

            await Task.Delay(TimeSpan.FromSeconds(2));

            await AdvanceTime(TimeSpan.FromSeconds(60));

            await InMemoryTestHarness.Sent.Any<GetJobAttemptStatus>();

            await InMemoryTestHarness.Consumed.Any<GetJobAttemptStatus>();

            await Task.Delay(TimeSpan.FromSeconds(2));

            await AdvanceTime(TimeSpan.FromSeconds(60));


            // just to capture all the test output in a single window
            ConsumeContext<JobFaulted> faulted = await _faulted;
        }

        [Test]
        [Order(4)]
        public async Task Should_have_published_the_job_faulted_event()
        {
            ConsumeContext<JobFaulted> faulted = await _faulted;
        }

        [Test]
        [Order(3)]
        public async Task Should_have_published_the_job_started_event()
        {
            ConsumeContext<JobStarted> started = await _started;
        }

        [Test]
        [Order(2)]
        public async Task Should_have_published_the_job_submitted_event()
        {
            ConsumeContext<JobSubmitted> submitted = await _submitted;
        }

        Guid _jobId;
        Task<ConsumeContext<JobFaulted>> _faulted;
        Task<ConsumeContext<JobSubmitted>> _submitted;
        Task<ConsumeContext<JobStarted>> _started;

        [OneTimeSetUp]
        public async Task Arrange()
        {
            _jobId = NewId.NextGuid();
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            base.ConfigureInMemoryBus(configurator);

            var options = new ServiceInstanceOptions()
                .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);

            configurator.ServiceInstance(options, instance =>
            {
                instance.ConfigureJobServiceEndpoints(x =>
                {
                    x.SuspectJobRetryCount = 0;
                });

                instance.ReceiveEndpoint(instance.EndpointNameFormatter.Message<GrindTheGears>(), e =>
                {
                    e.Consumer(() => new GrindTheGearsConsumer(), cfg =>
                    {
                        cfg.Options<JobOptions<GrindTheGears>>(jobOptions => jobOptions.SetJobTimeout(TimeSpan.FromSeconds(90)));
                    });
                });
            });
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _submitted = Handled<JobSubmitted>(configurator, context => context.Message.JobId == _jobId);
            _started = Handled<JobStarted>(configurator, context => context.Message.JobId == _jobId);
            _faulted = Handled<JobFaulted>(configurator, context => context.Message.JobId == _jobId);
        }


        class GrindTheGearsConsumer :
            IJobConsumer<GrindTheGears>
        {
            public async Task Run(JobContext<GrindTheGears> context)
            {
                await Task.Delay(context.Job.Duration);

                throw new OperationCanceledException();
            }
        }
    }


    [TestFixture]
    [Category("Flaky")]
    public class Submitting_a_job_to_turnout_that_is_abandoned_and_retried :
        QuartzInMemoryTestFixture
    {
        const int SuspectJobRetryCount = 1;

        [Test]
        [Order(1)]
        public async Task Should_get_the_job_accepted()
        {
            IRequestClient<SubmitJob<GrindTheGears>> requestClient = Bus.CreateRequestClient<SubmitJob<GrindTheGears>>();

            Response<JobSubmissionAccepted> response = await requestClient.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = _jobId,
                Job = new { Duration = TimeSpan.FromSeconds(1) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(_jobId));

            await InMemoryTestHarness.Consumed.Any<ScheduleMessage>();

            // The worker starts the attempt and then goes silent: the fault it reports never reaches the sagas.
            // Every wait below is a barrier on an observed message, never a sleep and never a wall clock.
            SuppressedFault suppressed = await AwaitResult("suppression of the reported fault", _silentWorker.ReportedFaultSuppressed);

            // The instance no longer knows the finished job, so every status check stays unanswered and the
            // attempt saga escalates Running -> CheckingStatus -> Suspect on its own schedule. One controlled
            // step of the scheduler clock is enough and is also the only safe amount: the status check is
            // rescheduled against real time, so every following check is already due once the clock moved.
            await AdvanceTime(TimeSpan.FromSeconds(60));

            await AwaitStep("first status check", _silentWorker.StatusCheckObserved(1));
            await AwaitStep("second status check", _silentWorker.StatusCheckObserved(2));

            ObservedFault suspectFault = await AwaitResult("suspect fault", _silentWorker.SuspectFaultObserved);

            // The fault is still held in the filter, so the clock can be put back without racing the retry.
            await AdvanceTime(-TimeSpan.FromSeconds(60));
            _silentWorker.ReleaseSuspectFault();

            ConsumeContext<JobCompleted> completed = await AwaitResult("job completion", _completed);

            Assert.Multiple(() =>
            {
                Assert.That(completed.Message.JobId, Is.EqualTo(_jobId));

                Assert.That(suppressed.AttemptId, Is.EqualTo(_silentWorker.StartedAttemptId),
                    "The suppressed fault has to belong to the attempt the worker actually started");
                Assert.That(suppressed.RetryAttempt, Is.EqualTo(0));
                Assert.That(suppressed.Endpoints, Is.EquivalentTo(new[] { "job", "job-attempt" }),
                    "Exactly the two saga endpoints of the one logical publish are suppressed");
                Assert.That(_silentWorker.SuppressionCount, Is.EqualTo(2),
                    "One logical communication loss is exactly the two deliveries of the same message");
                Assert.That(_silentWorker.RedeliveryCount, Is.Zero,
                    "The suppressed message must not be redelivered or retried");
                Assert.That(_silentWorker.FaultOfReportedFaultCount, Is.Zero,
                    "Suppression must not produce a Fault<JobAttemptFaulted>");

                Assert.That(suspectFault.AttemptId, Is.EqualTo(suppressed.AttemptId),
                    "The suspect fault has to belong to the very attempt whose report was lost");
                Assert.That(suspectFault.RetryDelay, Is.Not.Null,
                    "Only a suspect fault carrying a retry delay makes the job service retry the attempt");
                Assert.That(suspectFault.MessageId, Is.Not.EqualTo(suppressed.MessageId),
                    "The suspect fault is a new message and was never suppressed");
            });

            async Task StepCore(string what, Task step)
            {
                try
                {
                    await step.WaitAsync(TestCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Assert.Fail($"The {what} never happened: {_silentWorker.Diagnostics}");
                    throw;
                }
            }

            Task AwaitStep(string what, Task step)
            {
                return StepCore(what, step);
            }

            async Task<TResult> AwaitResult<TResult>(string what, Task<TResult> step)
            {
                await StepCore(what, step);

                return await step;
            }
        }

        [Test]
        [Order(4)]
        public async Task Should_have_published_the_job_completed_event()
        {
            ConsumeContext<JobCompleted> completed = await _completed;
        }

        [Test]
        [Order(3)]
        public async Task Should_have_published_the_job_started_event()
        {
            ConsumeContext<JobStarted> started = await _started;
        }

        [Test]
        [Order(2)]
        public async Task Should_have_published_the_job_submitted_event()
        {
            ConsumeContext<JobSubmitted> submitted = await _submitted;
        }

        [Test]
        [Order(5)]
        public async Task Should_have_completed_on_the_retry_of_the_suspect_attempt()
        {
            await _completed;

            Assert.That(_silentWorker.HighestRetryAttempt, Is.EqualTo(1),
                "The job has to complete on the retry attempt that follows the suspect fault");
        }

        [Test]
        [Order(6)]
        public async Task Should_not_have_published_the_job_faulted_event()
        {
            await _completed;

            Assert.That(_faulted.Status, Is.EqualTo(TaskStatus.WaitingForActivation),
                "A suspect attempt that is retried successfully must not fault the job");
        }

        Guid _jobId;
        SilentWorkerReceiveSuppression _silentWorker;
        Task<ConsumeContext<JobFaulted>> _faulted;
        Task<ConsumeContext<JobSubmitted>> _submitted;
        Task<ConsumeContext<JobStarted>> _started;
        Task<ConsumeContext<JobCompleted>> _completed;

        [OneTimeSetUp]
        public async Task Arrange()
        {
            _jobId = NewId.NextGuid();
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            base.ConfigureInMemoryBus(configurator);

            _silentWorker = new SilentWorkerReceiveSuppression(() => _jobId);

            // The one injected disturbance of this fixture, installed test locally in front of the saga
            // consumers. It loses the single fault the worker reports for its first attempt, which is exactly
            // how a worker that stops communicating after the start looks to the job service. The send side is
            // deliberately not used: a discarded send is still delivered, which was measured and recorded.
            configurator.UseFilter(_silentWorker.JobAttemptFaultedFilter);
            configurator.UseFilter(_silentWorker.GetJobAttemptStatusObserver);
            configurator.UseFilter(_silentWorker.FaultObserver);

            var options = new ServiceInstanceOptions()
                .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);

            configurator.ServiceInstance(options, instance =>
            {
                instance.ConfigureJobServiceEndpoints(x =>
                {
                    x.SuspectJobRetryCount = SuspectJobRetryCount;
                    x.SuspectJobRetryDelay = TimeSpan.FromSeconds(1);
                });

                instance.ReceiveEndpoint(instance.EndpointNameFormatter.Message<GrindTheGears>(), e =>
                {
                    e.Consumer(() => new GrindTheGearsConsumer(_silentWorker), cfg =>
                    {
                        cfg.Options<JobOptions<GrindTheGears>>(jobOptions => jobOptions.SetJobTimeout(TimeSpan.FromSeconds(90)));
                    });
                });
            });
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _submitted = Handled<JobSubmitted>(configurator, context => context.Message.JobId == _jobId);
            _started = Handled<JobStarted>(configurator, context => context.Message.JobId == _jobId);
            _completed = Handled<JobCompleted>(configurator, context => context.Message.JobId == _jobId);
            _faulted = Handled<JobFaulted>(configurator, context => context.Message.JobId == _jobId);
        }


        class GrindTheGearsConsumer :
            IJobConsumer<GrindTheGears>
        {
            readonly SilentWorkerReceiveSuppression _silentWorker;

            public GrindTheGearsConsumer(SilentWorkerReceiveSuppression silentWorker)
            {
                _silentWorker = silentWorker;
            }

            public async Task Run(JobContext<GrindTheGears> context)
            {
                _silentWorker.RecordAttempt(context.AttemptId, context.RetryAttempt);

                await Task.Delay(context.Job.Duration);

                if (context.RetryAttempt == 0)
                    throw new OperationCanceledException();
            }
        }
    }


    [TestFixture]
    [Category("Flaky")]
    public class Submitting_a_job_whose_previous_attempt_reports_late :
        QuartzInMemoryTestFixture
    {
        const int SuspectJobRetryCount = 1;

        [Test]
        [Order(1)]
        public async Task Should_ignore_a_completed_message_from_the_previous_attempt()
        {
            IRequestClient<SubmitJob<GrindTheGears>> requestClient = Bus.CreateRequestClient<SubmitJob<GrindTheGears>>();

            Response<JobSubmissionAccepted> response = await requestClient.GetResponse<JobSubmissionAccepted>(new
            {
                JobId = _jobId,
                Job = new { Duration = TimeSpan.FromSeconds(1) }
            });

            Assert.That(response.Message.JobId, Is.EqualTo(_jobId));

            await InMemoryTestHarness.Consumed.Any<ScheduleMessage>();

            SuppressedFault suppressed = await Await("suppression of the reported fault", _silentWorker.ReportedFaultSuppressed);

            await AdvanceTime(TimeSpan.FromSeconds(60));

            await AwaitVoid("first status check", _silentWorker.StatusCheckObserved(1));
            await AwaitVoid("second status check", _silentWorker.StatusCheckObserved(2));

            await Await("suspect fault", _silentWorker.SuspectFaultObserved);

            await AdvanceTime(-TimeSpan.FromSeconds(60));
            _silentWorker.ReleaseSuspectFault();

            // The retry is running. Only now the previous attempt reports, late and terminally, exactly as a
            // worker would that was considered lost. None of these messages belongs to the running attempt.
            Guid retryAttemptId = await Await("start of the retry attempt", _silentWorker.RetryAttemptStarted);

            Assert.That(retryAttemptId, Is.Not.EqualTo(suppressed.AttemptId));

            ISendEndpoint jobSaga = await Bus.GetSendEndpoint(new Uri("loopback://localhost/job"));

            await jobSaga.Send<JobAttemptCompleted>(new
            {
                JobId = _jobId,
                AttemptId = suppressed.AttemptId,
                RetryAttempt = 0,
                Timestamp = DateTime.UtcNow,
                Duration = TimeSpan.Zero
            });

            Assert.That(await InMemoryTestHarness.Consumed.Any<JobAttemptCompleted>(x =>
                    x.Exception == null
                    && x.Context.Message.JobId == _jobId
                    && x.Context.Message.AttemptId == suppressed.AttemptId,
                TestCancellationToken), Is.True, "The stale completion was not consumed successfully by the job saga");

            IRequestClient<GetJobState> stateClient = Bus.CreateRequestClient<GetJobState>();
            Response<JobState> state = await stateClient.GetResponse<JobState>(new { JobId = _jobId }, TestCancellationToken);

            Assert.Multiple(() =>
            {
                Assert.That(state.Message.CurrentState, Is.EqualTo("Started"),
                    "The stale completion ended the running retry attempt");
                Assert.That(state.Message.Completed, Is.Null,
                    "The stale completion wrote a completion timestamp for the running retry attempt");
                Assert.That(state.Message.LastRetryAttempt, Is.EqualTo(1));
                Assert.That(_completed.Status, Is.EqualTo(TaskStatus.WaitingForActivation),
                    "The stale completion published JobCompleted before the current retry completed");
            });

            _silentWorker.ReleaseRetryAttempt();

            ConsumeContext<JobCompleted> completed = await Await("job completion", _completed);

            Assert.Multiple(() =>
            {
                Assert.That(completed.Message.JobId, Is.EqualTo(_jobId));
                Assert.That(_faulted.Status, Is.EqualTo(TaskStatus.WaitingForActivation),
                    "A terminal message of the previous attempt must not fault the running job");
                Assert.That(_canceled.Status, Is.EqualTo(TaskStatus.WaitingForActivation),
                    "A terminal message of the previous attempt must not cancel the running job");
                Assert.That(_silentWorker.HighestRetryAttempt, Is.EqualTo(1),
                    "The job still has to complete on its retry attempt");
            });

            async Task AwaitCore(string what, Task step)
            {
                try
                {
                    await step.WaitAsync(TestCancellationToken);
                }
                catch (OperationCanceledException)
                {
                    Assert.Fail($"The {what} never happened: {_silentWorker.Diagnostics}");
                    throw;
                }
            }

            Task AwaitVoid(string what, Task step)
            {
                return AwaitCore(what, step);
            }

            async Task<TResult> Await<TResult>(string what, Task<TResult> step)
            {
                await AwaitCore(what, step);

                return await step;
            }
        }

        Guid _jobId;
        SilentWorkerReceiveSuppression _silentWorker;
        Task<ConsumeContext<JobCanceled>> _canceled;
        Task<ConsumeContext<JobCompleted>> _completed;
        Task<ConsumeContext<JobFaulted>> _faulted;

        [OneTimeSetUp]
        public async Task Arrange()
        {
            _jobId = NewId.NextGuid();
        }

        protected override void ConfigureInMemoryBus(IInMemoryBusFactoryConfigurator configurator)
        {
            base.ConfigureInMemoryBus(configurator);

            _silentWorker = new SilentWorkerReceiveSuppression(() => _jobId);

            configurator.UseFilter(_silentWorker.JobAttemptFaultedFilter);
            configurator.UseFilter(_silentWorker.GetJobAttemptStatusObserver);
            configurator.UseFilter(_silentWorker.FaultObserver);
            var options = new ServiceInstanceOptions()
                .SetEndpointNameFormatter(KebabCaseEndpointNameFormatter.Instance);

            configurator.ServiceInstance(options, instance =>
            {
                instance.ConfigureJobServiceEndpoints(x =>
                {
                    x.SuspectJobRetryCount = SuspectJobRetryCount;
                    x.SuspectJobRetryDelay = TimeSpan.FromSeconds(1);
                });

                instance.ReceiveEndpoint(instance.EndpointNameFormatter.Message<GrindTheGears>(), e =>
                {
                    e.Consumer(() => new GrindTheGearsConsumer(_silentWorker), cfg =>
                    {
                        cfg.Options<JobOptions<GrindTheGears>>(jobOptions => jobOptions.SetJobTimeout(TimeSpan.FromSeconds(90)));
                    });
                });
            });
        }

        protected override void ConfigureInMemoryReceiveEndpoint(IInMemoryReceiveEndpointConfigurator configurator)
        {
            _completed = Handled<JobCompleted>(configurator, context => context.Message.JobId == _jobId);
            _faulted = Handled<JobFaulted>(configurator, context => context.Message.JobId == _jobId);
            _canceled = Handled<JobCanceled>(configurator, context => context.Message.JobId == _jobId);
        }


        class GrindTheGearsConsumer :
            IJobConsumer<GrindTheGears>
        {
            readonly SilentWorkerReceiveSuppression _silentWorker;

            public GrindTheGearsConsumer(SilentWorkerReceiveSuppression silentWorker)
            {
                _silentWorker = silentWorker;
            }

            public async Task Run(JobContext<GrindTheGears> context)
            {
                _silentWorker.RecordAttempt(context.AttemptId, context.RetryAttempt);

                if (context.RetryAttempt == 0)
                {
                    await Task.Delay(context.Job.Duration);

                    throw new OperationCanceledException();
                }

                // The retry stays inside the consumer until the test has delivered the late messages of the
                // previous attempt, so they provably arrive while this attempt is still running.
                await _silentWorker.RetryAttemptGate;
            }
        }
    }


    /// <summary>
    /// The single reported fault of the first attempt, as it was suppressed on its way to the sagas.
    /// </summary>
    public class SuppressedFault
    {
        public Guid MessageId { get; init; }
        public Guid AttemptId { get; init; }
        public int RetryAttempt { get; init; }
        public string[] Endpoints { get; init; } = Array.Empty<string>();
    }


    /// <summary>
    /// A JobAttemptFaulted that was allowed to pass, with the transport identity it arrived under.
    /// </summary>
    public class ObservedFault
    {
        public Guid MessageId { get; init; }
        public Guid AttemptId { get; init; }
        public TimeSpan? RetryDelay { get; init; }
    }


    /// <summary>
    /// Test local receive suppression that turns the worker of one attempt silent.
    ///
    /// One logical publish of JobAttemptFaulted is delivered to both saga endpoints, so losing that one report
    /// means suppressing exactly those two deliveries of the same MessageId. Nothing else is touched: the later
    /// suspect fault carries a retry delay and passes, as does every other message.
    /// </summary>
    public class SilentWorkerReceiveSuppression
    {
        static readonly string[] SagaEndpoints = { "job", "job-attempt" };

        readonly Func<Guid> _jobId;
        readonly object _lock = new();
        readonly List<string> _suppressedEndpoints = new();
        readonly List<TaskCompletionSource<int>> _statusCheckWaiters = new();
        readonly TaskCompletionSource<SuppressedFault> _reportedFaultSuppressed =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<ObservedFault> _suspectFaultObserved =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<bool> _suspectFaultReleased =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<Guid> _retryAttemptStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<bool> _retryAttemptReleased =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        Guid? _suppressedMessageId;
        Guid? _suppressedAttemptId;
        Guid _startedAttemptId;
        int _faultObserverCount;
        int _highestRetryAttempt = -1;
        int _redeliveryCount;
        int _statusCheckCount;
        int _suppressionCount;

        public SilentWorkerReceiveSuppression(Func<Guid> jobId)
        {
            _jobId = jobId;
            JobAttemptFaultedFilter = new SuppressReportedFaultFilter(this);
            GetJobAttemptStatusObserver = new StatusCheckObserver(this);
            FaultObserver = new FaultOfFaultObserver(this);
        }

        /// <summary>
        /// Negative proof one: the whole injection is off and the reported fault reaches the sagas.
        /// </summary>
        public bool InjectionEnabled { get; set; } = true;

        /// <summary>
        /// Negative proof two: only one of the two deliveries is suppressed.
        /// </summary>
        public bool SuppressJobEndpoint { get; set; } = true;

        public bool SuppressJobAttemptEndpoint { get; set; } = true;

        public IFilter<ConsumeContext<JobAttemptFaulted>> JobAttemptFaultedFilter { get; }
        public IFilter<ConsumeContext<GetJobAttemptStatus>> GetJobAttemptStatusObserver { get; }
        public IFilter<ConsumeContext<Fault<JobAttemptFaulted>>> FaultObserver { get; }

        public Task<SuppressedFault> ReportedFaultSuppressed => _reportedFaultSuppressed.Task;
        public Task<ObservedFault> SuspectFaultObserved => _suspectFaultObserved.Task;

        /// <summary>
        /// Completes with the attempt id of the retry as soon as the worker really runs it.
        /// </summary>
        public Task<Guid> RetryAttemptStarted => _retryAttemptStarted.Task;

        /// <summary>
        /// The retry attempt waits here until the test releases it, so a late message of the previous attempt
        /// provably arrives while the retry is still running instead of racing its completion.
        /// </summary>
        public Task RetryAttemptGate => _retryAttemptReleased.Task;

        public void ReleaseRetryAttempt()
        {
            _retryAttemptReleased.TrySetResult(true);
        }

        public int SuppressionCount => Volatile.Read(ref _suppressionCount);
        public int RedeliveryCount => Volatile.Read(ref _redeliveryCount);
        public int FaultOfReportedFaultCount => Volatile.Read(ref _faultObserverCount);
        public int HighestRetryAttempt => Volatile.Read(ref _highestRetryAttempt);
        public Guid StartedAttemptId => _startedAttemptId;

        public string Diagnostics
        {
            get
            {
                lock (_lock)
                {
                    return $"suppressed={_suppressionCount} endpoints=[{string.Join(",", _suppressedEndpoints)}] "
                        + $"redeliveries={_redeliveryCount} statusChecks={_statusCheckCount} "
                        + $"faultOfFault={_faultObserverCount} highestRetryAttempt={_highestRetryAttempt}";
                }
            }
        }

        public void RecordAttempt(Guid attemptId, int retryAttempt)
        {
            lock (_lock)
            {
                if (retryAttempt <= _highestRetryAttempt)
                    return;

                _highestRetryAttempt = retryAttempt;

                if (retryAttempt == 0)
                    _startedAttemptId = attemptId;
                else
                    _retryAttemptStarted.TrySetResult(attemptId);
            }
        }

        /// <summary>
        /// Completes once the given number of job status checks has been delivered. Event driven, so the test
        /// never sleeps, polls or reads a wall clock.
        /// </summary>
        public Task StatusCheckObserved(int count)
        {
            TaskCompletionSource<int> waiter;
            lock (_lock)
            {
                if (_statusCheckCount >= count)
                    return Task.CompletedTask;

                waiter = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                _statusCheckWaiters.Add(waiter);
            }

            return WaitFor(waiter, count);

            async Task WaitFor(TaskCompletionSource<int> current, int required)
            {
                while (await current.Task.ConfigureAwait(false) < required)
                {
                    lock (_lock)
                    {
                        if (_statusCheckCount >= required)
                            return;

                        current = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                        _statusCheckWaiters.Add(current);
                    }
                }
            }
        }

        static string EndpointName(Uri address)
        {
            return address?.AbsolutePath.Trim('/') ?? string.Empty;
        }

        bool Suppress(ConsumeContext<JobAttemptFaulted> context, string endpoint)
        {
            var message = context.Message;

            lock (_lock)
            {
                var messageId = context.MessageId ?? Guid.Empty;

                if (_suppressedMessageId.HasValue && _suppressedMessageId.Value == messageId)
                {
                    // A third delivery of an already suppressed message would be a redelivery or a retry.
                    if (_suppressedEndpoints.Contains(endpoint))
                    {
                        _redeliveryCount++;
                        return false;
                    }
                }
                else if (_suppressedMessageId.HasValue
                    || !InjectionEnabled
                    || message.JobId != _jobId()
                    || message.RetryAttempt != 0
                    || message.RetryDelay.HasValue)
                {
                    return false;
                }

                if (endpoint == "job" && !SuppressJobEndpoint)
                    return false;

                if (endpoint == "job-attempt" && !SuppressJobAttemptEndpoint)
                    return false;

                _suppressedMessageId ??= messageId;
                _suppressedAttemptId ??= message.AttemptId;
                _suppressedEndpoints.Add(endpoint);
                _suppressionCount++;

                if (_suppressedEndpoints.Count == ExpectedSuppressions())
                {
                    _reportedFaultSuppressed.TrySetResult(new SuppressedFault
                    {
                        MessageId = _suppressedMessageId.Value,
                        AttemptId = _suppressedAttemptId.Value,
                        RetryAttempt = message.RetryAttempt,
                        Endpoints = _suppressedEndpoints.ToArray()
                    });
                }

                return true;
            }
        }

        int ExpectedSuppressions()
        {
            var expected = 0;
            if (SuppressJobEndpoint)
                expected++;
            if (SuppressJobAttemptEndpoint)
                expected++;

            return expected;
        }

        Task OnPassedFault(ConsumeContext<JobAttemptFaulted> context)
        {
            var message = context.Message;

            if (message.JobId != _jobId() || !message.RetryDelay.HasValue)
                return Task.CompletedTask;

            var observed = _suspectFaultObserved.TrySetResult(new ObservedFault
            {
                MessageId = context.MessageId ?? Guid.Empty,
                AttemptId = message.AttemptId,
                RetryDelay = message.RetryDelay
            });

            // Only the first suspect fault is held, and only while the test still has to reset the clock.
            return observed ? _suspectFaultReleased.Task : Task.CompletedTask;
        }

        /// <summary>
        /// Lets the held suspect fault continue to the saga. The test calls this after the scheduler clock is
        /// back, so the reset can never race with the forwarding.
        /// </summary>
        public void ReleaseSuspectFault()
        {
            _suspectFaultReleased.TrySetResult(true);
        }

        void OnStatusCheck()
        {
            List<TaskCompletionSource<int>> waiters;
            int count;
            lock (_lock)
            {
                count = ++_statusCheckCount;
                waiters = new List<TaskCompletionSource<int>>(_statusCheckWaiters);
                _statusCheckWaiters.Clear();
            }

            foreach (var waiter in waiters)
                waiter.TrySetResult(count);
        }


        class SuppressReportedFaultFilter :
            IFilter<ConsumeContext<JobAttemptFaulted>>
        {
            readonly SilentWorkerReceiveSuppression _suppression;

            public SuppressReportedFaultFilter(SilentWorkerReceiveSuppression suppression)
            {
                _suppression = suppression;
            }

            public async Task Send(ConsumeContext<JobAttemptFaulted> context, IPipe<ConsumeContext<JobAttemptFaulted>> next)
            {
                var endpoint = EndpointName(context.ReceiveContext.InputAddress);

                if (Array.IndexOf(SagaEndpoints, endpoint) >= 0 && _suppression.Suppress(context, endpoint))
                    return;

                // The suspect fault is held here until the test has put the scheduler clock back. Forwarding it
                // with a clock that still carries the offset would make the status check of the retry attempt
                // due while that attempt is still starting, which the job service correctly treats as a start
                // timeout of the new attempt.
                await _suppression.OnPassedFault(context).ConfigureAwait(false);

                await next.Send(context).ConfigureAwait(false);
            }

            public void Probe(ProbeContext context)
            {
                context.CreateScope("silent-worker-receive-suppression");
            }
        }


        class StatusCheckObserver :
            IFilter<ConsumeContext<GetJobAttemptStatus>>
        {
            readonly SilentWorkerReceiveSuppression _suppression;

            public StatusCheckObserver(SilentWorkerReceiveSuppression suppression)
            {
                _suppression = suppression;
            }

            public Task Send(ConsumeContext<GetJobAttemptStatus> context, IPipe<ConsumeContext<GetJobAttemptStatus>> next)
            {
                if (context.Message.JobId == _suppression._jobId())
                    _suppression.OnStatusCheck();

                return next.Send(context);
            }

            public void Probe(ProbeContext context)
            {
                context.CreateScope("silent-worker-status-check-observer");
            }
        }


        class FaultOfFaultObserver :
            IFilter<ConsumeContext<Fault<JobAttemptFaulted>>>
        {
            readonly SilentWorkerReceiveSuppression _suppression;

            public FaultOfFaultObserver(SilentWorkerReceiveSuppression suppression)
            {
                _suppression = suppression;
            }

            public Task Send(ConsumeContext<Fault<JobAttemptFaulted>> context, IPipe<ConsumeContext<Fault<JobAttemptFaulted>>> next)
            {
                Interlocked.Increment(ref _suppression._faultObserverCount);

                return next.Send(context);
            }

            public void Probe(ProbeContext context)
            {
                context.CreateScope("silent-worker-fault-observer");
            }
        }
    }
}
