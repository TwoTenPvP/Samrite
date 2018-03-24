using ProtoBuf;
namespace SamriteShared.Messages
{
    [ProtoContract]
    public class VideoDevice
    {
        [ProtoMember(1)]
        public string name;
        [ProtoMember(2)]
        public VideoDeviceCapabilities[] capabilities;
    }
}
