namespace ViciOne.ServiceBus.Tests.SagaStateMachineTests.Dynamic_Modify
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;


    [TestFixture(Category = "Dynamic Modify")]
    public class When_an_action_throws_an_exception
    {
        [Test]
        public void Should_capture_the_exception_message()
        {
            Assert.That(_instance.ExceptionMessage, Is.EqualTo("Boom!"));
        }

        [Test]
        public void Should_capture_the_exception_type()
        {
            Assert.That(_instance.ExceptionType, Is.EqualTo(typeof(ApplicationException)));
        }

        [Test]
        public void Should_have_called_the_async_if_block()
        {
            Assert.That(_instance.CalledThenClauseAsync, Is.True);
        }

        [Test]
        public void Should_have_called_the_async_then_block()
        {
            Assert.That(_instance.ThenAsyncShouldBeCalled, Is.True);
        }

        [Test]
        public void Should_have_called_the_exception_handler()
        {
            Assert.That(_instance.CurrentState, Is.EqualTo(Failed));
        }

        [Test]
        public void Should_have_called_the_false_async_condition_else_block()
        {
            Assert.That(_instance.ElseAsyncShouldBeCalled, Is.True);
        }

        [Test]
        public void Should_have_called_the_false_condition_else_block()
        {
            Assert.That(_instance.ElseShouldBeCalled, Is.True);
        }

        [Test]
        public void Should_have_called_the_first_action()
        {
            Assert.That(_instance.Called, Is.True);
        }

        [Test]
        public void Should_have_called_the_first_if_block()
        {
            Assert.That(_instance.CalledThenClause, Is.True);
        }

        [Test]
        public void Should_not_have_called_the_false_async_condition_then_block()
        {
            Assert.That(_instance.ThenAsyncShouldNotBeCalled, Is.False);
        }

        [Test]
        public void Should_not_have_called_the_false_condition_then_block()
        {
            Assert.That(_instance.ThenShouldNotBeCalled, Is.False);
        }

        [Test]
        public void Should_not_have_called_the_regular_exception()
        {
            Assert.That(_instance.ShouldNotBeCalled, Is.False);
        }

        [Test]
        public void Should_not_have_called_the_second_action()
        {
            Assert.That(_instance.NotCalled, Is.True);
        }

        State Failed;
        Event Initialized;
        Instance _instance;
        StateMachine<Instance> _machine;

        [OneTimeSetUp]
        public void Specifying_an_event_activity()
        {
            _instance = new Instance();
            _machine = ViciOneServiceBusStateMachine<Instance>
                .New(builder => builder
                    .State("Failed", out Failed)
                    .Event("Initialized", out Initialized)
                    .During(builder.Initial)
                    .When(Initialized, b => b
                        .Then(context => context.Saga.Called = true)
                        .Then(_ =>
                        {
                            throw new ApplicationException("Boom!");
                        })
                        .Then(context => context.Saga.NotCalled = false)
                        .Catch<ApplicationException>(ex => ex
                            .If(context => true, c => c
                                .Then(context => context.Saga.CalledThenClause = true)
                            )
                            .IfAsync(context => Task.FromResult(true), c => c
                                .Then(context => context.Saga.CalledThenClauseAsync = true)
                            )
                            .IfElse(context => false,
                                c => c.Then(context => context.Saga.ThenShouldNotBeCalled = true),
                                c => c.Then(context => context.Saga.ElseShouldBeCalled = true)
                            )
                            .IfElseAsync(context => Task.FromResult(false),
                                c => c.Then(context => context.Saga.ThenAsyncShouldNotBeCalled = true),
                                c => c.Then(context => context.Saga.ElseAsyncShouldBeCalled = true)
                            )
                            .Then(context =>
                            {
                                context.Saga.ExceptionMessage = context.Exception.Message;
                                context.Saga.ExceptionType = context.Exception.GetType();
                            })
                            .ThenAsync(context =>
                            {
                                context.Saga.ThenAsyncShouldBeCalled = true;
                                return Task.CompletedTask;
                            })
                            .TransitionTo(Failed)
                        )
                        .Catch<Exception>(ex => ex
                            .Then(context => context.Saga.ShouldNotBeCalled = true)
                        )
                    )
                );

            _machine.RaiseEvent(_instance, Initialized).Wait();
        }


        class Instance :
            SagaStateMachineInstance
        {
            public Instance()
            {
                NotCalled = true;
            }

            public bool Called { get; set; }
            public bool NotCalled { get; set; }
            public Type ExceptionType { get; set; }
            public string ExceptionMessage { get; set; }
            public State CurrentState { get; set; }

            public bool ShouldNotBeCalled { get; set; }

            public bool CalledThenClause { get; set; }
            public bool CalledThenClauseAsync { get; set; }

            public bool ThenShouldNotBeCalled { get; set; }
            public bool ElseShouldBeCalled { get; set; }
            public bool ThenAsyncShouldNotBeCalled { get; set; }
            public bool ElseAsyncShouldBeCalled { get; set; }

            public bool ThenAsyncShouldBeCalled { get; set; }
            public Guid CorrelationId { get; set; }
        }
    }


    [TestFixture(Category = "Dynamic Modify")]
    public class When_the_exception_does_not_match_the_type
    {
        [Test]
        public void Should_capture_the_exception_message()
        {
            Assert.That(_instance.ExceptionMessage, Is.EqualTo("Boom!"));
        }

        [Test]
        public void Should_capture_the_exception_type()
        {
            Assert.That(_instance.ExceptionType, Is.EqualTo(typeof(ApplicationException)));
        }

        [Test]
        public void Should_have_called_the_exception_handler()
        {
            Assert.That(_instance.CurrentState, Is.EqualTo(Failed));
        }

        [Test]
        public void Should_have_called_the_first_action()
        {
            Assert.That(_instance.Called, Is.True);
        }

        [Test]
        public void Should_not_have_called_the_second_action()
        {
            Assert.That(_instance.NotCalled, Is.True);
        }

        State Failed;
        Event Initialized;
        Instance _instance;
        StateMachine<Instance> _machine;

        [OneTimeSetUp]
        public void Specifying_an_event_activity()
        {
            _instance = new Instance();
            _machine = ViciOneServiceBusStateMachine<Instance>
                .New(builder => builder
                    .State("Failed", out Failed)
                    .Event("Initialized", out Initialized)
                    .InstanceState(b => b.CurrentState)
                    .During(builder.Initial)
                    .When(Initialized, b => b
                        .Then(context => context.Saga.Called = true)
                        .Then(_ =>
                        {
                            throw new ApplicationException("Boom!");
                        })
                        .Then(context => context.Saga.NotCalled = false)
                        .Catch<Exception>(ex => ex
                            .Then(context =>
                            {
                                context.Saga.ExceptionMessage = context.Exception.Message;
                                context.Saga.ExceptionType = context.Exception.GetType();
                            })
                            .TransitionTo(Failed)
                        )
                    )
                );

            _machine.RaiseEvent(_instance, Initialized).Wait();
        }


        class Instance :
            SagaStateMachineInstance
        {
            public Instance()
            {
                NotCalled = true;
            }

            public bool Called { get; set; }
            public bool NotCalled { get; set; }
            public Type ExceptionType { get; set; }
            public string ExceptionMessage { get; set; }
            public State CurrentState { get; set; }
            public Guid CorrelationId { get; set; }
        }
    }


    [TestFixture(Category = "Dynamic Modify")]
    public class When_the_exception_is_caught
    {
        [Test]
        public void Should_have_called_the_subsequent_action()
        {
            Assert.That(_instance.Called, Is.True);
        }

        Instance _instance;
        StateMachine<Instance> _machine;

        [OneTimeSetUp]
        public void Specifying_an_event_activity()
        {
            Event Initialized = null;

            _instance = new Instance();
            _machine = ViciOneServiceBusStateMachine<Instance>
                .New(builder => builder
                    .Event("Initialized", out Initialized)
                    .State("Failed", out State Failed)
                    .InstanceState(b => b.CurrentState)
                    .During(builder.Initial)
                    .When(Initialized, b => b
                        .Then(_ =>
                        {
                            throw new ApplicationException("Boom!");
                        })
                        .Catch<Exception>(ex => ex)
                        .Then(context => context.Saga.Called = true)
                    )
                );

            _machine.RaiseEvent(_instance, Initialized).Wait();
        }


        class Instance :
            SagaStateMachineInstance
        {
            public bool Called { get; set; }
            public State CurrentState { get; set; }
            public Guid CorrelationId { get; set; }
        }
    }


    [TestFixture(Category = "Dynamic Modify")]
    public class When_an_action_throws_an_exception_on_data_events
    {
        [Test]
        public void Should_capture_the_exception_message()
        {
            Assert.That(_instance.ExceptionMessage, Is.EqualTo("Boom!"));
        }

        [Test]
        public void Should_capture_the_exception_type()
        {
            Assert.That(_instance.ExceptionType, Is.EqualTo(typeof(ApplicationException)));
        }

        [Test]
        public void Should_have_called_the_async_if_block()
        {
            Assert.That(_instance.CalledSecondThenClause, Is.True);
        }

        [Test]
        public void Should_have_called_the_exception_handler()
        {
            Assert.That(_instance.CurrentState, Is.EqualTo(Failed));
        }

        [Test]
        public void Should_have_called_the_false_async_condition_else_block()
        {
            Assert.That(_instance.ElseAsyncShouldBeCalled, Is.True);
        }

        [Test]
        public void Should_have_called_the_false_condition_else_block()
        {
            Assert.That(_instance.ElseShouldBeCalled, Is.True);
        }

        [Test]
        public void Should_have_called_the_first_action()
        {
            Assert.That(_instance.Called, Is.True);
        }

        [Test]
        public void Should_have_called_the_first_if_block()
        {
            Assert.That(_instance.CalledThenClause, Is.True);
        }

        [Test]
        public void Should_not_have_called_the_false_async_condition_then_block()
        {
            Assert.That(_instance.ThenAsyncShouldNotBeCalled, Is.False);
        }

        [Test]
        public void Should_not_have_called_the_false_condition_then_block()
        {
            Assert.That(_instance.ThenShouldNotBeCalled, Is.False);
        }

        [Test]
        public void Should_not_have_called_the_second_action()
        {
            Assert.That(_instance.NotCalled, Is.True);
        }

        State Failed;
        Event<Init> Initialized;
        Instance _instance;
        StateMachine<Instance> _machine;

        [OneTimeSetUp]
        public void Specifying_an_event_activity()
        {
            _instance = new Instance();
            _machine = ViciOneServiceBusStateMachine<Instance>
                .New(builder => builder
                    .State("Failed", out Failed)
                    .Event("Initialized", out Initialized)
                    .InstanceState(b => b.CurrentState)
                    .During(builder.Initial)
                    .When(Initialized, b => b
                        .Then(context => context.Saga.Called = true)
                        .Then(_ =>
                        {
                            throw new ApplicationException("Boom!");
                        })
                        .Then(context => context.Saga.NotCalled = false)
                        .Catch<Exception>(ex => ex
                            .If(context => true, b => b
                                .Then(context => context.Saga.CalledThenClause = true)
                            )
                            .IfAsync(context => Task.FromResult(true), b => b
                                .Then(context => context.Saga.CalledSecondThenClause = true)
                            )
                            .IfElse(context => false,
                                b => b.Then(context => context.Saga.ThenShouldNotBeCalled = true),
                                b => b.Then(context => context.Saga.ElseShouldBeCalled = true)
                            )
                            .IfElseAsync(context => Task.FromResult(false),
                                b => b.Then(context => context.Saga.ThenAsyncShouldNotBeCalled = true),
                                b => b.Then(context => context.Saga.ElseAsyncShouldBeCalled = true)
                            )
                            .Then(context =>
                            {
                                context.Saga.ExceptionMessage = context.Exception.Message;
                                context.Saga.ExceptionType = context.Exception.GetType();
                            })
                            .TransitionTo(Failed)
                        )
                    )
                );

            _machine.RaiseEvent(_instance, Initialized, new Init()).Wait();
        }


        class Instance :
            SagaStateMachineInstance
        {
            public Instance()
            {
                NotCalled = true;
            }

            public bool Called { get; set; }
            public bool NotCalled { get; set; }
            public Type ExceptionType { get; set; }
            public string ExceptionMessage { get; set; }
            public State CurrentState { get; set; }

            public bool CalledThenClause { get; set; }
            public bool CalledSecondThenClause { get; set; }

            public bool ThenShouldNotBeCalled { get; set; }
            public bool ElseShouldBeCalled { get; set; }
            public bool ThenAsyncShouldNotBeCalled { get; set; }
            public bool ElseAsyncShouldBeCalled { get; set; }
            public Guid CorrelationId { get; set; }
        }


        class Init
        {
        }
    }
}
