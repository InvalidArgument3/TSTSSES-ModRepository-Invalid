using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VRage.Game.Components;
using FactionsStruct;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace FactionsApi
{

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, int.MaxValue)]
    public class Session : MySessionComponentBase
    {
        
        private Registration registration = new Registration();
        private FactionDefs factionDefs = new FactionDefs();
        private readonly ushort ASKFORFACTIONDEFS = 9963;
        private readonly ushort RECEIVEFACTIONDEFS = 9964;

        public override void LoadData()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(ASKFORFACTIONDEFS, MessageHandler);
            SendMessageToProvider();
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ASKFORFACTIONDEFS, MessageHandler);
        }

        private void MessageHandler(object o)
        {
            if (o == null) SendMessageToProvider();
        }

        private void SendMessageToProvider()
        {
            if (factionDefs.defs.Any())
                registration.FactionDefList = factionDefs.defs;

            var serialized = MyAPIGateway.Utilities.SerializeToBinary<Registration>(registration);

            MyAPIGateway.Utilities.SendModMessage(RECEIVEFACTIONDEFS, serialized);
        }
        
    }
    
}