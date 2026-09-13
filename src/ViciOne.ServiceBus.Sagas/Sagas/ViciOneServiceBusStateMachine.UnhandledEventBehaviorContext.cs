using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    class UnhandledEventBehaviorContext :
        BehaviorContextProxy,
        IUnhandledEventContext<TInstance>
    {
        readonly IBehaviorContext<TInstance> _context;
        readonly IStateMachine<TInstance> _machine;

        public UnhandledEventBehaviorContext(IStateMachine<TInstance> machine, IBehaviorContext<TInstance> context, IState state)
            : base(machine, context, context.Event)
        {
            _context = context;
            CurrentState = state;
            _machine = machine;
        }

        public IState CurrentState { get; }

        public IEvent Event => _context.Event;

        public Task IgnoreAsync(CancellationToken cancellationToken = default)
        {
            return cancellationToken.IsCancellationRequested
                ? Task.FromCanceled(cancellationToken)
                : Task.CompletedTask;
        }

        public Task ThrowAsync(CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);

            throw new UnhandledEventException(_machine.Name, _context.Event.Name, CurrentState.Name);
        }
    }
}
