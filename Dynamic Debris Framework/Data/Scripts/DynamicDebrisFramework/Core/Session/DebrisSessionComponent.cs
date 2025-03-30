using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using System;
using System.Collections.Generic;
using VRageMath;
using DynamicDebrisFramework.Entities;
using DynamicDebrisFramework.Spawning;
using DynamicDebrisFramework.Utils;
using DynamicDebrisFramework.Zones;
using VRage.Game;


namespace DynamicDebrisFramework.Core.Session {
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class DebrisSessionComponent : MySessionComponentBase {
        public static DebrisSessionComponent Instance { get; private set; }

        private readonly DebrisNetworkManager _networkManager;
        private readonly SpawnManager _spawnManager;
        private readonly ZoneManager _zoneManager;
        private readonly DebrisEntityManager _entityManager;
        private readonly DebrisLogger _logger;
        private readonly DebrisConfig _config;

        private bool _isInitialized;
        private readonly Random _random;

        public DebrisSessionComponent() {
            Instance = this;
            _random = new Random(_config.Seed);
            _logger = new DebrisLogger();
            _networkManager = new DebrisNetworkManager();
            _spawnManager = new SpawnManager();
            _zoneManager = new ZoneManager();
            _entityManager = new DebrisEntityManager();
        }

        public override void LoadData() {
            try {
                _logger.Info("Loading Dynamic Debris Framework...");
                _config.LoadConfig();

                if (MyAPIGateway.Session.IsServer) {
                    InitializeServer();
                }
                else {
                    InitializeClient();
                }

                RegisterNetworkHandlers();
                _isInitialized = true;
                _logger.Info("Dynamic Debris Framework loaded successfully.");
            }
            catch (Exception ex) {
                _logger.Error($"Failed to load Dynamic Debris Framework: {ex}");
            }
        }

        private void InitializeServer() {
            _logger.Info("Initializing server components...");
            _spawnManager.Initialize(_config);
            _zoneManager.Initialize(_config);
            _entityManager.Initialize(_config);
        }

        private void InitializeClient() {
            _logger.Info("Initializing client components...");
            _networkManager.InitializeClient();
        }

        private void RegisterNetworkHandlers() {
            _logger.Info("Registering network handlers...");
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(
                _config.NetworkChannels.EntitySync,
                _networkManager.HandleEntitySync);
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(
                _config.NetworkChannels.ZoneSync,
                _networkManager.HandleZoneSync);
        }

        public override void UpdateAfterSimulation() {
            try {
                if (!_isInitialized) return;

                if (MyAPIGateway.Session.IsServer) {
                    UpdateServer();
                }

                UpdateClient();
            }
            catch (Exception ex) {
                _logger.Error($"Error in update cycle: {ex}");
            }
        }

        private void UpdateServer() {
            _zoneManager.Update();
            _spawnManager.Update();
            _entityManager.Update();
            _networkManager.SyncToClients();
        }

        private void UpdateClient() {
            _entityManager.UpdateClient();
            _zoneManager.UpdateClient();
        }

        protected override void UnloadData() {
            try {
                _logger.Info("Unloading Dynamic Debris Framework...");

                if (MyAPIGateway.Session?.IsServer == true) {
                    _entityManager.CleanupEntities();
                }

                UnregisterNetworkHandlers();

                _spawnManager.Close();
                _zoneManager.Close();
                _entityManager.Close();
                _networkManager.Close();
                _logger.Close();

                Instance = null;
            }
            catch (Exception ex) {
                _logger.Error($"Error during framework shutdown: {ex}");
            }
        }

        private void UnregisterNetworkHandlers() {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(
                _config.NetworkChannels.EntitySync,
                _networkManager.HandleEntitySync);
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(
                _config.NetworkChannels.ZoneSync,
                _networkManager.HandleZoneSync);
        }

        public Vector3D GetRandomVector() {
            double theta = _random.NextDouble() * 2.0 * Math.PI;
            double phi = Math.Acos(2.0 * _random.NextDouble() - 1.0);
            double sinPhi = Math.Sin(phi);

            return new Vector3D(
                sinPhi * Math.Cos(theta),
                sinPhi * Math.Sin(theta),
                Math.Cos(phi)
            );
        }

        public bool IsPlayerAdmin(IMyPlayer player) {
            return MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE
                   || MyAPIGateway.Session.IsUserAdmin(player.SteamUserId);
        }
    }
}
