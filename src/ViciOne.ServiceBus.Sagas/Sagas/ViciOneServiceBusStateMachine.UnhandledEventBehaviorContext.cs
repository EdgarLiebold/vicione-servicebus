using System.Threading.Tasks;

namespace ViciOne.ServiceBus.Sagas;

public partial class ViciOneServiceBusStateMachine<TInstance>
    where TInstance : class, ISagaStateMachineInstance
{
    /// <summary>Exposes the ignored-or-faulted decision for an event that is not handled in the current state.</summary>
    class UnhandledEventBehaviorContext :
        BehaviorContextProxy,
        IUnhandledEventContext<TInstance>
    {
        readonly IBehaviorContext<TInstance> _context;
        readonly IStateMachine<TInstance> _machine;

        public UnhandledEventBehaviorContext(IStateMachine<TInstance> machine, IBehaviorContext<TInstance> context, IState state)
            : this(
                machine ?? throw new ArgumentNullException(nameof(machine)),
                context ?? throw new ArgumentNullException(nameof(context)),
                state,
                true)
        {
        }

        UnhandledEventBehaviorContext(IStateMachine<TInstance> machine, IBehaviorContext<TInstance> context, IState state, bool _)
            : base(machine, context, context.Event)
        {
            ArgumentNullException.ThrowIfNull(state);

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

            return Task.FromException(new UnhandledEventException(_machine.Name, _context.Event.Name, CurrentState.Name));
        }
    }
}
