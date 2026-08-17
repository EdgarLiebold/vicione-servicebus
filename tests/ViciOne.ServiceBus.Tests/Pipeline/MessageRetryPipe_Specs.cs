namespace ViciOne.ServiceBus.Tests.Pipeline
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using TestFramework;
    using TestFramework.Messages;


    /// <summary>
    /// The retry that keeps working on the consume context, configured through UseMessageRetry on a pipe
    /// built directly over ConsumeContext.
    /// <para>
    /// The last test is the one that matters for the specification type. A retry built from
    /// RetryPipeSpecification&lt;T&gt; repeats the call just as well, so counting attempts alone cannot
    /// tell the two apart. Only the consume context aware specification puts a ConsumeRetryContext on
    /// the context, which is what GetRetryAttempt reads, so with the wrong specification the attempt
    /// number stays at zero while the other assertions still pass.
    /// </para>
    /// </summary>
    [TestFixture]
    public class The_message_retry_on_a_consume_context_pipe
    {
        [Test]
        public async Task Should_repeat_until_the_call_succeeds()
        {
            var attempts = 0;

            IPipe<ConsumeContext> pipe = ViciOne.ServiceBus.Pipe.New<ConsumeContext>(x =>
            {
                x.UseMessageRetry(r => r.Immediate(2));
                x.UseExecute(_ =>
                {
                    attempts++;
                    if (attempts < 3)
                        throw new IntentionalTestException($"attempt {attempts} fails on purpose");
                });
            });

            await pipe.Send(new TestConsumeContext<PingMessage>(new PingMessage()));

            Assert.That(attempts, Is.EqualTo(3), "the call was not repeated until it succeeded");
        }

        [Test]
        public void Should_give_up_after_the_configured_number_of_attempts()
        {
            var attempts = 0;

            IPipe<ConsumeContext> pipe = ViciOne.ServiceBus.Pipe.New<ConsumeContext>(x =>
            {
                x.UseMessageRetry(r => r.Immediate(2));
                x.UseExecute(_ =>
                {
                    attempts++;
                    throw new IntentionalTestException($"attempt {attempts} fails on purpose");
                });
            });

            Assert.That(async () => await pipe.Send(new TestConsumeContext<PingMessage>(new PingMessage())),
                Throws.TypeOf<IntentionalTestException>(), "the final failure did not reach the caller");

            Assert.That(attempts, Is.EqualTo(3), "the number of attempts is not the configured one plus the first call");
        }

        [Test]
        public async Task Should_carry_the_attempt_number_on_the_consume_context()
        {
            var seen = new List<int>();

            IPipe<ConsumeContext> pipe = ViciOne.ServiceBus.Pipe.New<ConsumeContext>(x =>
            {
                x.UseMessageRetry(r => r.Immediate(2));
                x.UseExecute(context =>
                {
                    seen.Add(context.GetRetryAttempt());
                    if (seen.Count < 3)
                        throw new IntentionalTestException($"attempt {seen.Count} fails on purpose");
                });
            });

            await pipe.Send(new TestConsumeContext<PingMessage>(new PingMessage()));

            Assert.That(seen, Is.EqualTo(new[] { 0, 1, 2 }),
                "the consume context does not carry the retry attempt, so the retry is not the consume context aware one");
        }
    }
}
