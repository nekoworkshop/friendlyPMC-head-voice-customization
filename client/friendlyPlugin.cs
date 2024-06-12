using BepInEx;
using BepInEx.Configuration;
using friendlyPMC.Patches;
using System.Collections.Generic;
using UnityEngine;

using Logger = friendlyPMC.Components.Logger;

namespace friendlyPMC
{
    [BepInPlugin("xyz.pit.companion", "friendlyPMC", "3.0.5")]
    [BepInDependency("com.spt-aki.core", "3.8.0")]
    [BepInDependency("xyz.drakia.bigbrain")]
    [BepInDependency("xyz.drakia.waypoints")]
    [BepInDependency("com.Arys.UnityToolkit")]
    public class friendlyPMC : BaseUnityPlugin
    {
        public static bool awaken;

        internal static friendlyPMC Instance { get; private set; }

        public static Dictionary<GameObject, HashSet<Material>> objectsMaterials = new Dictionary<GameObject, HashSet<Material>>();
        
        const string baseSettings = "Base Settings";

        public static ConfigEntry<bool> squadSpawn;
        public static ConfigEntry<int> squadSize;
        public static ConfigEntry<bool> copyEquip;
        public static ConfigEntry<int> extraPickups;
        private void Awake()
        {

            squadSpawn = Config.Bind(baseSettings, "Squad Spawn", true, new ConfigDescription("Spawn with followers"));
            squadSize = Config.Bind(baseSettings, "Squad Size", 2, new ConfigDescription("Number of followers to spawn with", new AcceptableValueRange<int>(1, 3)));
            copyEquip = Config.Bind(baseSettings, "Clone Equipment", true, new ConfigDescription("When Squad Spawn is active, spawned followers will have the same equipment as the player"));
            extraPickups = Config.Bind(baseSettings, "Maximum followers", 2, new ConfigDescription("Maximum number of followers the player can have. Cannot be less than Squad Size if Squad Spawn is active", new AcceptableValueRange<int>(1, 4)));

            if (!awaken)
            {
                awaken = true;
                Instance = this;
                new Logger();
            }


            //new BotSpawnerAddPlayerPatch().Enable();

            new BotGroupIsEnemyPatch().Enable();
            new BotEnemiesControllerPatch().Enable();
            new BotOwnerDamagePatch().Enable();
            
            new BotOwnerIsFolowerPatch().Enable();
            new PatrolDataFollowerPatch().Enable();

            new FollowRequestPatch().Enable();
            new HoldRequestPatch().Enable();

            new BotReceiverInitPatch().Enable();
            new BotReceiverDisposePatch().Enable();
            new BotReceiverPhrasePatch().Enable();

            new AIDataDisposePatch().Enable();
            new AIDataContructPatch().Enable();
            new AIDataBossPlayerPatch().Enable();

            new QuickPanelPatch().Enable();
            
            new CreateNodePatch().Enable();

            new BotsControllerPatch().Enable();

            new LocalGamePatch().Enable();
            new LocalGameCleanupPatch().Enable();

        }

    }
}
