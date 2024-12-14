using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EFT;

namespace friendlyPMC.Utils
{
    public class Props
    {
        private static float _def_coverSearchRadius = 80f;
        private static float _def_sprintDistance = 15f;
        private static float _def_regroupMinDistance = 7f;
        private static float _def_searchRadius = 50f;
        private static float _def_nearSearchRadius = 30f;

        private static float _def_bossInnerRadius = 30f;

        private static float _def_bossOuterRadius = 50f;

        private static float _def_bossMaxCoverDistance = 35f;
        private static float _def_bossMinCoverDistance = 10f;

        public static float coverSearchRadius;
        public static float sprintDistance;
        public static float regroupMinDistance;
        public static float searchRadius;
        public static float nearSearchRadius;

        public static float bossInnerRadius;

        public static float bossOuterRadius;

        public static float bossMaxCoverDistance;
        public static float bossMinCoverDistance;

        public static void Reset()
        {
            coverSearchRadius = _def_coverSearchRadius;
            sprintDistance = _def_sprintDistance;
            regroupMinDistance = _def_regroupMinDistance;
            searchRadius = _def_searchRadius;
            nearSearchRadius = _def_nearSearchRadius;
            bossInnerRadius = _def_bossInnerRadius;
            bossOuterRadius = _def_bossOuterRadius;
            bossMaxCoverDistance = _def_bossMaxCoverDistance;
            bossMinCoverDistance = _def_bossMinCoverDistance;
        }

        public static void FactoryMapSett()
        {
            regroupMinDistance = 7f;
            bossInnerRadius = 20f;
            bossOuterRadius = 40f;
            coverSearchRadius = 40f;
            searchRadius = 40f;
        }

        public static Dictionary<string, List<string>> Quests = new Dictionary<string, List<string>>
        {
            {
                "Knight",
                // order is important
                new List<string>
                {
                    "friendlypmc-knight-competition",
                    "friendlypmc-knight-my-land",
                    "friendlypmc-knight-payback01",
                    "friendlypmc-knight-payback02"
                }
            },
            {
                "BigPipe",
                // order is important
                new List<string>
                {
                    "friendlypmc-knight-payback01",
                    "friendlypmc-knight-payback02",
                    "friendlypmc-knight-coverme01",
                    "friendlypmc-knight-coverme02"
                }
            },
            {
                "BirdEye",
                // order is important
                new List<string>
                {
                    "friendlypmc-knight-payback01",
                    "friendlypmc-knight-payback02",
                    "friendlypmc-knight-enemyspotted01",
                    "friendlypmc-knight-enemyspotted02",
                    "friendlypmc-knight-coverme01",
                    "friendlypmc-knight-coverme02",
                    "friendlypmc-knight-afavor01",
                    "friendlypmc-knight-afavor02"
                }
            }
        };

        public static Dictionary<string, List<string>> QuestsLocations = new Dictionary<
            string,
            List<string>
        >
        {
            {
                "friendlypmc-knight-competition",
                new List<string> { "lighthouse", "bigmap", "shoreline", "woods" }
            },
            {
                "friendlypmc-knight-my-land",
                new List<string> { "bigmap" }
            },
            {
                "friendlypmc-knight-payback01",
                new List<string> { "tarkovstreets" }
            },
            {
                "friendlypmc-knight-payback02",
                new List<string> { "rezervbase" }
            },
            {
                "friendlypmc-knight-enemyspotted01",
                new List<string> { "woods" }
            },
            {
                "friendlypmc-knight-enemyspotted02",
                new List<string> { "woods" }
            },
            {
                "friendlypmc-knight-coverme01",
                new List<string> { "shoreline" }
            },
            {
                "friendlypmc-knight-coverme02",
                new List<string> { "shoreline" }
            },
            {
                "friendlypmc-knight-afavor01",
                new List<string> { "woods" }
            },
            {
                "friendlypmc-knight-afavor02",
                new List<string> { "woods" }
            }
        };

        public static Dictionary<string, List<string>> QuestsTeamConditions = new Dictionary<
            string,
            List<string>
        >
        {
            {
                "Knight",
                new List<string> { "friendlypmc-knight-my-land-1" }
            },
            {
                "BigPipe",
                new List<string> { }
            },
            {
                "BirdEye",
                new List<string> { }
            },
            {
                "Any",
                new List<string>
                {
                    "friendlypmc-knight-payback01-target",
                    "friendlypmc-knight-payback02-target",
                    "friendlypmc-knight-coverme01-target",
                    "friendlypmc-knight-coverme02-target"
                }
            },
        };
        public static Dictionary<string, List<string>> QuestsKillConditions = new Dictionary<
            string,
            List<string>
        >
        {
            {
                "Knight",
                new List<string> { }
            },
            {
                "BigPipe",
                new List<string> { }
            },
            {
                "BirdEye",
                new List<string>
                {
                    "friendlypmc-knight-enemyspotted01-target",
                    "friendlypmc-knight-enemyspotted02-target",
                    "friendlypmc-knight-afavor01-target",
                    "friendlypmc-knight-afavor02-target"
                }
            },
            {
                "Player",
                new List<string>
                {
                    "friendlypmc-knight-competition-1",
                    "friendlypmc-knight-competition-2",
                    "friendlypmc-knight-competition-3"
                }
            }
        };

        public static Dictionary<WildSpawnType, List<string>> QuestBosses = new Dictionary<
            WildSpawnType,
            List<string>
        >
        {
            {
                WildSpawnType.bossKolontay,
                new List<string> { "friendlypmc-knight-payback01" }
            },
            {
                WildSpawnType.bossGluhar,
                new List<string> { "friendlypmc-knight-payback02" }
            },
            {
                WildSpawnType.bossKojaniy,
                new List<string> { "friendlypmc-knight-afavor01", "friendlypmc-knight-afavor02" }
            }
        };
    }
}
