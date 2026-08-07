// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Initializers.TypeConverters
{
    public class StateTypeConverter :
        ITypeConverter<string, State>
    {
        public bool TryConvert(State input, out string result)
        {
            if (input != null)
            {
                result = input.Name;
                return true;
            }

            result = null;
            return false;
        }
    }
}
