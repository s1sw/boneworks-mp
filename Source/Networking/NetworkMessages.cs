using Facepunch.Steamworks;
using MultiplayerMod.Boneworks;
using MultiplayerMod.MonoBehaviours;
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
        SetLocalSmallId,
        PoolSpawn,
        PlayerDamage
    }

    public interface INetworkMessage
    {
        P2PMessage MakeMsg();
    }

    public struct GunFireInfo
    {
        public byte handedness;
        public Vector3 firepointPos;
        public Quaternion firepointRotation;
        public float ammoDamage;
        public float projectileMass;
        public float exitVelocity;
        public float muzzleVelocity;
        public byte cartridgeType;

        public GunFireInfo(P2PMessage msg)
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

        public void WriteToMessage(P2PMessage msg)
        {
            msg.WriteByte(handedness);
            msg.WriteVector3(firepointPos);
            msg.WriteQuaternion(firepointRotation);
            msg.WriteFloat(ammoDamage);
            msg.WriteFloat(projectileMass);
            msg.WriteFloat(exitVelocity);
            msg.WriteFloat(muzzleVelocity);
            msg.WriteByte(cartridgeType);
        }
    }

    public struct GunFireMessage : INetworkMessage
    {
        public GunFireInfo fireInfo;

        public GunFireMessage(P2PMessage msg)
        {
            fireInfo = new GunFireInfo(msg);
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.GunFire);
            fireInfo.WriteToMessage(msg);
            return msg;
        }
    }

    public struct GunFireMessageOther : INetworkMessage
    {
        public byte playerId;
        public GunFireInfo fireInfo;

        public GunFireMessageOther(P2PMessage msg)
        {
            playerId = msg.ReadByte();

            fireInfo = new GunFireInfo(msg);
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.GunFire);
            msg.WriteByte(playerId);

            fireInfo.WriteToMessage(msg);
            return msg;
        }
    }

    // Server -> clients
    public struct OtherPlayerPositionMessage : INetworkMessage
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
    public struct PlayerPositionMessage : INetworkMessage
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

    public struct SceneTransitionMessage : INetworkMessage
    {
        public string sceneName;

        public SceneTransitionMessage(P2PMessage msg)
        {
            sceneName = msg.ReadUnicodeString();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.SceneTransition);
            msg.WriteUnicodeString(sceneName);
            return msg;
        }
    }

    public struct PlayerNameMessage : INetworkMessage
    {
        public string name;

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

    public struct OtherPlayerNameMessage : INetworkMessage
    {
        public byte playerId;
        public string name;

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

    public struct ClientJoinMessage : INetworkMessage
    {
        public byte playerId;
        public string name;
        public SteamId steamId;

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

    public struct RigTransforms
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

        public RigTransforms(P2PMessage msg)
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

        public void WriteToMessage(P2PMessage msg)
        {
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
        }
    }

    public struct FullRigTransformMessage : INetworkMessage
    {
        public RigTransforms transforms;

        public FullRigTransformMessage(P2PMessage msg)
        {
            transforms = new RigTransforms(msg);
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.FullRig);
            transforms.WriteToMessage(msg);

            return msg;
        }
    }

    public struct OtherFullRigTransformMessage : INetworkMessage
    {
        public byte playerId;
        public RigTransforms transforms;

        public OtherFullRigTransformMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();
            transforms = new RigTransforms(msg);
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.OtherFullRig);
            msg.WriteByte(playerId);
            transforms.WriteToMessage(msg);

            return msg;
        }
    }

    public struct EnemyRigTransformMessage : INetworkMessage
    {
        public byte poolChildIdx;
        public EnemyType enemyType;
        public RigTransforms transforms;

        public EnemyRigTransformMessage(P2PMessage msg)
        {
            poolChildIdx = msg.ReadByte();
            enemyType = (EnemyType)msg.ReadByte();
            transforms = new RigTransforms(msg);
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.EnemyRigTransform);
            msg.WriteByte(poolChildIdx);
            msg.WriteByte((byte)enemyType);
            transforms.WriteToMessage(msg);

            return msg;
        }
    }

    public struct SetPartyIdMessage : INetworkMessage
    {
        public string partyId;

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
    
    public struct IDAllocationMessage : INetworkMessage
    {
        public string namePath;
        public ushort allocatedId;
        public byte initialOwner;
        public OwnershipPriorityLevel initialPriority;

        public IDAllocationMessage(P2PMessage msg)
        {
            namePath = msg.ReadUnicodeString();
            allocatedId = msg.ReadUShort();
            initialOwner = msg.ReadByte();
            initialPriority = (OwnershipPriorityLevel)msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.IdAllocation);
            msg.WriteUnicodeString(namePath);
            msg.WriteUShort(allocatedId);
            msg.WriteByte(initialOwner);
            msg.WriteByte((byte)initialPriority);

            return msg;
        }
    }

    public struct IDRequestMessage : INetworkMessage
    {
        public string namePath;
        public byte initialOwner;
        public OwnershipPriorityLevel priorityLevel;

        public IDRequestMessage(P2PMessage msg)
        {
            namePath = msg.ReadUnicodeString();
            initialOwner = msg.ReadByte();
            priorityLevel = (OwnershipPriorityLevel)msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.IdRequest);
            msg.WriteUnicodeString(namePath);
            msg.WriteByte(initialOwner);
            msg.WriteByte((byte)priorityLevel);

            return msg;
        }
    }

    public struct ObjectSyncMessage : INetworkMessage
    {
        public ushort id;
        public Vector3 position;
        public Quaternion rotation;

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

    public struct ChangeObjectOwnershipMessage : INetworkMessage
    {
        public ushort objectId;
        public byte ownerId;
        public Vector3 linVelocity;
        public Vector3 angVelocity;

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

    public struct SetLocalSmallIdMessage : INetworkMessage
    {
        public byte smallId;

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

    public struct PoolSpawnMessage : INetworkMessage
    {
        public string poolId;
        public Vector3 position;
        public Quaternion rotation;

        public PoolSpawnMessage(P2PMessage msg)
        {
            poolId = msg.ReadUnicodeString();
            position = msg.ReadVector3();
            rotation = msg.ReadQuaternion();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.PoolSpawn);
            msg.WriteUnicodeString(poolId);
            msg.WriteVector3(position);
            msg.WriteQuaternion(rotation);

            return msg;
        }
    }

    public struct PlayerDamageMessage : INetworkMessage
    {
        public byte playerId;
        public float damage;

        public PlayerDamageMessage(P2PMessage msg)
        {
            playerId = msg.ReadByte();
            damage = msg.ReadFloat();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.PlayerDamage);
            msg.WriteByte(playerId);
            msg.WriteFloat(damage);

            return msg;
        }
    }
}
