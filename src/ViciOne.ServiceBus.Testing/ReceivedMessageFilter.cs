namespace ViciOne.ServiceBus.Testing
{
    public class ReceivedMessageFilter
    {
        readonly ReceivedMessageFilterSet _excludes = new ReceivedMessageFilterSet();
        readonly ReceivedMessageFilterSet _includes = new ReceivedMessageFilterSet();

        public ReceivedMessageFilterSet Includes => _includes;

        public ReceivedMessageFilterSet Excludes => _excludes;

        public bool Any(IReceivedMessage element)
        {
            return _includes.Any(element) && _excludes.None(element);
        }

        public bool None(IReceivedMessage element)
        {
            return _includes.None(element);
        }
    }
}
