using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.UI;
using EFT.UI.Gestures;
using friendlyPMC.Modules;
using friendlyPMC.Patches;
using friendlyPMC.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using UnityEngine;

using Logger = friendlyPMC.Components.Logger;

namespace friendlyPMC
{

    public enum CustomBotRequestType
    {
        TakeLoot = 20,
        Regroup = 30,
        OverThere = 40
    }

    [BepInPlugin("xyz.pit.companion", "friendlyPMC", "3.3.5")]
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
        const string testSettings = "Testing";

        public static ConfigEntry<bool> squadSpawn;
        public static ConfigEntry<int> squadSize;
        public static ConfigEntry <int> squadDelay;
        public static ConfigEntry<bool> copyEquip;
        public static ConfigEntry<int> extraPickups;

        public static ConfigEntry<bool> alternativeSpawn;

        public static ConfigEntry<int> enemyRemember;

        public static ConfigEntry<int> fightOuterRadius;
        public static ConfigEntry <int> fightInnerRadius;

        public static ConfigEntry<int> regroupMinDistance;
        public static ConfigEntry<int> maximumCover;
        public static ConfigEntry<int> maximumCoverDistance;
        public static ConfigEntry<int> maximumRadius;

        public static ConfigEntry<int> scanDistance;

        public static ConfigEntry<int> returnChanceDeath;

        public static ConfigEntry<bool> knightSpawn;
        public static ConfigEntry<bool> bigPipeSpawn;
        public static ConfigEntry<bool> birdEyeSpawn;
        public static ConfigEntry<bool> justKnightSpawn;

        public static ConfigEntry<KeyboardShortcut> pingKey;
        private void Awake()
        {

            squadSpawn = Config.Bind(baseSettings, "1 Squad spawn", true, new ConfigDescription("Spawn with followers"));
            squadSize = Config.Bind(baseSettings, "1.4  -  Squad size", 2, new ConfigDescription("Number of followers to spawn with", new AcceptableValueRange<int>(1, 30)));

            copyEquip = Config.Bind(baseSettings, "1.6  -  Clone equipment", true, new ConfigDescription("When Squad Spawn is active, spawned followers will have the same equipment as the player"));
            extraPickups = Config.Bind(baseSettings, "2 Maximum followers", 3, new ConfigDescription("Maximum number of followers the player can have. Cannot be less than Squad Size if Squad Spawn is active", new AcceptableValueRange<int>(1, 30)));

            alternativeSpawn = Config.Bind(baseSettings, "1.2  -  Alternative Spawn", false, new ConfigDescription("Try alternative Spawning method to help with Swag+Donuts"));
            squadDelay = Config.Bind(baseSettings, "1.3  -  Squad spawn Delay", 0, new ConfigDescription("When Squad Spawn is active, how much to delay the spawn of the squad ( in sec.). This is useful in case you have Swag+Donuts. Set delay above 10 seconds.", new AcceptableValueRange<int>(0, 30)));

            returnChanceDeath = Config.Bind(baseSettings, "1.5  -  Squadmate return chance after death", 50, new ConfigDescription("Chance your followers will return the items you gave them should you die. This applies only to members you spawned with.", new AcceptableValueRange<int>(1, 100)));

            pingKey = Config.Bind(baseSettings, "2 Ping Squad", new KeyboardShortcut(KeyCode.F10), new ConfigDescription("Configurable key to trigger location of where your squad is"));

            regroupMinDistance = Config.Bind(miscSettings, "3 Regroup minimum distance", 7, new ConfigDescription("The minimum distance for the regroup call to have effect, in combat", new AcceptableValueRange<int>(5, 30)));

            maximumRadius = Config.Bind(miscSettings, "1 Maximum distance to Boss", 100, new ConfigDescription("The maximum distance a follower can go out relative to the player. This is applied only at the begining of a raid", new AcceptableValueRange<int>(80, 300)));

            scanDistance = Config.Bind(miscSettings, "6 Maximum scan distance", 140, new ConfigDescription("Maximum distance to pick up any visible enemy that the player is signaling when issuing 'Contact' phrase", new AcceptableValueRange<int>(50, 300)));

            enemyRemember = Config.Bind(miscSettings, "6,1  -  Time to forget about enemy (in sec.)", 20, new ConfigDescription("Maximum time a follower will remember an enemy. This is applied only at the begining of a raid", new AcceptableValueRange<int>(5, 60)));

            maximumCover = Config.Bind(miscSettings, "2.1  -  Combat cover stay (in sec.)", 10, new ConfigDescription("Maximum time a follower will stay in cover when in 'defend' mode before trying to get closer to the player", new AcceptableValueRange<int>(2, 20)));
            maximumCoverDistance = Config.Bind(miscSettings, "2 Combat cover distance", 30, new ConfigDescription("Maximum distance allowed between the follower and the player while the follower is in cover, when in 'defend' mode", new AcceptableValueRange<int>(10, 50)));

            fightOuterRadius = Config.Bind(miscSettings, "4 Combat outer radius", 50, new ConfigDescription("The upper limit to search for cover during combat relative the current goal (player or enemy)", new AcceptableValueRange<int>(30, 100)));
            fightInnerRadius = Config.Bind(miscSettings, "5 Combat inner radius", 30, new ConfigDescription("The lower limit to search for cover during combat relative the current goal (player or enemy)", new AcceptableValueRange<int>(15, 50)));

            knightSpawn = Config.Bind(testSettings, "1 Spawn with The Goons", false, new ConfigDescription("Experimental: Spawn with the goons squad. This works in combination with your own squad. Take note that a boss and his followers do not accept the same commands as your squad"));

            justKnightSpawn = Config.Bind(testSettings, "1.1  -  Spawn with Knight", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));

            bigPipeSpawn = Config.Bind(testSettings, "1.2  -  Spawn with BigPipe", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));

            birdEyeSpawn = Config.Bind(testSettings, "1.3  -  Spawn with BirdEye", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));

            ConsoleScreen.Processor.RegisterCommand("followerstome", delegate ()
            {
                GameWorld gameWorld = Singleton<GameWorld>.Instance;

                bool flag = !Singleton<AbstractGame>.Instantiated;
                if (flag)
                {
                    ConsoleScreen.LogError("This command may only be used inraid");
                    return;
                }


                if (GamePlayerOwner.MyPlayer.HealthController == null || !GamePlayerOwner.MyPlayer.HealthController.IsAlive)
                {
                    return;
                }

                string id = GamePlayerOwner.MyPlayer.ProfileId;

                if (BossPlayers.Instance != null)
                {
                    var followers = BossPlayers.Instance.GetBossFollowers(id);
                    Vector3 position = GamePlayerOwner.MyPlayer.Transform.position;
                    foreach (var follower in followers)
                    {
                        if (follower != null && follower.GetBot().HealthController.IsAlive)
                        {
                            follower.GetBot().GetPlayer.Teleport(position);
                        }
                    }
                }

            });


            if (!awaken)
            {
                awaken = true;
                Instance = this;
                new Logger();
            }

            new BotGroupReportAboutEnemyy().Enable();
            new BotGroupIsPlayerEnemy().Enable();
            new BotGroupAddEnemy().Enable();

            new BotMemoryAddEnemyPatch().Enable();

            new BotGroupUsecEnemyPatch().Enable();

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
            new Glass579RunPatch().Enable();

            new BotsControllerStopPatch().Enable();
            new LocalGameCleanupPatch().Enable();

            new AIDataDisposePatch().Enable();
            new AIDataContructPatch().Enable();
            new AIDataBossPlayerPatch().Enable();

            new QuickPanelPatch().Enable();
            new GestureMenuPatch().Enable();
            new EPhraseTriggerPatch().Enable();
        }

        void Update()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return;

            if (GamePlayerOwner.MyPlayer == null || GamePlayerOwner.MyPlayer.HealthController == null || !GamePlayerOwner.MyPlayer.HealthController.IsAlive)
            {
                return;
            }

            if (pingKey.Value.IsPressed())
            {

                string id = GamePlayerOwner.MyPlayer.ProfileId;

                if (BossPlayers.Instance != null && PingTeamates.Instance != null)
                {
                    var boss = BossPlayers.Instance.GetBossPlayer(id);
                    if (boss != null)
                    {
                        PingTeamates.Instance.Ping(boss);
                    }
                }

            }

        }

    }
}
