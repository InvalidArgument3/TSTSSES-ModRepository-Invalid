using System;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRage.Game.ModAPI;
using System.IO;
using VRage.Game;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace TeleportMechanisms
{
    public static class MyLogger
    {
        private const string LogFileName = "TPGatewayMod.log";
        private const string ConfigFileName = "TPGateway.ini";
        private static System.IO.TextWriter _writer = null;
        private static StringBuilder _cache = new StringBuilder();

        private static MyIni _config = new MyIni();
        private static bool _writeToCustomLog;
        private static bool _writeToIngameLog;

        private const int CurrentConfigVersion = 1;

        private static object _lock = new object();
        private static bool _isInitialized = false;

        public static void LoadConfig()
        {
            lock (_lock)
            {
                if (_isInitialized) return;

                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(ConfigFileName, typeof(MyLogger)))
                {
                    CreateDefaultConfig();
                }
                else
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(ConfigFileName, typeof(MyLogger)))
                    {
                        string configContent = reader.ReadToEnd();
                        MyIniParseResult result;
                        if (!_config.TryParse(configContent, out result))
                        {
                            throw new Exception($"Failed to parse config file: {result.ToString()}");
                        }
                    }

                    if (!_config.ContainsSection("Version") || _config.Get("Version", "ConfigVersion").ToInt32() < CurrentConfigVersion)
                    {
                        UpdateConfig();
                    }
                }

                _writeToCustomLog = _config.Get("Logging", "WriteToCustomLog").ToBoolean(false);
                _writeToIngameLog = _config.Get("Logging", "WriteToIngameLog").ToBoolean(false);

                WriteLocationsToIngameLog();

                _isInitialized = true;
            }
        }

        private static void WriteLocationsToIngameLog()
        {
            string message = $"TPGatewayMod: Startup Information\n" +
                             $"Config file: {ConfigFileName}\n" +
                             $"Log file: {LogFileName}\n" +
                             $"Current settings - WriteToCustomLog: {_writeToCustomLog}, WriteToIngameLog: {_writeToIngameLog}\n";

            MyLog.Default.WriteLineAndConsole(message);
        }

        private static void CreateDefaultConfig()
        {
            _config.Clear();
            _config.Set("Version", "ConfigVersion", CurrentConfigVersion);
            _config.Set("Logging", "WriteToCustomLog", false);
            _config.Set("Logging", "WriteToIngameLog", false);
            SaveConfig();
        }

        private static void UpdateConfig()
        {
            _config.Set("Version", "ConfigVersion", CurrentConfigVersion);
            SaveConfig();
        }

        private static void SaveConfig()
        {
            using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(ConfigFileName, typeof(MyLogger)))
            {
                writer.Write(_config.ToString());
            }
        }

        public static void Log(string message)
        {
            string logMessage = $"TPGatewayMod: {DateTime.Now}: {message}";

            if (_writeToCustomLog)
            {
                WriteToFile(logMessage);
            }

            if (_writeToIngameLog)
            {
                MyLog.Default.WriteLine(logMessage);
            }
        }

        private static void WriteToFile(string message)
        {
            try
            {
                lock (_cache)
                {
                    _cache.Append(DateTime.Now.ToString("u")).Append(": ").Append(message).AppendLine();

                    if (_writer == null)
                    {
                        _writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(LogFileName, typeof(MyLogger));
                    }

                    if (_writer != null)
                    {
                        _writer.Write(_cache);
                        _writer.Flush();
                        _cache.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole($"TPGatewayMod Error while logging message='{message}'\nLogger error: {e.Message}\n{e.StackTrace}");
            }
        }

        public static void Close()
        {
            lock (_cache)
            {
                if (_writer != null)
                {
                    _writer.Flush();
                    _writer.Close();
                    _writer.Dispose();
                    _writer = null;
                }

                _cache.Clear();
                _isInitialized = false;
            }
        }
    }
}