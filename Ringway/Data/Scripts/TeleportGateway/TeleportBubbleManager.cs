using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace TeleportMechanisms
{
    public static class TeleportBubbleManager
    {
        private static readonly Dictionary<long, Vector3D> _bubblePositions = new Dictionary<long, Vector3D>();
        private static readonly object _lock = new object();
        private static readonly Color _bubbleColor = new Color(0, 0, 255, 64);  // Blue with 25% opacity

        public static void CreateOrUpdateBubble(IMyCollector gateway, Color color) {
            var gatewayLogic = gateway.GameLogic.GetAs<TeleportGateway>();
            if (gatewayLogic == null || !gatewayLogic.Settings.ShowSphere) {
                RemoveBubble(gateway); // Remove the bubble if ShowSphere is false
                return;
            }

            lock (_lock) {
                float sphereRadius = gatewayLogic.Settings.SphereDiameter / 2.0f;
                Vector3D position = gateway.GetPosition() + gateway.WorldMatrix.Forward * sphereRadius;
                _bubblePositions[gateway.EntityId] = position;
            }
        }

        public static void DrawBubble(IMyCollector gateway, Color color) {
            try {
                // Check if we're on a dedicated server
                if (MyAPIGateway.Utilities.IsDedicated) {
                    return;
                }

                // Check if the Session object is available
                if (MyAPIGateway.Session == null) {
                    return;
                }

                var gatewayLogic = gateway.GameLogic.GetAs<TeleportGateway>();
                if (gatewayLogic == null || !gatewayLogic.Settings.ShowSphere) {
                    return;
                }

                Vector3D position;
                lock (_lock) {
                    if (!_bubblePositions.TryGetValue(gateway.EntityId, out position)) {
                        return;
                    }
                }

                float radius = gatewayLogic.Settings.SphereDiameter / 2.0f;
                MatrixD worldMatrix = gateway.WorldMatrix;
                MatrixD adjustedMatrix = MatrixD.CreateWorld(position, worldMatrix.Forward, worldMatrix.Up);

                // Use the specified color
                MySimpleObjectDraw.DrawTransparentSphere(ref adjustedMatrix, radius, ref color, MySimpleObjectRasterizer.Solid, 20, null, MyStringId.GetOrCompute("Square"));
            }
            catch (Exception exc) {
                MyLog.Default.WriteLineAndConsole($"TeleportBubbleManager: Error drawing bubble: {exc.Message}\n{exc.StackTrace}");
            }
        }

        public static void RemoveBubble(IMyCollector gateway)
        {
            lock (_lock)
            {
                _bubblePositions.Remove(gateway.EntityId);
            }
        }
    }
}
