using ProtoBuf;

namespace SamriteShared.Messages
{
    [ProtoContract]
    public class VideoDevices
    {
        [ProtoMember(1)]
        public VideoDevice[] videoDevices;
    }
}
