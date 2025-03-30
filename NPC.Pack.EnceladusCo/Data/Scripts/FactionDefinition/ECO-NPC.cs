using System.Collections.Generic;
using FactionsStruct;
using VRageMath;

namespace FactionsStruct
{

    public partial class FactionDefs
    {
        public FactionDefinition EnceladusCorporationFaction => new FactionDefinition
        {
            // basic information
            Tag = "ECO-NPC", // faction tag from your vanilla faction definition
            CanSpawnCombatShipsInMissions = true, // can spawn as an enemy faction in missions
            CanSpawnInRandomEncounters = true, // can spawn random space and planetary encounters (affects trade ship spawns as well)
            CanSpawnMissions = true, // missions offered by this faction can be created

            // spawn lists
            CombatShips = new List<string> // prefab names of combat ships used by this faction, these ships are used for random spawns and mission spawns
            {
                "(NPC-CIV) Hauler A",
                "(NPC-CIV) Hauler B",
                "(NPC-CIV) Hauler C",
                "(NPC-CIV) Longsword Fighter",
                "(NPC-CIV) Mining Ship A",
                "(NPC-CIV) Mining Ship B",
                "(NPC-CIV) Mining Ship C",
                "(NPC-CIV) Mining Ship D",
                "(NPC-CIV) Mining Vessel",
                "(NPC-CIV) Hostile Takeover",
                "(NPC-CIV) Armed B-980",
                "(NPC-CIV) Mercenary Corvette",
                "(NPC-CIV) Mercenary Ship",
                "(NPC-CIV) Armed Freighter",
                "(NPC-CIV) Burstfire Fighter",
                "(NPC-CIV) Streamfire Fighter",
                "(NPC-CIV) Heavy Armed Freighter",
                "(NPC-CIV) Light Fighter",
                "(NPC-CIV) Medium Fighter",
            },
            CivilianBuildings = new List<string> // prefab names of civilian structures used by this faction, these are used for random spawns and mission spawns
            {
                "(NPC-CIV) Habitat module",
                "(NPC-CIV) Habitat Small Module",
                "(NPC-CIV) Housing Unit",
                "(NPC-CIV) Housing Building",
                "(NPC-CIV) Small Habitat",
                "(NPC-CIV) Lunar Habitat",
                "(NPC-CIV) Housing Unit 3",
                "(NPC-CIV) Housing Unit 2",
                "(NPC-CIV) Manufacturing Facility",
                "(NPC-CIV) Ore Processing Facility",
                "(NPC-CIV) Regional Headquarters",
                "(NPC-CIV) Shipping Platform",
                "(NPC-CIV) Storage Depot",
                "(NPC-CIV) Processing Plant",
            },
            MilitaryBuildings = new List<string> // prefab names of military structures used by this faction, these are used in mission spawns
            {
                "(NPC-CIV) Barracks",
                "(NPC-CIV) Bunker Fortification",
                "(NPC-CIV) Fortress Tower",
                "(NPC-CIV) Frontier Outpost",
                "(NPC-CIV) Living Quarters",
                "(NPC-CIV) Security Outpost",
                "(NPC-CIV) Sentry Tower",
                "(NPC-CIV) Signal Defense Tower",
                "(NPC-CIV) Signal Defense Tower",
                "(NPC-CIV) Simple Tower",
                "(NPC-CIV) Watchtower",
                "(NPC-CIV) Storage Depot",
                "(NPC-CIV) Regional Headquarters",
            },
            TradeShips = new List<string> // prefab names of trade ships used by this faction, these are used for random spawns and mission spawns
            {
                "(NPC-TRADE) Space Cargo Ship",
                "(NPC-TRADE) Space Trade Ship",
                "(NPC-TRADE) Planetary Freighter",
            },

            // economy definitions
            TradeContainers = new List<string> // container type definitions used to sell items on this faction's trade stations and trade ships
            {
                "ComponentsSell",
                "GasSell",
                "IngotSell",
                "OreSell",
                "ItemsSell",
            },
            BuyContainers = new List<string> // container type definitions used to buy items on this faction's trade stations and trade ships
            {
                "ComponentsBuy",
                "IceBuy",
                "IngotBuy",
                "OreBuy",
            },
            SellGridsHq = new List<string> // prefab subtypes that will be sold on this faction HQ economy stations
            {
                "ECO Fighter",
                "ECO Frigate",
                "ECO Miner",
                "ECO Ringway Node",

            },
            SellGridsPlanets = new List<string> // prefab subtypes that will be sold on planetary economy stations when held by this faction (on top of the default ones)
            {
                "ECO Fighter",
                "ECO Frigate",
                "ECO Miner",
                "ECO Ringway Node",

            },
            SellGridsSpace = new List<string> // prefab subtypes that will be sold on space economy stations when held by this faction (on top of the default ones)
            {
                "ECO Fighter",
                "ECO Frigate",
                "ECO Miner",
                "ECO Ringway Node",
            },

            // politics and relations
            // list of faction traits assigned to this faction
            // Traders - should represent factions with major focus on trade and transportation
            // Military - should represent a major law enforcing military power
            // Police - should represent police forces and minor law enforcing power
            // Criminal - should represent factions with criminal background
            // Revolutionary - should represent factions trying to subvert major powers
            // Religious - should represent religious zealots
            // Terrorist - should represent factions trying to subvert major powers, but with any means necessary
            // Industrialists - should represent factions focusing on mining and heavy industry
            // Corporation - should represent factions focused on profit generation
            // Scientific - should represent factions focused on scientific progress
            // Explorers - should represent factions trying to push the frontier
            // Nomads - should represent factions that have no major bases but mostly travel through space
            // Menace - should represent constantly aggressive factions hostile to everyone
            // HINT: Menace should be used for factions that are to be permanently hostile to everyone. Being a Menace overrides all other policies and relations.
            Politics = new List<Policies>
            {
                Policies.Corporation,
                Policies.Industrialists,
            },
            Friendly = new List<Policies>
            {
                Policies.Police,
            },
            Hostile = new List<Policies>
            {
                Policies.Criminal,
                Policies.Terrorist,
            },
            Neutral = new List<Policies>
            {
                Policies.Traders,
                Policies.Explorers,
            },
            Player = new List<string>
            {
               // "ECO"
            },

            // formations
            Formations = new List<Formation>
            {
                new Formation
                {
                    FormationPositions = new List<FormationPosition>
                    {
                        new FormationPosition
                        {
                            Position = new Vector3(0, 0, 0),
                            ShipSizes = new List<string>
                            {
                                "Tiny", "Small", "Medium", "Big", "Titan",
                            },
                        },
                    }
                },
                new Formation
                {
                    FormationPositions = new List<FormationPosition>
                    {
                        new FormationPosition
                        {
                            Position = new Vector3(0, 100, 0),
                            ShipSizes = new List<string>
                            {
                                "Tiny", "Small",
                            },
                        },
                        new FormationPosition
                        {
                            Position = new Vector3(0, -100, 0),
                            ShipSizes = new List<string>
                            {
                                "Tiny", "Small",
                            },
                        }
                    }
                },
                new Formation
                {
                    FormationPositions = new List<FormationPosition>
                    {
                        new FormationPosition
                        {
                            Position = new Vector3(0, 0, 0),
                            ShipSizes = new List<string>
                            {
                                "Medium", "Big", "Titan",
                            },
                        },
                        new FormationPosition
                        {
                            Position = new Vector3(0, -150, 0),
                            ShipSizes = new List<string>
                            {
                                "Tiny", "Small",
                            },
                        },
                        new FormationPosition
                        {
                            Position = new Vector3(0, 150, 0),
                            ShipSizes = new List<string>
                            {
                                "Tiny", "Small",
                            },
                        },
                    }
                }
            }
        };
    }
    
}