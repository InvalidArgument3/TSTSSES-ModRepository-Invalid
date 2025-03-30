using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using Sandbox.Game;
using VRage.Game;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRage;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using VRage.Input;
using System.Runtime.CompilerServices;
using Sandbox.Definitions;
using VRage.GameServices;
using System.Net;
using System.Linq;
using Sandbox.Game.Weapons.Guns;
using System.Collections.Concurrent;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Text;
using VRageRender;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Entities.Character;
using System.Net.Sockets;

namespace CustomHangar
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class Session : MySessionComponentBase
    {
        public bool isServer;
        public bool isDedicated;
        public ConcurrentDictionary<IMyFaction, ConcurrentQueue<IMyCubeGrid>> factionGrids = new ConcurrentDictionary<IMyFaction, ConcurrentQueue<IMyCubeGrid>>();
        public ConcurrentDictionary<long, ConcurrentQueue<IMyCubeGrid>> playerGrids = new ConcurrentDictionary<long, ConcurrentQueue<IMyCubeGrid>>();
        public static Session Instance;
        public readonly ushort NetworkHandle = 2355;
        
        public Dictionary<long, int> factionSlotTotals = new Dictionary<long, int>();
        public Dictionary<long, int> privateSlotTotals = new Dictionary<long, int>();
        public Config config;
        public List<MyObjectBuilder_Identity> allIdentities = new List<MyObjectBuilder_Identity>(); 
        public const string path = "{0}.xml";
        public List<HangarDelayData> hangarDelay = new List<HangarDelayData>();
        public int ticks;
        public List<long> npcs = new List<long>();

        public AllHangarData allHangarData = new AllHangarData();
        public CacheGridsForStorage gridsToStore = new CacheGridsForStorage();
        public List<IMyCubeBlock> enemyBlockCheckList = new List<IMyCubeBlock>();
        public Dictionary<IMyFaction, FactionTimers> cooldownTimers = new Dictionary<IMyFaction, FactionTimers>();
        public List<string> cacheGridPaths = new List<string>();

        //ClientSide Vars
        public List<MyCubeGrid> previewGrids = new List<MyCubeGrid>();
        private bool enableInput;
        private bool RotateX;
        private bool RotateY;
        private bool RotateZ;
        private bool nRotateX;
        private bool nRotateY;
        private bool nRotateZ;
        private float RotationSpeed = 0.01f;
        private int previewDistance = 50;
        private bool allowSpawn;
        private int spawnIndex = -1;
        private HangarType hangarType = HangarType.Faction;
        public Vector3D original = new Vector3D();
        public float previewMass;
        public SpawnType spawnType = SpawnType.None;
        public bool useInverseSpawnArea;
        public IMyPlayer playerCache = null;
        private Vector3D startCoordsCache = new Vector3D();
        private Vector3D endCoordCache= new Vector3D();
        public List<IMyGps> clientSpawnLocations = new List<IMyGps>();
        private bool init;
        private IMyHudNotification hudNotify;
        private long spawnCost;
        private SpawnError spawnError = SpawnError.None;
        public long playerWallet;
        public long factionWallet;
        private bool drawClientSphereDebug = true;
        public bool spawnClientGPS = true;

        // Client timers
        public int retrievalTimer;
        public int storeTimer;



        // TODO
        // Complete spawn areas inverse 
        // Complete enemy checks by block type
        // Check spawn for players/grids intersection before pasteing
        // Remove funds from faction/player wallets

        // Add check for small grids voxel intersection if config is set to false

        // Add change ownership of grid if leader spawns it and the owner is no longer apart of the faction
        // Add spawn config limits
        // Add private hangar load/transfer/list
        // Add potiental grids that can be stored nearby command
        // Remove items from hands when projecting grid clientside
        // Adding items to hand after projection will clear projection


        // Add an exclusion list to omit grids from being hangared by autohangar
        // ent.Components.Get<MyResourceSourceComponent>()
        // Add enemy checks for storing for faction/private hangars

        // Add exclusions to autohangar
        // Add Cost to spawn other options
        // Add bypass cost to spawn for autohangar on alls spawning options

        // Add velocity check when storing grids
        // Add limits check when using autohangar

        // *Fix check enemies by block type not working
        // *Add more logging/nofications
        // *Run spawning grids per physical connection in offset ticks
        // *Add grid below surface to autohangar
        // *Fix connectors not staying connected when spawning grid from autohangar(physical connections)
        // *Add nofication to first player that logins when their stuff has been autohangared
        // *Add damage resets/stops timer to delay grid storage
        // *Add plugin for admin commands via console
        // *Add velocity check when storing grids
        // *Add save file for client commands
        // *Check for SC cost for all spawning options
        // *Fix allPlayers not syncing in mp
        // *Fix FSZ crashing when checking smallgrids static/subgrids because autohangar is deleting grids, need to check all grids grid.MarkedForClose()
        // *Adjust spawn areas to be free for spawning autohangared grids but doesn't exist otherwise
        // *Clamp min cost for dynamic spawning with config
        // *Remove mechanical connections from potencial grids to store

        public override void BeforeStart()
        {
            Instance = this;
            isServer = MyAPIGateway.Session.IsServer;
            isDedicated = MyAPIGateway.Utilities.IsDedicated;
            MyAPIGateway.Multiplayer.RegisterMessageHandler(NetworkHandle, Comms.MessageHandler);
            MyAPIGateway.Utilities.MessageEntered += ChatHandler;

            if (isServer)
            {
                config = Config.LoadConfig();
                foreach (var area in config.spawnAreas)
                {
                    if (area.inverseArea)
                    {
                        useInverseSpawnArea = true;
                        break;
                    }
                }

                allHangarData = AllHangarData.LoadHangarData();

                if (isDedicated)
                {
                    if (config.enemyCheckConfig.enableBlockCheck)
                    {
                        //MyAPIGateway.Entities.OnEntityAdd += OnBlockAdded;
                        //MyAPIGateway.Entities.OnEntityAdd += OnBlockRemoved;
                        MyEntities.OnEntityCreate += OnBlockAdded;
                        MyEntities.OnEntityRemove += OnBlockRemoved;
                    }
                }
            }

            if (!isDedicated)
                MyVisualScriptLogicProvider.ToolEquipped += ToolEquipped;
        }

        public void UpdateIdentities()
        {
            var save = MyAPIGateway.Session.GetCheckpoint(MyAPIGateway.Session.Name);
            if (save == null) return;

            allIdentities = save.Identities;
            if (allIdentities == null)
                return;

            Comms.SendIdentitiesToClients(allIdentities);
        }

        private void CheckLastLogOff()
        {
            UpdateIdentities();
            if (!config.autoHangarConfig.enableAutoHangar) return;

            Dictionary<IMyFaction, bool> factionsToHangar = new Dictionary<IMyFaction, bool>();
            List<IMyFaction> expiredFactions = new List<IMyFaction>();
            List<long> expiredPlayers = new List<long>();
            
            foreach(var identity in allIdentities)
            {
                IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(identity.IdentityId);

                if (faction != null)
                {
                    if (faction.Tag.Length > 3 || faction.IsEveryoneNpc()) continue;

                    bool excludeFaction = false;
                    foreach(var excludedTag in config.autoHangarConfig.exclusions.excludedFactions.excludedFaction)
                    {
                        if (faction.Tag != excludedTag) continue;
                        excludeFaction = true;
                        break;
                    }
                    if (excludeFaction) continue;

                    if (!factionsToHangar.ContainsKey(faction))
                        factionsToHangar.Add(faction, true);
                }

                MyLog.Default.WriteLineAndConsole($"[FactionHangar] Player = {identity.DisplayName} Last Login: {DateTime.Now - identity.LastLogoutTime} / Config: {TimeSpan.FromDays(config.autoHangarConfig.daysFactionLogin)}");

                if (DateTime.Now - identity.LastLogoutTime >= TimeSpan.FromDays(config.autoHangarConfig.daysFactionLogin))
                {
                    MyLog.Default.WriteLineAndConsole($"[FactionHangar] Player {identity.DisplayName} hasn't logged in within {config.autoHangarConfig.daysFactionLogin} days.");

                    if (faction != null)
                    {
                        if (!factionsToHangar.ContainsKey(faction)) continue;
                        if (!factionsToHangar[faction]) continue;
                    }    
                    else
                    {
                        if (expiredPlayers.Contains(identity.IdentityId))
                            continue;
                        else
                            expiredPlayers.Add(identity.IdentityId);
                    }
                }
                else
                {
                    if (faction != null)
                    {
                        if (!factionsToHangar.ContainsKey(faction)) continue;
                        factionsToHangar[faction] = false;
                    }
                }
            }

            foreach(var faction in factionsToHangar.Keys)
            {
                if (factionsToHangar[faction])
                    expiredFactions.Add(faction);
            }

            if (expiredFactions.Count > 0 || expiredPlayers.Count > 0)
            {
                HashSet<IMyEntity> ents = new HashSet<IMyEntity>();
                List<IMyCubeGrid> physicalGroup = new List<IMyCubeGrid>();
                List<IMyCubeGrid> processedGrids = new List<IMyCubeGrid>();
                MyAPIGateway.Entities.GetEntities(ents);

                factionGrids.Clear();
                playerGrids.Clear();

                foreach(var ent in ents)
                {
                    IMyCubeGrid grid = ent as IMyCubeGrid;
                    if (grid == null) continue;

                    if (processedGrids.Contains(grid)) continue;
                    long owner = grid.BigOwners.FirstOrDefault();
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
                    GetGroupByType(grid, physicalGroup, GridLinkTypeEnum.Physical);
                    if (faction != null)
                    {
                        if (factionGrids.ContainsKey(faction))
                        {
                            if (factionGrids[faction].Contains(grid)) continue;

                            factionGrids[faction].Enqueue(grid);
                            processedGrids.Add(grid);
                            foreach(var connectedGrid in physicalGroup)
                            {
                                if (grid == connectedGrid) continue;
                                factionGrids[faction].Enqueue(grid);
                                processedGrids.Add(grid);
                            }
                        }
                        else
                        {
                            ConcurrentQueue<IMyCubeGrid> temp = new ConcurrentQueue<IMyCubeGrid>();
                            temp.Enqueue(grid);
                            processedGrids.Add(grid);
                            foreach (var connectedGrid in physicalGroup)
                            {
                                if (grid == connectedGrid) continue;
                                temp.Enqueue(grid);
                                processedGrids.Add(grid);
                            }
                            factionGrids.TryAdd(faction, temp);
                        }
                            
                    }
                    else
                    {
                        if (playerGrids.ContainsKey(owner))
                        {
                            playerGrids[owner].Enqueue(grid);
                            processedGrids.Add(grid);
                            foreach (var connectedGrid in physicalGroup)
                            {
                                if (grid == connectedGrid) continue;
                                playerGrids[owner].Enqueue(grid);
                                processedGrids.Add(grid);
                            }
                        } 
                        else
                        {
                            ConcurrentQueue<IMyCubeGrid> temp = new ConcurrentQueue<IMyCubeGrid>();
                            temp.Enqueue(grid);
                            processedGrids.Add(grid);
                            foreach (var connectedGrid in physicalGroup)
                            {
                                if (grid == connectedGrid) continue;
                                temp.Enqueue(grid);
                                processedGrids.Add(grid);
                            }
                            playerGrids.TryAdd(owner, temp);
                        }
                           
                    }
                }
            }

            if (expiredFactions.Count > 0)
            {
                foreach (var faction in expiredFactions)
                    AutoHangar(faction, 0);

            }

            if (expiredPlayers.Count > 0)
            {
                foreach (var playerId in expiredPlayers)
                    AutoHangar(null, playerId);
            }
            
        }

        private void AutoHangar(IMyFaction faction, long playerId)
        {
            if (faction != null)
            {
                ConcurrentQueue<IMyCubeGrid> grids = new ConcurrentQueue<IMyCubeGrid>();
                IMyCubeGrid grid = null;
                factionGrids.TryGetValue(faction, out grids);
                List<IMyCubeGrid> foundGridsList = new List<IMyCubeGrid>();

                if (grids == null)
                    return;

                while (grids.Count > 0)
                {
                    grid = null;
                    if (grids.TryDequeue(out grid))
                    {
                        if (grid == null) continue;

                        if (foundGridsList.Contains(grid)) continue;

                        List<IMyCubeGrid> connectedGrids = new List<IMyCubeGrid>();
                        GetGroupByType(grid, connectedGrids, GridLinkTypeEnum.Physical);
                        MyCubeGrid cubeGrid = grid as MyCubeGrid;
                        MyCubeGrid biggestGrid = cubeGrid.GetBiggestGridInGroup();
                        if (biggestGrid == null)
                            return;

                        bool excludeGrid = false;
                        foundGridsList.Add(grid);
                        if (Utils.CheckForExcludedBlock(grid as MyCubeGrid))
                            excludeGrid = true;

                        foreach (var connectedGrid in connectedGrids)
                        {
                            if (connectedGrid == grid) continue;
                            foundGridsList.Add(connectedGrid);
                            if (!excludeGrid)
                                if (Utils.CheckForExcludedBlock(connectedGrid as MyCubeGrid))
                                    excludeGrid = true;
                        }
                        if (excludeGrid) continue;

                        long owner = biggestGrid.BigOwners.FirstOrDefault();
                        string playerName = GetPlayerName(owner);
                        RequestingGridStorage(owner, owner, biggestGrid.EntityId, playerName, false, true);
                    }
                }
            }

            if (playerId != 0)
            {
                ConcurrentQueue<IMyCubeGrid> grids = new ConcurrentQueue<IMyCubeGrid>();
                IMyCubeGrid grid = null;
                playerGrids.TryGetValue(playerId, out grids);
                List<IMyCubeGrid> foundGridsList = new List<IMyCubeGrid>();

                if (grids == null)
                    return;

                while (grids.Count > 0)
                {
                    grid = null;
                    if (grids.TryDequeue(out grid))
                    {
                        if (grid == null) continue;
                        if (foundGridsList.Contains(grid)) continue;

                        List<IMyCubeGrid> connectedGrids = new List<IMyCubeGrid>();
                        GetGroupByType(grid, connectedGrids, GridLinkTypeEnum.Physical);
                        MyCubeGrid cubeGrid = grid as MyCubeGrid;
                        MyCubeGrid biggestGrid = cubeGrid.GetBiggestGridInGroup();

                        foundGridsList.Add(grid);
                        foreach (var connectedGrid in connectedGrids)
                        {
                            if (connectedGrid == grid) continue;
                            foundGridsList.Add(connectedGrid);
                        }

                        long owner = biggestGrid.BigOwners.FirstOrDefault();
                        string playerName = GetPlayerName(owner);
                        RequestingGridStorage(owner, owner, biggestGrid.EntityId, playerName, true, true);
                    }
                }
            }
        }

        public new void HandleInput()
        {
            if (isServer && isDedicated) return;
            if (!enableInput) return;

            if (playerCache.Character == null || playerCache.Character.IsDead)
                return;

            UpdatePosition();
            DetectSpawnType();
            //spawnCost = CalculateCost();
            //UpdateHudMessage();

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.Home))
                RotateX = true;
            else
                RotateX = false;
                
            if (MyAPIGateway.Input.IsKeyPress(MyKeys.End))
                nRotateX = true;
            else
                nRotateX = false;

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.Delete))
                RotateY = true;
            else
                RotateY = false;

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.PageDown))
                nRotateY = true;
            else
                nRotateY = false;

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.Insert))
                RotateZ = true;
            else
                RotateZ = false;

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.PageUp))
                nRotateZ = true;
            else
                nRotateZ = false;

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.D0))
                RemovePreviewGrids();

            if (MyAPIGateway.Input.IsNewLeftMousePressed())
                TrySpawnPlacement();

            if (MyAPIGateway.Input.IsKeyPress(MyKeys.LeftAlt))
            {
                var rotateSpeed = MyAPIGateway.Input.DeltaMouseScrollWheelValue();
                if (rotateSpeed > 0)
                    RotationSpeed += .001f;
                else if (rotateSpeed < 0)
                    RotationSpeed -= .001f;

                RotationSpeed = MathHelper.Clamp(RotationSpeed, .01f, .2f);
                return;
            }

            var dScroll = MyAPIGateway.Input.DeltaMouseScrollWheelValue();

            if (dScroll > 0)
                previewDistance++;
            else if (dScroll < 0)
                previewDistance--;

            previewDistance = MathHelper.Clamp(previewDistance, 10, 70);
        }

        private void UpdatePosition()
        {
            //startCoordsCache = playerCache.Character.GetPosition();
            //endCoordCache = playerCache.Character.WorldMatrix.Forward * previewDistance + startCoordsCache;
            startCoordsCache = MyAPIGateway.Session.Camera.Position;
            endCoordCache = MyAPIGateway.Session.Camera.WorldMatrix.Forward * previewDistance + startCoordsCache;
        }

        private void UpdateHudMessage()
        {
            if (allowSpawn)
                spawnError = SpawnError.None;

            if (hudNotify != null)
            {
                hudNotify.Hide();
                hudNotify.Text = $"[Valid Spawn] = {allowSpawn} | [Cost To Spawn] = {spawnCost} SC | [SpawnOption] = {spawnType} | [SpawningError] = {spawnError}";
                hudNotify.Show();
            }
        }

        private void DetectSpawnType(bool force = false)
        {
            if (!force)
                if (ticks % 10 != 0) return;

            if (previewGrids == null || previewGrids.Count == 0) return;
            spawnError = SpawnError.None;
            
            

            double distance = Vector3D.Distance(endCoordCache, original);

            if (config.spawnNearbyConfig.allowSpawnNearby)
            {
                if (distance <= config.spawnNearbyConfig.nearbyRadius)
                {
                    spawnType = SpawnType.Nearby;
                    bool result = IsEnemyNear();
                    if (result)
                        spawnError = SpawnError.EnemyNearby;

                    spawnCost = config.spawnNearbyConfig.nearbySpawnCost;
                    bool canAfford = spawnCost <= factionWallet ? true : spawnCost <= playerWallet;
                    if (!canAfford)
                        spawnError = SpawnError.InsuffientFunds;

                    bool intersecting = Utils.IsGridIntersecting(previewGrids);
                    if (intersecting)
                        spawnError = SpawnError.EntityBlockingPlacement;

                    allowSpawn = !result && canAfford && !intersecting;
                    UpdateHudMessage();
                    return;
                }
            }

            foreach (var area in config.spawnAreas)
            {
                if (!area.enableSpawnArea) continue;
                if (useInverseSpawnArea)
                {
                    if (Vector3D.Distance(endCoordCache, area.areaCenter) <= area.areaRadius) continue;
                    spawnType = SpawnType.SpawnArea;
                    spawnCost = area.spawnAreaCost;
                    bool result = IsEnemyNear(area);
                    if (result)
                        spawnError = SpawnError.EnemyNearby;

                    bool canAfford = spawnCost <= factionWallet ? true : spawnCost <= playerWallet;
                    if (!canAfford)
                        spawnError = SpawnError.InsuffientFunds;

                    bool intersecting = Utils.IsGridIntersecting(previewGrids);
                    if (intersecting)
                        spawnError = SpawnError.EntityBlockingPlacement;

                    allowSpawn = !result && canAfford && !intersecting;
                    UpdateHudMessage();
                    return;
                }
                else
                {
                    if (Vector3D.Distance(endCoordCache, area.areaCenter) > area.areaRadius) continue;
                    spawnType = SpawnType.SpawnArea;
                    spawnCost = area.spawnAreaCost;
                    bool result2 = IsEnemyNear(area);
                    if (result2)
                        spawnError = SpawnError.EnemyNearby;

                    bool canAfford2 = spawnCost <= factionWallet ? true : spawnCost <= playerWallet;
                    if (!canAfford2)
                        spawnError = SpawnError.InsuffientFunds;

                    bool intersecting2 = Utils.IsGridIntersecting(previewGrids);
                    if (intersecting2)
                        spawnError = SpawnError.EntityBlockingPlacement;

                    allowSpawn = !result2 && canAfford2 && !intersecting2;
                    UpdateHudMessage();
                    return;
                }
            }

            if (config.dynamicSpawningConfig.enableDynamicSpawning)
            {
                spawnCost = CalculateCost();
                if (!useInverseSpawnArea)
                {
                    spawnType = SpawnType.Dynamic;
                    bool result = IsEnemyNear();
                    if (result)
                        spawnError = SpawnError.EnemyNearby;

                    bool canAfford = spawnCost <= factionWallet ? true : spawnCost <= playerWallet;
                    if (!canAfford)
                        spawnError = SpawnError.InsuffientFunds;

                    bool intersecting = Utils.IsGridIntersecting(previewGrids);
                    if (intersecting)
                        spawnError = SpawnError.EntityBlockingPlacement;

                    allowSpawn = !result && canAfford && !intersecting;
                    UpdateHudMessage();
                    return;
                }
            }

            allowSpawn = false;
            spawnError = SpawnError.InvalidSpawningLocation;
            UpdateHudMessage();
        }

        public bool IsEnemyNear()
        {
            if (previewGrids == null || previewGrids.Count == 0) return false;
            long spawnOwner = isDedicated && isServer ? previewGrids[0].BigOwners.FirstOrDefault() : playerCache.IdentityId;
            List<MyEntity> ents = new List<MyEntity>();
            if (!config.enemyCheckConfig.enableBlockCheck)
            {
                if (spawnType == SpawnType.Nearby && config.spawnNearbyConfig.nearbyEnemyCheck.checkEnemiesNearby)
                {
                    GetEntitiesInSphere(ents, endCoordCache, config.spawnNearbyConfig.nearbyEnemyCheck.enemyDistanceCheck);
                    return IsGridEnemy(ents, spawnOwner, config.spawnNearbyConfig.nearbyEnemyCheck.alliesFriendly, config.spawnNearbyConfig.nearbyEnemyCheck.omitNPCs);
                }

                if (spawnType == SpawnType.Dynamic && config.dynamicSpawningConfig.dynamicEnemyCheck.checkEnemiesNearby)
                {
                    GetEntitiesInSphere(ents, endCoordCache, config.dynamicSpawningConfig.dynamicEnemyCheck.enemyDistanceCheck);
                    return IsGridEnemy(ents, spawnOwner, config.dynamicSpawningConfig.dynamicEnemyCheck.alliesFriendly, config.dynamicSpawningConfig.dynamicEnemyCheck.omitNPCs);
                }
            }
            else
            {
                if (spawnType == SpawnType.Nearby && config.spawnNearbyConfig.nearbyEnemyCheck.checkEnemiesNearby)
                {
                    foreach (var block in enemyBlockCheckList)
                    {
                        if (Vector3D.Distance(endCoordCache, block.GetPosition()) > config.spawnNearbyConfig.nearbyEnemyCheck.enemyDistanceCheck) continue;
                        bool result = IsBlockEnemy(block, spawnOwner, config.spawnNearbyConfig.nearbyEnemyCheck.alliesFriendly, config.spawnNearbyConfig.nearbyEnemyCheck.omitNPCs);

                        if (result)
                            return true;
                    }
                }

                if (spawnType == SpawnType.Dynamic && config.dynamicSpawningConfig.dynamicEnemyCheck.checkEnemiesNearby)
                {
                    foreach (var block in enemyBlockCheckList)
                    {
                        if (Vector3D.Distance(endCoordCache, block.GetPosition()) > config.dynamicSpawningConfig.dynamicEnemyCheck.enemyDistanceCheck) continue;
                        bool result = IsBlockEnemy(block, spawnOwner, config.dynamicSpawningConfig.dynamicEnemyCheck.alliesFriendly, config.dynamicSpawningConfig.dynamicEnemyCheck.omitNPCs);

                        if (result)
                            return true;
                    }
                }
            }

            return false;
        }

        public bool IsEnemyNear(MyObjectBuilder_CubeGrid gridOb, long gridOwner)
        {
            gridOwner = isServer && isDedicated ? gridOwner : playerCache.IdentityId;
            Vector3D pos = gridOb.PositionAndOrientation.Value.Position;
            List<MyEntity> ents = new List<MyEntity>();

            if (!config.enemyCheckConfig.enableBlockCheck)
            {
                if (spawnType == SpawnType.Original && config.spawnOriginalConfig.originalEnemyCheck.checkEnemiesNearby)
                {
                    GetEntitiesInSphere(ents, pos, config.spawnOriginalConfig.originalEnemyCheck.enemyDistanceCheck);
                    return IsGridEnemy(ents, gridOwner, config.spawnOriginalConfig.originalEnemyCheck.alliesFriendly, config.spawnOriginalConfig.originalEnemyCheck.omitNPCs);
                }
            }
            else
            {
                if (spawnType == SpawnType.Original && config.spawnOriginalConfig.originalEnemyCheck.checkEnemiesNearby)
                {
                    foreach (var block in enemyBlockCheckList)
                    {
                        if (Vector3D.Distance(pos, block.GetPosition()) > config.spawnOriginalConfig.originalEnemyCheck.enemyDistanceCheck) continue;
                        bool result = IsBlockEnemy(block, gridOwner, config.spawnOriginalConfig.originalEnemyCheck.alliesFriendly, config.spawnOriginalConfig.originalEnemyCheck.omitNPCs);

                        if (result)
                            return true;
                    }
                }
            }
            
            return false;
        }

        private bool IsEnemyNear(SpawnAreas area)
        {
            if (previewGrids == null || previewGrids.Count == 0) return false;
            long spawnOwner = isServer && isDedicated ? previewGrids[0].BigOwners.FirstOrDefault() : playerCache.IdentityId;
            List<MyEntity> ents = new List<MyEntity>();

            if (!config.enemyCheckConfig.enableBlockCheck)
            {
                if (area.spawnAreasEnemyCheck.checkEnemiesNearby)
                {
                    GetEntitiesInSphere(ents, endCoordCache, area.spawnAreasEnemyCheck.enemyDistanceCheck);
                    return IsGridEnemy(ents, spawnOwner, area.spawnAreasEnemyCheck.alliesFriendly, area.spawnAreasEnemyCheck.omitNPCs);
                }
            }
            else
            {
                if (area.spawnAreasEnemyCheck.checkEnemiesNearby)
                {
                    foreach (var block in enemyBlockCheckList)
                    {
                        if (Vector3D.Distance(block.GetPosition(), endCoordCache) > area.spawnAreasEnemyCheck.enemyDistanceCheck) continue;
                        bool result = IsBlockEnemy(block, spawnOwner, area.spawnAreasEnemyCheck.alliesFriendly, area.spawnAreasEnemyCheck.omitNPCs);

                        if (result)
                            return true;
                    }
                }
            }
                
            return false;
        }

        private bool IsEnemyNearStoring(IMyCubeGrid grid, HangarType hangarType)
        {
            if (grid == null) return false;
            long owner = isServer && isDedicated ? grid.BigOwners.FirstOrDefault() : playerCache.IdentityId;
            List<MyEntity> ents = new List<MyEntity>();

            if (!config.enemyCheckConfig.enableBlockCheck)
            {
                if (hangarType == HangarType.Faction)
                {
                    if (config.factionHangarConfig.factionHangarEnemyCheck.checkEnemiesNearby)
                    {
                        GetEntitiesInSphere(ents, grid.GetPosition(), config.factionHangarConfig.factionHangarEnemyCheck.enemyDistanceCheck);
                        return IsGridEnemy(ents, owner, config.factionHangarConfig.factionHangarEnemyCheck.alliesFriendly, config.factionHangarConfig.factionHangarEnemyCheck.omitNPCs);
                    }
                }

                if (hangarType == HangarType.Private)
                {
                    if (config.privateHangarConfig.privateHangarEnemyCheck.checkEnemiesNearby)
                    {
                        GetEntitiesInSphere(ents, grid.GetPosition(), config.privateHangarConfig.privateHangarEnemyCheck.enemyDistanceCheck);
                        return IsGridEnemy(ents, owner, config.privateHangarConfig.privateHangarEnemyCheck.alliesFriendly, config.privateHangarConfig.privateHangarEnemyCheck.omitNPCs);
                    }
                }
            }
            else
            {
                if (hangarType == HangarType.Faction)
                {
                    if (config.factionHangarConfig.factionHangarEnemyCheck.checkEnemiesNearby)
                    {
                        foreach (var block in enemyBlockCheckList)
                        {
                            if (Vector3D.Distance(grid.GetPosition(), block.GetPosition()) > config.factionHangarConfig.factionHangarEnemyCheck.enemyDistanceCheck) continue;
                            bool result = IsBlockEnemy(block, owner, config.factionHangarConfig.factionHangarEnemyCheck.alliesFriendly, config.privateHangarConfig.privateHangarEnemyCheck.omitNPCs);

                            if (result)
                                return true;
                        }
                    }

                    if (config.privateHangarConfig.privateHangarEnemyCheck.checkEnemiesNearby)
                    {
                        foreach (var block in enemyBlockCheckList)
                        {
                            if (Vector3D.Distance(grid.GetPosition(), block.GetPosition()) > config.privateHangarConfig.privateHangarEnemyCheck.enemyDistanceCheck) continue;
                            bool result = IsBlockEnemy(block, owner, config.privateHangarConfig.privateHangarEnemyCheck.alliesFriendly, config.privateHangarConfig.privateHangarEnemyCheck.omitNPCs);

                            if (result)
                                return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool IsGridEnemy(List<MyEntity> ents, long spawnOwner, bool alliesFriendly, bool omitNPC)
        {
            IMyFaction myFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(spawnOwner);
            foreach (var ent in ents)
            {
                if (ent.MarkedForClose) continue;
                IMyCubeGrid grid = ent as IMyCubeGrid;
                if (grid == null) continue;
                if (grid.Physics == null) continue;
                //if (grid == previewGrids[0] || grid.IsSameConstructAs(previewGrids[0])) continue;

                long owner = grid.BigOwners.FirstOrDefault();
                if (owner == spawnOwner || owner == 0) continue;
                IMyFaction otherFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
                if (otherFaction != null && omitNPC)
                    if (otherFaction.IsEveryoneNpc() || otherFaction.Tag.Length > 3) return false;

                bool result = AreFactionsEnemies(myFaction, otherFaction, alliesFriendly);

                if (result) return true;
            }

            return false;
        }

        private bool IsBlockEnemy(IMyCubeBlock block, long spawnOwner, bool alliesFriendly, bool omitNPC)
        {
            IMyFaction myFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(spawnOwner);


            long owner = block.OwnerId;
            if (owner == spawnOwner || owner == 0) return false;
            IMyFaction otherFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
            if (otherFaction != null && omitNPC)
                if (otherFaction.IsEveryoneNpc() || otherFaction.Tag.Length > 3) return false;

            bool result = AreFactionsEnemies(myFaction, otherFaction, alliesFriendly);
            if (result) return true;

            return false;
        }

        private bool AreFactionsEnemies(IMyFaction faction1, IMyFaction faction2, bool alliesFriendly)
        {
            if (faction1 == null || faction2 == null) return true;
            if (faction1 == faction2) return false;

            var relation = MyAPIGateway.Session.Factions.GetRelationBetweenFactions(faction1.FactionId, faction2.FactionId);
            if (relation == MyRelationsBetweenFactions.Enemies) return true;
            if (relation == MyRelationsBetweenFactions.Friends)
                if (!alliesFriendly) return true;

            return false;
        }

        public long CalculateCost()
        {
            float cost = 0;
            double distance = Vector3D.Distance(endCoordCache, original);

            if (spawnType == SpawnType.Dynamic)
            {
                cost = (previewMass * (float)distance) * config.dynamicSpawningConfig.costMultiplier;
                return (long)cost;
            }

            return 0;
        }

        private void DrawBoundingBox()
        {
            List<MyEntity> entities = new List<MyEntity>();
            GetEntitiesInSphere(entities, playerCache.GetPosition(), 200);

            Color otherCol = Color.White;
            foreach (var ent in entities)
            {
                IMyCubeGrid grid = ent as IMyCubeGrid;
                if (grid == null) continue;

                if (previewGrids.Contains(grid)) continue;
                var myPosition = playerCache.GetPosition();
                var vectorToAste = grid.GetPosition() - myPosition;
                var relativeVector = Vector3D.TransformNormal(vectorToAste, MatrixD.Transpose(playerCache.Character.WorldMatrix));
                if (relativeVector.Z > 0) // behind us
                    continue;

                BoundingBoxD boundingBoxD = grid.PositionComp.LocalAABB;
                MatrixD matrixD = grid.PositionComp.WorldMatrixRef;
                MySimpleObjectDraw.DrawTransparentBox(ref matrixD, ref boundingBoxD, ref otherCol, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, null, MyStringId.GetOrCompute("WeaponLaser"), false, -1, MyBillboard.BlendTypeEnum.Standard, 1f, null);
            }

            Color color = allowSpawn ? Color.LightGreen : Color.Red;
            foreach (var grid in previewGrids)
            {
                BoundingBoxD boundingBoxD = grid.PositionComp.LocalAABB;
                MatrixD matrixD = grid.PositionComp.WorldMatrixRef;
                MySimpleObjectDraw.DrawTransparentBox(ref matrixD, ref boundingBoxD, ref color, MySimpleObjectRasterizer.Wireframe, 1, 0.04f, null, MyStringId.GetOrCompute("WeaponLaser"), false, -1, MyBillboard.BlendTypeEnum.Standard, 1f, null);
            }
        }

        public override void Draw()
        {
            if (isServer && isDedicated) return;
            if (previewGrids != null && previewGrids.Count != 0 && previewMass != 0)
            {
                if (playerCache.Character == null || playerCache.Character.IsDead)
                {
                    RemovePreviewGrids();
                    return;
                }

                DrawSpawnAreas();
                DrawBoundingBox();
                enableInput = true;

                IMyEntity ent = previewGrids[0] as IMyEntity;
                var center = ent.WorldAABB.Center;
                var matrix = ent.WorldMatrix;
                var entPos = ent.GetPosition();
                var diff = entPos - center;

                //Vector3D startCoords = playerCache.Character.GetPosition();
                //Vector3D endCoords = playerCache.Character.WorldMatrix.Forward * previewDistance + startCoords;
                Vector3D startCoords = MyAPIGateway.Session.Camera.Position;
                Vector3D endCoords = MyAPIGateway.Session.Camera.WorldMatrix.Forward * previewDistance + startCoords;
                //MatrixD endCoordsMatrix = MatrixD.CreateWorld(endCoords);

                ent.PositionComp.SetPosition(endCoords + diff);

                for (int i = 0; i < previewGrids.Count; i++)
                {
                    if (i == 0) continue;
                    IMyEntity subEnt = previewGrids[i] as IMyEntity;
                    var subDiff = subEnt.GetPosition() - entPos;
                    subEnt.PositionComp.SetPosition(endCoords + subDiff + diff);
                }

                if (RotateX || RotateY || RotateZ || nRotateX || nRotateY || nRotateZ)
                {
                    center = ent.WorldAABB.Center;
                    matrix = ent.WorldMatrix;
                    var OffsetVector3 = center;
                    MatrixD rotationMatrix = MatrixD.Zero;
                    var up = matrix.Up;
                    var left = matrix.Left;
                    var forward = matrix.Forward;
                    up = Vector3D.Normalize(up);
                    left = Vector3D.Normalize(left);
                    forward = Vector3D.Normalize(forward);

                    if (RotateX) rotationMatrix = MatrixD.CreateFromAxisAngle(left, RotationSpeed);
                    if (nRotateX) rotationMatrix = MatrixD.CreateFromAxisAngle(-left, RotationSpeed);

                    if (RotateY) rotationMatrix = MatrixD.CreateFromAxisAngle(up, RotationSpeed);
                    if (nRotateY) rotationMatrix = MatrixD.CreateFromAxisAngle(-up, RotationSpeed);

                    if (RotateZ) rotationMatrix = MatrixD.CreateFromAxisAngle(forward, RotationSpeed);
                    if (nRotateZ) rotationMatrix = MatrixD.CreateFromAxisAngle(-forward, RotationSpeed);

                    /*if (RotateY)
                    {
                        if (RotateX) rotationMatrix *= MatrixD.CreateFromAxisAngle(up, RotationSpeed);
                        else rotationMatrix = MatrixD.CreateFromAxisAngle(up, RotationSpeed);
                    }

                    if (RotateZ)
                    {
                        if (RotateY || RotateX) rotationMatrix *= MatrixD.CreateFromAxisAngle(forward, RotationSpeed);
                        else rotationMatrix = MatrixD.CreateFromAxisAngle(forward, RotationSpeed);
                    }*/

                    rotationMatrix = MatrixD.CreateTranslation(-OffsetVector3) * rotationMatrix * MatrixD.CreateTranslation(OffsetVector3);

                    matrix *= rotationMatrix;
                    ent.SetWorldMatrix(matrix);
                    UpdateSubgrids(rotationMatrix);
                }

            }
            else
                enableInput = false;
        }

        public void UpdateSubgrids(MatrixD rotationMat)
        {
            for (int i = 0; i < previewGrids.Count; i++)
            {
                if (i == 0) continue;

                MatrixD matrix = previewGrids[i].WorldMatrix;
                matrix *= rotationMat;
                previewGrids[i].PositionComp.SetWorldMatrix(ref matrix);
            }
        }

        private void DrawSpawnAreas()
        {
            if (!drawClientSphereDebug) return;
            foreach(var area in config.spawnAreas)
            {
                if (!area.enableSpawnArea) continue;

                MatrixD mat = MatrixD.CreateWorld(area.areaCenter);
                Color color = Color.LightBlue;
                MySimpleObjectDraw.DrawTransparentSphere(ref mat, area.areaRadius, ref color, MySimpleObjectRasterizer.Wireframe, 70, null, MyStringId.GetOrCompute("WeaponLaser"), 1f, -1, null, VRageRender.MyBillboard.BlendTypeEnum.Standard, 10f);
            }

            if (config.spawnNearbyConfig.allowSpawnNearby)
            {
                MatrixD mat = MatrixD.CreateWorld(original);
                Color color = Color.LightGreen;
                MySimpleObjectDraw.DrawTransparentSphere(ref mat, config.spawnNearbyConfig.nearbyRadius, ref color, MySimpleObjectRasterizer.Wireframe, 70, null, MyStringId.GetOrCompute("WeaponLaser"), .09f, -1, null, VRageRender.MyBillboard.BlendTypeEnum.Standard, 10f);
            }
        }

        public override void UpdateBeforeSimulation()
        {
            Init();
            HandleInput();

            ticks++;

            RunClientTimers();

            // Server Only
            if (!isServer) return;
            

            // Runs every 60 ticks (1 sec)
            RunDelayTimers();
        }

        private void Init()
        {
            if (isServer && isDedicated)
            {
                if (init) return;

                if (config.autoHangarConfig.enableAutoHangar)
                    CheckLastLogOff();
                else
                    UpdateIdentities();

                init = true;
                return;
            }

            if (init) return;

            if (playerCache == null)
                playerCache = MyAPIGateway.Session.LocalHumanPlayer;
                
            if (playerCache != null)
            {
                if (config == null)
                {
                    Comms.ClientRequestConfig(playerCache.SteamUserId);
                    return;
                }

                if (config.enemyCheckConfig.enableBlockCheck)
                {
                    //MyAPIGateway.Entities.OnEntityAdd += OnBlockAdded;
                    //MyAPIGateway.Entities.OnEntityRemove += OnBlockRemoved;
                    MyEntities.OnEntityCreate += OnBlockAdded;
                    MyEntities.OnEntityRemove += OnBlockRemoved;
                    ClientGetBlocks();
                }

                if (isServer)
                {
                    if (config.autoHangarConfig.enableAutoHangar)
                        CheckLastLogOff();
                    else
                        UpdateIdentities();
                }

                init = true;
            }
                
        }

        public bool TrySpawnPlacement()
        {
            if (!allowSpawn) return false;

            long playerId = MyAPIGateway.Session.LocalHumanPlayer.IdentityId;
            List<MyObjectBuilder_CubeGrid> obs = new List<MyObjectBuilder_CubeGrid>();
            GetObFromPreview(obs);
            Comms.SendGridsToSpawn(obs, spawnIndex, hangarType, playerId, spawnCost, spawnType);

            RemovePreviewGrids();

            return true;
        }

        public void RemovePreviewGrids()
        {
            foreach (var grid in previewGrids)
                grid.Close();

            previewGrids.Clear();
            ResetClientValues();
            
        }

        private void ResetClientValues()
        {
            previewDistance = 50;
            allowSpawn = false;
            spawnIndex = -1;
            hangarType = HangarType.Faction;
            original = new Vector3D();
            previewMass = 0;
            useInverseSpawnArea = false;
            spawnType = SpawnType.None;
            spawnCost = 0;
            hudNotify.Hide();
            hudNotify = null;
            spawnError = SpawnError.None;
            playerWallet = 0;
            factionWallet = 0;
            enableInput = false;
            Utils.RemoveSpawnLocationsClientGPS();
        }

        public void GetObFromPreview(List<MyObjectBuilder_CubeGrid> list)
        {
            list.Clear();
            foreach(var grid in previewGrids)
            {
                MyObjectBuilder_CubeGrid ob = grid.GetObjectBuilder() as MyObjectBuilder_CubeGrid;
                list.Add(ob);
            }
        }

        // Server
        public void SpawnGridsFromOb(List<MyObjectBuilder_CubeGrid> obs, int index, HangarType sentHangarType, long playerId, long cost, SpawnType spawnType, bool checkForIntersections)
        {
            IMyPlayer player = GetPlayerfromID(playerId);
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerId);
            MyAPIGateway.Entities.RemapObjectBuilderCollection(obs);
            IMyCubeGrid mainGrid = null;
            List<MyCubeGrid> gridsToSpawn = new List<MyCubeGrid>();
            Utils.CheckGridSpawnLimitsInOB(obs, faction, sentHangarType, index, spawnType, playerId);

            for (int i = 0; i < obs.Count; i++)
            {
                MyObjectBuilder_CubeGrid cloneOb = obs[i].Clone() as MyObjectBuilder_CubeGrid;
                if (cloneOb == null) continue;

                cloneOb.CreatePhysics = true;
                IMyEntity ent = MyAPIGateway.Entities.CreateFromObjectBuilder(cloneOb);
                var cubeGrid = ent as MyCubeGrid;
                var grid = ent as IMyCubeGrid;
                if (cubeGrid == null || grid == null) return;

                cubeGrid.Save = true;
                cubeGrid.SyncFlag = true;
                cubeGrid.IsPreview = false;

                if (i == 0)
                {
                    if (cubeGrid.GridSizeEnum == MyCubeSize.Small)
                    {
                        if (config.spawnSGStatic)
                            grid.IsStatic = true;
                    }else
                        grid.IsStatic = true;

                    mainGrid = cubeGrid;
                }

                if (sentHangarType == HangarType.Faction)
                    Utils.CheckOwnerValidFaction(faction, cubeGrid, playerId);

                gridsToSpawn.Add(cubeGrid);
            }

            if (checkForIntersections)
            {
                if (Utils.IsGridIntersecting(gridsToSpawn))
                {
                    MyVisualScriptLogicProvider.SendChatMessageColored($"Failed to spawn grid from hangar, something is blocking placement.", Color.Red, "[FactionHangar]", playerId, "Red");
                    return;
                }
            }
            
            foreach(var grid in gridsToSpawn)
                MyAPIGateway.Entities.AddEntity(grid, true);

            IMyEntity foundEnt;
            if (mainGrid == null)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored($"Failed to spawn grid from hangar.", Color.Red, "[FactionHangar]", playerId, "Red");
                return;
            }

            MyAPIGateway.Entities.TryGetEntityById(mainGrid.EntityId, out foundEnt);
            if (foundEnt == null)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored($"Failed to spawn grid from hangar.", Color.Red, "[FactionHangar]", playerId, "Red");
                return;
            }

            if (sentHangarType == HangarType.Faction)
            {
                if (faction != null)
                {
                    Utils.CheckGridSpawnLimits(mainGrid, faction, sentHangarType, index, spawnType, playerId);
                    Utils.UpdateBalance(playerId, cost, index, sentHangarType);
                    FactionTimers.AddTimer(faction, TimerType.RetrievalCooldown, config.factionHangarConfig.factionHangarCooldown);
                    allHangarData.RemoveFactionData(faction.FactionId, index, true);
                }

                if (player != null)
                    Comms.AddClientCooldown(player.SteamUserId, false, TimerType.RetrievalCooldown);
            }

            if (sentHangarType == HangarType.Private)
            {
                Utils.CheckGridSpawnLimits(mainGrid, null, sentHangarType, index, spawnType, playerId);
                Utils.UpdateBalance(playerId, cost, index, sentHangarType);
                allHangarData.RemovePrivateData(playerId, index, true);
                if (player != null)
                    Comms.AddClientCooldown(player.SteamUserId, true, TimerType.RetrievalCooldown);
            }

            if (player != null)
            {
                IMyCubeGrid grid = gridsToSpawn[0] as IMyCubeGrid;
                if (grid == null) return;
                MyVisualScriptLogicProvider.SendChatMessageColored($"Successfully spawned in grid '{grid.CustomName}'.", Color.Green, "[FactionHangar]", playerId, "Green");
            }
        }

        private void RunClientTimers()
        {
            if (isServer && isDedicated) return;
            if (ticks % 60 != 0) return;

            if (retrievalTimer > 0)
                retrievalTimer--;

            if (storeTimer > 0)
                storeTimer--;
        }

        private void RunDelayTimers()
        {
            if (ticks % 60 != 0) return;

            if (cooldownTimers.Count > 0)
            {
                List<IMyFaction> keys = cooldownTimers.Keys.ToList();
                foreach (var faction in keys)
                {
                    for (int i = cooldownTimers[faction].timers.Count - 1; i >= 0; i--)
                    {
                        if (cooldownTimers[faction].timers[i].time > 0)
                            cooldownTimers[faction].timers[i].time--;
                        else
                            cooldownTimers[faction].timers.RemoveAt(i);

                        if (cooldownTimers[faction].timers.Count == 0)
                            cooldownTimers.Remove(faction);
                    }
                }
            }
            
            if (hangarDelay.Count == 0) return;
            for (int i = hangarDelay.Count - 1; i >= 0; i--)
            {
                if (hangarDelay[i].hangarType == HangarType.Faction)
                {
                    if (hangarDelay[i].timer >= config.factionHangarConfig.factionStoreDelay)
                    {
                        foreach (var grid in hangarDelay[i].gridData)
                            RequestingGridStorage(hangarDelay[i].requesterId, hangarDelay[i].playerId, grid.gridId, hangarDelay[i].playerName);

                        hangarDelay.RemoveAtFast(i);
                        continue;
                    }
                }

                if (hangarDelay[i].hangarType == HangarType.Private)
                {
                    if (hangarDelay[i].timer >= config.privateHangarConfig.privateStoreDelay)
                    {
                        foreach (var grid in hangarDelay[i].gridData)
                            RequestingGridStorage(hangarDelay[i].requesterId, hangarDelay[i].playerId, grid.gridId, hangarDelay[i].playerName, true);

                        hangarDelay.RemoveAtFast(i);
                        continue;
                    }
                }
                /*else
                {
                    foreach (var grid in hangarDelay[i].gridData)
                        RequestingGridStorage(hangarDelay[i].playerId, grid.gridId, hangarDelay[i].playerName);

                    hangarDelay.RemoveAtFast(i);
                    continue;
                }*/

                hangarDelay[i].timer++;

                if (hangarDelay[i].hangarType == HangarType.Faction)
                {
                    if (config.factionHangarConfig.factionStoreDelay - hangarDelay[i].timer == 10)
                        foreach (var grid in hangarDelay[i].gridData)
                            MyVisualScriptLogicProvider.SendChatMessageColored($"Storing Grid {grid.gridName} in 10 seconds", Color.Green, "[FactionHangar]", hangarDelay[i].playerId, "Green");
                }
                else
                {
                    if (config.privateHangarConfig.privateStoreDelay - hangarDelay[i].timer == 10)
                        foreach (var grid in hangarDelay[i].gridData)
                            MyVisualScriptLogicProvider.SendChatMessageColored($"Storing Grid {grid.gridName} in 10 seconds", Color.Green, "[FactionHangar]", hangarDelay[i].playerId, "Green");
                }
            }
        }

        public void ChatHandler(string messageText, ref bool sendToOthers)
        {
            if (isDedicated) return;
            IMyPlayer client = MyAPIGateway.Session.LocalHumanPlayer;
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(client.IdentityId);
            bool isLeader = false;

            if (faction != null)
                isLeader = faction.IsLeader(client.IdentityId);

            if (messageText.Equals("/fh list") || messageText.Equals("/factionhangar list"))
            {
                sendToOthers = false;
                Comms.ClientRequestFactionList(client.IdentityId, client.DisplayName);
     
                return;
            }

            if (messageText.StartsWith("/fh store") || messageText.StartsWith("/factionhangar store"))
            {
                List<MyEntity> entities = new List<MyEntity>();
                sendToOthers = false;

                if (faction == null)
                {
                    Comms.SendChatMessage("Need to be in a faction to store a grid in faction hangar.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Need to be in a faction to store a grid in faction hangar.", Color.Red, "[FactionHangar]", 0, "Red");
                    return;
                }

                if (storeTimer > 0)
                {
                    Comms.SendChatMessage($"Must wait {storeTimer} seconds before you can store another grid.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Must wait {storeTimer} seconds before you can store another grid.", Color.Red, "[FactionHangar]", 0, "Red");
                    return;
                }

                var split = messageText.Split(' ');
                string gridName = "";
                IMyCubeGrid choosenGrid = null;
                if (split.Length == 1)
                {
                    GetEntitiesInSphere(entities, playerCache.GetPosition(), 500);
                    if (string.IsNullOrEmpty(gridName))
                    {
                        string list = Utils.GetGridsToStore(entities, HangarType.Faction, client.IdentityId);
                        Comms.SendChatMessage($"{list}", "Green", client.IdentityId, Color.Green);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"{list}", Color.Green, "[FactionHangar]", 0, "Green");

                        return;
                    }
                }

                if (split.Length > 1)
                {
                    for (int i = 2; i < split.Length; i++)
                    {
                        if (i == 2)
                        {
                            gridName = split[i];
                            continue;
                        }

                        gridName += $" {split[i]}";
                    }

                    GetEntitiesInSphere(entities, playerCache.GetPosition(), 500);
                    if (string.IsNullOrEmpty(gridName))
                    {
                        string list = Utils.GetGridsToStore(entities, HangarType.Faction, client.IdentityId);
                        Comms.SendChatMessage($"{list}", "Green", client.IdentityId, Color.Green);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"{list}", Color.Green, "[FactionHangar]", 0, "Green");

                        return;
                    }

                    foreach (var ent in entities)
                    {
                        
                        IMyCubeGrid grid = ent as IMyCubeGrid;
                        if (grid == null) continue;
                        if (gridName == grid.CustomName)
                        {
                            choosenGrid = grid;
                            break;
                        }
                    }

                    if (choosenGrid == null)
                    {
                        Comms.SendChatMessage($"{gridName} is NOT a valid grid.", "Red", client.IdentityId, Color.Red);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"{gridName} is NOT a valid grid", Color.Red, "[FactionHangar]", 0, "Red");
                        string list = Utils.GetGridsToStore(entities, HangarType.Faction, client.IdentityId);
                        Comms.SendChatMessage($"{list}", "Green", client.IdentityId, Color.Green);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"{list}", Color.Green, "[FactionHangar]", 0, "Green");

                        return;
                    }

                    long gridOwner = choosenGrid.BigOwners.FirstOrDefault();

                    if (faction != null)
                    {
                        if (!DoesFactionOwnGrid(choosenGrid, faction))
                        {
                            Comms.SendChatMessage($"Grid {choosenGrid.CustomName} is not owned by you or your faction", "Red", client.IdentityId, Color.Red);
                            //MyVisualScriptLogicProvider.SendChatMessageColored($"Grid {choosenGrid.CustomName} is not owned by you or your faction", Color.Red, "[FactionHangar]", 0, "Red");
                            return;
                        }
                        else
                        {
                            if (!isLeader)
                            {
                                if (!DoesPlayerOwnGrid(choosenGrid, client.IdentityId))
                                {
                                    Comms.SendChatMessage($"Grid {choosenGrid.CustomName} is not owned by you, must be a faction leader to store grids that you don't own", "Red", client.IdentityId, Color.Red);
                                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Grid {choosenGrid.CustomName} is not owned by you, must be a faction leader to store grids that you don't own", Color.Red, "[FactionHangar]", 0, "Red");
                                    return;
                                }
                            }
                        }
                    }

                    if (IsEnemyNearStoring(choosenGrid, HangarType.Faction))
                    {
                        Comms.SendChatMessage($"Cannot store when enemy is nearby...", "Red", client.IdentityId, Color.Red);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"Cannot store when enemy is nearby...", Color.Red, "[FactionHangar]", 0, "Red");
                        return;
                    }

                    gridsToStore = new CacheGridsForStorage();
                    List<IMyCubeGrid> connectedGrids = new List<IMyCubeGrid>();
                    GetConnectedGrids(choosenGrid, connectedGrids);


                    if (connectedGrids.Count > 0)
                    {
                        gridsToStore.ownerId = gridOwner;
                        gridsToStore.requesterId = client.IdentityId;
                        gridsToStore.grids = new List<IMyCubeGrid>() { choosenGrid };

                        //gridsToStore.Add(choosenGrid);
                        string message = "The following grids are connected and will count as slots in the faction hangar:\n\n";
                        foreach (var grid in connectedGrids)
                        {
                            gridsToStore.grids.Add(grid);
                            //gridsToStore.Add(grid);
                            message += $"{grid.CustomName}\n";
                        }

                        message += "\nPress 'Process' to proceed or cancel with the X on the top corner";

                        hangarType = HangarType.Faction;
                        MyAPIGateway.Utilities.ShowMissionScreen("Connected Grids Detected!", "", null, message, ConnectedGridsResult, "Process");
                        return;
                    }

                    List<GridData> gridDatas = new List<GridData>();
                    GridData gridData = new GridData()
                    {
                        gridId = choosenGrid.EntityId,
                        gridName = choosenGrid.CustomName
                    };

                    string ownerName = GetPlayerName(gridOwner);
                    gridDatas.Add(gridData);
                    //storeTimer = config.factionHangarConfig.factionHangarCooldown;
                    //Comms.AddTimer(faction.FactionId, TimerType.StorageCooldown, config.factionHangarConfig.factionHangarCooldown);
                    Comms.ClientRequestStoreGrid(client.IdentityId, gridOwner, gridDatas, ownerName, HangarType.Faction);
                }
            }

            if (messageText.StartsWith("/fh load") || messageText.StartsWith("/factionhangar load"))
            {
                sendToOthers = false;
                if (faction == null)
                {
                    Comms.SendChatMessage($"Need to be in a faction to load a grid in faction hangar.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Need to be in a faction to load a grid in faction hangar.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                if (retrievalTimer > 0)
                {
                    Comms.SendChatMessage($"Must wait {retrievalTimer} seconds before you can load another grid.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Must wait {retrievalTimer} seconds before you can load another grid.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                var split = messageText.Split(' ');
                bool originalLocation = false;
                bool force = false;
                string gridIndex = "";
                int index = -1;

                if (split.Length <= 2)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                gridIndex = split[2];

                if (!int.TryParse(gridIndex, out index))
                {
                    var splitBool = split[2].Split('.');
                    if (splitBool.Length > 1)
                        bool.TryParse(splitBool[1], out originalLocation);

                    if (splitBool.Length > 2)
                        if (splitBool[2].Equals("force", StringComparison.OrdinalIgnoreCase))
                        {
                            if (originalLocation)
                                force = true;
                            else
                            {
                                Comms.SendChatMessage("Force command is only allowed when spawning to original location.", "Red", client.IdentityId, Color.Red);
                                return;
                            }
                        }

                    if (!int.TryParse(splitBool[0], out index))
                    {
                        Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                        return;
                    }
                }

                if (index < 0)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                
                        

                Comms.ClientRequestGridData(index, client.IdentityId, HangarType.Faction, client.SteamUserId, originalLocation, force);
            }

            if (messageText.StartsWith("/fh transfer") || messageText.StartsWith("/factionhangar transfer"))
            {
                sendToOthers = false;
                if (faction == null)
                {
                    Comms.SendChatMessage("Need to be in a faction to transfer a grid in to private hangar.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Need to be in a faction to transfer a grid in to private hangar.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                var split = messageText.Split(' ');

                bool originalLocation = false;
                string gridIndex = "";
                int index = -1;

                if (split.Length <= 1)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                gridIndex = split[2];
                int.TryParse(gridIndex, out index);
                if (index < 0)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                Comms.RequestTransferFactionToPrivate(client.IdentityId, index);
                //allHangarData.TransferFactionToPrivate(faction.FactionId, index, client.IdentityId);
            }

            if (messageText.StartsWith("/fh remove") || messageText.StartsWith("/factionhangar remove"))
            {
                sendToOthers = false;
                if (faction == null)
                {
                    Comms.SendChatMessage("Need to be in a faction to remove grids from hangar.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Need to be in a faction to remove grids from hangar.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                var split = messageText.Split(' ');

                string gridIndex = "";
                int index = -1;

                if (split.Length <= 1)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                gridIndex = split[2];
                int.TryParse(gridIndex, out index);
                if (index < 0)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                Comms.RequestGridRemoval(index, client.IdentityId, HangarType.Faction);
            }

            if (messageText.StartsWith("/ph store") || messageText.StartsWith("/privatehangar store"))
            {
                List<MyEntity> entities = new List<MyEntity>();
                sendToOthers = false;
                if (storeTimer > 0)
                {
                    Comms.SendChatMessage($"Must wait {storeTimer} seconds before you can store another grid.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Must wait {storeTimer} seconds before you can store another grid.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                var split = messageText.Split(' ');
                string gridName = "";
                IMyCubeGrid choosenGrid = null;
                if (split.Length == 1)
                {
                    GetEntitiesInSphere(entities, playerCache.GetPosition(), 500);
                    if (string.IsNullOrEmpty(gridName))
                    {
                        string list = Utils.GetGridsToStore(entities, HangarType.Private, client.IdentityId);
                        Comms.SendChatMessage($"{list}", "Green", client.IdentityId, Color.Green);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"{list}", Color.Green, "[FactionHangar]", client.IdentityId, "Green");

                        return;
                    }
                }

                if (split.Length > 1)
                {
                    for (int i = 2; i < split.Length; i++)
                    {
                        if (i == 2)
                        {
                            gridName = split[i];
                            continue;
                        }

                        gridName += $" {split[i]}";
                    }

                    GetEntitiesInSphere(entities, playerCache.GetPosition(), 500);
                    if (string.IsNullOrEmpty(gridName))
                    {
                        string list = Utils.GetGridsToStore(entities, HangarType.Private, client.IdentityId);
                        Comms.SendChatMessage($"{list}", "Green", client.IdentityId, Color.Green);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"{list}", Color.Green, "[FactionHangar]", client.IdentityId, "Green");

                        return;
                    }

                    foreach (var ent in entities)
                    {
                        IMyCubeGrid grid = ent as IMyCubeGrid;
                        if (grid == null) continue;
                        if (gridName == grid.CustomName)
                        {
                            choosenGrid = grid;
                            break;
                        }
                    }

                    if (choosenGrid == null)
                    {
                        Comms.SendChatMessage($"{gridName} is NOT a valid grid.", "Red", client.IdentityId, Color.Red);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"{gridName} is NOT a valid grid", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                        string list = Utils.GetGridsToStore(entities, HangarType.Private, client.IdentityId);
                        Comms.SendChatMessage($"{list}", "Green", client.IdentityId, Color.Green);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"{list}", Color.Green, "[FactionHangar]", client.IdentityId, "Green");

                        return;
                    }

                    if (!DoesPlayerOwnGrid(choosenGrid, client.IdentityId))
                    {
                        Comms.SendChatMessage($"Grid {choosenGrid.CustomName} is not owned by you.", "Red", client.IdentityId, Color.Red);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"Grid {choosenGrid.CustomName} is not owned by you.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                        return;
                    }

                    if (IsEnemyNearStoring(choosenGrid, HangarType.Private))
                    {
                        Comms.SendChatMessage($"Cannot store when enemy is nearby.", "Red", client.IdentityId, Color.Red);
                        //MyVisualScriptLogicProvider.SendChatMessageColored($"Cannot store when enemy is nearby...", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                        return;
                    }

                    gridsToStore = new CacheGridsForStorage();
                    List<IMyCubeGrid> connectedGrids = new List<IMyCubeGrid>();
                    GetConnectedGrids(choosenGrid, connectedGrids);


                    if (connectedGrids.Count > 0)
                    {
                        gridsToStore.ownerId = client.IdentityId;
                        gridsToStore.requesterId = client.IdentityId;
                        gridsToStore.grids = new List<IMyCubeGrid>() { choosenGrid };

                        //gridsToStore.Add(choosenGrid);
                        string message = "The following grids are connected and will count as slots in the private hangar:\n\n";
                        foreach (var grid in connectedGrids)
                        {
                            gridsToStore.grids.Add(grid);
                            //gridsToStore.Add(grid);
                            message += $"{grid.CustomName}\n";
                        }

                        message += "\nPress 'Process' to proceed or cancel with the X on the top corner";

                        hangarType = HangarType.Private;
                        MyAPIGateway.Utilities.ShowMissionScreen("Connected Grids Detected!", "", null, message, ConnectedGridsResult, "Process");
                        return;
                    }

                    List<GridData> gridDatas = new List<GridData>();
                    GridData gridData = new GridData()
                    {
                        gridId = choosenGrid.EntityId,
                        gridName = choosenGrid.CustomName
                    };

                    gridDatas.Add(gridData);
                    //storeTimer = config.privateHangarConfig.privateHangarCooldown;
                    Comms.ClientRequestStoreGrid(client.IdentityId, client.IdentityId, gridDatas, client.DisplayName, HangarType.Private);
                }
            }

            if (messageText.StartsWith("/ph load") || messageText.StartsWith("/privatehangar load"))
            {
                sendToOthers = false;

                if (retrievalTimer > 0)
                {
                    Comms.SendChatMessage($"Must wait {retrievalTimer} seconds before you can load another grid.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Must wait {retrievalTimer} seconds before you can load another grid.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                var split = messageText.Split(' ');

                bool originalLocation = false;
                bool force = false;
                string gridIndex = "";
                int index = -1;

                if (split.Length <= 2)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    return;
                }

                gridIndex = split[2];
                if (!int.TryParse(gridIndex, out index))
                {
                    var splitBool = split[2].Split('.');
                    if (splitBool.Length > 1)
                        bool.TryParse(splitBool[1], out originalLocation);

                    if (splitBool.Length > 2)
                        if (splitBool[2].Equals("force", StringComparison.OrdinalIgnoreCase))
                        {
                            if (originalLocation)
                                force = true;
                            else
                            {
                                Comms.SendChatMessage("Force command is only allowed when spawning to original location.", "Red", client.IdentityId, Color.Red);
                                return;
                            }
                        }

                    if (!int.TryParse(splitBool[0], out index))
                    {
                        Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                        return;
                    }
                }

                if (index < 0)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    return;
                }

                Comms.ClientRequestGridData(index, client.IdentityId, HangarType.Private, client.SteamUserId, originalLocation, force);
            }

            if (messageText.StartsWith("/ph transfer") || messageText.StartsWith("/privatehangar transfer"))
            {
                sendToOthers = false;
                if (faction == null)
                {
                    Comms.SendChatMessage("Need to be in a faction to transfer a grid in to faction hangar.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Need to be in a faction to transfer a grid in to faction hangar.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                var split = messageText.Split(' ');

                bool originalLocation = false;
                string gridIndex = "";
                int index = -1;

                if (split.Length <= 1)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                gridIndex = split[2];
                int.TryParse(gridIndex, out index);
                if (index < 0)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                Comms.RequestTransferPrivateToFaction(client.IdentityId, index);
                //allHangarData.TransferPrivateToFaction(faction.FactionId, index, client.IdentityId);
            }

            if (messageText.Equals("/ph list") || messageText.Equals("/privatehangar list"))
            {
                sendToOthers = false;
                Comms.ClientRequestPrivateList(client.IdentityId, client.DisplayName);

                return;
            }

            if (messageText.StartsWith("/ph remove") || messageText.StartsWith("/privatehangar remove"))
            {
                sendToOthers = false;

                var split = messageText.Split(' ');

                string gridIndex = "";
                int index = -1;

                if (split.Length <= 1)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                gridIndex = split[2];
                int.TryParse(gridIndex, out index);
                if (index < 0)
                {
                    Comms.SendChatMessage("Invalid index.", "Red", client.IdentityId, Color.Red);
                    //MyVisualScriptLogicProvider.SendChatMessageColored($"Invalid index.", Color.Red, "[FactionHangar]", client.IdentityId, "Red");
                    return;
                }

                Comms.RequestGridRemoval(index, client.IdentityId, HangarType.Private);
                //allHangarData.RemovePrivateData(client.IdentityId, index, true);
            }

            if (messageText.Equals("/fh togglesphere"))
            {
                sendToOthers = false;
                drawClientSphereDebug = !drawClientSphereDebug;
                Comms.SendChatMessage($"Client spawn spheres are now set to '{drawClientSphereDebug}'", "Green", client.IdentityId, Color.Green);
                //MyVisualScriptLogicProvider.SendChatMessageColored($"Client spawn spheres are now set to '{drawClientSphereDebug}'", Color.Green, "[FactionHangar]", client.IdentityId, "Green");
            }

            if (messageText.Equals("/fh togglegps"))
            {
                sendToOthers = false;
                spawnClientGPS = !spawnClientGPS;
                Comms.SendChatMessage($"Client spawn gps locations are now set to '{spawnClientGPS}'", "Green", client.IdentityId, Color.Green);
                //MyVisualScriptLogicProvider.SendChatMessageColored($"Client spawn gps locations are now set to '{spawnClientGPS}'", Color.Green, "[FactionHangar]", client.IdentityId, "Green");

            }

            if (messageText.StartsWith("/fh help"))
            {
                sendToOthers = false;
                Utils.LoadHelpPopup();
            }
        }

        private void ConnectedGridsResult(ResultEnum result)
        {
            if (result == ResultEnum.OK)
            {
                IMyPlayer client = MyAPIGateway.Session.LocalHumanPlayer;
                List<GridData> gridDatas= new List<GridData>();
                foreach (var grid in gridsToStore.grids)
                {
                    GridData gridData = new GridData()
                    {
                        gridId = grid.EntityId,
                        gridName = grid.CustomName
                    };

                    gridDatas.Add(gridData);
                }

                string ownerName = GetPlayerName(gridsToStore.ownerId);
                //storeTimer = hangarType == HangarType.Faction ? config.factionHangarConfig.factionHangarCooldown : config.privateHangarConfig.privateHangarCooldown;
                
                /*if (hangarType == HangarType.Faction)
                {
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(client.IdentityId);
                    if (faction != null)
                        Comms.AddTimer(faction.FactionId, TimerType.StorageCooldown, storeTimer);
                }*/
                    
                Comms.ClientRequestStoreGrid(client.IdentityId, gridsToStore.ownerId, gridDatas, ownerName, hangarType);
            }
        }

        public void GetConnectedGrids(IMyCubeGrid targetGrid, List<IMyCubeGrid> connectedGrids)
        {
            List<IMyCubeGrid> physicalGroup = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(targetGrid, GridLinkTypeEnum.Physical, physicalGroup);
            foreach (var grid in physicalGroup)
            {
                if (!targetGrid.IsSameConstructAs(grid))
                    connectedGrids.Add(grid);
            }

            for (int i = connectedGrids.Count - 1; i >= 0; i--)
            {
                var blocks = connectedGrids[i].GetFatBlocks<IMyAttachableTopBlock>();
                bool attached = false;
                foreach(var top in blocks)
                {
                    if (top.IsAttached)
                    {
                        attached = true;
                        break;
                    }
                }
                    
                if (attached)
                    connectedGrids.RemoveAt(i);
            }
        }

        public bool DoesPlayerOwnGrid(IMyCubeGrid grid, long playerId)
        {
            if (grid == null) return false;
            long owner = 0;
            if (grid.BigOwners.Count != 0)
                owner = grid.BigOwners[0];

            if (owner != 0 && owner == playerId) return true;

            return false;
        }

        public bool DoesFactionOwnGrid(IMyCubeGrid grid, IMyFaction faction)
        {
            if (grid == null) return false;
            long owner = 0;
            if (grid.BigOwners.Count != 0)
                owner = grid.BigOwners[0];

            IMyFaction gridFaction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);
            if (gridFaction != null && gridFaction == faction) return true;

            return false;
        }

        public void RequestingGridStorage(long requesterId, long ownerId, long gridId, string playerName, bool privateStorage = false, bool autoHangar = false)
        {
            IMyPlayer player = GetPlayerfromID(requesterId);
            IMyPlayer gridOwner = GetPlayerfromID(ownerId);
            /*if (player == null)
            {
                MyLog.Default.WriteLineAndConsole($"[FactionHangar] - Invalid Player");
                return;
            }*/

            IMyEntity entity = null;
            MyAPIGateway.Entities.TryGetEntityById(gridId, out entity);
            if (entity == null)
            {
                if (player != null)
                    MyVisualScriptLogicProvider.SendChatMessageColored($"Failed to store grid", Color.Red, "[FactionHangar]", requesterId, "Red");

                MyLog.Default.WriteLineAndConsole($"[FactionHangar] - Unable to get grid by Id {gridId}");
                return;
            }

            IMyCubeGrid grid = entity as IMyCubeGrid;
            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(ownerId);

            playerName = RemoveSpecialCharacters(playerName);
            string gridName = RemoveSpecialCharacters(grid.CustomName);

            string path = "";
            path = Path.Combine(MyAPIGateway.Utilities.GamePaths.UserDataPath, $"FactionHangarSaves", $"{playerName}", $"{gridName}_{grid.EntityId}", "bp.sbc");
            //path = Path.Combine("C:\\", $"FactionHangarSaves", $"{playerName}", $"{gridName}_{grid.EntityId}", "bp.sbc");

            /*if (faction != null)
            {
                if (privateStorage)
                    path = Path.Combine(MyAPIGateway.Utilities.GamePaths.ModsPath, $"FactionHangarSaves", $"PrivateHangars", $"{playerName}", $"{grid.CustomName}_{grid.EntityId}","bp.sbc");
                    //path = Path.Combine(MyAPIGateway.Utilities.GamePaths.ModsPath, $"{faction.Name}_{grid.CustomName}_{grid.EntityId}.sbc");

                else
                    path = Path.Combine(MyAPIGateway.Utilities.GamePaths.ModsPath, $"FactionHangarSaves", $"FactionHangars", $"{faction.Name}", $"{grid.CustomName}_{grid.EntityId}", "bp.sbc");
                    //path = Path.Combine(MyAPIGateway.Utilities.GamePaths.ModsPath, $"{faction.Name}_{grid.CustomName}_{grid.EntityId}.sbc");

            }
            else
                path = Path.Combine(MyAPIGateway.Utilities.GamePaths.ModsPath, $"FactionHangarSaves", $"PrivateHangars", $"{playerName}", $"{grid.CustomName}_{grid.EntityId}", "bp.sbc");
                //path = Path.Combine(MyAPIGateway.Utilities.GamePaths.ModsPath, $"{playerName}_{grid.CustomName}_{grid.EntityId}.sbc");*/


            if (path != "")
            {
                Utils.RemovePlayersFromSeats(grid as MyCubeGrid);

                if (CreateShipBlueprint(grid as MyCubeGrid, grid.CustomName, path, autoHangar ? GridLinkTypeEnum.Physical : GridLinkTypeEnum.Mechanical))
                {
                    if (player != null && !autoHangar)
                    {
                        //Comms.AddClientCooldown(player.SteamUserId, privateStorage, TimerType.StorageCooldown);
                        MyVisualScriptLogicProvider.SendChatMessageColored($"Successfully stored grid {grid.CustomName}", Color.Green, "[FactionHangar]", requesterId, "Green");
                        MyLog.Default.WriteLineAndConsole($"[FactionHangar] - Player {playerName} successfully stored grid {grid.CustomName}");
                    }

                    //if (faction != null && !autoHangar)
                        //FactionTimers.AddTimer(faction, TimerType.StorageCooldown, config.factionHangarConfig.factionHangarCooldown);

                    if (autoHangar)
                        MyLog.Default.WriteLineAndConsole($"[FactionHangar] - AutoHangar successfully stored grid {grid.CustomName}");

                    if (!privateStorage)
                        allHangarData.AddFactionData(faction.FactionId, grid.CustomName, grid.EntityId, ownerId, path, playerName, autoHangar);
                    else
                        allHangarData.AddPrivateData(grid.CustomName, grid.EntityId, ownerId, path, playerName, autoHangar);

                    CloseGridGroup(grid, autoHangar ? GridLinkTypeEnum.Physical : GridLinkTypeEnum.Mechanical);
                    return;
                }
            }

            
            if (autoHangar)
                MyLog.Default.WriteLineAndConsole($"[FactionHangar] - AutoHangar failed to store grid {grid.CustomName}");
            else
                MyLog.Default.WriteLineAndConsole($"[FactionHangar] - Player {playerName} failed to store grid {grid.CustomName}");

            if (player != null)
                MyVisualScriptLogicProvider.SendChatMessageColored($"Failed to store grid {grid.CustomName}", Color.Red, "[FactionHangar]", requesterId, "Red");
        }

        public string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void CloseGridGroup(IMyCubeGrid grid, GridLinkTypeEnum groupType)
        {
            List<IMyCubeGrid> myCubeGrids= new List<IMyCubeGrid>();
            GetGroupByType(grid, myCubeGrids, groupType);
            foreach(var cubeGrid in myCubeGrids)
            {
                if (!cubeGrid.MarkedForClose)
                    cubeGrid.Close();
            }
        }

        public bool CreateShipBlueprint(MyCubeGrid myCubeGrid, string blueprintName, string path, GridLinkTypeEnum groupType)
        {
            List<MyObjectBuilder_CubeGrid> list = GetGridGroupObs(myCubeGrid, groupType);
            MyObjectBuilder_ShipBlueprintDefinition myObjectBuilder_ShipBlueprintDefinition = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_ShipBlueprintDefinition>();
            myObjectBuilder_ShipBlueprintDefinition.Id = new MyDefinitionId(new MyObjectBuilderType(typeof(MyObjectBuilder_ShipBlueprintDefinition)), MyUtils.StripInvalidChars(blueprintName));
            myObjectBuilder_ShipBlueprintDefinition.CubeGrids = list.ToArray();
            myObjectBuilder_ShipBlueprintDefinition.RespawnShip = false;
            myObjectBuilder_ShipBlueprintDefinition.DisplayName = blueprintName;
            myObjectBuilder_ShipBlueprintDefinition.CubeGrids[0].DisplayName = blueprintName;
            MyObjectBuilder_Definitions myObjectBuilder_Definitions = MyObjectBuilderSerializer.CreateNewObject<MyObjectBuilder_Definitions>();
            myObjectBuilder_Definitions.ShipBlueprints = new MyObjectBuilder_ShipBlueprintDefinition[1];
            myObjectBuilder_Definitions.ShipBlueprints[0] = myObjectBuilder_ShipBlueprintDefinition;
            MyObjectBuilderSerializer.SerializeXML(path, false, myObjectBuilder_Definitions);
            return true;
        }

        public  List<MyObjectBuilder_CubeGrid> GetGridGroupObs(MyCubeGrid cubeGrid, GridLinkTypeEnum groupType)
        {
            List<MyObjectBuilder_CubeGrid> tmp = new List<MyObjectBuilder_CubeGrid>();
            List<IMyCubeGrid> groupList = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(cubeGrid, groupType, groupList);
            tmp.Add(cubeGrid.GetObjectBuilder() as MyObjectBuilder_CubeGrid);

            foreach (var grid in groupList)
            {
                if (grid == cubeGrid) continue;
                tmp.Add(grid.GetObjectBuilder() as MyObjectBuilder_CubeGrid);
            }

            return tmp;
        }

        public void GetGroupByType(IMyCubeGrid grid, List<IMyCubeGrid> gridList, GridLinkTypeEnum type)
        {
            gridList.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, type, gridList);
        }

        public MyObjectBuilder_CubeGrid[] GetGridFromGridData(MyObjectBuilder_Base data, int index, HangarType sentHangarType)
        {
            spawnIndex = index;
            hangarType = sentHangarType;
            if (data == null) return null;
            MyObjectBuilder_Definitions def = data as MyObjectBuilder_Definitions;
            if (def == null) return null;

            if (def.ShipBlueprints == null) return null;
            if (def.ShipBlueprints.Length == 0) return null;

            MyObjectBuilder_ShipBlueprintDefinition bpDef = def.ShipBlueprints[0];
            if (bpDef == null) return null;

            if (bpDef.CubeGrids == null) return null;
            MyObjectBuilder_CubeGrid[] cubeGridObs = bpDef.CubeGrids;
            if (cubeGridObs.Length == 0) return null;

            return cubeGridObs;
        }

        public void SpawnClientSideProjectedGrid(MyObjectBuilder_CubeGrid[] cubeGridObs)
        {
            if (cubeGridObs == null || cubeGridObs.Length == 0) return;

            RotationSpeed = 0.01f;
            previewDistance = 50;
            original = cubeGridObs[0].PositionAndOrientation.Value.Position;
            hudNotify = MyAPIGateway.Utilities.CreateNotification($"[Valid Spawn] = {allowSpawn} | Cost To Spawn = {spawnCost} SC", int.MaxValue, "White");
            hudNotify.Show();
            hudNotify.ResetAliveTime();
            UpdatePosition();
            DetectSpawnType(true);
            Utils.AddSpawnLocationsClientGPS();

            MyAPIGateway.Entities.RemapObjectBuilderCollection(cubeGridObs);
            MatrixD baseMat = new MatrixD();
            if (cubeGridObs[0].PositionAndOrientation.HasValue)
                baseMat = cubeGridObs[0].PositionAndOrientation.Value.GetMatrix();

            if (baseMat == MatrixD.Zero) return;

            if (cubeGridObs.Length > 1)
                AssignSubgridSpawnLocation(cubeGridObs, endCoordCache, baseMat);

            cubeGridObs[0].PositionAndOrientation = new MyPositionAndOrientation(endCoordCache, baseMat.Forward, baseMat.Up);
            List<MyCubeGrid> tempGrids = new List<MyCubeGrid>();
            for (int i = 0; i < cubeGridObs.Length; i++)
            {
                MyObjectBuilder_CubeGrid cloneOb = cubeGridObs[i].Clone() as MyObjectBuilder_CubeGrid;
                if (cloneOb == null) continue;

                cloneOb.CreatePhysics = false;

                IMyEntity ent = MyAPIGateway.Entities.CreateFromObjectBuilder(cloneOb);
                var cubeGrid = ent as MyCubeGrid;
                if (cubeGrid == null) return;

                cubeGrid.Save = false;
                cubeGrid.SyncFlag = false;
                cubeGrid.IsPreview = true;
                tempGrids.Add(cubeGrid);
            }

            var massGatherWorkData = new MassGathererWorkData();
            massGatherWorkData.grids = tempGrids;
            massGatherWorkData.faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(playerCache.IdentityId);
            var massGatherTask = MyAPIGateway.Parallel.Start(massGatherWorkData.ScanGridMassAction, massGatherWorkData.ScanGridMassCallback, massGatherWorkData);
        }

        public void AssignSubgridSpawnLocation(MyObjectBuilder_CubeGrid[] cubeGridObs, Vector3D spawnLoc, MatrixD baseMat)
        {
            if (baseMat == MatrixD.Zero) return;

            for (int i = 1; i < cubeGridObs.Length; i++)
            {
                MatrixD subMat = cubeGridObs[i].PositionAndOrientation.Value.GetMatrix();
                MatrixD newMat = MatrixD.CreateWorld(subMat.Translation - baseMat.Translation, subMat.Forward, subMat.Up);
                cubeGridObs[i].PositionAndOrientation = new MyPositionAndOrientation(spawnLoc + newMat.Translation, subMat.Forward, subMat.Up);
            }
        }

        public void GetEntitiesInSphere(List<MyEntity> ents, Vector3D center, double radius)
        {
            ents.Clear();
            //Vector3D center = MyAPIGateway.Session.LocalHumanPlayer.GetPosition();
            BoundingSphereD sphere = new BoundingSphereD(center, radius);
            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref sphere, ents);
        }

        public IMyPlayer GetPlayerfromID(long playerId)
        {
            List<IMyPlayer> players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players);
            foreach(var player in players)
                if (player.IdentityId == playerId) return player;

            return null;
        }

        public string GetPlayerName(long playerId)
        {
            foreach(var identity in allIdentities)
            {
                if (identity.IdentityId == playerId)
                    return identity.DisplayName;
            }

            return string.Empty;
        }

        private void OnBlockAdded(IMyEntity entity)
        {
            IMyCubeBlock block = entity as IMyCubeBlock;
            if (block == null) return;

            MyDefinitionId blockDef = block.BlockDefinition;
            foreach(var data in config.enemyCheckConfig.blockTypes)
            {
                foreach(var subtype in data.blockSubtypes.subtype)
                {
                    MyDefinitionId id;
                    if (string.IsNullOrEmpty(subtype))
                        MyDefinitionId.TryParse(data.blockType, out id);
                    else
                        MyDefinitionId.TryParse(data.blockType, subtype, out id);

                    if (id == null) continue;
                    if (blockDef == id)
                    {
                        if (!enemyBlockCheckList.Contains(block))
                            enemyBlockCheckList.Add(block);
                    }
                }
            }
        }

        private void ClientGetBlocks()
        {
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities);
            foreach (var entity in entities)
            {
                IMyCubeGrid grid = entity as IMyCubeGrid;
                if (grid == null) continue;

                var blocks = grid.GetFatBlocks<IMyCubeBlock>();
                foreach(var block in blocks)
                    OnBlockAdded(block);
            }
        }

        private void OnBlockRemoved(IMyEntity entity)
        {
            IMyCubeBlock block = entity as IMyCubeBlock;
            if (block == null) return;

            if (enemyBlockCheckList.Contains(block))
                enemyBlockCheckList.Remove(block);
        }

        public void ToolEquipped(long playerId, string typeId, string subTypeId)
        {
            if (isDedicated) return;
            if (previewGrids.Count == 0) return;
            if (!enableInput)
            {
                if (playerCache.Character.EquippedTool == null) return;
                MyAPIGateway.Parallel.StartBackground(() =>
                {
                    var controlEnt = playerCache.Character as Sandbox.Game.Entities.IMyControllableEntity;
                    MyAPIGateway.Utilities.InvokeOnGameThread(() => controlEnt?.SwitchToWeapon(null));
                });
            }
            else
                RemovePreviewGrids();
            
        }

        protected override void UnloadData() {
            try {
                if (config != null) {
                    if (config.enemyCheckConfig.enableBlockCheck) {
                        if (MyAPIGateway.Entities != null) {
                            MyAPIGateway.Entities.OnEntityAdd -= OnBlockAdded;
                            MyAPIGateway.Entities.OnEntityRemove -= OnBlockRemoved;
                        }
                    }
                }

                // Add this unhook
                if (!isDedicated) {
                    MyVisualScriptLogicProvider.ToolEquipped -= ToolEquipped;
                }

                if (MyAPIGateway.Utilities != null) {
                    MyAPIGateway.Utilities.MessageEntered -= ChatHandler;
                }

                if (MyAPIGateway.Multiplayer != null) {
                    MyAPIGateway.Multiplayer.UnregisterMessageHandler(NetworkHandle, Comms.MessageHandler);
                }

                Instance = null;
            }
            catch (Exception ex) {
                MyLog.Default.WriteLineAndConsole($"FactionHangar: Error in UnloadData() - {ex.ToString()}");
            }
        }

        public override void SaveData()
        {
            try
            {
                UpdateIdentities();
                using (var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("FactionHangarStorage.xml", typeof(AllHangarData)))
                {
                    writer.Write(MyAPIGateway.Utilities.SerializeToXML(allHangarData));
                    writer.Close();
                }

                foreach(var path in cacheGridPaths)
                    Utils.CreateNullShipBlueprint(path);

                cacheGridPaths.Clear();
            }
            catch (Exception ex)
            {
                VRage.Utils.MyLog.Default.WriteLineAndConsole($"FactionHangar: Error trying to save hangar data!\n {ex.ToString()}");
            }
        }
    }

    public enum SpawnError
    {
        None,
        EnemyNearby,
        InsuffientFunds,
        EntityBlockingPlacement,
        InvalidPlacementInVoxel,
        InvalidSpawningLocation
    }
}