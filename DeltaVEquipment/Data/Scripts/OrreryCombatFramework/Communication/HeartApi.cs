using System;
using System.Collections.Generic;
using DeltaVEquipment.ProjectileBases;
using DeltaVEquipment.WeaponBases;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace DeltaVEquipment
{
    public class HeartApi
    {
        public static HeartApi I;

        #region API Loading
        private const long HeartApiChannel = 8644; // https://xkcd.com/221/
        private Dictionary<string, Delegate> methodMap;
        private Action onLoad;
        private IMyModContext modContext;

        public static void LoadData(IMyModContext modContext, Action onLoad = null)
        {
            if (I != null && Inited)
                return;
            I = new HeartApi
            {
                onLoad = onLoad,
                modContext = modContext
            };
            MyAPIGateway.Utilities.RegisterMessageHandler(HeartApiChannel, I.RecieveApiMethods);
            MyAPIGateway.Utilities.SendModMessage(HeartApiChannel, true);
            MyLog.Default.WriteLineAndConsole("Orrery Combat Framework: HeartAPI awaiting methods.");
        }

        public static void UnloadData()
        {
            if (I == null)
                return;

            MyAPIGateway.Utilities.UnregisterMessageHandler(HeartApiChannel, I.RecieveApiMethods);
            I = null;
        }

        private void RecieveApiMethods(object data)
        {
            try
            {
                if (data == null)
                    return;

                if (!Inited && data is Dictionary<string, Delegate>)
                {
                    methodMap = (Dictionary<string, Delegate>)data;

                    // Standard
                    SetApiMethod("LogWriteLine", ref logWriteLine);
                    SetApiMethod("GetNetworkLoad", ref getNetworkLoad);

                    // Projectile LiveMethods
                    SetApiMethod("AddOnProjectileSpawn", ref addOnProjectileSpawn);
                    SetApiMethod("AddOnProjectileImpact", ref addOnImpact);
                    SetApiMethod("AddOnEndOfLife", ref addOnEndOfLife);
                    //SetApiMethod("AddOnGuidanceStage", ref addOnGuidanceStage); // TODO: Cannot pass type Guidance or type ProjectileDefinitionBase!

                    // Projectile Generics
                    SetApiMethod("GetProjectileDefinitionId", ref getProjectileDefinitionId);
                    SetApiMethod("GetProjectileDefinition", ref getProjectileDefinition);
                    SetApiMethod("RegisterProjectileDefinition", ref registerProjectileDefinition);
                    SetApiMethod("UpdateProjectileDefinition", ref updateProjectileDefinition);
                    SetApiMethod("RemoveProjectileDefinition", ref removeProjectileDefinition);
                    SetApiMethod("SpawnProjectile", ref spawnProjectile);
                    SetApiMethod("GetProjectileInfo", ref getProjectileInfo);
                    SetApiMethod("GetProjectileDefinitionId", ref getProjectileDefinitionId);

                    // Weapon Generics
                    SetApiMethod("BlockHasWeapon", ref blockHasWeapon);
                    SetApiMethod("SubtypeHasDefinition", ref subtypeHasDefinition);
                    SetApiMethod("GetWeaponDefinitions", ref getWeaponDefinitions);
                    SetApiMethod("GetWeaponDefinition", ref getWeaponDefinition);
                    SetApiMethod("RegisterWeaponDefinition", ref registerWeaponDefinition);
                    SetApiMethod("UpdateWeaponDefinition", ref updateWeaponDefinition);
                    SetApiMethod("RemoveWeaponDefinition", ref removeWeaponDefinition);


                    Inited = true;
                    LogWriteLine($"HeartAPI inited.");
                    onLoad?.Invoke();
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole($"Orrery Combat Framework: [{modContext.ModName}] ERR: Failed to init HeartAPI! {ex}");
                logWriteLine?.Invoke($"ERR: Failed to init HeartAPI! {ex}");
            }

            methodMap = null;
        }

        private void SetApiMethod<T>(string name, ref T method) where T : class
        {
            if (!methodMap.ContainsKey(name))
                throw new Exception("Method Map does not contain method " + name);
            Delegate del = methodMap[name];
            if (del.GetType() != typeof(T))
                throw new Exception($"Method {name} type mismatch! [MapMethod: {del.GetType().Name} | ApiMethod: {typeof(T).Name}]");
            method = methodMap[name] as T;
        }

        #endregion

        public static bool Inited = false;

        #region Projectiles

        private Action<string, Action<uint, MyEntity>> addOnProjectileSpawn;
        /// <summary>
        /// Adds an action triggered on projectile spawn.
        /// </summary>
        /// <param name="projectileDefinition"></param>
        /// <param name="onSpawn"></param>
        public static void AddOnProjectileSpawn(string projectileDefinition, Action<uint, MyEntity> onSpawn) => I?.addOnProjectileSpawn?.Invoke(projectileDefinition, onSpawn);

        private Action<string, Action<uint, Vector3D, Vector3D, MyEntity>> addOnImpact;
        /// <summary>
        /// Adds an action triggered on projectile impact.
        /// </summary>
        /// <param name="projectileDefinition"></param>
        /// <param name="onImpact"></param>
        public static void AddOnImpact(string projectileDefinition, Action<uint, Vector3D, Vector3D, MyEntity> onImpact) => I?.addOnImpact?.Invoke(projectileDefinition, onImpact);

        private Action<string, Action<uint>> addOnEndOfLife;
        /// <summary>
        /// Adds an action triggered on projectile end-of-life.
        /// </summary>
        public static void AddOnEndOfLife(string projectileDefinition, Action<uint> onEndOfLife) => I?.addOnEndOfLife?.Invoke(projectileDefinition, onEndOfLife);

        //private Action<string, Action<uint, Guidance?>> addOnGuidanceStage;
        /// <summary>
        /// Adds an action triggered when a projectile's guidance stages.
        /// </summary>
        //public static void AddOnGuidanceStage(string projectileDefinition, Action<uint, Guidance?> onStage) => I?.addOnGuidanceStage?.Invoke(projectileDefinition, onStage);


        private Func<string, int> getProjectileDefinitionId;
        public static int GetProjectileDefinitionId(string projectileName) => I?.getProjectileDefinitionId?.Invoke(projectileName) ?? -1;

        private Func<int, byte[]> getProjectileDefinition;
        public static ProjectileDefinitionBase GetProjectileDefinition(int projectileDefId)
        {
            byte[] serialized = I?.getProjectileDefinition?.Invoke(projectileDefId);
            if (serialized == null)
                return null;
            return MyAPIGateway.Utilities.SerializeFromBinary<ProjectileDefinitionBase>(serialized);
        }

        private Func<byte[], int> registerProjectileDefinition;
        public static int RegisterProjectileDefinition(ProjectileDefinitionBase definition) => I?.registerProjectileDefinition?.Invoke(MyAPIGateway.Utilities.SerializeToBinary(definition)) ?? -1;

        private Func<int, byte[], bool> updateProjectileDefinition;
        public static bool UpdateProjectileDefinition(int definitionId, ProjectileDefinitionBase definition) => I?.updateProjectileDefinition?.Invoke(definitionId, MyAPIGateway.Utilities.SerializeToBinary(definition)) ?? false;

        private Action<int> removeProjectileDefinition;
        public static void RemoveProjectileDefinition(int definitionId) => I?.removeProjectileDefinition?.Invoke(definitionId);

        private Func<int, Vector3D, Vector3D, long, Vector3D, uint> spawnProjectile;
        public static uint SpawnProjectile(int definitionId, Vector3D position, Vector3D direction, long firerId, Vector3D initialVelocity) => I?.spawnProjectile?.Invoke(definitionId, position, direction, firerId, initialVelocity) ?? uint.MaxValue;



        public static List<uint> SpawnProjectilesInCone(int definitionId, Vector3D position, Vector3D direction, int count, double angleRads)
        {
            List<uint> spawned = new List<uint>();
            Random random = new Random();
            for (int i = 0; i < count; i++)
                spawned.Add(SpawnProjectile(definitionId, position, direction.Rotate(Vector3D.CalculatePerpendicularVector(direction).Rotate(direction, Math.PI * 2 * random.NextDouble()), angleRads * random.NextDouble()), 0, Vector3D.Zero));

            return spawned;
        }

        private Func<uint, int, byte[]> getProjectileInfo;
        /// <summary>
        /// DetailLevel 0 = full
        /// <para>DetailLevel 1 = Position, Direction, Id, IsActive, Timestamp</para>
        /// DetailLevel 2 = Id, IsActive, Timestamp
        /// </summary>
        /// <param name="projectileId"></param>
        /// <param name="detailLevel"></param>
        /// <returns></returns>
        public static NSerializableProjectile GetProjectileInfo(uint projectileId, int detailLevel)
        {
            byte[] serialized = I?.getProjectileInfo?.Invoke(projectileId, detailLevel);
            if (serialized == null)
                return null;
            return MyAPIGateway.Utilities.SerializeFromBinary<NSerializableProjectile>(serialized);
        }

        #endregion

        #region Weapons

        private Func<MyEntity, bool> blockHasWeapon;
        public static bool BlockHasWeapon(MyEntity block) => I?.blockHasWeapon?.Invoke(block) ?? false;

        private Func<string, bool> subtypeHasDefinition;
        public static bool SubtypeHasDefinition(string subtype) => I?.subtypeHasDefinition?.Invoke(subtype) ?? false;

        private Func<string[]> getWeaponDefinitions;
        public static string[] GetWeaponDefinitions() => I?.getWeaponDefinitions?.Invoke();

        private Func<string, byte[]> getWeaponDefinition;
        public static WeaponDefinitionBase GetWeaponDefinition(string subtype)
        {
            byte[] serialized = I?.getWeaponDefinition?.Invoke(subtype);
            if (serialized == null || serialized.Length == 0)
                return null;

            return MyAPIGateway.Utilities.SerializeFromBinary<WeaponDefinitionBase>(serialized);
        }

        private Func<byte[], bool> registerWeaponDefinition;
        public static bool RegisterWeaponDefinition(WeaponDefinitionBase definition)
        {
            if (definition == null)
                return false;

            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(definition);

            if (serialized == null || serialized.Length == 0)
                return false;

            return I?.registerWeaponDefinition?.Invoke(serialized) ?? false;
        }

        private Func<byte[], bool> updateWeaponDefinition;
        public static bool UpdateWeaponDefinition(WeaponDefinitionBase definition)
        {
            if (definition == null)
                return false;

            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(definition);

            if (serialized == null || serialized.Length == 0)
                return false;

            return I?.updateWeaponDefinition?.Invoke(serialized) ?? false;
        }

        private Action<string> removeWeaponDefinition;
        public static void RemoveWeaponDefinition(string definitionType) => I?.removeWeaponDefinition?.Invoke(definitionType);

        #endregion

        #region Standard

        private Action<string> logWriteLine;
        /// <summary>
        /// Prints a line to the HeartModule log.
        /// </summary>
        /// <param name="text"></param>
        public static void LogWriteLine(string text) => I?.logWriteLine?.Invoke($"[{I.modContext.ModName}] {text}");

        private Func<int> getNetworkLoad;
        /// <summary>
        /// Returns current Orrery network load, in bytes per second.
        /// </summary>
        /// <returns></returns>
        public static int GetNetworkLoad() => I?.getNetworkLoad?.Invoke() ?? -1;

        #endregion
    }
}
