using BepInEx;
using BepInEx.Configuration;
using friendlyPMC.Patches;
using System.Collections.Generic;
using UnityEngine;

using Logger = friendlyPMC.Components.Logger;

namespace friendlyPMC
{
    [BepInPlugin("xyz.pit.companion", "friendlyPMC", "3.1.4")]
    [BepInDependency("com.spt-aki.core", "3.8.0")]
    [BepInDependency("xyz.drakia.bigbrain")]
    [BepInDependency("xyz.drakia.waypoints")]
    [BepInDependency("com.Arys.UnityToolkit")]
    [BepInDependency("me.skwizzy.lootingbots")]
    public class friendlyPMC : BaseUnityPlugin
    {
        public static bool awaken;

        internal static friendlyPMC Instance { get; private set; }

        public static Dictionary<GameObject, HashSet<Material>> objectsMaterials = new Dictionary<GameObject, HashSet<Material>>();
        
        const string baseSettings = "Base Settings";
        const string miscSettings = "Miscellaneous";

        public static ConfigEntry<bool> squadSpawn;
        public static ConfigEntry<int> squadSize;
        public static ConfigEntry<bool> copyEquip;
        public static ConfigEntry<int> extraPickups;

        public static ConfigEntry<int> enemyRemember;

        public static ConfigEntry<int> fightOuterRadius;
        public static ConfigEntry <int> fightInnerRadius;

        public static ConfigEntry<int> regroupMinDistance;
        public static ConfigEntry<int> maximumCover;
        public static ConfigEntry<int> maximumCoverDistance;
        public static ConfigEntry<int> maximumRadius;

        public static ConfigEntry<int> scanDistance;

        private void Awake()
        {

            squadSpawn = Config.Bind(baseSettings, "Squad Spawn", true, new ConfigDescription("Spawn with followers"));
            squadSize = Config.Bind(baseSettings, "Squad Size", 2, new ConfigDescription("Number of followers to spawn with", new AcceptableValueRange<int>(1, 3)));
            copyEquip = Config.Bind(baseSettings, "Clone Equipment", true, new ConfigDescription("When Squad Spawn is active, spawned followers will have the same equipment as the player"));
            extraPickups = Config.Bind(baseSettings, "Maximum followers", 3, new ConfigDescription("Maximum number of followers the player can have. Cannot be less than Squad Size if Squad Spawn is active", new AcceptableValueRange<int>(1, 4)));

            regroupMinDistance = Config.Bind(miscSettings, "Regroup Minimum Distance", 7, new ConfigDescription("The minimum distance for the regroup call to have effect, in combat", new AcceptableValueRange<int>(5, 30)));

            maximumRadius = Config.Bind(miscSettings, "Maximum distance to Boss", 100, new ConfigDescription("The maximum distance a follower can go out relative to the player. This is applied only at the begining of a raid", new AcceptableValueRange<int>(80, 300)));

            scanDistance = Config.Bind(miscSettings, "Maximum scan distance", 140, new ConfigDescription("Maximum distance to pick up any visible enemy that the player is signaling when issuing 'Contact' phrase", new AcceptableValueRange<int>(50, 300)));

            enemyRemember = Config.Bind(miscSettings, "Time to forget about enemy (in sec.)", 20, new ConfigDescription("Maximum time a follower will remember an enemy. This is applied only at the begining of a raid", new AcceptableValueRange<int>(5, 60)));

            maximumCover = Config.Bind(miscSettings, "Combat Cover Stay (in sec.)", 10, new ConfigDescription("Maximum time a follower will stay in cover when in 'defend' mode before trying to get closer to the player", new AcceptableValueRange<int>(2, 20)));
            maximumCoverDistance = Config.Bind(miscSettings, "Combat Cover distance", 30, new ConfigDescription("Maximum distance allowed between the follower and the player while the follower is in cover, when in 'defend' mode", new AcceptableValueRange<int>(10, 50)));

            fightOuterRadius = Config.Bind(miscSettings,"Combat Outer Radius", 50, new ConfigDescription("The upper limit to search for cover during combat relative the current goal (player or enemy)", new AcceptableValueRange<int>(30, 100)));
            fightInnerRadius = Config.Bind(miscSettings, "Combat Inner Radius", 30, new ConfigDescription("The lower limit to search for cover during combat relative the current goal (player or enemy)", new AcceptableValueRange<int>(15, 50)));

            if (!awaken)
            {
                awaken = true;
                Instance = this;
                new Logger();
            }

            new BotGroupIsEnemyPatch().Enable();
            new BotGroupCheckAndAddEnemy().Enable();
            new BotGroupReportAboutEnemyy().Enable();
            new BotGroupIsPlayerEnemy().Enable();
            new BotMemoryAddEnemyPatch().Enable();

            new BotOwnerIsFolowerPatch().Enable();
            new BotOwnerManualUpdatePatch().Enable();

            new PatrolDataFollowerPatch().Enable();

            new FollowRequestPatch().Enable();
            new HoldRequestPatch().Enable();

            new BotReceiverInitPatch().Enable();
            new BotReceiverDisposePatch().Enable();
            new BotReceiverPhrasePatch().Enable();

            new BotTalkTrySayPatch().Enable();
            new BotTalkSayPatch().Enable();

            new BotsControllerPatch().Enable();

            new NonWavesSpawnScenarioRunPatch().Enable();
            new WavesSpawnScenarioRunPatch().Enable();

            new BotsControllerStopPatch().Enable();
            new LocalGameCleanupPatch().Enable();

            new AIDataDisposePatch().Enable();
            new AIDataContructPatch().Enable();
            new AIDataBossPlayerPatch().Enable();

            new QuickPanelPatch().Enable();
            new GestureMenuPatch().Enable();
            new EPhraseTriggerPatch().Enable();

        }

    }
}
