using System;
using UnityEngine;

namespace MultiplayerMod.MonoBehaviours
{
    public class IDHolder : MonoBehaviour
    {
        public IDHolder(IntPtr ptr) : base(ptr) { }

        public short ID;
    }
}
