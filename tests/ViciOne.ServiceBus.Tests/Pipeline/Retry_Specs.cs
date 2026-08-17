namespace ViciOne.ServiceBus.Tests.Pipeline
{
    using System;
    using ViciOne.ServiceBus.Middleware;
    using NUnit.Framework;
    using TestFramework;


    [TestFixture]
    public class Using_the_retry_filter
    {
        [Test]
        public void Should_retry_the_specified_times_and_fail()
        {
            var count = 0;
            IPipe<ConsumeContext<A>> pipe = Pipe.New<ConsumeContext<A>>(x =>
            {
                x.UseMessageRetry(r => r.Interval(4, TimeSpan.FromMilliseconds(2)));
                x.UseExecute(payload =>
                {
                    count++;
                    throw new IntentionalTestException("Kaboom!");
                });
            });

            var context = new TestConsumeContext<A>(new A());

            Assert.That(async () => await pipe.Send(context), Throws.TypeOf<IntentionalTestException>());

            Assert.That(count, Is.EqualTo(5));
        }

        [Test]
        public void Should_support_overloading_downstream()
        {
            var count = 0;
            IPipe<ConsumeContext<A>> pipe = Pipe.New<ConsumeContext<A>>(x =>
            {
                x.UseMessageRetry(r => r.Interval(4, TimeSpan.FromMilliseconds(2)));
                x.UseMessageRetry(r => r.None());
                x.UseExecute(payload =>
                {
                    count++;
                    throw new IntentionalTestException("Kaboom!");
                });
            });

            var context = new TestConsumeContext<A>(new A());

            Assert.That(async () => await pipe.Send(context), Throws.TypeOf<IntentionalTestException>());

            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Should_support_overloading_downstream_either_way()
        {
            var count = 0;
            IPipe<ConsumeContext<A>> pipe = Pipe.New<ConsumeContext<A>>(x =>
            {
                x.UseMessageRetry(r => r.None());
                x.UseMessageRetry(r => r.Interval(4, TimeSpan.FromMilliseconds(2)));
                x.UseExecute(payload =>
                {
                    count++;
                    throw new IntentionalTestException("Kaboom!");
                });
            });

            var context = new TestConsumeContext<A>(new A());

            Assert.That(async () => await pipe.Send(context), Throws.TypeOf<IntentionalTestException>());

            Assert.That(count, Is.EqualTo(5));
        }

        [Test]
        public void Should_support_overloading_downstream_on_cc()
        {
            var count = 0;
            IPipe<ConsumeContext> pipe = Pipe.New<ConsumeContext>(x =>
            {
                x.UseMessageRetry(r =>
                {
                    r.Handle<IntentionalTestException>();
                    r.Interval(4, TimeSpan.FromMilliseconds(2));
                });
                x.UseMessageRetry(r =>
                {
                    r.Handle<IntentionalTestException>();
                    r.None();
                });
                x.UseDispatch(new ConsumeContextConverterFactory(), d =>
                {
                    d.Pipe<ConsumeContext<A>>(a =>
                    {
                        a.UseExecute(payload =>
                        {
                            count++;
                            throw new IntentionalTestException("Kaboom!");
                        });
                    });
                });
            });

            var context = new TestConsumeContext<A>(new A());

            Assert.That(async () => await pipe.Send(context), Throws.TypeOf<IntentionalTestException>());

            Assert.That(count, Is.EqualTo(1));
        }


        class A
        {
        }
    }
}
