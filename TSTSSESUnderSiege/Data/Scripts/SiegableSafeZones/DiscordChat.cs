using ProtoBuf;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI;

namespace SiegableSafeZones
{
    [ProtoContract]
    public class ModMessage
    {
        [ProtoMember(1)] public long RoleID;
        [ProtoMember(2)] public string RoleName;
        [ProtoMember(3)] public string Message;
        [ProtoMember(4)] public string Author;
        [ProtoMember(5)] public bool BroadCastToDiscordOnly;
        [ProtoMember(6)] public VRageMath.Color Color;

        public ModMessage(string MessageTxt, VRageMath.Color color, bool BrodcastDiscordOnly = false, string ChannelId = null)
        {
            RoleID = Session.Instance.config._discordRoleId;
            RoleName = Session.Instance.config._discordRoleName;
            Author = "[Siegable Safezones]";
            Color = color;
            BroadCastToDiscordOnly = BrodcastDiscordOnly;
            Message = MessageTxt;

            MyVisualScriptLogicProvider.SendChatMessageColored($"<@&{RoleID}> [{RoleName}]: {MessageTxt}", color, Author, 0);

            /*if (!string.IsNullOrEmpty(factionTag))
            {
                List<IMyPlayer> players = new List<IMyPlayer>();
                MyAPIGateway.Players.GetPlayers(players);

                foreach(var player in players)
                {
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId);
                    if (faction == null) continue;
                    if (faction.Tag != factionTag) continue;

                    MyVisualScriptLogicProvider.SendChatMessageColored($" {MessageTxt}", color, Author, player.IdentityId, "Orange");
                }
            }*/

            /*if (Session.Instance.IsNexusInstalled)
            {
                //MyLog.Default.WriteLineAndConsole($"NexusAPI: Chat Message Init");
                NexusAPI.SendMessageToDiscord($"<@&{DiscordRole}> [{RoleName}]: {MessageTxt}");
                NexusComms.SendChatAllServers(this);
            }*/
        }

        public ModMessage() { }
    }
}
