using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;

namespace Math0424.AnimationCoreAPI
{

    /// <summary>
    /// Author: Math0424
    /// An API for easily creating animated blocks
    /// </summary>
    public class AnimationCoreAPI
    {

        #region Helpers
        public static readonly Vector3D Up = new Vector3D(0, 1, 0);
        public static readonly Vector3D Right = new Vector3D(0, 0, 1);
        public static readonly Vector3D Forward = new Vector3D(1, 0, 0);
        public static Vector3D Vector(double x, double y, double z)
        {
            return new Vector3D(x, y, z);
        }
        public static Vector3D Axis(double x, double y, double z)
        {
            return new Vector3D(x, y, z);
        }
        public static Vector3D Transform(double x, double y, double z)
        {
            return new Vector3D(x, y, z);
        }
        public static string[] List(params string[] list)
        {
            return list;
        }
        public static void Log(object o)
        {
            MyAPIGateway.Utilities.ShowMessage("AnimationCore", (o ?? "Null").ToString());
            MyLog.Default.WriteLineAndConsole("AnimationCore: " + (o ?? "Null"));
        }
        #endregion

        #region Block
        public class BlockAnimation
        {
            public string[] Subparts;
            public string[] Chain;
            public double Angle;
            public double Scale;
            public int Delay;
            public int Frames;
            public TriggerType Trigger;
            public Components Component;
            public MoveTypes Movement;
            public LerpType Lerp;
            public EaseType Ease;
            public Vector3D Axis;
            public Vector3D Transform;
            public int LoopCount;
        }

        #endregion

        #region Subpart
        public class ParticleAnimation
        {
            public string Name;
            public bool AutoDelete = true;
            public float Scale = 1;
            public float LifeMultiplier = 1;
            public Vector4? Color;
            public Vector3? Velocity;
            public Vector3? AnimationUp;
            public ParticleAnimationType Animation;
        }
        #endregion

        #region Enums

        public enum TriggerType
        {
            None = 0,

            OnCreate = 1 << 1,
            OnBuilt = 1 << 2,

            OnWorkingChanged = 1 << 3,
            OnWorking = 1 << 4,
            OnNotWorking = 1 << 5,

            OnEnabledChanged = 1 << 6,
            OnOwnershipChanged = 1 << 7,
            OnNameChanged = 1 << 8,

            OnStartedProducing = 1 << 9,
            OnStoppedProducing = 1 << 10,

            OnLockedToGround = 1 << 11,
            OnUnLockedToGround = 1 << 12,
            OnReadyLockToGround = 1 << 13,

            OnDoorOpen = 1 << 14,
            OnDoorClose = 1 << 15,
        }

        public enum Components
        {
            None,
            Move,
            Effects,
        }

        public enum MoveTypes
        {
            None,
            ResetPos,
            ResetLerp,
            ResetAll,
            SetResetPos,
            Translate,
            Rotate,
            Spin,
            SpinAroundOrgin,
            Vibrate,
            Trigger,
        }

        //https://easings.net/
        public enum LerpType
        {
            Instant,
            Linear,

            Bounce,
            Elastic,
            Expo,
            Cubic,
            Back
        }

        public enum EaseType
        {
            In,
            Out,
            InOut,
        }

        public enum ParticleAnimationType
        {
            Circle,
            Ring,
            Swirl,
            Wormhole,
            Random,
            RandomAdd,
            Lightning,

        }

        #endregion

    }
}
