using Facepunch.Steamworks;
using MultiplayerMod.Boneworks;
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

using Convert = System.Convert;

namespace MultiplayerMod.Networking
{
    public enum MessageType
    {
        Join,
        PlayerName,
        OtherPlayerName,
        PlayerPosition,
        OtherPlayerPosition,
        Disconnect,
        ServerShutdown,
        JoinRejected,
        SceneTransition,
        FullRig,
        OtherFullRig,
        HandGunChange,
        OtherHandGunChange,
        SetPartyId,
        EnemyRigTransform,
        Attack,
        SetServerSetting,
        IdAllocation,
        IdRequest,
        ObjectSync,
        GunFire,
        GunFireHit,
        Death,
        ChangeObjectOwnership,
        SetLocalSmallId
    }

    public interface INetworkMessage
    {
        P2PMessage MakeMsg();
    }

    public class GunFireBase
    {
        public byte handedness;
        public Vector3 firepointPos;
        public Quaternion firepointRotation;
        public float ammoDamage;
        public float projectileMass;
        public float exitVelocity;
        public float muzzleVelocity;
        public byte cartridgeType;
    }

    public class GunFireMessage : GunFireBase, INetworkMessage
    {
        public GunFireMessage()
        {
            //fortnite fortnite fornitet fnioretneit a
        }

        public GunFireMessage(P2PMessage msg)
        {
            handedness = msg.ReadByte();
            firepointPos = msg.ReadVector3();
            firepointRotation = msg.ReadQuaternion();
            ammoDamage = msg.ReadFloat();
            projectileMass = msg.ReadFloat();
            exitVelocity = msg.ReadFloat();
            muzzleVelocity = msg.ReadFloat();
            cartridgeType = msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.GunFire);
            msg.WriteByte(handedness);
            msg.WriteVector3(firepointPos);
            msg.WriteQuaternion(firepointRotation);
            msg.WriteFloat(ammoDamage);
            msg.WriteFloat(projectileMass);
            msg.WriteFloat(exitVelocity);
            msg.WriteFloat(muzzleVelocity);
            msg.WriteByte(cartridgeType);
            return msg;
        }
    }

    public class GunFireHitToServer : INetworkMessage
    {
        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.GunFireHit);
            return msg;
        }
    }

    public class GunFireMessageOther : GunFireBase, INetworkMessage
    {
        public byte playerId;
        public GunFireMessageOther()
        {

        }

        public GunFireMessageOther(P2PMessage msg)
        {
            handedness = msg.ReadByte();
            playerId = msg.ReadByte();
            firepointPos = msg.ReadVector3();
            firepointRotation = msg.ReadQuaternion();
            ammoDamage = msg.ReadFloat();
            projectileMass = msg.ReadFloat();
            exitVelocity = msg.ReadFloat();
            muzzleVelocity = msg.ReadFloat();
            cartridgeType = msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.GunFire);
            msg.WriteByte(handedness);
            msg.WriteByte(playerId);
            msg.WriteVector3(firepointPos);
            msg.WriteQuaternion(firepointRotation);
            msg.WriteFloat(ammoDamage);
            msg.WriteFloat(projectileMass);
            msg.WriteFloat(exitVelocity);
            msg.WriteFloat(muzzleVelocity);
            msg.WriteByte(cartridgeType);
            return msg;
        }
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
        public string sceneName;

        public SceneTransitionMessage(P2PMessage msg)
        {
            sceneName = msg.ReadUnicodeString();
        }

        public SceneTransitionMessage()
        {

        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.SceneTransition);
            msg.WriteUnicodeString(sceneName);
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

            rotMain = msg.ReadSmallerCompressedQuaternion();
            rotRoot = msg.ReadSmallerCompressedQuaternion();
            rotLHip = msg.ReadSmallerCompressedQuaternion();
            rotRHip = msg.ReadSmallerCompressedQuaternion();
            rotLKnee = msg.ReadSmallerCompressedQuaternion();
            rotRKnee = msg.ReadSmallerCompressedQuaternion();
            rotLAnkle = msg.ReadSmallerCompressedQuaternion();
            rotRAnkle = msg.ReadSmallerCompressedQuaternion();
            rotSpine1 = msg.ReadSmallerCompressedQuaternion();
            rotSpine2 = msg.ReadSmallerCompressedQuaternion();
            rotSpineTop = msg.ReadSmallerCompressedQuaternion();
            rotLClavicle = msg.ReadSmallerCompressedQuaternion();
            rotRClavicle = msg.ReadSmallerCompressedQuaternion();
            rotNeck = msg.ReadSmallerCompressedQuaternion();
            rotLShoulder = msg.ReadSmallerCompressedQuaternion();
            rotRShoulder = msg.ReadSmallerCompressedQuaternion();
            rotLElbow = msg.ReadSmallerCompressedQuaternion();
            rotRElbow = msg.ReadSmallerCompressedQuaternion();
            rotLWrist = msg.ReadSmallerCompressedQuaternion();
            rotRWrist = msg.ReadSmallerCompressedQuaternion();
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

            msg.WriteSmallerCompressedQuaternion(rotMain);
            msg.WriteSmallerCompressedQuaternion(rotRoot);
            msg.WriteSmallerCompressedQuaternion(rotLHip);
            msg.WriteSmallerCompressedQuaternion(rotRHip);
            msg.WriteSmallerCompressedQuaternion(rotLKnee);
            msg.WriteSmallerCompressedQuaternion(rotRKnee);
            msg.WriteSmallerCompressedQuaternion(rotLAnkle);
            msg.WriteSmallerCompressedQuaternion(rotRAnkle);

            msg.WriteSmallerCompressedQuaternion(rotSpine1);
            msg.WriteSmallerCompressedQuaternion(rotSpine2);
            msg.WriteSmallerCompressedQuaternion(rotSpineTop);
            msg.WriteSmallerCompressedQuaternion(rotLClavicle);
            msg.WriteSmallerCompressedQuaternion(rotRClavicle);
            msg.WriteSmallerCompressedQuaternion(rotNeck);
            msg.WriteSmallerCompressedQuaternion(rotLShoulder);
            msg.WriteSmallerCompressedQuaternion(rotRShoulder);
            msg.WriteSmallerCompressedQuaternion(rotLElbow);
            msg.WriteSmallerCompressedQuaternion(rotRElbow);
            msg.WriteSmallerCompressedQuaternion(rotLWrist);
            msg.WriteSmallerCompressedQuaternion(rotRWrist);

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

            rotMain = msg.ReadSmallerCompressedQuaternion();
            rotRoot = msg.ReadSmallerCompressedQuaternion();
            rotLHip = msg.ReadSmallerCompressedQuaternion();
            rotRHip = msg.ReadSmallerCompressedQuaternion();
            rotLKnee = msg.ReadSmallerCompressedQuaternion();
            rotRKnee = msg.ReadSmallerCompressedQuaternion();
            rotLAnkle = msg.ReadSmallerCompressedQuaternion();
            rotRAnkle = msg.ReadSmallerCompressedQuaternion();
            rotSpine1 = msg.ReadSmallerCompressedQuaternion();
            rotSpine2 = msg.ReadSmallerCompressedQuaternion();
            rotSpineTop = msg.ReadSmallerCompressedQuaternion();
            rotLClavicle = msg.ReadSmallerCompressedQuaternion();
            rotRClavicle = msg.ReadSmallerCompressedQuaternion();
            rotNeck = msg.ReadSmallerCompressedQuaternion();
            rotLShoulder = msg.ReadSmallerCompressedQuaternion();
            rotRShoulder = msg.ReadSmallerCompressedQuaternion();
            rotLElbow = msg.ReadSmallerCompressedQuaternion();
            rotRElbow = msg.ReadSmallerCompressedQuaternion();
            rotLWrist = msg.ReadSmallerCompressedQuaternion();
            rotRWrist = msg.ReadSmallerCompressedQuaternion();
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

            msg.WriteSmallerCompressedQuaternion(rotMain);
            msg.WriteSmallerCompressedQuaternion(rotRoot);
            msg.WriteSmallerCompressedQuaternion(rotLHip);
            msg.WriteSmallerCompressedQuaternion(rotRHip);
            msg.WriteSmallerCompressedQuaternion(rotLKnee);
            msg.WriteSmallerCompressedQuaternion(rotRKnee);
            msg.WriteSmallerCompressedQuaternion(rotLAnkle);
            msg.WriteSmallerCompressedQuaternion(rotRAnkle);

            msg.WriteSmallerCompressedQuaternion(rotSpine1);
            msg.WriteSmallerCompressedQuaternion(rotSpine2);
            msg.WriteSmallerCompressedQuaternion(rotSpineTop);
            msg.WriteSmallerCompressedQuaternion(rotLClavicle);
            msg.WriteSmallerCompressedQuaternion(rotRClavicle);
            msg.WriteSmallerCompressedQuaternion(rotNeck);
            msg.WriteSmallerCompressedQuaternion(rotLShoulder);
            msg.WriteSmallerCompressedQuaternion(rotRShoulder);
            msg.WriteSmallerCompressedQuaternion(rotLElbow);
            msg.WriteSmallerCompressedQuaternion(rotRElbow);
            msg.WriteSmallerCompressedQuaternion(rotLWrist);
            msg.WriteSmallerCompressedQuaternion(rotRWrist);

            return msg;
        }
    }

    public class EnemyRigTransformMessage : RigTFMsgBase, INetworkMessage
    {
        public byte poolChildIdx;
        public EnemyType enemyType;

        public EnemyRigTransformMessage()
        {

        }

        public EnemyRigTransformMessage(P2PMessage msg)
        {
            poolChildIdx = msg.ReadByte();
            enemyType = (EnemyType)msg.ReadByte();
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

            rotMain = msg.ReadSmallerCompressedQuaternion();
            rotRoot = msg.ReadSmallerCompressedQuaternion();
            rotLHip = msg.ReadSmallerCompressedQuaternion();
            rotRHip = msg.ReadSmallerCompressedQuaternion();
            rotLKnee = msg.ReadSmallerCompressedQuaternion();
            rotRKnee = msg.ReadSmallerCompressedQuaternion();
            rotLAnkle = msg.ReadSmallerCompressedQuaternion();
            rotRAnkle = msg.ReadSmallerCompressedQuaternion();
            rotSpine1 = msg.ReadSmallerCompressedQuaternion();
            rotSpine2 = msg.ReadSmallerCompressedQuaternion();
            rotSpineTop = msg.ReadSmallerCompressedQuaternion();
            rotLClavicle = msg.ReadSmallerCompressedQuaternion();
            rotRClavicle = msg.ReadSmallerCompressedQuaternion();
            rotNeck = msg.ReadSmallerCompressedQuaternion();
            rotLShoulder = msg.ReadSmallerCompressedQuaternion();
            rotRShoulder = msg.ReadSmallerCompressedQuaternion();
            rotLElbow = msg.ReadSmallerCompressedQuaternion();
            rotRElbow = msg.ReadSmallerCompressedQuaternion();
            rotLWrist = msg.ReadSmallerCompressedQuaternion();
            rotRWrist = msg.ReadSmallerCompressedQuaternion();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.EnemyRigTransform);
            msg.WriteByte(poolChildIdx);
            msg.WriteByte((byte)enemyType);
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

            msg.WriteSmallerCompressedQuaternion(rotMain);
            msg.WriteSmallerCompressedQuaternion(rotRoot);
            msg.WriteSmallerCompressedQuaternion(rotLHip);
            msg.WriteSmallerCompressedQuaternion(rotRHip);
            msg.WriteSmallerCompressedQuaternion(rotLKnee);
            msg.WriteSmallerCompressedQuaternion(rotRKnee);
            msg.WriteSmallerCompressedQuaternion(rotLAnkle);
            msg.WriteSmallerCompressedQuaternion(rotRAnkle);

            msg.WriteSmallerCompressedQuaternion(rotSpine1);
            msg.WriteSmallerCompressedQuaternion(rotSpine2);
            msg.WriteSmallerCompressedQuaternion(rotSpineTop);
            msg.WriteSmallerCompressedQuaternion(rotLClavicle);
            msg.WriteSmallerCompressedQuaternion(rotRClavicle);
            msg.WriteSmallerCompressedQuaternion(rotNeck);
            msg.WriteSmallerCompressedQuaternion(rotLShoulder);
            msg.WriteSmallerCompressedQuaternion(rotRShoulder);
            msg.WriteSmallerCompressedQuaternion(rotLElbow);
            msg.WriteSmallerCompressedQuaternion(rotRElbow);
            msg.WriteSmallerCompressedQuaternion(rotLWrist);
            msg.WriteSmallerCompressedQuaternion(rotRWrist);

            return msg;
        }
    }

    public class SetPartyIdMessage : INetworkMessage
    {
        public string partyId;

        public SetPartyIdMessage()
        {

        }

        public SetPartyIdMessage(P2PMessage msg)
        {
            partyId = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.SetPartyId);
            msg.WriteUnicodeString(partyId);
            return msg;
        }
    }
    
    public class IDAllocationMessage : INetworkMessage
    {
        public string namePath;
        public ushort allocatedId;
        public byte initialOwner;

        public IDAllocationMessage()
        {

        }

        public IDAllocationMessage(P2PMessage msg)
        {
            namePath = msg.ReadUnicodeString();
            allocatedId = msg.ReadUShort();
            initialOwner = msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.IdAllocation);
            msg.WriteUnicodeString(namePath);
            msg.WriteUShort(allocatedId);
            msg.WriteByte(initialOwner);

            return msg;
        }
    }

    public class IDRequestMessage : INetworkMessage
    {
        public string namePath;
        public byte initialOwner;

        public IDRequestMessage()
        {

        }

        public IDRequestMessage(P2PMessage msg)
        {
            namePath = msg.ReadUnicodeString();
            initialOwner = msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.IdRequest);
            msg.WriteUnicodeString(namePath);
            msg.WriteByte(initialOwner);

            return msg;
        }
    }

    public class ObjectSyncMessage : INetworkMessage
    {
        public ushort id;
        public Vector3 position;
        public Quaternion rotation;

        public ObjectSyncMessage()
        { }

        public ObjectSyncMessage(P2PMessage msg)
        {
            id = msg.ReadUShort();
            position = msg.ReadVector3();
            rotation = msg.ReadCompressedQuaternion();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.ObjectSync);
            msg.WriteUShort(id);
            msg.WriteVector3(position);
            msg.WriteCompressedQuaternion(rotation);

            return msg;
        }
    }

    public class ChangeObjectOwnershipMessage : INetworkMessage
    {
        public ushort objectId;
        public byte ownerId;
        public Vector3 linVelocity;
        public Vector3 angVelocity;

        public ChangeObjectOwnershipMessage()
        { }

        public ChangeObjectOwnershipMessage(P2PMessage msg)
        {
            objectId = msg.ReadUShort();
            ownerId = msg.ReadByte();
            linVelocity = msg.ReadVector3();
            angVelocity = msg.ReadVector3();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.ChangeObjectOwnership);
            msg.WriteUShort(objectId);
            msg.WriteByte(ownerId);
            msg.WriteVector3(linVelocity);
            msg.WriteVector3(angVelocity);
            return msg;
        }
    }

    public class SetLocalSmallIdMessage : INetworkMessage
    {
        public byte smallId;

        public SetLocalSmallIdMessage() { }
        public SetLocalSmallIdMessage(P2PMessage msg)
        {
            smallId = msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.SetLocalSmallId);
            msg.WriteByte(smallId);
            return msg;
        }
    }
}
