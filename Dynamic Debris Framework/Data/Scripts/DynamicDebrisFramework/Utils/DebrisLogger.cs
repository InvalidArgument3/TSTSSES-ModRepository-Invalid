using Sandbox.ModAPI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicDebrisFramework.Utils {
    internal class DebrisLogger {
        private class LogEntry {
            public string Message { get; set; }
            public int Count { get; set; }
            public DateTime FirstOccurrence { get; set; }
            public DateTime LastOccurrence { get; set; }
        }

        private static DebrisLogger I;
        private readonly TextWriter _writer;
        private readonly ConcurrentDictionary<string, LogEntry> _cachedMessages;
        private readonly object _lockObject;
        private readonly int _flushIntervalSeconds;
        private DateTime _lastFlushTime;

        private DebrisLogger() {
            var logFileName = MyAPIGateway.Session.IsServer ? "DynamicAsteroids_Server.log" : "DynamicAsteroids_Client.log";
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(logFileName);
            _writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(logFileName);
            _writer.WriteLine($"      Dynamic Asteroids - {(MyAPIGateway.Session.IsServer ? "Server" : "Client")} Debug DebrisLogger\n===========================================\n");
            _writer.WriteLine($"{DateTime.UtcNow:HH:mm:ss}: Logger initialized for {(MyAPIGateway.Session.IsServer ? "Server" : "Client")}");
            _writer.Flush();

            _cachedMessages = new ConcurrentDictionary<string, LogEntry>();
            _flushIntervalSeconds = 1;
            _lockObject = new object();
            _lastFlushTime = DateTime.UtcNow;
        }

        public static void Info(string message) {
            if (DebrisConfig.EnableLogging)
                I?.CacheLogMessage(message);
        }

        public static void Warning(string message) {
            if (DebrisConfig.EnableLogging)
                I?.WriteToFile("WARNING: " + message);
        }

        public static void Exception(Exception ex, Type callingType, string prefix = "") {
            if (DebrisConfig.EnableLogging)
                I?._LogException(ex, callingType, prefix);
        }

        public static void Init() {
            Close();
            I = new DebrisLogger();
        }

        public static void Close() {
            if (I != null) {
                Info("Closing log writer.");
                I.FlushCache();
                I._writer.Close();
            }

            I = null;
        }

        public static void Update() {
            if (I != null && (DateTime.UtcNow - I._lastFlushTime).TotalSeconds >= I._flushIntervalSeconds) {
                I.FlushCache();
            }
        }

        private void CacheLogMessage(string message) {
            _cachedMessages.AddOrUpdate(
                message,
                new LogEntry {
                    Message = message,
                    Count = 1,
                    FirstOccurrence = DateTime.UtcNow,
                    LastOccurrence = DateTime.UtcNow
                },
                (key, existing) => {
                    existing.Count++;
                    existing.LastOccurrence = DateTime.UtcNow;
                    return existing;
                });
        }

        private void FlushCache() {
            lock (_lockObject) {
                DateTime currentTime = DateTime.UtcNow;
                var entriesToRemove = new List<string>();

                foreach (var kvp in _cachedMessages) {
                    LogEntry entry = kvp.Value;
                    if (!((currentTime - entry.LastOccurrence).TotalSeconds >= _flushIntervalSeconds)) continue;
                    string logMessage = entry.Count > 1
                        ? $"[{currentTime:HH:mm:ss}]: Repeated {entry.Count} times in {(entry.LastOccurrence - entry.FirstOccurrence).TotalSeconds:F1}s: {entry.Message}"
                        : $"[{currentTime:HH:mm:ss}]: {entry.Message}";

                    WriteToFile(logMessage);
                    entriesToRemove.Add(kvp.Key);
                }

                foreach (var key in entriesToRemove) {
                    LogEntry removedEntry;
                    _cachedMessages.TryRemove(key, out removedEntry);
                }

                _lastFlushTime = currentTime;
            }
        }

        private void WriteToFile(string message) {
            _writer.WriteLine(message);
            _writer.Flush();
        }

        private void _LogException(Exception ex, Type callingType, string prefix = "") {
            if (ex == null) {
                WriteToFile("Null exception! CallingType: " + callingType.FullName);
                return;
            }

            string exceptionMessage = prefix + $"Exception in {callingType.FullName}! {ex.Message}\n{ex.StackTrace}\n{ex.InnerException}";

            WriteToFile(exceptionMessage);
            MyAPIGateway.Utilities.ShowNotification($"{ex.GetType().Name} in Dynamic Asteroids! Check logs for more info.", 10000, "Red");
        }
    }

}
