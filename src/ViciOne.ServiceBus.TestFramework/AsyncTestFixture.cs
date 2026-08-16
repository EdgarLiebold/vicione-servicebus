namespace ViciOne.ServiceBus.TestFramework
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Testing;
    using Testing.Implementations;


    public abstract class AsyncTestFixture
    {
        protected AsyncTestFixture(AsyncTestHarness harness)
        {
            AsyncTestHarness = harness;
        }

        protected AsyncTestHarness AsyncTestHarness { get; }

        /// <summary>
        /// Grants every test its own <see cref="TestTimeout" />.
        /// <para>
        /// NUnit builds one fixture instance and runs all of its test methods on it, so a budget created
        /// once would be shared: what the early tests spend is missing from the later ones, and after it
        /// is exhausted every remaining test that honours the token fails immediately regardless of its
        /// own speed. Measured before this hook existed, a test that passes on its own in 60.3 s failed
        /// after 0.037 s once two predecessors in the same fixture had spent 40 s.
        /// </para>
        /// <para>
        /// This is the single place the NUnit lifecycle is bound to the harness. It is not repeated in
        /// any transport fixture, and ViciOne.ServiceBus itself stays free of a test framework reference.
        /// NUnit runs a base class SetUp before the SetUp of a derived fixture, so the budget is already
        /// fresh when a fixture's own set up runs.
        /// </para>
        /// </summary>
        [SetUp]
        public void BeginAsyncTestScope()
        {
            AsyncTestHarness.BeginTestScope();
        }

        /// <summary>
        /// Task that is canceled when the test is aborted, for continueWith usage
        /// </summary>
        protected Task TestCancelledTask => AsyncTestHarness.TestCancelledTask;

        /// <summary>
        /// CancellationToken that is canceled when the test is being aborted
        /// </summary>
        protected CancellationToken TestCancellationToken => AsyncTestHarness.TestCancellationToken;

        /// <summary>
        /// CancellationToken that is cancelled when the test inactivity timeout has elapsed with no bus activity
        /// </summary>
        protected CancellationToken InactivityToken => AsyncTestHarness.InactivityToken;

        /// <summary>
        /// Task that is completed when the bus inactivity timeout has elapsed with no bus activity
        /// </summary>
        protected Task InactivityTask => AsyncTestHarness.InactivityTask;

        /// <summary>
        /// Timeout for the test, used for any delay timers
        /// </summary>
        protected TimeSpan TestTimeout
        {
            get => AsyncTestHarness.TestTimeout;
            set => AsyncTestHarness.TestTimeout = value;
        }

        /// <summary>
        /// Timeout for detecting bus activity
        /// </summary>
        protected TimeSpan TestInactivityTimeout
        {
            get => AsyncTestHarness.TestInactivityTimeout;
            set => AsyncTestHarness.TestInactivityTimeout = value;
        }

        /// <summary>
        /// Forces the test to be cancelled, aborting any awaiting tasks
        /// </summary>
        protected void CancelTest()
        {
            AsyncTestHarness.Cancel();
        }

        /// <summary>
        /// Returns a task completion that is automatically canceled when the test is canceled
        /// </summary>
        /// <typeparam name="T">The task type</typeparam>
        /// <returns></returns>
        public TaskCompletionSource<T> GetTask<T>()
        {
            return AsyncTestHarness.GetTask<T>();
        }

        protected TestConsumeMessageObserver<T> GetConsumeObserver<T>()
            where T : class
        {
            return AsyncTestHarness.GetConsumeObserver<T>();
        }

        protected TestConsumeObserver GetConsumeObserver()
        {
            return AsyncTestHarness.GetConsumeObserver();
        }
    }
}
