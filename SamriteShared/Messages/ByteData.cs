using ProtoBuf;

namespace SamriteShared.Messages
{
    [ProtoContract]
    public class ByteData
    {
        [ProtoMember(1)]
        public byte[] data;
    }
}
