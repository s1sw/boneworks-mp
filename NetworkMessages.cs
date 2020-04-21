using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace MultiplayerMod
{
    public interface INetworkMessage
    {
        P2PMessage MakeMsg();
        int GetSize();
    }

    // Server -> clients
    public class OtherPlayerPositionMessage : INetworkMessage
    {
        public Vector3 headPos;
        public Quaternion headRot;
        public Vector3 lHandPos;
        public Quaternion lHandRot;
        public Vector3 rHandPos;
        public Quaternion rHandRot;
        public Vector3 pelvisPos;
        public Quaternion pelvisRot;
        public Vector3 lFootPos;
        public Quaternion lFootRot;
        public Vector3 rFootPos;
        public Quaternion rFootRot;
        public byte playerId;

        public OtherPlayerPositionMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();

            headPos = msg.ReadVector3();
            lHandPos = msg.ReadVector3();
            rHandPos = msg.ReadVector3();
            pelvisPos = msg.ReadVector3();
            lFootPos = msg.ReadVector3();
            rFootPos = msg.ReadVector3();   

            headRot = msg.ReadQuaternion();
            lHandRot = msg.ReadQuaternion();
            rHandRot = msg.ReadQuaternion();
            pelvisRot = msg.ReadQuaternion();
            lFootRot = msg.ReadQuaternion();
            rFootRot = msg.ReadQuaternion();
        }

        public OtherPlayerPositionMessage()
        { }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.OtherPlayerPosition);
            msg.WriteByte(playerId);

            msg.WriteVector3(headPos);
            msg.WriteVector3(lHandPos);
            msg.WriteVector3(rHandPos);
            msg.WriteVector3(pelvisPos);

            msg.WriteQuaternion(headRot);
            msg.WriteQuaternion(lHandRot);
            msg.WriteQuaternion(rHandRot);
            msg.WriteQuaternion(pelvisRot);
            return msg;
        }

        public int GetSize()
        {
            return (sizeof(float) * 3 * 4) + (sizeof(byte) * 2) + (sizeof(float) * 4 * 4);
        }
    }

    // Client player -> server
    public class PlayerPositionMessage : INetworkMessage
    {
        public Vector3 headPos;
        public Quaternion headRot;
        public Vector3 lHandPos;
        public Quaternion lHandRot;
        public Vector3 rHandPos;
        public Quaternion rHandRot;
        public Vector3 pelvisPos;
        public Quaternion pelvisRot;

        public PlayerPositionMessage(P2PMessage msg)
        {
            headPos = msg.ReadVector3();
            lHandPos = msg.ReadVector3();
            rHandPos = msg.ReadVector3();
            pelvisPos = msg.ReadVector3();

            headRot = msg.ReadQuaternion();
            lHandRot = msg.ReadQuaternion();
            rHandRot = msg.ReadQuaternion();
            pelvisRot = msg.ReadQuaternion();
        }

        public PlayerPositionMessage()
        { }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.PlayerPosition);
            msg.WriteVector3(headPos);
            msg.WriteVector3(lHandPos);
            msg.WriteVector3(rHandPos);
            msg.WriteVector3(pelvisPos);
            msg.WriteQuaternion(headRot);
            msg.WriteQuaternion(lHandRot);
            msg.WriteQuaternion(rHandRot);
            msg.WriteQuaternion(pelvisRot);
            return msg;
        }

        public int GetSize()
        {
            return (sizeof(float) * 3 * 4) + sizeof(byte) + (sizeof(float) * 4 * 4);
        }
    }
}
