using BepInEx;
using BepInEx.Configuration;
using ConfigurationManager;
using Comfort.Common;
using EFT;
using EFT.UI;

using friendlyPMC.Modules;
using friendlyPMC.Patches;
using HarmonyLib;

using System.Collections.Generic;
using UnityEngine;

using Logger = friendlyPMC.Components.Logger;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using System;
using EFT.Builds;
using static UnityEngine.EventSystems.EventTrigger;
using static GClass1750;
using System.Security.Policy;
using BepInEx.Bootstrap;

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

    [BepInPlugin("xyz.pit.companion", "friendlyPMC", "3.4.1")]
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
        const string equipSettings = "Squad Equipment";
        const string miscSettings = "Miscellaneous";
        const string testSettings = "Testing";

        public static ConfigEntry<bool> squadSpawn;
        public static ConfigEntry<int> squadSize;
        public static ConfigEntry<int> extraPickups;

        public static ConfigEntry<bool> squadSetup;

        public static Dictionary<int, List<ConfigEntry<string>>> squadMembers = new Dictionary<int, List<ConfigEntry<string>>>();

        public static ConfigEntry<bool> copyEquip;

        public static ConfigEntry<int> enemyRemember;

        public static ConfigEntry<float> heatlhMultiplier;

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

        private Dictionary<string, ConfigDefinition> equipmentEntries = new Dictionary<string, ConfigDefinition>();

        private string[] equipPresets = new string[] {
            "Random",
            "Player Equipment"
        };

        public static Dictionary<string, ConfigEntry<int>> equipmentValues = new Dictionary<string, ConfigEntry<int>>();
        public static ConfigEntry<bool> useEquipPresets;

        public static TarkovApplication application;

        private static Coroutine equipmentWatcher;

        private static ConfigurationManager.ConfigurationManager configurationManager;

        private void Awake()
        {

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
            new BotOwnerActivatePatch().Enable();

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
            new GestureMenuAvailablePhrasesPatch().Enable();
            new EPhraseTriggerPatch().Enable();

            var harmony = new Harmony("xyz.pit.companion");
            harmony.PatchAll(typeof(LocalGameCtorPatch).Assembly);

            SAINPatch.PatchSAINIfInstalled();

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

            configurationManager = Chainloader.PluginInfos
            .Values
            .FirstOrDefault(x => x.Instance.GetType().Name == "ConfigurationManager")
            ?.Instance as ConfigurationManager.ConfigurationManager;



            StartEquipmentBuildWatch();

            ConfigSet();
        }

        public static void StartEquipmentBuildWatch()
        {
            equipmentWatcher = Instance.StartCoroutine(Instance.GetEquipmentBuilds());
        }

        public static void StopEquipmentBuildWatch()
        {
            Instance.StopCoroutine(equipmentWatcher);
        }

        private IEnumerator GetEquipmentBuilds()
        {
            while (true)
            {
                if (application == null)
                {
                    try
                    {
                        application = SPT.Reflection.Utils.ClientAppUtils.GetMainApp();
                    }
                    catch
                    {

                    }
                }

                if (application != null)
                {
                    var buildStorage = application.GetClientBackEndSession()?.EquipmentBuildsStorage;

                    if (buildStorage != null)
                    {
                        var equipBuilds = buildStorage.EquipmentBuilds;
                        if (equipBuilds != null)
                        {
                            Utils.Equipment.CustomPresets.Clear();

                            equipBuilds.Values.Where((GClass3205 build) =>
                            {
                                return build.BuildType == EEquipmentBuildType.Custom;
                            }).ExecuteForEach((GClass3205 build) =>
                            {
                                Utils.Equipment.CustomPresets.Add(build);
                            });
                        }
                    }

                    BuildEquipmentPresets();
                }

                yield return new WaitForSeconds(5f);
            }
        }


        private void ConfigSet()
        {
            squadSpawn = Config.Bind(baseSettings, "1 Squad spawn", true, new ConfigDescription("Spawn with followers"));
            squadSize = Config.Bind(baseSettings, "1.2  -  Squad size", 2, new ConfigDescription("Number of followers to spawn with", new AcceptableValueRange<int>(1, 30)));

            returnChanceDeath = Config.Bind(baseSettings, "1.3  -  Squadmate return chance after death", 50, new ConfigDescription("Chance your followers will return the items you gave them should you die. This applies only to members you spawned with.", new AcceptableValueRange<int>(1, 100)));

            squadSetup = Config.Bind(baseSettings, "1.4  -  Use Squad setup", false, new ConfigDescription("Use specific setup for your squad"));

            Config.SettingChanged += (sender, args) =>
            {
                if (
                    args.ChangedSetting.Definition == squadSize.Definition ||
                    args.ChangedSetting.Definition == squadSetup.Definition
                )
                {
                    Components.Logger.LogInfo(args.ChangedSetting.Definition.Key);

                    ConfigSquadMembersSet();
                    Config.Save();
                    configurationManager.BuildSettingList();
                }
            };

            ConfigSquadMembersSet();

            extraPickups = Config.Bind(baseSettings, "2 Maximum followers", 1, new ConfigDescription("Maximum followers the player can have. This is in addition to the squad.", new AcceptableValueRange<int>(1, 30)));

            scanDistance = Config.Bind(miscSettings, "1 Maximum scan distance", 140, new ConfigDescription("Maximum distance to pick up any visible enemy that the player is signaling when issuing 'Contact' phrase", new AcceptableValueRange<int>(50, 300)));

            enemyRemember = Config.Bind(miscSettings, "2 Time to forget about enemy (in sec.)", 20, new ConfigDescription("Maximum time a follower will remember an enemy. This is applied only at the begining of a raid", new AcceptableValueRange<int>(5, 60)));

            heatlhMultiplier = Config.Bind(miscSettings, "3 Squad Health Multiplier", 1f, new ConfigDescription("Health multiplier for the followers you spawn with. This is applied per each body part. Does not apply to boss followers.", new AcceptableValueRange<float>(1, 5)));

            knightSpawn = Config.Bind(testSettings, "1 Spawn with The Goons", false, new ConfigDescription("Experimental: Spawn with the goons squad. This works in combination with your own squad. Take note that a boss and his followers do not accept the same commands as your squad"));

            justKnightSpawn = Config.Bind(testSettings, "1.1  -  Spawn with Knight", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));

            bigPipeSpawn = Config.Bind(testSettings, "1.2  -  Spawn with BigPipe", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));

            birdEyeSpawn = Config.Bind(testSettings, "1.3  -  Spawn with BirdEye", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));
        }

        private void ConfigSquadMembersSet()
        {
            if (squadSetup.Value)
            {

                for (int i = 0; i < squadSize.Value; i++)
                {

                    if (!squadMembers.ContainsKey(i))
                    {
                        List<ConfigEntry<string>> configEntries = new List<ConfigEntry<string>>
                        {
                            Config.Bind(
                                baseSettings,
                                "1.4.1  -    -  Squad Member #" + (i + 1) + " Tactic",
                                "Default",
                                new ConfigDescription("Set Squad member fight tactic. Default is a combination of Pusher and Holder. Pusher tries to push the enemy often. Holder will stay in place around the boss. Marksman will try to get a position from where he can shoot preferably from behind the player, at a distance and will not push even if ordered.",
                                    new AcceptableValueList<string>(new string[] {
                                        "Default",
                                        "Marksman",
                                        "Pusher",
                                        "Holder"
                                    })
                                )
                            ),
                            EquipmentOptions("1.4.1  -    -  Squad Member #" + (i + 1) + " Equipment","Random")
                        };

                        squadMembers.Add(i, configEntries);
                    }
                }

                if (squadSize.Value < squadMembers.Count)
                {
                    int i = squadMembers.Count - squadSize.Value;

                    while (i > squadSize.Value)
                    {
                        var entries = squadMembers[i];
                        entries.ForEach(e =>
                        {
                            if (Config.TryGetEntry<string>(e.Definition, out var entry))
                            {
                                Config.Remove(e.Definition);
                            }
                        });
                        squadMembers.Remove(i - 1);
                        i--;
                    }
                }
            } else
            {
                foreach (var item in squadMembers)
                {
                    var entries = item.Value;
                    entries.ForEach(e =>
                    {
                        if (Config.TryGetEntry<string>(e.Definition, out var entry))
                        {
                            Config.Remove(e.Definition);
                        }
                    });
                }

                squadMembers.Clear();
            }
        }

        private ConfigEntry<string> EquipmentOptions(string name, string value)
        {
            return Config.Bind(
                baseSettings,
                name,
                value,
                new ConfigDescription("Set Squad member equipment. You can choose between random, which is default SPT, user's current equipment or user created presets (recommended if using a tactic different than default.", new AcceptableValueList<string>(equipPresets))
             );
        }

        private void BuildEquipmentPresets()
        {
            var presets = Utils.Equipment.CustomPresets;

            var updatedPresets = new string[] {
                "Random",
                "Player Equipment"
            };

            foreach (var item in presets)
            {
                updatedPresets = updatedPresets.AddItem(item.Name).ToArray();
            }
            
            bool wasUpdated = false;

            foreach (var item in updatedPresets)
            {
                if(!equipPresets.Contains(item))
                {
                    wasUpdated = true;
                    break;
                }
            }

            if (wasUpdated)
            {

                equipPresets = updatedPresets;

                foreach (var member in squadMembers)
                {
                    var entry = member.Value[1];
     
                    if(Config.TryGetEntry<string>(entry.Definition, out var e))
                    {
                        string name = entry.Definition.Key;
                        string value = entry.Value;

                        if(!equipPresets.Contains(value))
                        {
                            value = "Random";
                        }

                        member.Value[1] = EquipmentOptions(name, value);

                        Config.Remove(entry.Definition);
                    }
                }
            }
        }
    }
}
