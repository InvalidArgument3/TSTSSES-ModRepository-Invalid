using Sandbox.ModAPI;
using System;
using System.IO;

namespace CargoTeleporter
{
	public static class Logging
    {
        private static TextWriter writer = null;
        public static void Setup()
        {
            try
            {
                writer = MyAPIGateway.Utilities.WriteFileInLocalStorage("CargoTeleport" + ".log", typeof(Logging));
            }
            catch { }
        }

        public static void WriteLine(string s)
        {
            try
            {
                if (writer == null)
                    Setup();
                writer.WriteLine(DateTime.Now.ToString("[HH:mm:ss] ") + s);
                writer.Flush();
            }
            catch { }
        }

        public static void Close()
        {
            try
            {
                if (writer != null)
                {
                    writer.Flush();
                    writer.Close();
                }
            }
            catch { }

        }
    }
}
