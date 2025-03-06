using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace ResourceNodes
{
    static class Utils
    {
        public static void DrawDebug(Vector3D pos, Vector3D dir, int r, int g, int b)
        {
            Vector4 color = new Vector4(r / 255, g / 255, b / 255, 1);
            MySimpleObjectDraw.DrawLine(pos, pos + dir * 10, MyStringId.GetOrCompute("Square"), ref color, 0.01f);
        }

        public static void DrawDebug(Vector3D pos, Quaternion quat, int r, int g, int b)
        {
            Vector4 color = new Vector4(r / 255, g / 255, b / 255, 1);
            Matrix m = Matrix.CreateFromQuaternion(quat);
            MySimpleObjectDraw.DrawLine(pos, pos + m.Forward, MyStringId.GetOrCompute("Square"), ref color, 0.01f);
            MySimpleObjectDraw.DrawLine(pos, pos + m.Up, MyStringId.GetOrCompute("Square"), ref color, 0.01f);
            MySimpleObjectDraw.DrawLine(pos, pos + m.Left, MyStringId.GetOrCompute("Square"), ref color, 0.01f);
        }

        public static void DrawDebug(Vector3D pos, Matrix m, int r, int g, int b)
        {
            Vector4 color = new Vector4(r / 255, g / 255, b / 255, 1);
            MySimpleObjectDraw.DrawLine(pos, pos + m.Forward, MyStringId.GetOrCompute("Square"), ref color, 0.01f);
            MySimpleObjectDraw.DrawLine(pos, pos + m.Up, MyStringId.GetOrCompute("Square"), ref color, 0.01f);
            MySimpleObjectDraw.DrawLine(pos, pos + m.Left, MyStringId.GetOrCompute("Square"), ref color, 0.01f);
        }

        public static void Log(object message)
        {
            if (message == null)
            {
                MyLog.Default.WriteLineAndConsole("Log: null");
                return;
            }
            MyLog.Default.WriteLineAndConsole("Log: " + message.ToString());
            MyAPIGateway.Utilities.ShowMessage("Log", message.ToString());
        }

        public static void Message(object message)
        {
            if (message == null)
            {
                MyAPIGateway.Utilities.ShowMessage("Log", "null");
                return;
            }
            MyAPIGateway.Utilities.ShowMessage("Log", message.ToString());
        }

        public static void Notify(object message, int ms = 2000, string color = "Blue", bool ping = false)
        {
            MyAPIGateway.Utilities.ShowNotification(message.ToString(), ms, color);
            if (ping)
                MyVisualScriptLogicProvider.PlayHudSoundLocal();
        }

    }
}
