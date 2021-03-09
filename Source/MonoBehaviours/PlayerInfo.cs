using Facepunch.Steamworks;
using MultiplayerMod.Representations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerMod.Source.MonoBehaviours
{
    public class PlayerInfo : MonoBehaviour
    {
        public PlayerInfo(IntPtr ptr) : base(ptr) { }
        public PlayerRep rep;
        //https://www.youtube.com/watch?v=w5x7956gchI
    }
}
