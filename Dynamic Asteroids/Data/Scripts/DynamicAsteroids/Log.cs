using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using DynamicAsteroids;
using Sandbox.ModAPI;

namespace Invalid.DynamicRoids
{
    internal class Log
    {
        private static Log I;
        private readonly TextWriter _writer;
        private static List<string> _logBuffer = new List<string>();
        private static Timer _logTimer;

        private Log()
        {
            var logFileName = MyAPIGateway.Session.IsServer ? "DynamicAsteroids_Server.log" : "DynamicAsteroids_Client.log";
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(logFileName);
            _writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(logFileName);
            _writer.WriteLine($"      Dynamic Asteroids - {(MyAPIGateway.Session.IsServer ? "Server" : "Client")} Debug Log\n===========================================\n");
            _writer.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: Logger initialized for {(MyAPIGateway.Session.IsServer ? "Server" : "Client")}");
            _writer.Flush();

            _logTimer = new Timer(5000); // Set the interval to 5 seconds
            _logTimer.Elapsed += FlushLogs;
            _logTimer.Start();
        }

        public static void Info(string message)
        {
            if (AsteroidSettings.EnableLogging)
                I?._BufferLog(message);
        }

        public static void ServerInfo(string message)
        {
            if (AsteroidSettings.EnableLogging && MyAPIGateway.Session.IsServer)
                I?._BufferLog("Server: " + message);
        }

        public static void ClientInfo(string message)
        {
            if (AsteroidSettings.EnableLogging && !MyAPIGateway.Session.IsServer)
                I?._BufferLog("Client: " + message);
        }

        public static void Warning(string message)
        {
            if (AsteroidSettings.EnableLogging)
                I?._BufferLog("WARNING: " + message);
        }

        public static void Exception(Exception ex, Type callingType, string prefix = "")
        {
            if (AsteroidSettings.EnableLogging)
                I?._BufferLogException(ex, callingType, prefix);
        }

        public static void Init()
        {
            Close();
            I = new Log();
        }

        public static void Close()
        {
            if (I != null)
            {
                Info("Closing log writer.");
                FlushLogs(null, null);
                I._writer.Close();
            }

            I = null;
        }

        private void _BufferLog(string message)
        {
            lock (_logBuffer)
            {
                _logBuffer.Add($"{DateTime.UtcNow:HH:mm:ss}: {message}");
            }
        }

        private void _BufferLogException(Exception ex, Type callingType, string prefix = "")
        {
            if (ex == null)
            {
                _BufferLog("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            _BufferLog(prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}");
            MyAPIGateway.Utilities.ShowNotification($"{ex.GetType().Name} in Dynamic Asteroids! Check logs for more info.", 10000, "Red");
        }

        private static void FlushLogs(object sender, ElapsedEventArgs e)
        {
            lock (_logBuffer)
            {
                if (_logBuffer.Count > 0)
                {
                    foreach (var log in _logBuffer)
                    {
                        I?._writer.WriteLine(log);
                    }
                    _logBuffer.Clear();
                    I?._writer.Flush();
                }
            }
        }
    }
}
