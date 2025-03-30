using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace DeltaVEquipment
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, Priority = int.MaxValue)]
    internal class DefinitionSender : MySessionComponentBase
    {
        const int DefinitionMessageId = 8643;

        byte[] serializedStorage;
        DefinitionContainer storedDef = null;

        public override void LoadData()
        {
            //if (!MyAPIGateway.Session.IsServer)
            //    return;
            HeartApi.LoadData(ModContext, InitAndSendDefinitions); // Doing it this way because we don't get async stuff :(

            MyAPIGateway.Utilities.RegisterMessageHandler(DefinitionMessageId, InputHandler);
        }

        private void InitAndSendDefinitions()
        {
            storedDef = HeartDefinitions.GetBaseDefinitions();
            serializedStorage = MyAPIGateway.Utilities.SerializeToBinary(storedDef);
            HeartApi.LogWriteLine($"Packaged definitions & preparing to send.");

            MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, serializedStorage);
            foreach (var def in storedDef.AmmoDefs)
                def.LiveMethods.RegisterMethods(def.Name);
            HeartApi.LogWriteLine($"Sent definitions & returning to sleep.");
        }

        private void InputHandler(object o)
        {
            if (o is bool && (bool)o && storedDef != null)
            {
                MyAPIGateway.Utilities.SendModMessage(DefinitionMessageId, serializedStorage);
                foreach (var def in storedDef.AmmoDefs)
                    def.LiveMethods.RegisterMethods(def.Name);
                MyLog.Default.WriteLineAndConsole($"OrreryDefinition [{ModContext.ModName}]: Sent definitions & returning to sleep.");
            }
        }

        protected override void UnloadData()
        {
            HeartApi.UnloadData();
            MyAPIGateway.Utilities.UnregisterMessageHandler(DefinitionMessageId, InputHandler);
        }
    }
}
