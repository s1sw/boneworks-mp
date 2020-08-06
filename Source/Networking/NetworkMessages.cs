using Facepunch.Steamworks;
using Oculus.Platform;
using Oculus.Platform.Models;
using StressLevelZero.Arena;
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
        GunFire,
        GunFireHit,
        ZWWaveStart,
        ZWModeStart,
        ZWDifficultyChange,
        ZWAmmoReward,
        ZWPuppetDeath,
        ZWSetCustomEnemies,
        ZWPlayerDamage,
        ZWSetWave
    }

    public class GunFireBase
    {
        public Vector3 fireOrigin;
        public Vector3 fireDirection;
        public float bulletDamage;
    }

    public class GunFireMessage : GunFireBase, INetworkMessage
    {
        public GunFireMessage()
        {

        }

        public GunFireMessage(P2PMessage msg)
        {
            fireOrigin = msg.ReadVector3();
            fireDirection = msg.ReadVector3();
            bulletDamage = msg.ReadFloat();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.GunFire);
            msg.WriteVector3(fireOrigin);
            msg.WriteVector3(fireDirection);
            msg.WriteFloat(bulletDamage);
            return msg;
        }
    }

    public class GunFireFeedbackToServer : INetworkMessage
    {
        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.GunFireHit);
            return msg;
        }
    }

    public class GunFireFeedbackBase {
        public byte playerId;
    }

    public class GunFireFeedback : GunFireFeedbackBase, INetworkMessage
    {
        public GunFireFeedback()
        {

        }

        public GunFireFeedback(P2PMessage msg)
        {
            playerId = msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.GunFireHit);
            msg.WriteByte(playerId);
            return msg;
        }
    }

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

        public RigTFMsgBase()
        {

        }

        protected void ReadRigTransforms(P2PMessage msg)
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

        protected void WriteRigTransforms(P2PMessage msg)
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

    public class FullRigTransformMessage : RigTFMsgBase, INetworkMessage
    {

        public FullRigTransformMessage()
        {

        }

        public FullRigTransformMessage(P2PMessage msg)
        {
            ReadRigTransforms(msg);
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.FullRig);
            WriteRigTransforms(msg);

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
            ReadRigTransforms(msg);
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.OtherFullRig);
            msg.WriteByte(playerId);
            WriteRigTransforms(msg);

            return msg;
        }
    }

    public class EnemyRigTransformMessage : INetworkMessage
    {
        public byte poolChildIdx;
        public Vector3 rootPos;
        public Vector3 lHipPos;
        public Vector3 rHipPos;
        public Vector3 lKneePos;
        public Vector3 rKneePos;
        public Vector3 lAnklePos;
        public Vector3 rAnklePos;
        public Vector3 spinePos;
        public Vector3 chestPos;
        public Vector3 lShoulderPos;
        public Vector3 rShoulderPos;
        public Vector3 lElbowPos;
        public Vector3 rElbowPos;
        public Vector3 lWristPos;
        public Vector3 rWristPos;

        public Quaternion rootRot;
        public Quaternion lHipRot;
        public Quaternion rHipRot;
        public Quaternion lKneeRot;
        public Quaternion rKneeRot;
        public Quaternion lAnkleRot;
        public Quaternion rAnkleRot;
        public Quaternion spineRot;
        public Quaternion chestRot;
        public Quaternion lShoulderRot;
        public Quaternion rShoulderRot;
        public Quaternion lElbowRot;
        public Quaternion rElbowRot;
        public Quaternion lWristRot;
        public Quaternion rWristRot;

        public EnemyRigTransformMessage()
        {

        }

        public EnemyRigTransformMessage(P2PMessage msg)
        {
            poolChildIdx = msg.ReadByte();

            rootPos = msg.ReadVector3();

            lHipPos = msg.ReadCompressedVector3(rootPos);
            rHipPos = msg.ReadCompressedVector3(rootPos);
            lKneePos = msg.ReadCompressedVector3(rootPos);
            rKneePos = msg.ReadCompressedVector3(rootPos);
            lAnklePos = msg.ReadCompressedVector3(rootPos);
            rAnklePos = msg.ReadCompressedVector3(rootPos);
            spinePos = msg.ReadCompressedVector3(rootPos);
            chestPos = msg.ReadCompressedVector3(rootPos);
            lShoulderPos = msg.ReadCompressedVector3(rootPos);
            rShoulderPos = msg.ReadCompressedVector3(rootPos);
            lElbowPos = msg.ReadCompressedVector3(rootPos);
            rElbowPos = msg.ReadCompressedVector3(rootPos);
            lWristPos = msg.ReadCompressedVector3(rootPos);
            rWristPos = msg.ReadCompressedVector3(rootPos);

            rootRot = msg.ReadSmallerCompressedQuaternion();
            lHipRot = msg.ReadSmallerCompressedQuaternion();
            rHipRot = msg.ReadSmallerCompressedQuaternion();
            lKneeRot = msg.ReadSmallerCompressedQuaternion();
            rKneeRot = msg.ReadSmallerCompressedQuaternion();
            lAnkleRot = msg.ReadSmallerCompressedQuaternion();
            rAnkleRot = msg.ReadSmallerCompressedQuaternion();
            spineRot = msg.ReadSmallerCompressedQuaternion();
            chestRot = msg.ReadSmallerCompressedQuaternion();
            lShoulderRot = msg.ReadSmallerCompressedQuaternion();
            rShoulderRot = msg.ReadSmallerCompressedQuaternion();
            lElbowRot = msg.ReadSmallerCompressedQuaternion();
            rElbowRot = msg.ReadSmallerCompressedQuaternion();
            lWristRot = msg.ReadSmallerCompressedQuaternion();
            rWristRot = msg.ReadSmallerCompressedQuaternion();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.EnemyRigTransform);
            msg.WriteByte(poolChildIdx);

            msg.WriteVector3(rootPos);
            msg.WriteCompressedVector3(lHipPos, rootPos);
            msg.WriteCompressedVector3(rHipPos, rootPos);
            msg.WriteCompressedVector3(lKneePos, rootPos);
            msg.WriteCompressedVector3(rKneePos, rootPos);
            msg.WriteCompressedVector3(lAnklePos, rootPos);
            msg.WriteCompressedVector3(rAnklePos, rootPos);
            msg.WriteCompressedVector3(spinePos, rootPos);
            msg.WriteCompressedVector3(chestPos, rootPos);
            msg.WriteCompressedVector3(lShoulderPos, rootPos);
            msg.WriteCompressedVector3(rShoulderPos, rootPos);
            msg.WriteCompressedVector3(lElbowPos, rootPos);
            msg.WriteCompressedVector3(rElbowPos, rootPos);
            msg.WriteCompressedVector3(lWristPos, rootPos);
            msg.WriteCompressedVector3(rWristPos, rootPos);

            msg.WriteSmallerCompressedQuaternion(rootRot);
            msg.WriteSmallerCompressedQuaternion(lHipRot);
            msg.WriteSmallerCompressedQuaternion(rHipRot);
            msg.WriteSmallerCompressedQuaternion(lKneeRot);
            msg.WriteSmallerCompressedQuaternion(rKneeRot);
            msg.WriteSmallerCompressedQuaternion(lAnkleRot);
            msg.WriteSmallerCompressedQuaternion(rAnkleRot);
            msg.WriteSmallerCompressedQuaternion(spineRot);
            msg.WriteSmallerCompressedQuaternion(chestRot);
            msg.WriteSmallerCompressedQuaternion(lShoulderRot);
            msg.WriteSmallerCompressedQuaternion(rShoulderRot);
            msg.WriteSmallerCompressedQuaternion(lElbowRot);
            msg.WriteSmallerCompressedQuaternion(rElbowRot);
            msg.WriteSmallerCompressedQuaternion(lWristRot);
            msg.WriteSmallerCompressedQuaternion(rWristRot);

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

    public class AttackMessage : INetworkMessage
    {
        public AttackMessage()
        {
            
        }

        public P2PMessage MakeMsg()
        {
            throw new NotImplementedException();
        }
    }

    public class ZWWaveStartMessage : INetworkMessage
    {
        public ZWWaveStartMessage()
        {

        }

        public ZWWaveStartMessage(P2PMessage msg)
        {

        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();
            msg.WriteByte((byte)MessageType.ZWWaveStart);

            return msg;
        }
    }

    public class ZWModeStartMessage : INetworkMessage
    {
        public int mode;
        public ZWModeStartMessage()
        {

        }

        public ZWModeStartMessage(P2PMessage msg)
        {
            mode = msg.ReadInt();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.ZWModeStart);
            msg.WriteInt(mode);

            return msg;
        }
    }

    public class ZWDifficultyChange : INetworkMessage
    {
        public int difficulty;

        public ZWDifficultyChange()
        {

        }

        public ZWDifficultyChange(P2PMessage msg)
        {
            difficulty = msg.ReadInt();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.ZWDifficultyChange);
            msg.WriteInt(difficulty);

            return msg;
        }
    }

    public class ZWAmmoReward : INetworkMessage
    {
        public ZWAmmoReward()
        {

        }

        public ZWAmmoReward(P2PMessage msg)
        {

        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.ZWAmmoReward);

            return msg;
        }
    }

    public class ZWPuppetDeath : INetworkMessage
    {
        public int puppetId;
        public EnemyType enemyType;

        public ZWPuppetDeath()
        {

        }

        public ZWPuppetDeath(P2PMessage msg)
        {
            puppetId = msg.ReadInt();
            enemyType = (EnemyType)msg.ReadByte();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.ZWPuppetDeath);
            msg.WriteInt(puppetId);
            msg.WriteByte((byte)enemyType);

            return msg;
        }
    }

    public class ZWSetCustomEnemies : INetworkMessage
    {
        public Arena_EnemyReference.EnemyType[] enemyTypes;

        public ZWSetCustomEnemies()
        {

        }

        public ZWSetCustomEnemies(P2PMessage msg)
        {
            int numEnemyTypes = msg.ReadByte();

            enemyTypes = new Arena_EnemyReference.EnemyType[numEnemyTypes];

            for (int i = 0; i < numEnemyTypes; i++)
            {
                enemyTypes[i] = (Arena_EnemyReference.EnemyType)msg.ReadByte();
            }
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.ZWSetCustomEnemies);
            msg.WriteByte((byte)enemyTypes.Length);

            for (int i = 0; i < enemyTypes.Length; i++)
            {
                msg.WriteByte((byte)enemyTypes[i]);
            }

            return msg;
        }
    }

    public class ZWPlayerDamage : INetworkMessage
    {
        public float damage;
        public bool crit;

        public ZWPlayerDamage()
        {

        }

        public ZWPlayerDamage(P2PMessage msg)
        {
            damage = msg.ReadFloat();
            crit = msg.ReadBool();
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.ZWPlayerDamage);
            msg.WriteFloat(damage);
            msg.WriteBool(crit);

            return msg;
        }
    }

    public class ZWSetWave : INetworkMessage
    {
        public int wave;
        public bool showWave;
        public int enemyCount;
        public List<EnemyProfile> enemyProfiles;

        public ZWSetWave()
        {

        }

        public ZWSetWave(P2PMessage msg)
        {
            wave = msg.ReadInt();

            showWave = msg.ReadBool();
            enemyCount = msg.ReadInt();

            int enemyProfileCount = msg.ReadInt();

            for (int i = 0; i < enemyProfileCount; i++)
            {
                EnemyProfile profile = new EnemyProfile();
                profile.showEnemy = msg.ReadBool();
                profile.enemyType = (Arena_EnemyReference.EnemyType)msg.ReadByte();
                profile.entranceType = (EnemyProfile.EntranceType)msg.ReadByte();
            }
        }

        public P2PMessage MakeMsg()
        {
            P2PMessage msg = new P2PMessage();

            msg.WriteByte((byte)MessageType.ZWSetWave);
            msg.WriteInt(wave);
            msg.WriteBool(showWave);
            msg.WriteInt(enemyCount);

            msg.WriteInt(enemyProfiles.Count);

            foreach (var profile in enemyProfiles)
            {
                msg.WriteBool(profile.showEnemy);
                msg.WriteByte((byte)profile.enemyType);
                msg.WriteByte((byte)profile.entranceType);
            }

            return msg;
        }
    }
}
