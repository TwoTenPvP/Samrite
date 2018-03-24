using ProtoBuf;

namespace SamriteShared.Messages
{
    [ProtoContract]
    public class MessageBase
    {
        [ProtoMember(1)]
        public byte[] Salt;
        [ProtoMember(2)]
        public byte[] Payload;
    }
}
