// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Context
{
    public class HostCompensateActivityContext<TActivity, TLog> :
        CompensateContextProxy<TLog>,
        CompensateActivityContext<TActivity, TLog>
        where TActivity : class, ICompensateActivity<TLog>
        where TLog : class
    {
        readonly TActivity _activity;

        public HostCompensateActivityContext(TActivity activity, CompensateContext<TLog> context)
            : base(context)
        {
            _activity = activity;
        }

        TActivity CompensateActivityContext<TActivity, TLog>.Activity => _activity;

        public void Method7()
        {
        }

        public void Method8()
        {
        }

        public void Method9()
        {
        }
    }
}
