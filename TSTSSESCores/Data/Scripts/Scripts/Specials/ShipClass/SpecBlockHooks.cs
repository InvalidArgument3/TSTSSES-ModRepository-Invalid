﻿/*using System;
using System.Collections.Generic;
using Digi;
using MIG.Shared.SE;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using F = System.Func<object, bool>;
using FF = System.Func<Sandbox.ModAPI.IMyTerminalBlock, object, bool>;
using A = System.Action<object>;
using C = System.Func<Sandbox.ModAPI.IMyTerminalBlock, object>;
using U = System.Collections.Generic.List<int>;
using L = System.Collections.Generic.IDictionary<int, float>;

namespace Scripts.Specials.ShipClass
{
    public class SpecBlockHooks
    {
        public interface ILimitedBlock
        {
            bool IsDrainingPoints();
            void Disable();
            void CanWork();
            bool CanBeDisabled();
            bool CheckConditions(IMyTerminalBlock specBlock);
        }
        
        public enum GetSpecCoreLimitsEnum
        {
            StaticLimits = 1,
            DynamicLimits = 2,
            FoundLimits = 3,
            TotalLimits = 4,
            CustomStatic = 5,
            CustomDynamic = 6,
            CurrentStaticOrDynamic = 7
        }
        

        private static Action<string, C, A, FF, F, F, A> RegisterCustomLimitConsumerImpl;
        private static Func<IMyCubeGrid, object> getMainSpecCore;
        private static Func<IMyCubeGrid, IMyTerminalBlock> getMainSpecCoreBlock;
        private static Action<object, L, int> getSpecCoreLimits;
        private static Action<object, U> getSpecCoreUpgrades;
        private static Action<object, L,L> setSpecCoreCustomValues;
        
        private static Func<IMyTerminalBlock, object> getSpecCoreBlock;
        private static Func<object, IMyTerminalBlock> getBlockSpecCore;
        private static Func<IMyTerminalBlock, object> getLimitedBlock;
        private static Func<object, IMyTerminalBlock> getLimitedBlockBlock;

        private static Func<IMyCubeGrid, Dictionary<Type, HashSet<IMyCubeBlock>>> getGridBlocksByType;
        private static Func<IMyCubeGrid, Dictionary<MyDefinitionId, HashSet<IMyCubeBlock>>> getGridBlocksById;
        
        private static Action <int, Func<object, List<IMyCubeGrid>, float>> registerSpecCorePointCustomFx;
        private static Action<Action<IMyTerminalBlock, object, Dictionary<int, float>, Dictionary<int, float>>> addSpecCoreLimitsInterceptor;
        
        public static event Action OnReady;
        
        public static event Action<object> OnSpecBlockCreated;
        public static event Action<object> OnSpecBlockDestroyed;
        public static event Action<object> OnLimitedBlockCreated;
        public static event Action<object> OnLimitedBlockDestroyed;

        public static event Action<object, List<IMyCubeGrid>> OnSpecBlockChanged;

        public static bool IsReady()
        {
            return
                RegisterCustomLimitConsumerImpl != null &&
                getMainSpecCore != null &&
                getMainSpecCoreBlock != null &&
                getSpecCoreLimits != null &&
                getSpecCoreUpgrades != null &&
                setSpecCoreCustomValues != null &&

                getSpecCoreBlock != null &&
                getBlockSpecCore != null &&
                getLimitedBlock != null &&
                getLimitedBlockBlock != null &&
                
                getGridBlocksByType != null &&
                getGridBlocksById != null &&
                
                
                addSpecCoreLimitsInterceptor != null &&
                registerSpecCorePointCustomFx != null;
        }

        private static void TriggerIsReady()
        {
            if (IsReady())
            {
                OnReady?.Invoke();
            }
        }
        
        /// <summary>
        /// Must be inited in LoadData of MySessionComponentBase
        /// </summary>
        public static void Init()
        {
            Log.ChatError("SpecBlockHooks:Init");
            ModConnection.Init();
            ModConnection.Subscribe<Action<string, C, A, FF, F, F, A>>("MIG.SpecCores.RegisterCustomLimitConsumer", (x) => { RegisterCustomLimitConsumerImpl = x; TriggerIsReady(); });
            ModConnection.Subscribe<Func<IMyCubeGrid, object>>("MIG.SpecCores.GetMainSpecCore", (x) => { getMainSpecCore = x; TriggerIsReady(); });
            ModConnection.Subscribe<Func<IMyCubeGrid, IMyTerminalBlock>>("MIG.SpecCores.GetMainSpecCoreBlock", (x) => { getMainSpecCoreBlock = x; TriggerIsReady(); });
            ModConnection.Subscribe<Action<object, L, int>>("MIG.SpecCores.GetSpecCoreLimits", (x) => { getSpecCoreLimits = x; TriggerIsReady(); });
            ModConnection.Subscribe<Action<object, U>>("MIG.SpecCores.GetSpecCoreUpgrades", (x) => { getSpecCoreUpgrades = x; TriggerIsReady(); });
            ModConnection.Subscribe<Action<object, L,L>>("MIG.SpecCores.SetSpecCoreCustomValues", (x) => { setSpecCoreCustomValues = x; TriggerIsReady(); });
            
            ModConnection.Subscribe<Func<IMyCubeGrid, Dictionary<Type, HashSet<IMyCubeBlock>>>>("MIG.SpecCores.GetGridBlocksByType", (x) => { getGridBlocksByType = x; TriggerIsReady(); });
            ModConnection.Subscribe<Func<IMyCubeGrid, Dictionary<MyDefinitionId, HashSet<IMyCubeBlock>>>>("MIG.SpecCores.GetGridBlocksById", (x) => { getGridBlocksById = x; TriggerIsReady(); });
            
            ModConnection.Subscribe<Func<IMyTerminalBlock, object>>("MIG.SpecCores.GetSpecCoreBlock", (x) => { getSpecCoreBlock = x; TriggerIsReady(); });
            ModConnection.Subscribe<Func<object, IMyTerminalBlock>>("MIG.SpecCores.GetBlockSpecCore", (x) => { getBlockSpecCore = x; TriggerIsReady(); });
            ModConnection.Subscribe<Func<IMyTerminalBlock, object>>("MIG.SpecCores.GetLimitedBlock", (x) => { getLimitedBlock = x; TriggerIsReady(); });
            ModConnection.Subscribe<Func<object, IMyTerminalBlock>>("MIG.SpecCores.GetLimitedBlockBlock", (x) => { getLimitedBlockBlock = x; TriggerIsReady(); });
            
            ModConnection.Subscribe<Action <int, Func<object, List<IMyCubeGrid>, float>>>("MIG.SpecCores.RegisterSpecCorePointCustomFx", (x) => { registerSpecCorePointCustomFx = x; TriggerIsReady(); });
            ModConnection.Subscribe<Action<Action<IMyTerminalBlock, object, Dictionary<int, float>, Dictionary<int, float>>>>("MIG.SpecCores.AddSpecCoreLimitsInterceptor", (x)=>{ addSpecCoreLimitsInterceptor = x; TriggerIsReady(); });
            
            
            Action<object> onSpecBlockCreated = (x) => OnSpecBlockCreated?.Invoke(x);
            Action<object> onSpecBlockDestroyed = (x) => OnSpecBlockDestroyed?.Invoke(x);
            Action<object> onLimitedBlockCreated = (x) => OnLimitedBlockCreated?.Invoke(x);
            Action<object> onLimitedBlockDestroyed = (x) => OnLimitedBlockDestroyed?.Invoke(x);

            Action<object, List<IMyCubeGrid>> onSpecBlockChanged = (x,y) => OnSpecBlockChanged?.Invoke(x,y);
            
            ModConnection.SetValue("MIG.SpecCores.OnSpecBlockCreated", onSpecBlockCreated);
            ModConnection.SetValue("MIG.SpecCores.OnLimitedBlockCreated", onSpecBlockDestroyed);
            ModConnection.SetValue("MIG.SpecCores.OnSpecBlockDestroyed", onLimitedBlockCreated);
            ModConnection.SetValue("MIG.SpecCores.OnLimitedBlockDestroyed", onLimitedBlockDestroyed);

            ModConnection.SetValue("MIG.SpecCores.OnSpecBlockChanged", onSpecBlockChanged);
        }

        public static void Close()
        {
            ModConnection.Close();
        }

        public static void SetCanSpecCoreWorkFx(Func<object, List<IMyCubeGrid>, string> fx)
        {
            ModConnection.SetValue("MIG.SpecCores.CanSpecCoreWork", fx);
        }
        
        public static void RegisterSpecCorePointCustomFx(int pointId, Func<object, List<IMyCubeGrid>, float> fx)
        {
            registerSpecCorePointCustomFx.Invoke(pointId, fx);
        }
        
        public static void RegisterCustomLimitConsumer(string Id, C OnNewConsumerRegistered, A CanWork, FF CheckConditions, F CanBeDisabled, F IsDrainingPoints, A Disable)
        {
            RegisterCustomLimitConsumerImpl(Id, OnNewConsumerRegistered, CanWork, CheckConditions, CanBeDisabled, IsDrainingPoints, Disable);
        }

        public static object GetMainSpecCore(IMyCubeGrid grid)
        {
            return getMainSpecCore.Invoke(grid);
        }
        
        public static IMyTerminalBlock GetMainSpecCoreBlock(IMyCubeGrid grid)
        {
            return getMainSpecCoreBlock.Invoke(grid);
        }
        
        public static void GetSpecCoreLimits(object specCore, IDictionary<int, float> buffer, GetSpecCoreLimitsEnum limits)
        {
            getSpecCoreLimits.Invoke(specCore, buffer, (int) limits);
        }
        
        public static void GetSpecCoreUpgrades(object specCore, List<int> buffer)
        {
            getSpecCoreUpgrades.Invoke(specCore, buffer);
        }
        
        public static void SetSpecCoreCustomValues(object specCore, IDictionary<int, float> staticValues, IDictionary<int, float> dynamicValues)
        {
            setSpecCoreCustomValues.Invoke(specCore, staticValues, dynamicValues);
        }
        
        public static void AddSpecCoreLimitsInterceptor(Action<IMyTerminalBlock, object, Dictionary<int, float>, Dictionary<int, float>> fx)
        {
            addSpecCoreLimitsInterceptor.Invoke(fx);
        }

        public static object GetSpecCoreBlock(IMyTerminalBlock block)
        {
            return getSpecCoreBlock.Invoke(block);
        }

        public static IMyTerminalBlock GetBlockSpecCore(object block)
        {
            return getBlockSpecCore.Invoke(block);
        }
        
        public static object GetLimitedBlock(IMyTerminalBlock block)
        {
            return getLimitedBlock.Invoke(block);
        }

        public static IMyTerminalBlock GetLimitedBlockBlock(object block)
        {
            return getLimitedBlockBlock.Invoke(block);
        }

        public static void RegisterCustomLimitConsumer(string Id, Func<IMyTerminalBlock, ILimitedBlock> creator)
        {
            SpecBlockHooks.RegisterCustomLimitConsumer(Id, 
                creator,
                (logic)=>
                {
                    ((ILimitedBlock) logic).CanWork();
                },
                (block, logic) =>
                {
                    return ((ILimitedBlock) logic).CheckConditions(block);
                },
                (logic)=>
                {
                    return ((ILimitedBlock) logic).CanBeDisabled();
                },
                (logic)=>
                {
                    return ((ILimitedBlock) logic).IsDrainingPoints();
                },
                (logic)=>
                {
                    ((ILimitedBlock) logic).Disable();
                });
        }
        
        public static Dictionary<Type, HashSet<IMyCubeBlock>> GetGridBlocksByType(IMyCubeGrid grid)
        {
            return getGridBlocksByType?.Invoke(grid) ?? null;
        }
        
        public static Dictionary<MyDefinitionId, HashSet<IMyCubeBlock>> GetGridBlocksById(IMyCubeGrid grid)
        {
            return getGridBlocksById?.Invoke(grid) ?? null;
        }
    }
}*/