using Sandbox.ModAPI;
using System;
using System.Collections.Generic;

namespace Math0424.AnimationCore
{
    internal class AnimationAPI
    {
        private const long ID = 62844726653184368;
        private const string Version = "1";

        public bool IsReady { get; private set; }
        internal readonly Dictionary<string, Delegate> ModApiMethods;
        private readonly Dictionary<ulong, InternalAnimatedBlock> registered = new Dictionary<ulong, InternalAnimatedBlock>();

        public void LoadAPI()
        {
            MyAPIGateway.Utilities.RegisterMessageHandler(ID, APIRequest);
        }

        public void UnloadAPI()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(ID, APIRequest);
            MyAPIGateway.Utilities.SendModMessage(ID, new Dictionary<string, Delegate>());
        }

        private void APIRequest(object obj)
        {
            string[] call = (obj as string).Split(':');

            if (call != null && call.Length == 2 && call[0] == "RequestingAPI")
            {
                if (call[1] != Version)
                {
                    MyAPIGateway.Utilities.ShowMessage("AnimationAPI", $"Animation API outdated :: Expected '{Version}' Got '{call[1]}'");
                }
                MyAPIGateway.Utilities.SendModMessage(ID, ModApiMethods);
            }
        }

        public void RegisterBlock(long id)
        {

        }

        public void RegisterSubpart(long block, long subpart)
        {

        }

        //https://github.com/sstixrud/WeaponCore/blob/master/Data/Scripts/WeaponCore/Api/ApiBackend.cs





    }
}
