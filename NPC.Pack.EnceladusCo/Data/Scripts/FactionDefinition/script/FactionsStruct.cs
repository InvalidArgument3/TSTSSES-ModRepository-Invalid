using System.Collections.Generic;
using ProtoBuf;
using VRageMath;

namespace FactionsStruct
{
    
    public enum Policies
    {
        Traders,
        Military,
        Police,
        Criminal,
        Revolutionary,
        Religious,
        Terrorist,
        Industrialists,
        Corporation,
        Scientific,
        Explorers,
        Nomads,
        Menace
    }
    
    [ProtoContract]
    public class Registration
    {
        [ProtoMember(1)] public List<FactionDefinition> FactionDefList = new List<FactionDefinition>();
    }
    
    [ProtoContract]
    public class FactionDefinition
    {
        [ProtoMember(1)] public string Tag;
        [ProtoMember(2)] public bool CanSpawnCombatShipsInMissions = false;
        [ProtoMember(3)] public bool CanSpawnInRandomEncounters = false;
        [ProtoMember(4)] public bool CanSpawnMissions = false;
        [ProtoMember(5)] public List<string> CombatShips = new List<string>();
        [ProtoMember(6)] public List<string> CivilianBuildings = new List<string>();
        [ProtoMember(7)] public List<string> MilitaryBuildings = new List<string>();
        [ProtoMember(8)] public List<string> TradeShips = new List<string>();
        [ProtoMember(9)] public List<string> TradeContainers = new List<string>();
        [ProtoMember(10)] public List<string> BuyContainers = new List<string>();
        [ProtoMember(11)] public List<string> SellGridsHq = new List<string>();
        [ProtoMember(12)] public List<string> SellGridsPlanets = new List<string>();
        [ProtoMember(13)] public List<string> SellGridsSpace = new List<string>();
        [ProtoMember(14)] public List<Formation> Formations = new List<Formation>();
        [ProtoMember(15)] public List<Policies> Politics = new List<Policies>();
        [ProtoMember(16)] public List<Policies> Friendly = new List<Policies>();
        [ProtoMember(17)] public List<Policies> Hostile = new List<Policies>();
        [ProtoMember(18)] public List<Policies> Neutral = new List<Policies>();
        [ProtoMember(19)] public List<string> Player = new List<string>();
    }
    
    [ProtoContract]
    public class Formation
    {
        [ProtoMember(1)] public List<FormationPosition> FormationPositions = new List<FormationPosition>();
    }
    
    [ProtoContract]
    public class FormationPosition
    {
        [ProtoMember(1)] public Vector3 Position = Vector3.Zero;
        [ProtoMember(2)] public List<string> ShipSizes = new List<string>();
    }
    
}