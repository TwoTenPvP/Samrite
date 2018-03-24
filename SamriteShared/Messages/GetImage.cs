using ProtoBuf;

namespace SamriteShared.Messages
{
    [ProtoContract]
    public class GetImage
    {
        [ProtoMember(1)]
        public int width;
        [ProtoMember(2)]
        public int height;
        [ProtoMember(3)]
        public int deviceIndex;
    }
}
