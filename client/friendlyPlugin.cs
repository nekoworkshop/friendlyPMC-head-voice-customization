using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.UI;

using friendlyPMC.Modules;
using friendlyPMC.Patches;
using friendlyPMC.Utils;
using HarmonyLib;

using System.Collections.Generic;
using UnityEngine;

using Logger = friendlyPMC.Components.Logger;

namespace friendlyPMC
{

    public enum CustomBotRequestType
    {
        TakeLoot = 20,
        Regroup = 30,
        OverThere = 40,
        NeedHelp = 50
    }

    public enum CustomBotDecisions
    {
        SniperSearch = 100,
        CoverToCover = 101,
        EnemySearch = 102
    }

    public enum CustomPhrases
    {
        TeamStatus = 200
    }

    public class FollowerUtils
    {
        public static bool IsFollower(BotOwner Bot)
        {
            return BossPlayers.Instance.IsFollower(Bot);
        }
    }

    [BepInPlugin("xyz.pit.companion", "friendlyPMC", "3.4.0")]
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

        public static readonly float fightOuterRadius = 50f;
        public static readonly float fightInnerRadius = 30f;

        public static readonly float regroupMinDistance = 7f;

        public static readonly float maximumCover = 10f;
        public static readonly float maximumCoverDistance = 35f;

        public static ConfigEntry<int> scanDistance;

        public static ConfigEntry<int> returnChanceDeath;

        public static ConfigEntry<bool> knightSpawn;
        public static ConfigEntry<bool> bigPipeSpawn;
        public static ConfigEntry<bool> birdEyeSpawn;
        public static ConfigEntry<bool> justKnightSpawn;

        private void Awake()
        {

            squadSpawn = Config.Bind(baseSettings, "1 Squad spawn", true, new ConfigDescription("Spawn with followers"));
            squadSize = Config.Bind(baseSettings, "1.4  -  Squad size", 2, new ConfigDescription("Number of followers to spawn with", new AcceptableValueRange<int>(1, 30)));

            copyEquip = Config.Bind(baseSettings, "1.6  -  Clone equipment", true, new ConfigDescription("When Squad Spawn is active, spawned followers will have the same equipment as the player"));
            extraPickups = Config.Bind(baseSettings, "2 Maximum followers", 3, new ConfigDescription("Maximum number of followers the player can have. Cannot be less than Squad Size if Squad Spawn is active", new AcceptableValueRange<int>(1, 30)));

            alternativeSpawn = Config.Bind(baseSettings, "1.2  -  Alternative Spawn", false, new ConfigDescription("Try alternative Spawning method to help with Swag+Donuts"));
            squadDelay = Config.Bind(baseSettings, "1.3  -  Squad spawn Delay", 0, new ConfigDescription("When Squad Spawn is active, how much to delay the spawn of the squad ( in sec.). This is useful in case you have Swag+Donuts. Set delay above 10 seconds.", new AcceptableValueRange<int>(0, 30)));

            returnChanceDeath = Config.Bind(baseSettings, "1.5  -  Squadmate return chance after death", 50, new ConfigDescription("Chance your followers will return the items you gave them should you die. This applies only to members you spawned with.", new AcceptableValueRange<int>(1, 100)));

            scanDistance = Config.Bind(miscSettings, "1 Maximum scan distance", 140, new ConfigDescription("Maximum distance to pick up any visible enemy that the player is signaling when issuing 'Contact' phrase", new AcceptableValueRange<int>(50, 300)));

            enemyRemember = Config.Bind(miscSettings, "2 Time to forget about enemy (in sec.)", 20, new ConfigDescription("Maximum time a follower will remember an enemy. This is applied only at the begining of a raid", new AcceptableValueRange<int>(5, 60)));

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

            var harmony = new Harmony("xyz.pit.companion");
            harmony.PatchAll(typeof(GoalEnemyTracePatch).Assembly);
            harmony.PatchAll(typeof(LocalGameCtorPatch).Assembly);

            //SAINPatch.PatchSAINIfInstalled();

        }

    }
}
