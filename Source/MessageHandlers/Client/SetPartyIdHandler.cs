using Discord;
using MultiplayerMod.Core;
using MultiplayerMod.Networking;

namespace MultiplayerMod.MessageHandlers.Client
{
    [MessageHandler(MessageType.SetPartyId, PeerType.Client)]
    class SetPartyIdHandler : MessageHandler
    {
        public override void HandleMessage(MessageType msgType, ITransportConnection connection, P2PMessage msg)
        {
            SetPartyIdMessage spid = new SetPartyIdMessage(msg);
            RichPresence.SetActivity(
                new Activity()
                {
                    Details = "Connected to a server",
                    Secrets = new ActivitySecrets()
                    {
                        Join = ((Core.Client)peer).ServerFullId.ToString()
                    },
                    Party = new ActivityParty()
                    {
                        Id = spid.partyId,
                        Size = new PartySize()
                        {
                            CurrentSize = 1,
                            MaxSize = MultiplayerMod.MAX_PLAYERS
                        }
                    }
                });
        }
    }
}
