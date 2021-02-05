using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerMod.MonoBehaviours
{
    public class ServerSyncedObject : MonoBehaviour
    {
        public ServerSyncedObject(IntPtr ptr) : base(ptr) { }
        public Vector3 lastSyncedPos = Vector3.zero;
        public Quaternion lastSyncedRotation = Quaternion.identity;
        public IDHolder holder;

        public void Start()
        {
            holder = GetComponent<IDHolder>();
        }

        public bool NeedsSync()
        {
            return (transform.position - lastSyncedPos).sqrMagnitude > 0.05f || Quaternion.Angle(transform.rotation, lastSyncedRotation) > 2.0f;
        }

        public void UpdateLastSync()
        {
            lastSyncedPos = transform.position;
            lastSyncedRotation = transform.rotation;
        }
    }
}
