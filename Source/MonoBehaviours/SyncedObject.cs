using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MonoBehaviours
{
    public class SyncedObject : MonoBehaviour
    {
        public SyncedObject(IntPtr ptr) : base(ptr) { }
        public Vector3 lastSyncedPos = Vector3.zero;
        public Quaternion lastSyncedRotation = Quaternion.identity;
        public ushort ID;
        public byte owner = 0;
        public Rigidbody rb;

        public bool NeedsSync()
        {
            return (transform.position - lastSyncedPos).sqrMagnitude > 0.005f || Quaternion.Angle(transform.rotation, lastSyncedRotation) > 0.25f;
        }

        public void UpdateLastSync()
        {
            lastSyncedPos = transform.position;
            lastSyncedRotation = transform.rotation;
        }

        public ObjectSyncMessage CreateSyncMessage()
        {
            var osm = new ObjectSyncMessage()
            {
                id = ID,
                position = transform.position,
                rotation = transform.rotation
            };

            return osm;
        }
    }
}
