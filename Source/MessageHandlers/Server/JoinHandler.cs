﻿using Discord;
using Facepunch.Steamworks;
using MelonLoader;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers.Server
{
    [MessageHandler(MessageType.Join, PeerType.Server)]
    class JoinHandler : MessageHandler
    {
        byte smallIdCounter = 1;

        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            if (msg.ReadByte() != MultiplayerMod.PROTOCOL_VERSION)
            {
                // Somebody tried to join with an incompatible verison. Kick 'em!
                P2PMessage m2 = new P2PMessage();
                m2.WriteByte((byte)MessageType.JoinRejected);
                connection.SendMessage(m2, SendReliability.Reliable);
                connection.Disconnect();
            }
            else
            {
                MelonLogger.Msg("Player joined with ID: " + connection.ConnectedTo);

                if (players.Contains(connection.ConnectedTo))
                    players.Remove(connection.ConnectedTo);

                string name = msg.ReadUnicodeString();
                byte newPlayerId = smallIdCounter;
                smallIdCounter++;

                var player = new MPPlayer(name, connection.ConnectedTo, newPlayerId, connection);

                MelonLogger.Msg("Player count: " + players.Count);
                MelonLogger.Msg("Name: " + name);

                ClientJoinMessage cjm3 = new ClientJoinMessage
                {
                    playerId = newPlayerId,
                    name = name,
                    steamId = connection.ConnectedTo
                };
                players.SendMessageToAllExcept(cjm3, SendReliability.Reliable, connection.ConnectedTo);

                SetLocalSmallIdMessage slsi = new SetLocalSmallIdMessage()
                {
                    smallId = newPlayerId
                };
                connection.SendMessage(slsi.MakeMsg(), SendReliability.Reliable);

                players.Add(player);

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
                            Id = ((Core.Server)peer).PartyID,
                            Size = new PartySize()
                            {
                                CurrentSize = players.Count,
                                MaxSize = MultiplayerMod.MAX_PLAYERS
                            }
                        }
                    });

                foreach (MPPlayer p in players)
                {
                    p.PlayerRep.faceAnimator.faceState = Representations.FaceAnimator.FaceState.Happy;
                    p.PlayerRep.faceAnimator.faceTime = 15;
                }
            }
        }
    }
}
