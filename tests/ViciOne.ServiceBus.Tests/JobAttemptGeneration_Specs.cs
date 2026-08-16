namespace ViciOne.ServiceBus.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Contracts.JobService;
    using Events;
    using JobService.Messages;
    using NUnit.Framework;
    using ViciOne.ServiceBus.Saga;
    using ViciOne.ServiceBus.SagaStateMachine;
    using ViciOne.ServiceBus.TestFramework;


    [TestFixture]
    public class Ignoring_messages_from_a_previous_job_attempt
    {
        [TestCase(JobSagaState.Submitted)]
        [TestCase(JobSagaState.WaitingToStart)]
        [TestCase(JobSagaState.WaitingToRetry)]
        [TestCase(JobSagaState.WaitingForSlot)]
        [TestCase(JobSagaState.StartingJobAttempt)]
        [TestCase(JobSagaState.Started)]
        [TestCase(JobSagaState.Completed)]
        [TestCase(JobSagaState.Faulted)]
        [TestCase(JobSagaState.Canceled)]
        [TestCase(JobSagaState.AllocatingJobSlot)]
        [TestCase(JobSagaState.CancellationPending)]
        public async Task Should_leave_the_job_saga_unchanged(JobSagaState sagaState)
        {
            var machine = new JobStateMachine();
            var currentAttemptId = NewId.NextGuid();
            var saga = new JobSaga
            {
                CorrelationId = NewId.NextGuid(),
                AttemptId = currentAttemptId,
                RetryAttempt = 1,
                Submitted = new DateTime(2026, 8, 15, 8, 0, 0, DateTimeKind.Utc),
                Started = new DateTime(2026, 8, 15, 8, 1, 0, DateTimeKind.Utc),
                ServiceAddress = new Uri("loopback://localhost/job-service"),
                Job = new Dictionary<string, object>(),
                JobTypeId = NewId.NextGuid()
            };

            State expectedState = GetState(machine, sagaState);
            await SetState(machine, saga, expectedState);

            DateTime? expectedStarted = saga.Started;
            DateTime? expectedCompleted = saga.Completed;
            TimeSpan? expectedDuration = saga.Duration;
            DateTime? expectedFaulted = saga.Faulted;
            string expectedReason = saga.Reason;
            int expectedRetryAttempt = saga.RetryAttempt;

            foreach ((StaleAttemptEvent @event, object message) in CreateStaleEvents(saga))
            {
                await Raise(machine, saga, @event, message);

                State<JobSaga> actualState = await machine.GetState(saga);

                Assert.Multiple(() =>
                {
                    Assert.That(actualState.Name, Is.EqualTo(expectedState.Name), $"{@event} changed the saga state");
                    Assert.That(saga.AttemptId, Is.EqualTo(currentAttemptId), $"{@event} replaced the current attempt");
                    Assert.That(saga.RetryAttempt, Is.EqualTo(expectedRetryAttempt), $"{@event} changed the retry generation");
                    Assert.That(saga.Started, Is.EqualTo(expectedStarted), $"{@event} changed Started");
                    Assert.That(saga.Completed, Is.EqualTo(expectedCompleted), $"{@event} changed Completed");
                    Assert.That(saga.Duration, Is.EqualTo(expectedDuration), $"{@event} changed Duration");
                    Assert.That(saga.Faulted, Is.EqualTo(expectedFaulted), $"{@event} changed Faulted");
                    Assert.That(saga.Reason, Is.EqualTo(expectedReason), $"{@event} changed Reason");
                });
            }
        }

        [Test]
        public async Task Should_preserve_the_state_rules_for_the_current_attempt()
        {
            var machine = new JobStateMachine();
            var saga = new JobSaga
            {
                CorrelationId = NewId.NextGuid(),
                AttemptId = NewId.NextGuid(),
                ServiceAddress = new Uri("loopback://localhost/job-service"),
                Job = new Dictionary<string, object>(),
                JobTypeId = NewId.NextGuid()
            };
            var message = new JobAttemptStartedEvent
            {
                JobId = saga.CorrelationId,
                AttemptId = saga.AttemptId,
                RetryAttempt = 0,
                Timestamp = new DateTime(2026, 8, 15, 7, 0, 0, DateTimeKind.Utc),
                InstanceAddress = new Uri("loopback://localhost/current-instance")
            };

            await SetState(machine, saga, machine.Submitted);

            Assert.That(async () => await machine.RaiseEvent(saga, machine.AttemptStarted, message),
                Throws.TypeOf<UnhandledEventException>());
        }

        static IEnumerable<(StaleAttemptEvent Event, object Message)> CreateStaleEvents(JobSaga saga)
        {
            var staleAttemptId = NewId.NextGuid();

            yield return (StaleAttemptEvent.Started, (JobAttemptStarted)new JobAttemptStartedEvent
            {
                JobId = saga.CorrelationId,
                AttemptId = staleAttemptId,
                RetryAttempt = 0,
                Timestamp = new DateTime(2026, 8, 15, 7, 0, 0, DateTimeKind.Utc),
                InstanceAddress = new Uri("loopback://localhost/old-instance")
            });
            yield return (StaleAttemptEvent.Completed, (JobAttemptCompleted)new JobAttemptCompletedEvent
            {
                JobId = saga.CorrelationId,
                AttemptId = staleAttemptId,
                RetryAttempt = 0,
                Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
                Duration = TimeSpan.FromMinutes(1)
            });
            yield return (StaleAttemptEvent.Faulted, (JobAttemptFaulted)new JobAttemptFaultedEvent
            {
                JobId = saga.CorrelationId,
                AttemptId = staleAttemptId,
                RetryAttempt = 0,
                Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc)
            });
            yield return (StaleAttemptEvent.Canceled, (JobAttemptCanceled)new JobAttemptCanceledEvent
            {
                JobId = saga.CorrelationId,
                AttemptId = staleAttemptId,
                Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
                Reason = "late cancellation from the previous attempt"
            });

            StartJobAttempt command = new StartJobAttemptCommand
            {
                JobId = saga.CorrelationId,
                AttemptId = staleAttemptId,
                RetryAttempt = 0,
                ServiceAddress = saga.ServiceAddress,
                InstanceAddress = new Uri("loopback://localhost/old-instance"),
                Job = saga.Job,
                JobTypeId = saga.JobTypeId
            };
            yield return (StaleAttemptEvent.StartFaulted, (Fault<StartJobAttempt>)new FaultEvent<StartJobAttempt>
            {
                FaultId = NewId.NextGuid(),
                Timestamp = new DateTime(2026, 8, 15, 7, 1, 0, DateTimeKind.Utc),
                Message = command,
                Exceptions = []
            });
        }

        static Task Raise(JobStateMachine machine, JobSaga saga, StaleAttemptEvent @event, object message)
        {
            return @event switch
            {
                StaleAttemptEvent.Started => machine.RaiseEvent(saga, machine.AttemptStarted, (JobAttemptStarted)message),
                StaleAttemptEvent.Completed => machine.RaiseEvent(saga, machine.AttemptCompleted, (JobAttemptCompleted)message),
                StaleAttemptEvent.Faulted => machine.RaiseEvent(saga, machine.AttemptFaulted, (JobAttemptFaulted)message),
                StaleAttemptEvent.Canceled => machine.RaiseEvent(saga, machine.AttemptCanceled, (JobAttemptCanceled)message),
                StaleAttemptEvent.StartFaulted => machine.RaiseEvent(saga, machine.StartJobAttemptFaulted, (Fault<StartJobAttempt>)message),
                _ => throw new ArgumentOutOfRangeException(nameof(@event), @event, null)
            };
        }

        static Task SetState(JobStateMachine machine, JobSaga saga, State state)
        {
            var consumeContext = new TestConsumeContext<StateSetupMessage>(new StateSetupMessage());
            var sagaConsumeContext = new InMemorySagaConsumeContext<JobSaga, StateSetupMessage>(consumeContext, new SagaInstance<JobSaga>(saga));
            var behaviorContext = new ViciOneServiceBusStateMachine<JobSaga>.BehaviorContextProxy(machine, sagaConsumeContext, machine.Initial.Enter);

            return machine.Accessor.Set(behaviorContext, machine.GetState(state.Name));
        }

        static State GetState(JobStateMachine machine, JobSagaState state)
        {
            return state switch
            {
                JobSagaState.Submitted => machine.Submitted,
                JobSagaState.WaitingToStart => machine.WaitingToStart,
                JobSagaState.WaitingToRetry => machine.WaitingToRetry,
                JobSagaState.WaitingForSlot => machine.WaitingForSlot,
                JobSagaState.StartingJobAttempt => machine.StartingJobAttempt,
                JobSagaState.Started => machine.Started,
                JobSagaState.Completed => machine.Completed,
                JobSagaState.Faulted => machine.Faulted,
                JobSagaState.Canceled => machine.Canceled,
                JobSagaState.AllocatingJobSlot => machine.AllocatingJobSlot,
                JobSagaState.CancellationPending => machine.CancellationPending,
                _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
            };
        }

        public enum JobSagaState
        {
            Submitted,
            WaitingToStart,
            WaitingToRetry,
            WaitingForSlot,
            StartingJobAttempt,
            Started,
            Completed,
            Faulted,
            Canceled,
            AllocatingJobSlot,
            CancellationPending
        }

        enum StaleAttemptEvent
        {
            Started,
            Completed,
            Faulted,
            Canceled,
            StartFaulted
        }

        class StateSetupMessage
        {
        }
    }
}
