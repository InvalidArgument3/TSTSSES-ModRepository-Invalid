using Sandbox.ModAPI;

namespace Invalid.DeltaVQuestLog
{
    internal class QuestLogNetworking
    {
        public const ushort NetworkId = 45782;

        public QuestLogNetworking()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(NetworkId, MessageHandler);
        }

        public void Close()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(NetworkId, MessageHandler);
        }

        public void SendMessageToAll(QuestLogManager manager)
        {
            if (MyAPIGateway.Session.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToOthers(NetworkId, MyAPIGateway.Utilities.SerializeToBinary(manager));
            else
                MyAPIGateway.Multiplayer.SendMessageToServer(NetworkId, MyAPIGateway.Utilities.SerializeToBinary(manager));
        }

        private void MessageHandler(ushort handlerId, byte[] data, ulong senderId, bool fromServer)
        {
            QuestLogManager manager = MyAPIGateway.Utilities.SerializeFromBinary<QuestLogManager>(data);
            if (manager == null)
                return;
            PersistentFactionObjectives.I.UpdateManager(manager);

            if (MyAPIGateway.Session.IsServer && !fromServer)
                MyAPIGateway.Multiplayer.SendMessageToOthers(NetworkId, MyAPIGateway.Utilities.SerializeToBinary(manager));
        }
    }
}
