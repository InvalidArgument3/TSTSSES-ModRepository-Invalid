using System.Collections.Generic;
using FactionsStruct;
using VRageMath;

namespace FactionsStruct
{

    public partial class FactionDefs
    {
        public FactionDefinition GanymedeLiberationFrontFaction => new FactionDefinition
        {
            // basic information
            Tag = "GLF-NPC", // faction tag from your vanilla faction definition
            CanSpawnCombatShipsInMissions = true, // can spawn as an enemy faction in missions
            CanSpawnInRandomEncounters = true, // can spawn random space and planetary encounters (affects trade ship spawns as well)
            CanSpawnMissions = true, // missions offered by this faction can be created

            // spawn lists
            CombatShips = new List<string> // prefab names of combat ships used by this faction, these ships are used for random spawns and mission spawns
            {
                "(NPC-NOM) Alca Fighter",
                "(NPC-NOM) Arcadius",
                "(NPC-NOM) Caligula",
                "(NPC-NOM) Constantine",
                "(NPC-NOM) Julian",
                "(NPC-NOM) Justinian",
                "(NPC-NOM) Nero",
                "(NPC-NOM) Vespasian",
                "(NPC-NOM) Vespid Fighter",
                "(NPC-NOM) Light Fighter",
                "(NPC-NOM) Medium Fighter",
            },
            CivilianBuildings = new List<string> // prefab names of civilian structures used by this faction, these are used for random spawns and mission spawns
            {
                "(NPC-NOM) Habitat module",
                "(NPC-NOM) Habitat Small Module",
                "(NPC-NOM) Housing Unit",
                "(NPC-NOM) Housing Building",
                "(NPC-NOM) Small Habitat",
                "(NPC-NOM) Lunar Habitat",
                "(NPC-NOM) Housing Unit 3",
                "(NPC-NOM) Housing Unit 2",
                "(NPC-NOM) Logistics Post",
                "(NPC-NOM) Prospecting Station",
                "(NPC-NOM) Relay Post",
                "(NPC-NOM) Salvage Yard",
                "(NPC-NOM) Shipping Platform",
                "(NPC-NOM) Supply Post",
            },
            MilitaryBuildings = new List<string> // prefab names of military structures used by this faction, these are used in mission spawns
            {
                "(NPC-NOM) Barracks",
                "(NPC-NOM) Bunker Fortification",
                "(NPC-NOM) Fortress Tower",
                "(NPC-NOM) Frontier Outpost",
                "(NPC-NOM) Living Quarters",
                "(NPC-NOM) Security Outpost",
                "(NPC-NOM) Sentry Tower",
                "(NPC-NOM) Signal Defense Tower",
                "(NPC-NOM) Signal Defense Tower",
                "(NPC-NOM) Simple Tower",
                "(NPC-NOM) Watchtower",
                "(NPC-NOM) Relay Post",
                "(NPC-NOM) Salvage Yard",
                "(NPC-NOM) Supply Post",
            },
            TradeShips = new List<string> // prefab names of trade ships used by this faction, these are used for random spawns and mission spawns
            {
                "(NPC-TRADE) Armed Transport",
                "(NPC-TRADE) Space Cargo Ship",
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
                 "GLF Fighter",
                 "GLF Frigate",
                 "GLF Miner",
                 "GLF Ringway Node",
            },
            SellGridsPlanets = new List<string> // prefab subtypes that will be sold on planetary economy stations when held by this faction (on top of the default ones)
            {
                "GLF Fighter",
                "GLF Frigate",
                "GLF Miner",
                "GLF Ringway Node",
            },
            SellGridsSpace = new List<string> // prefab subtypes that will be sold on space economy stations when held by this faction (on top of the default ones)
            {
                "GLF Fighter",
                "GLF Frigate",
                "GLF Miner",
                "GLF Ringway Node",
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
                Policies.Revolutionary,
            },
            Friendly = new List<Policies>
            {
                Policies.Criminal,
                Policies.Terrorist,
            },
            Hostile = new List<Policies>
            {
                Policies.Police,
                Policies.Corporation,
                Policies.Industrialists,
            },
            Neutral = new List<Policies>
            {
                Policies.Traders,
                Policies.Explorers,
            },
            Player = new List<string>
            {
                //"GLF"
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