using ProtoBuf;

namespace SamriteShared.Messages
{
    [ProtoContract]
    public class DeviceLocation
    {
        [ProtoMember(1)]
        public float longitude;
        [ProtoMember(2)]
        public float latitude;
        [ProtoMember(3)]
        public float accuracy;
        [ProtoMember(4)]
        public bool isValid;
    }
}
