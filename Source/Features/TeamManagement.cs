using MelonLoader;
using MultiplayerMod.Representations;
using MultiplayerMod.Source.Boneworks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static MultiplayerMod.Source.Structs.Teams;

namespace MultiplayerMod.Source.Features
{
    public static class TeamManagement
    {
        public static void ChangeTeam(Team team)
        {
            MelonLogger.Log($"Changing team to: {team.ToString()}");
            ProjectilePatch.myTeam = team;
            if (MultiplayerMod.client.isConnected)
                MultiplayerMod.client.UpdateTeam(team);
            else if (MultiplayerMod.server.IsRunning)
                MultiplayerMod.server.UpdateTeam(team);
        }

        static Color anarchy = new Color32(187,187,187,255);
        static Color passive = new Color32(170,51,119,255);
        static Color red = new Color32(238,102,119,255);
        static Color green = new Color32(34, 136, 51, 255);
        static Color blue = new Color32(68,119,170,255);
        static Color yellow = new Color32(204,187,68,255);
        public static void ChangePlayerRepTeam(PlayerRep rep, Team team)
        {
            rep.team = team;
            switch (team)
            {
                case Team.Anarchy:
                    rep.namePlateText.color = anarchy;
                    break;
                case Team.Passive:
                    rep.namePlateText.color = passive;
                    break;
                case Team.Red:
                    rep.namePlateText.color = red;
                    break;
                case Team.Green:
                    rep.namePlateText.color = green;
                    break;
                case Team.Blue:
                    rep.namePlateText.color = blue;
                    break;
                case Team.Yellow:
                    rep.namePlateText.color = yellow;
                    break;
            }
        }
    }
}
