using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerMod.MonoBehaviours
{
    public class SyncOnCollide : MonoBehaviour
    {
        public SyncOnCollide(IntPtr ptr) : base(ptr) { }

        public Action<GameObject, OwnershipPriorityLevel> SyncObjectCallback;

        public void OnCollisionEnter(Collision collision)
        {
            SyncObjectCallback(collision.rigidbody.gameObject, OwnershipPriorityLevel.Touched);
        }
    }
}
