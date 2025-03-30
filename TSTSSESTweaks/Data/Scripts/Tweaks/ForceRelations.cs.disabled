using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using System.Collections.Generic;

namespace FORCERELATIONS
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Relations : MySessionComponentBase
    {
        public static bool isInit = false;
        public int runCount = 0;
        private static readonly HashSet<string> AdmPeaceWhitelist = new HashSet<string> { "ECO", "ECO-NPC", "GLF", "GLF-NPC" };

        public override void UpdateBeforeSimulation()
        {
            // Ensure that only the server runs this logic.
            if (!MyAPIGateway.Multiplayer.IsServer && !MyAPIGateway.Utilities.IsDedicated)
                return;

            if (!isInit)
                init();

            if (++runCount % 3600 > 0) // Runs every minute (3600 ticks = 1 minute)
                return;

            runCount = 0;
            main();
        }

        public void init()
        {
            isInit = true;
            MyLog.Default.WriteLineAndConsole("ForceRelations: Initialized with faction war/peace logic.");
            //MyVisualScriptLogicProvider.SendChatMessage("ForceRelations: Initialized faction logic", "Server");
        }

        public void main()
        {
            try
            {
                var factionList = MyAPIGateway.Session.Factions.Factions;

                foreach (var faction in factionList)
                {
                    // ADM should make peace with factions in the whitelist only (ECO, ECO-NPC, GLF, GLF-NPC)
                    if (faction.Value.Tag == "ADM")
                    {
                        foreach (var otherFaction in factionList)
                        {
                            if (AdmPeaceWhitelist.Contains(otherFaction.Value.Tag))
                            {
                                // ADM makes peace with the whitelisted factions
                                MyAPIGateway.Session.Factions.SendPeaceRequest(faction.Value.FactionId, otherFaction.Value.FactionId);
                                MyAPIGateway.Session.Factions.AcceptPeace(faction.Value.FactionId, otherFaction.Value.FactionId);

                                //MyVisualScriptLogicProvider.SendChatMessage($"ADM has made peace with {otherFaction.Value.Tag}", "Server");
                            }
                        }
                    }

                    // GLF and GLF-NPC should declare war on ECO and ECO-NPC
                    if (faction.Value.Tag == "GLF" || faction.Value.Tag == "GLF-NPC")
                    {
                        MyAPIGateway.Session.Factions.DeclareWar(faction.Value.FactionId, MyAPIGateway.Session.Factions.TryGetFactionByTag("ECO").FactionId);
                        MyAPIGateway.Session.Factions.DeclareWar(faction.Value.FactionId, MyAPIGateway.Session.Factions.TryGetFactionByTag("ECO-NPC").FactionId);

                        //MyVisualScriptLogicProvider.SendChatMessage($"{faction.Value.Tag} has declared war on ECO and ECO-NPC", "Server");
                    }

                    // ECO and ECO-NPC should declare war on GLF and GLF-NPC
                    if (faction.Value.Tag == "ECO" || faction.Value.Tag == "ECO-NPC")
                    {
                        MyAPIGateway.Session.Factions.DeclareWar(faction.Value.FactionId, MyAPIGateway.Session.Factions.TryGetFactionByTag("GLF").FactionId);
                        MyAPIGateway.Session.Factions.DeclareWar(faction.Value.FactionId, MyAPIGateway.Session.Factions.TryGetFactionByTag("GLF-NPC").FactionId);

                        //MyVisualScriptLogicProvider.SendChatMessage($"{faction.Value.Tag} has declared war on GLF and GLF-NPC", "Server");
                    }
                }
            }
            catch (System.Exception ex)
            {
                MyLog.Default.WriteLineAndConsole("Relations Main error: " + ex.ToString());
                //MyVisualScriptLogicProvider.SendChatMessage("Relations error: " + ex.ToString(), "Server");
            }
        }
    }
}
