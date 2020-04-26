using Facepunch.Steamworks;
using Oculus.Platform;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MultiplayerMod
{
    public interface INetworkMessage
    {
        P2PMessage MakeMsg();
    }

    // Server -> clients
    public class OtherPlayerPositionMessage : INetworkMessage
    {
        public Vector3 headPos;
        public Vector3 lHandPos;
        public Vector3 rHandPos;
        public Vector3 pelvisPos;
        public Vector3 lFootPos;
        public Vector3 rFootPos;

        public Quaternion headRot;
        public Quaternion lHandRot;
        public Quaternion rHandRot;
        public Quaternion pelvisRot;
        public Quaternion lFootRot;
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

            headRot = msg.ReadCompressedQuaternion();
            lHandRot = msg.ReadCompressedQuaternion();
            rHandRot = msg.ReadCompressedQuaternion();
            pelvisRot = msg.ReadCompressedQuaternion();
            lFootRot = msg.ReadCompressedQuaternion();
            rFootRot = msg.ReadCompressedQuaternion();
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
            msg.WriteVector3(lFootPos);
            msg.WriteVector3(rFootPos);

            msg.WriteCompressedQuaternion(headRot);
            msg.WriteCompressedQuaternion(lHandRot);
            msg.WriteCompressedQuaternion(rHandRot);
            msg.WriteCompressedQuaternion(pelvisRot);
            msg.WriteCompressedQuaternion(lFootRot);
            msg.WriteCompressedQuaternion(rFootRot);
            return msg;
        }
    }

    // Client player -> server
    public class PlayerPositionMessage : INetworkMessage
    {
        public Vector3 headPos;
        public Vector3 lHandPos;
        public Vector3 rHandPos;
        public Vector3 pelvisPos;
        public Vector3 lFootPos;
        public Vector3 rFootPos;

        public Quaternion headRot;
        public Quaternion lHandRot;
        public Quaternion rHandRot;
        public Quaternion pelvisRot;
        public Quaternion lFootRot;
        public Quaternion rFootRot;

        public PlayerPositionMessage(P2PMessage msg)
        {
            headPos = msg.ReadVector3();
            lHandPos = msg.ReadVector3();
            rHandPos = msg.ReadVector3();
            pelvisPos = msg.ReadVector3();
            lFootPos = msg.ReadVector3();
            rFootPos = msg.ReadVector3();

            headRot = msg.ReadCompressedQuaternion();
            lHandRot = msg.ReadCompressedQuaternion();
            rHandRot = msg.ReadCompressedQuaternion();
            pelvisRot = msg.ReadCompressedQuaternion();
            lFootRot = msg.ReadCompressedQuaternion();
            rFootRot = msg.ReadCompressedQuaternion();
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
            msg.WriteVector3(lFootPos);
            msg.WriteVector3(rFootPos);

            msg.WriteCompressedQuaternion(headRot);
            msg.WriteCompressedQuaternion(lHandRot);
            msg.WriteCompressedQuaternion(rHandRot);
            msg.WriteCompressedQuaternion(pelvisRot);
            msg.WriteCompressedQuaternion(lFootRot);
            msg.WriteCompressedQuaternion(rFootRot);
            return msg;
        }
    }

    public class SceneTransitionMessage : INetworkMessage
    {
        public byte sceneByte;

        public SceneTransitionMessage(P2PMessage msg)
        {
            sceneByte = msg.ReadByte();
        }

        public SceneTransitionMessage()
        {

        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.SceneTransition);
            msg.WriteByte(sceneByte);
            return msg;
        }
    }

    public class PlayerNameMessage : INetworkMessage
    {
        public string name;

        public PlayerNameMessage()
        {

        }

        public PlayerNameMessage(P2PMessage msg)
        {
            name = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.PlayerName);
            msg.WriteUnicodeString(name);
            return msg;
        }
    }

    public class OtherPlayerNameMessage : INetworkMessage
    {
        public byte playerId;
        public string name;

        public OtherPlayerNameMessage()
        {

        }

        public OtherPlayerNameMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();
            name = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.OtherPlayerName);
            msg.WriteByte(playerId);
            msg.WriteUnicodeString(name);
            return msg;
        }
    }

    public class ClientJoinMessage : INetworkMessage
    {
        public byte playerId;
        public string name;
        public SteamId steamId;

        public ClientJoinMessage()
        {

        }

        public ClientJoinMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();
            steamId.Value = msg.ReadUlong();
            name = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.Join);
            msg.WriteByte(playerId);
            msg.WriteUlong(steamId.Value);
            msg.WriteUnicodeString(name);
            return msg;
        }
    }

    public class RigTFMsgBase
    {
        public Vector3 posMain;
        public Vector3 posRoot;
        public Vector3 posLHip;
        public Vector3 posRHip;
        public Vector3 posLKnee;
        public Vector3 posRKnee;
        public Vector3 posLAnkle;
        public Vector3 posRAnkle;

        public Vector3 posSpine1;
        public Vector3 posSpine2;
        public Vector3 posSpineTop;
        public Vector3 posLClavicle;
        public Vector3 posRClavicle;
        public Vector3 posNeck;
        public Vector3 posLShoulder;
        public Vector3 posRShoulder;

        public Vector3 posLElbow;
        public Vector3 posRElbow;

        public Vector3 posLWrist;
        public Vector3 posRWrist;

        public Quaternion rotMain;
        public Quaternion rotRoot;
        public Quaternion rotLHip;
        public Quaternion rotRHip;
        public Quaternion rotLKnee;
        public Quaternion rotRKnee;
        public Quaternion rotLAnkle;
        public Quaternion rotRAnkle;
        public Quaternion rotSpine1;
        public Quaternion rotSpine2;
        public Quaternion rotSpineTop;
        public Quaternion rotLClavicle;
        public Quaternion rotRClavicle;
        public Quaternion rotNeck;
        public Quaternion rotLShoulder;
        public Quaternion rotRShoulder;
        public Quaternion rotLElbow;
        public Quaternion rotRElbow;
        public Quaternion rotLWrist;
        public Quaternion rotRWrist;
    }


    public class FullRigTransformMessage : RigTFMsgBase, INetworkMessage
    {

        public FullRigTransformMessage()
        {

        }

        public FullRigTransformMessage(P2PMessage msg)
        {
            posMain = msg.ReadVector3();
            posRoot = msg.ReadVector3();

            posLHip = msg.ReadCompressedVector3(posRoot);
            posRHip = msg.ReadCompressedVector3(posRoot);

            posLKnee = msg.ReadCompressedVector3(posRoot);
            posRKnee = msg.ReadCompressedVector3(posRoot);
            posLAnkle = msg.ReadCompressedVector3(posRoot);
            posRAnkle = msg.ReadCompressedVector3(posRoot);

            posSpine1 = msg.ReadCompressedVector3(posRoot);
            posSpine2 = msg.ReadCompressedVector3(posRoot);
            posSpineTop = msg.ReadCompressedVector3(posRoot);
            posLClavicle = msg.ReadCompressedVector3(posRoot);
            posRClavicle = msg.ReadCompressedVector3(posRoot);
            posNeck = msg.ReadCompressedVector3(posRoot);
            posLShoulder = msg.ReadCompressedVector3(posRoot);
            posRShoulder = msg.ReadCompressedVector3(posRoot);
            posLElbow = msg.ReadCompressedVector3(posRoot);
            posRElbow = msg.ReadCompressedVector3(posRoot);
            posLWrist = msg.ReadCompressedVector3(posRoot);
            posRWrist = msg.ReadCompressedVector3(posRoot);

            rotMain = msg.ReadCompressedQuaternion();
            rotRoot = msg.ReadCompressedQuaternion();
            rotLHip = msg.ReadCompressedQuaternion();
            rotRHip = msg.ReadCompressedQuaternion();
            rotLKnee = msg.ReadCompressedQuaternion();
            rotRKnee = msg.ReadCompressedQuaternion();
            rotLAnkle = msg.ReadCompressedQuaternion();
            rotRAnkle = msg.ReadCompressedQuaternion();
            rotSpine1 = msg.ReadCompressedQuaternion();
            rotSpine2 = msg.ReadCompressedQuaternion();
            rotSpineTop = msg.ReadCompressedQuaternion();
            rotLClavicle = msg.ReadCompressedQuaternion();
            rotRClavicle = msg.ReadCompressedQuaternion();
            rotNeck = msg.ReadCompressedQuaternion();
            rotLShoulder = msg.ReadCompressedQuaternion();
            rotRShoulder = msg.ReadCompressedQuaternion();
            rotLElbow = msg.ReadCompressedQuaternion();
            rotRElbow = msg.ReadCompressedQuaternion();
            rotLWrist = msg.ReadCompressedQuaternion();
            rotRWrist = msg.ReadCompressedQuaternion();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.FullRig);
            msg.WriteVector3(posMain);
            msg.WriteVector3(posRoot);

            msg.WriteCompressedVector3(posLHip, posRoot);
            msg.WriteCompressedVector3(posRHip, posRoot);
            msg.WriteCompressedVector3(posLKnee, posRoot);
            msg.WriteCompressedVector3(posRKnee, posRoot);
            msg.WriteCompressedVector3(posLAnkle, posRoot);
            msg.WriteCompressedVector3(posRAnkle, posRoot);

            msg.WriteCompressedVector3(posSpine1, posRoot);
            msg.WriteCompressedVector3(posSpine2, posRoot);
            msg.WriteCompressedVector3(posSpineTop, posRoot);
            msg.WriteCompressedVector3(posLClavicle, posRoot);
            msg.WriteCompressedVector3(posRClavicle, posRoot);
            msg.WriteCompressedVector3(posNeck, posRoot);
            msg.WriteCompressedVector3(posLShoulder, posRoot);
            msg.WriteCompressedVector3(posRShoulder, posRoot);
            msg.WriteCompressedVector3(posLElbow, posRoot);
            msg.WriteCompressedVector3(posRElbow, posRoot);
            msg.WriteCompressedVector3(posLWrist, posRoot);
            msg.WriteCompressedVector3(posRWrist, posRoot);

            msg.WriteCompressedQuaternion(rotMain);
            msg.WriteCompressedQuaternion(rotRoot);
            msg.WriteCompressedQuaternion(rotLHip);
            msg.WriteCompressedQuaternion(rotRHip);
            msg.WriteCompressedQuaternion(rotLKnee);
            msg.WriteCompressedQuaternion(rotRKnee);
            msg.WriteCompressedQuaternion(rotLAnkle);
            msg.WriteCompressedQuaternion(rotRAnkle);

            msg.WriteCompressedQuaternion(rotSpine1);
            msg.WriteCompressedQuaternion(rotSpine2);
            msg.WriteCompressedQuaternion(rotSpineTop);
            msg.WriteCompressedQuaternion(rotLClavicle);
            msg.WriteCompressedQuaternion(rotRClavicle);
            msg.WriteCompressedQuaternion(rotNeck);
            msg.WriteCompressedQuaternion(rotLShoulder);
            msg.WriteCompressedQuaternion(rotRShoulder);
            msg.WriteCompressedQuaternion(rotLElbow);
            msg.WriteCompressedQuaternion(rotRElbow);
            msg.WriteCompressedQuaternion(rotLWrist);
            msg.WriteCompressedQuaternion(rotRWrist);

            return msg;
        }
    }

    public class OtherFullRigTransformMessage : RigTFMsgBase, INetworkMessage
    {
        public byte playerId;

        public OtherFullRigTransformMessage()
        {

        }

        public OtherFullRigTransformMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();
            posMain = msg.ReadVector3();
            posRoot = msg.ReadVector3();
            posLHip = msg.ReadCompressedVector3(posRoot);
            posRHip = msg.ReadCompressedVector3(posRoot);

            posLKnee = msg.ReadCompressedVector3(posRoot);
            posRKnee = msg.ReadCompressedVector3(posRoot);
            posLAnkle = msg.ReadCompressedVector3(posRoot);
            posRAnkle = msg.ReadCompressedVector3(posRoot);

            posSpine1 = msg.ReadCompressedVector3(posRoot);
            posSpine2 = msg.ReadCompressedVector3(posRoot);
            posSpineTop = msg.ReadCompressedVector3(posRoot);
            posLClavicle = msg.ReadCompressedVector3(posRoot);
            posRClavicle = msg.ReadCompressedVector3(posRoot);
            posNeck = msg.ReadCompressedVector3(posRoot);
            posLShoulder = msg.ReadCompressedVector3(posRoot);
            posRShoulder = msg.ReadCompressedVector3(posRoot);
            posLElbow = msg.ReadCompressedVector3(posRoot);
            posRElbow = msg.ReadCompressedVector3(posRoot);
            posLWrist = msg.ReadCompressedVector3(posRoot);
            posRWrist = msg.ReadCompressedVector3(posRoot);

            rotMain = msg.ReadCompressedQuaternion();
            rotRoot = msg.ReadCompressedQuaternion();
            rotLHip = msg.ReadCompressedQuaternion();
            rotRHip = msg.ReadCompressedQuaternion();
            rotLKnee = msg.ReadCompressedQuaternion();
            rotRKnee = msg.ReadCompressedQuaternion();
            rotLAnkle = msg.ReadCompressedQuaternion();
            rotRAnkle = msg.ReadCompressedQuaternion();
            rotSpine1 = msg.ReadCompressedQuaternion();
            rotSpine2 = msg.ReadCompressedQuaternion();
            rotSpineTop = msg.ReadCompressedQuaternion();
            rotLClavicle = msg.ReadCompressedQuaternion();
            rotRClavicle = msg.ReadCompressedQuaternion();
            rotNeck = msg.ReadCompressedQuaternion();
            rotLShoulder = msg.ReadCompressedQuaternion();
            rotRShoulder = msg.ReadCompressedQuaternion();
            rotLElbow = msg.ReadCompressedQuaternion();
            rotRElbow = msg.ReadCompressedQuaternion();
            rotLWrist = msg.ReadCompressedQuaternion();
            rotRWrist = msg.ReadCompressedQuaternion();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.OtherFullRig);
            msg.WriteByte(playerId);
            msg.WriteVector3(posMain);
            msg.WriteVector3(posRoot);

            msg.WriteCompressedVector3(posLHip, posRoot);
            msg.WriteCompressedVector3(posRHip, posRoot);
            msg.WriteCompressedVector3(posLKnee, posRoot);
            msg.WriteCompressedVector3(posRKnee, posRoot);
            msg.WriteCompressedVector3(posLAnkle, posRoot);
            msg.WriteCompressedVector3(posRAnkle, posRoot);

            msg.WriteCompressedVector3(posSpine1, posRoot);
            msg.WriteCompressedVector3(posSpine2, posRoot);
            msg.WriteCompressedVector3(posSpineTop, posRoot);
            msg.WriteCompressedVector3(posLClavicle, posRoot);
            msg.WriteCompressedVector3(posRClavicle, posRoot);
            msg.WriteCompressedVector3(posNeck, posRoot);
            msg.WriteCompressedVector3(posLShoulder, posRoot);
            msg.WriteCompressedVector3(posRShoulder, posRoot);
            msg.WriteCompressedVector3(posLElbow, posRoot);
            msg.WriteCompressedVector3(posRElbow, posRoot);
            msg.WriteCompressedVector3(posLWrist, posRoot);
            msg.WriteCompressedVector3(posRWrist, posRoot);

            msg.WriteCompressedQuaternion(rotMain);
            msg.WriteCompressedQuaternion(rotRoot);
            msg.WriteCompressedQuaternion(rotLHip);
            msg.WriteCompressedQuaternion(rotRHip);
            msg.WriteCompressedQuaternion(rotLKnee);
            msg.WriteCompressedQuaternion(rotRKnee);
            msg.WriteCompressedQuaternion(rotLAnkle);
            msg.WriteCompressedQuaternion(rotRAnkle);

            msg.WriteCompressedQuaternion(rotSpine1);
            msg.WriteCompressedQuaternion(rotSpine2);
            msg.WriteCompressedQuaternion(rotSpineTop);
            msg.WriteCompressedQuaternion(rotLClavicle);
            msg.WriteCompressedQuaternion(rotRClavicle);
            msg.WriteCompressedQuaternion(rotNeck);
            msg.WriteCompressedQuaternion(rotLShoulder);
            msg.WriteCompressedQuaternion(rotRShoulder);
            msg.WriteCompressedQuaternion(rotLElbow);
            msg.WriteCompressedQuaternion(rotRElbow);
            msg.WriteCompressedQuaternion(rotLWrist);
            msg.WriteCompressedQuaternion(rotRWrist);

            return msg;
        }
    }

    public class HandGunChangeMessage : INetworkMessage
    {
        public bool isForOtherPlayer = false;
        public byte playerId;
        public GunType type;
        public bool destroy;

        public HandGunChangeMessage()
        {
            
        }

        public HandGunChangeMessage(P2PMessage msg, bool forOtherPlayer = false)
        {
            isForOtherPlayer = forOtherPlayer;
            if (isForOtherPlayer)
                playerId = msg.ReadByte();

            destroy = Convert.ToBoolean(msg.ReadByte());
            type = (GunType)msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            if (isForOtherPlayer)
            {
                msg.WriteByte((byte)MessageType.OtherHandGunChange);
                msg.WriteByte(playerId);
            }
            else
                msg.WriteByte((byte)MessageType.HandGunChange);

            msg.WriteByte(Convert.ToByte(destroy));
            msg.WriteByte((byte)type);
            return msg;
        }
    }

    //public class HoverJunkerUpdateMessage : INetworkMessage
    //{ 
    //    public HoverJunkerUpdateMessage()
    //    {

    //    }

    //    public HoverJunkerUpdateMessage(P2PMessage msg)
    //    {

    //    }

    //    public P2PMessage MakeMsg()
    //    {
    //        P2PMessage msg = new P2PMessage();
            
    //    }
    //}

}
