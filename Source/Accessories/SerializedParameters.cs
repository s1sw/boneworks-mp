using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.Accessories
{
    [System.Serializable]
    public struct SerializedMasterList
    {
        public SerializedParameters[] serializedParams;
    }

    [System.Serializable]
    public struct SerializedParameters
    {
        public string fileLocation;
        public AttachPoint attachPoint;
    }
}
