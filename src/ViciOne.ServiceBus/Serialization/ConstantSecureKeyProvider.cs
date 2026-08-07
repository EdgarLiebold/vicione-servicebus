// ViciOne modification: WP-F2-SERVICEBUS-IDENTITY, 2026-08-07.
namespace ViciOne.ServiceBus.Serialization
{
    public class ConstantSecureKeyProvider :
        ISecureKeyProvider
    {
        readonly byte[] _key;

        public ConstantSecureKeyProvider(byte[] key)
        {
            _key = key;
        }

        public void Probe(ProbeContext context)
        {
            context.Add("key", "constant");
        }

        public byte[] GetKey(Headers headers)
        {
            return _key;
        }
    }
}
