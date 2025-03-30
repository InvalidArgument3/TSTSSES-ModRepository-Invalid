using Sandbox.ModAPI;
using VRage.Game.Components;

namespace TeleportMechanisms
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class Session : MySessionComponentBase
    {
        private static bool _isInitialized = false;

        public override void LoadData()
        {
            if (!_isInitialized && MyAPIGateway.Session.IsServer)
            {
                MyLogger.LoadConfig();
                _isInitialized = true;
            }

            MyLogger.Log("Session: LoadData called");
            NetworkHandler.Register();
        }

        protected override void UnloadData()
        {
            MyLogger.Log("Session: UnloadData called");
            NetworkHandler.Unregister();

            if (_isInitialized && MyAPIGateway.Session.IsServer)
            {
                MyLogger.Close();
                _isInitialized = false;
            }
        }
    }
}