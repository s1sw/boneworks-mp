using MelonLoader;
using MultiplayerMod.Networking;
using StressLevelZero.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiplayerMod.Core.MessageHandlers
{
    [MessageHandler(MessageType.Join)]
    public class JoinMessageHandler : IMessageHandler
    {
        public MessageType GetHandledMessageType() => MessageType.Join;

        public void HandleMessage(P2PMessage msg, ITransportConnection connection, NetController networkController)
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
                MelonModLogger.Log("Player count: " + networkController.GetPlayerCount());


                string name = msg.ReadUnicodeString();
                MelonModLogger.Log("Name: " + name);

                byte newPlayerId = networkController.RegisterNewPlayer(connection, name);

                #region Sync of initial state

                #region Sending players to new client
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
                #endregion

                SceneTransitionMessage stm = new SceneTransitionMessage()
                {
                    sceneName = BoneworksSceneManager.GetCurrentSceneName()
                };
                connection.SendMessage(stm.MakeMsg(), MessageSendType.Reliable);

                #endregion

                // Sending new join to other players
                ClientJoinMessage cjm3 = new ClientJoinMessage
                {
                    playerId = newPlayerId,
                    name = name,
                    steamId = connection.ConnectedTo
                };
                ServerSendToAllExcept(cjm3, MessageSendType.Reliable, connection.ConnectedTo);

                #region Rich presence
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

                SetPartyIdMessage spid = new SetPartyIdMessage()
                {
                    partyId = partyId
                };
                connection.SendMessage(spid.MakeMsg(), MessageSendType.Reliable);
                #endregion
                
                // Update UI
                ui.SetPlayerCount(players.Count, MultiplayerUIState.Server);
            }
        }
    }
}
