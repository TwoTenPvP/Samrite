using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SamriteShared.Messages
{
    [ProtoContract]
    public class VideoDeviceCapabilities
    {
        [ProtoMember(1)]
        public int width;
        [ProtoMember(2)]
        public int height;
        [ProtoMember(3)]
        public int maxFramerate;
        [ProtoMember(4)]
        public int averageFramerate;
    }
}
