using Sandbox.ModAPI;
using VRageMath;
using System;
using RealGasGiants;
using VRage.Game;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.Library.Utils;
using VRage;
using VRage.ModAPI;
using Sandbox.Game.Entities.Planet;
using VRage.Game.ModAPI; // Import for MyPlanet

namespace RingMoonletSpawner
{
    public class RingMoonletSpawner
    {
        private Random _rand;

        // Hardcoded planet and ring properties based on your clarification
        private const double PlanetRadiusKm = 60268; // Planet's radius in kilometers
        private const double RingInnerScale = 1.2;   // Start the ring at 20% beyond the planet's radius
        private const double RingOuterScale = 2.5;   // Outer ring scale, based on your preference

        // Moonlet size range (in kilometers)
        private const double MoonletMinSizeKm = 8.0;  // Minimum moonlet size (8 km diameter)
        private const double MoonletMaxSizeKm = 30.0; // Maximum moonlet size (30 km diameter)

        // Ring Normal (tilted plane) based on config
        private readonly Vector3D RingNormal = new Vector3D(1, 10, 0.5).Normalized();

        // Calculate the inner and outer ring distances from the planet's surface
        private const double RingInnerRadiusKm = PlanetRadiusKm * RingInnerScale; // 72,321.6 km
        private const double RingOuterRadiusKm = PlanetRadiusKm * RingOuterScale; // 150,670 km

        public RingMoonletSpawner(int seed)
        {
            _rand = new Random(seed);
        }

        public void PopulateMoonlets(int moonletCount, MyPlanet targetPlanet)
        {
            Vector3D planetCenter = targetPlanet.PositionComp.GetPosition(); // Get the planet's center

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Spawning moonlets along Saturn's ring. Planet center: {planetCenter}");
            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Ring Inner Radius: {RingInnerRadiusKm:N2} km, Outer Radius: {RingOuterRadiusKm:N2} km");

            long playerId = MyAPIGateway.Session.Player.IdentityId;
            int spawnedMoonlets = 0;

            for (int i = 0; i < moonletCount; i++)
            {
                Vector3D moonletPosition = GetRandomPositionInTiltedRingPlane(planetCenter);
                double moonletSizeKm = GetRandomMoonletSize(); // Get a random size for the moonlet
                SpawnMoonlet(moonletPosition, moonletSizeKm);

                // Create a GPS marker for the moonlet
                CreateGpsMarker(playerId, $"Moonlet {i + 1}", moonletPosition);

                spawnedMoonlets++;
            }

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Spawned {spawnedMoonlets} moonlets out of {moonletCount} requested.");
        }

        // Generate a random position in the tilted ring plane, respecting inner and outer radii
        private Vector3D GetRandomPositionInTiltedRingPlane(Vector3D planetCenter)
        {
            // Generate a random angle to distribute the moonlets in a circular manner
            double angle = _rand.NextDouble() * Math.PI * 2;

            // Random distance between inner and outer ring bounds, now in kilometers
            double distanceFromCenterKm = RingInnerRadiusKm + (RingOuterRadiusKm - RingInnerRadiusKm) * _rand.NextDouble();

            // Convert distance to meters for position calculation (because we need positions in meters)
            double distanceFromCenterMeters = distanceFromCenterKm * 1000;

            // Create two perpendicular vectors in the ring plane
            Vector3D ringRight = Vector3D.CalculatePerpendicularVector(RingNormal); // Perpendicular vector to RingNormal
            Vector3D ringUp = Vector3D.Cross(ringRight, RingNormal); // Orthogonal vector for the ring's plane

            // Calculate the moonlet's position in the tilted ring plane
            Vector3D positionInPlane = (Math.Cos(angle) * ringRight + Math.Sin(angle) * ringUp) * distanceFromCenterMeters;

            // Final moonlet position relative to the planet's center
            Vector3D finalPosition = planetCenter + positionInPlane;

            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Generated random position at {distanceFromCenterKm:N2} km from the gas giant, along the tilted ring plane.");
            return finalPosition;
        }

        // Generate a random size for the moonlet (in kilometers)
        private double GetRandomMoonletSize()
        {
            double moonletSizeKm = MoonletMinSizeKm + (MoonletMaxSizeKm - MoonletMinSizeKm) * _rand.NextDouble();
            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Generated moonlet size: {moonletSizeKm:N2} km");
            return moonletSizeKm;
        }

        // Spawn the moonlet with a given position and size (size is in kilometers)
        private void SpawnMoonlet(Vector3D position, double sizeKm)
        {
            // Convert size from kilometers to meters for the game
            double sizeMeters = sizeKm * 1000;

            MyPlanet moonlet = MyAPIGateway.Session.VoxelMaps.SpawnPlanet("Moonlet", (float)sizeMeters, _rand.Next(), position) as MyPlanet;

            if (moonlet != null)
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Moonlet spawned successfully at {position} with size {sizeKm:N2} km");
            }
            else
            {
                MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Failed to spawn moonlet at {position}. Check position and spawn parameters.");
            }
        }

        // Create a GPS marker at the given position and send it to the player
        private void CreateGpsMarker(long identityId, string gpsName, Vector3D position)
        {
            IMyGps gps = MyAPIGateway.Session.GPS.Create(gpsName, "Moonlet location", position, true, false);
            MyAPIGateway.Session.GPS.AddGps(identityId, gps);
            MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"GPS marker '{gpsName}' created at {position}");
        }

        public static void HandleChatCommand(string messageText, ref bool sendToOthers)
        {
            if (messageText.StartsWith("/populatemoonlets"))
            {
                sendToOthers = false;
                string[] tokens = messageText.Split(' ');

                if (tokens.Length != 2)
                {
                    MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "Usage: /populatemoonlets <moonletCount>");
                    return;
                }

                try
                {
                    int moonletCount = int.Parse(tokens[1]);

                    // Get the closest planet using MyGamePruningStructure.GetClosestPlanet
                    Vector3D playerPosition = MyAPIGateway.Session.Player.GetPosition();
                    MyPlanet closestPlanet = MyGamePruningStructure.GetClosestPlanet(playerPosition);

                    if (closestPlanet == null)
                    {
                        MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", "No planet found!");
                        return;
                    }

                    var spawner = new RingMoonletSpawner(MyRandom.Instance.Next());
                    spawner.PopulateMoonlets(moonletCount, closestPlanet);
                }
                catch (Exception ex)
                {
                    MyAPIGateway.Utilities.ShowMessage("RingMoonletSpawner", $"Invalid command parameters: {ex.Message}");
                }
            }
        }
    }

    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class RingMoonletSpawnerSession : MySessionComponentBase
    {
        public override void LoadData()
        {
            base.LoadData();
            MyAPIGateway.Utilities.MessageEntered += RingMoonletSpawner.HandleChatCommand;
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            MyAPIGateway.Utilities.MessageEntered -= RingMoonletSpawner.HandleChatCommand;
        }
    }
}
