// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.TestFramework.Courier
{
    using System.Threading.Tasks;


    public class SetVariablesFaultyActivity :
        IExecuteActivity<SetVariablesFaultyArguments>
    {
        public async Task<ExecutionResult> Execute(ExecuteContext<SetVariablesFaultyArguments> context)
        {
            return context.FaultedWithVariables(new IntentionalTestException("Things that make you go boom!"),
                new {Test = "Data"});
        }
    }
}
