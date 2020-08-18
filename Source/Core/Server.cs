using Discord;
using Facepunch.Steamworks;
using Facepunch.Steamworks.Data;
using MelonLoader;
using StressLevelZero.Pool;
using StressLevelZero.Props.Weapons;
using StressLevelZero.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Object;

using MultiplayerMod.Structs;
using MultiplayerMod.Networking;
using MultiplayerMod.Representations;
using Oculus.Platform;
using Oculus.Platform.Samples.VrHoops;
using MultiplayerMod.MonoBehaviours;

namespace MultiplayerMod.Core
{
    public class Server
    {
        private readonly Dictionary<byte, PlayerRep> playerObjects = new Dictionary<byte, PlayerRep>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<byte, string> playerNames = new Dictionary<byte, string>(MultiplayerMod.MAX_PLAYERS);
        private readonly List<ulong> players = new List<ulong>();
        private readonly Dictionary<SteamId, byte> smallPlayerIds = new Dictionary<SteamId, byte>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<byte, SteamId> largePlayerIds = new Dictionary<byte, SteamId>(MultiplayerMod.MAX_PLAYERS);
        private readonly Dictionary<ulong, ITransportConnection> playerConnections = new Dictionary<ulong, ITransportConnection>(MultiplayerMod.MAX_PLAYERS);
        private readonly EnemyPoolManager enemyPoolManager = new EnemyPoolManager();
        private readonly Dictionary<GameObject, ServerSyncedObject> syncedObjectCache = new Dictionary<GameObject, ServerSyncedObject>();
        private string partyId = "";
        private byte smallIdCounter = 0;
        private BoneworksRigTransforms localRigTransforms;
        private readonly MultiplayerUI ui;
        private readonly ITransportLayer transportLayer;

        public bool IsRunning { get; private set; }

        public Server(MultiplayerUI ui, ITransportLayer transportLayer)
        {
            this.ui = ui;
            this.transportLayer = transportLayer;
        }

        public void Update()
        {
            if (SceneLoader.loading) return;

            if (localRigTransforms.main == null)
                localRigTransforms = BWUtil.GetLocalRigTransforms();

            if (localRigTransforms.main != null)
            {
                OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
                {
                    playerId = 0,
                    posMain = localRigTransforms.main.position,
                    posRoot = localRigTransforms.root.position,
                    posLHip = localRigTransforms.lHip.position,
                    posRHip = localRigTransforms.rHip.position,
                    posLKnee = localRigTransforms.lKnee.position,
                    posRKnee = localRigTransforms.rKnee.position,
                    posLAnkle = localRigTransforms.lAnkle.position,
                    posRAnkle = localRigTransforms.rAnkle.position,

                    posSpine1 = localRigTransforms.spine1.position,
                    posSpine2 = localRigTransforms.spine2.position,
                    posSpineTop = localRigTransforms.spineTop.position,
                    posLClavicle = localRigTransforms.lClavicle.position,
                    posRClavicle = localRigTransforms.rClavicle.position,
                    posNeck = localRigTransforms.neck.position,
                    posLShoulder = localRigTransforms.lShoulder.position,
                    posRShoulder = localRigTransforms.rShoulder.position,
                    posLElbow = localRigTransforms.lElbow.position,
                    posRElbow = localRigTransforms.rElbow.position,
                    posLWrist = localRigTransforms.lWrist.position,
                    posRWrist = localRigTransforms.rWrist.position,

                    rotMain = localRigTransforms.main.rotation,
                    rotRoot = localRigTransforms.root.rotation,
                    rotLHip = localRigTransforms.lHip.rotation,
                    rotRHip = localRigTransforms.rHip.rotation,
                    rotLKnee = localRigTransforms.lKnee.rotation,
                    rotRKnee = localRigTransforms.rKnee.rotation,
                    rotLAnkle = localRigTransforms.lAnkle.rotation,
                    rotRAnkle = localRigTransforms.rAnkle.rotation,
                    rotSpine1 = localRigTransforms.spine1.rotation,
                    rotSpine2 = localRigTransforms.spine2.rotation,
                    rotSpineTop = localRigTransforms.spineTop.rotation,
                    rotLClavicle = localRigTransforms.lClavicle.rotation,
                    rotRClavicle = localRigTransforms.rClavicle.rotation,
                    rotNeck = localRigTransforms.neck.rotation,
                    rotLShoulder = localRigTransforms.lShoulder.rotation,
                    rotRShoulder = localRigTransforms.rShoulder.rotation,
                    rotLElbow = localRigTransforms.lElbow.rotation,
                    rotRElbow = localRigTransforms.rElbow.rotation,
                    rotLWrist = localRigTransforms.lWrist.rotation,
                    rotRWrist = localRigTransforms.rWrist.rotation
                };

                ServerSendToAll(ofrtm, MessageSendType.Unreliable);
            }

            foreach (PlayerRep pr in playerObjects.Values)
            {
                pr.UpdateNameplateFacing(Camera.current.transform);
            }

            foreach (var pair in ObjectIDManager.objects)
            {
                if (!syncedObjectCache.ContainsKey(pair.Value))
                    syncedObjectCache.Add(pair.Value, pair.Value.GetComponent<ServerSyncedObject>());
                ServerSyncedObject sso = syncedObjectCache[pair.Value];
                if (sso.NeedsSync())
                {

                }
            }
        }

        private void MultiplayerMod_OnLevelWasLoadedEvent(int level)
        {
            SceneTransitionMessage stm = new SceneTransitionMessage
            {
                sceneName = BoneworksSceneManager.GetSceneNameFromScenePath(level)
            };
            ServerSendToAll(stm, MessageSendType.Reliable);
            enemyPoolManager.FindAllPools();
        }

        public void StartServer()
        {
            ui.SetState(MultiplayerUIState.Server);
            MelonModLogger.Log("Starting server...");
            localRigTransforms = BWUtil.GetLocalRigTransforms();
            partyId = SteamClient.SteamId + "P" + DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();

            RichPresence.SetActivity(
                new Activity()
                {
                    Details = "Hosting a server",
                    Secrets = new ActivitySecrets()
                    {
                        Join = SteamClient.SteamId.ToString()
                    },
                    Party = new ActivityParty()
                    {
                        Id = partyId,
                        Size = new PartySize()
                        {
                            CurrentSize = 1,
                            MaxSize = MultiplayerMod.MAX_PLAYERS
                        }
                    }
                });
            transportLayer.OnMessageReceived += TransportLayer_OnMessageReceived;
            transportLayer.OnConnectionClosed += TransportLayer_OnConnectionClosed;

            MultiplayerMod.OnLevelWasLoadedEvent += MultiplayerMod_OnLevelWasLoadedEvent;

            IsRunning = true;
        }

        private void TransportLayer_OnConnectionClosed(ITransportConnection connection, ConnectionClosedReason reason)
        {
            switch (reason)
            {
                case ConnectionClosedReason.Timeout:
                case ConnectionClosedReason.ClosedByRemote:
                    if (smallPlayerIds.ContainsKey(connection.ConnectedTo))
                    {
                        MelonModLogger.Log("Player left with ID: " + connection.ConnectedTo);
                        byte smallId = smallPlayerIds[connection.ConnectedTo];

                        P2PMessage disconnectMsg = new P2PMessage();
                        disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                        disconnectMsg.WriteByte(smallId);

                        playerObjects[smallId].Destroy();
                        playerObjects.Remove(smallId);
                        players.RemoveAll((ulong val) => val == connection.ConnectedTo);
                        smallPlayerIds.Remove(connection.ConnectedTo);

                        foreach (SteamId p in players)
                        {
                            playerConnections[p].SendMessage(disconnectMsg, MessageSendType.Reliable);
                        }
                    }
                    break;
                case ConnectionClosedReason.Other:
                    break;
                
            }
        }

        private void TransportLayer_OnMessageReceived(ITransportConnection connection, P2PMessage msg)
        {
            MessageType type = (MessageType)msg.ReadByte();

            switch (type)
            {
                case MessageType.Join:
                    {
                        if (msg.ReadByte() != MultiplayerMod.PROTOCOL_VERSION)
                        {
                            // Somebody tried to join with an incompatible verison
                            P2PMessage m2 = new P2PMessage();
                            m2.WriteByte((byte)MessageType.JoinRejected);
                            connection.SendMessage(m2, MessageSendType.Reliable);
                            connection.Disconnect();
                        }
                        else
                        {
                            MelonModLogger.Log("Player joined with ID: " + connection.ConnectedTo);
                            players.Add(connection.ConnectedTo);
                            MelonModLogger.Log("Player count: " + players.Count);
                            byte newPlayerId = smallIdCounter;
                            smallPlayerIds.Add(connection.ConnectedTo, newPlayerId);
                            largePlayerIds.Add(newPlayerId, connection.ConnectedTo);
                            smallIdCounter++;

                            playerConnections.Add(connection.ConnectedTo, connection);

                            string name = msg.ReadUnicodeString();
                            MelonModLogger.Log("Name: " + name);

                            foreach (var smallId in playerNames.Keys)
                            {
                                ClientJoinMessage cjm = new ClientJoinMessage
                                {
                                    playerId = smallId,
                                    name = playerNames[smallId],
                                    steamId = largePlayerIds[smallId]
                                };
                                connection.SendMessage(cjm.MakeMsg(), MessageSendType.Reliable);
                            }

                            ClientJoinMessage cjm2 = new ClientJoinMessage
                            {
                                playerId = 0,
                                name = SteamClient.Name,
                                steamId = SteamClient.SteamId
                            };
                            connection.SendMessage(cjm2.MakeMsg(), MessageSendType.Reliable);

                            playerNames.Add(newPlayerId, name);

                            ClientJoinMessage cjm3 = new ClientJoinMessage
                            {
                                playerId = newPlayerId,
                                name = name,
                                steamId = connection.ConnectedTo
                            };
                            ServerSendToAllExcept(cjm3, MessageSendType.Reliable, connection.ConnectedTo);

                            playerObjects.Add(newPlayerId, new PlayerRep(name, connection.ConnectedTo));

                            RichPresence.SetActivity(
                                new Activity()
                                {
                                    Details = "Hosting a server",
                                    Secrets = new ActivitySecrets()
                                    {
                                        Join = SteamClient.SteamId.ToString()
                                    },
                                    Party = new ActivityParty()
                                    {
                                        Id = partyId,
                                        Size = new PartySize()
                                        {
                                            CurrentSize = players.Count + 1,
                                            MaxSize = MultiplayerMod.MAX_PLAYERS
                                        }
                                    }
                                });

                            SceneTransitionMessage stm = new SceneTransitionMessage()
                            {
                                sceneName = BoneworksSceneManager.GetCurrentSceneName()
                            };
                            connection.SendMessage(stm.MakeMsg(), MessageSendType.Reliable);

                            SetPartyIdMessage spid = new SetPartyIdMessage()
                            {
                                partyId = partyId
                            };
                            connection.SendMessage(spid.MakeMsg(), MessageSendType.Reliable);

                            ui.SetPlayerCount(players.Count, MultiplayerUIState.Server);
                        }
                        break;
                    }
                case MessageType.Disconnect:
                    {
                        MelonModLogger.Log("Player left with ID: " + connection.ConnectedTo);
                        byte smallId = smallPlayerIds[connection.ConnectedTo];

                        playerObjects[smallId].Destroy();
                        playerObjects.Remove(smallId);
                        players.RemoveAll((ulong val) => val == connection.ConnectedTo);
                        smallPlayerIds.Remove(connection.ConnectedTo);

                        P2PMessage disconnectMsg = new P2PMessage();
                        disconnectMsg.WriteByte((byte)MessageType.Disconnect);
                        disconnectMsg.WriteByte(smallId);

                        foreach (SteamId p in players)
                        {
                            playerConnections[p].SendMessage(disconnectMsg, MessageSendType.Reliable);
                        }
                        break;
                    }
                case MessageType.FullRig:
                    {
                        FullRigTransformMessage frtm = new FullRigTransformMessage(msg);

                        byte playerId = smallPlayerIds[connection.ConnectedTo];
                        if (playerObjects.ContainsKey(playerId))
                        {
                            PlayerRep pr = playerObjects[playerId];

                            if (pr.rigTransforms.main != null)
                            {
                                //ApplyTransformMessage(pr, frtm);
                                pr.ApplyTransformMessage(frtm);

                                OtherFullRigTransformMessage ofrtm = new OtherFullRigTransformMessage
                                {
                                    playerId = playerId,

                                    posMain = frtm.posMain,
                                    posRoot = frtm.posRoot,
                                    posLHip = frtm.posLHip,
                                    posRHip = frtm.posRHip,
                                    posLKnee = frtm.posLKnee,
                                    posRKnee = frtm.posRKnee,
                                    posLAnkle = frtm.posLAnkle,
                                    posRAnkle = frtm.posRAnkle,

                                    posSpine1 = frtm.posSpine1,
                                    posSpine2 = frtm.posSpine2,
                                    posSpineTop = frtm.posSpineTop,
                                    posLClavicle = frtm.posLClavicle,
                                    posRClavicle = frtm.posRClavicle,
                                    posNeck = frtm.posNeck,
                                    posLShoulder = frtm.posLShoulder,
                                    posRShoulder = frtm.posRShoulder,
                                    posLElbow = frtm.posLElbow,
                                    posRElbow = frtm.posRElbow,
                                    posLWrist = frtm.posLWrist,
                                    posRWrist = frtm.posRWrist,

                                    rotMain = frtm.rotMain,
                                    rotRoot = frtm.rotRoot,
                                    rotLHip = frtm.rotLHip,
                                    rotRHip = frtm.rotRHip,
                                    rotLKnee = frtm.rotLKnee,
                                    rotRKnee = frtm.rotRKnee,
                                    rotLAnkle = frtm.rotLAnkle,
                                    rotRAnkle = frtm.rotRAnkle,
                                    rotSpine1 = frtm.rotSpine1,
                                    rotSpine2 = frtm.rotSpine2,
                                    rotSpineTop = frtm.rotSpineTop,
                                    rotLClavicle = frtm.rotLClavicle,
                                    rotRClavicle = frtm.rotRClavicle,
                                    rotNeck = frtm.rotNeck,
                                    rotLShoulder = frtm.rotLShoulder,
                                    rotRShoulder = frtm.rotRShoulder,
                                    rotLElbow = frtm.rotLElbow,
                                    rotRElbow = frtm.rotRElbow,
                                    rotLWrist = frtm.rotLWrist,
                                    rotRWrist = frtm.rotRWrist
                                };

                                ServerSendToAllExcept(ofrtm, MessageSendType.Unreliable, connection.ConnectedTo);
                            }
                        }
                        break;
                    }
                case MessageType.IdRequest:
                    {
                        IDRequestMessage idrqm = new IDRequestMessage(msg);
                        MelonModLogger.Log("ID request: " + idrqm.namePath);
                        BWUtil.GetObjectFromFullPath(idrqm.namePath);
                        break;
                    }
                default:
                    MelonModLogger.Log("Unknown message type: " + type.ToString());
                    break;
            }
        }

        public void StopServer()
        {
            ui.SetState(MultiplayerUIState.PreConnect);
            IsRunning = false;

            try
            {
                foreach (PlayerRep r in playerObjects.Values)
                {
                    r.Destroy();
                }
            }
            catch (Exception)
            {
                MelonModLogger.LogError("Caught exception destroying player objects");
            }

            playerObjects.Clear();
            playerNames.Clear();
            smallPlayerIds.Clear();
            largePlayerIds.Clear();
            smallIdCounter = 1;

            P2PMessage shutdownMsg = new P2PMessage();
            shutdownMsg.WriteByte((byte)MessageType.ServerShutdown);

            foreach (SteamId p in players)
            {
                playerConnections[p].SendMessage(shutdownMsg, MessageSendType.Reliable);
                playerConnections[p].Disconnect();
            }

            players.Clear();

            transportLayer.OnMessageReceived -= TransportLayer_OnMessageReceived;
            transportLayer.OnConnectionClosed -= TransportLayer_OnConnectionClosed;

            MultiplayerMod.OnLevelWasLoadedEvent -= MultiplayerMod_OnLevelWasLoadedEvent;
        }

        private void ServerSendToAll(INetworkMessage msg, MessageSendType send)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (SteamId p in players)
            {
                playerConnections[p].SendMessage(pMsg, send);
            }
        }

        private void ServerSendToAllExcept(INetworkMessage msg, MessageSendType send, SteamId except)
        {
            P2PMessage pMsg = msg.MakeMsg();
            foreach (SteamId p in players)
            {
                if (p != except)
                    playerConnections[p].SendMessage(pMsg, send);
            }
        }

        private void SendToId(INetworkMessage msg, MessageSendType send, SteamId id)
        {
            P2PMessage pMsg = msg.MakeMsg();
            playerConnections[id].SendMessage(pMsg, send);
        }
    }
}
