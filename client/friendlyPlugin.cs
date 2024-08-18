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
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;

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
            return BossPlayers.IsFollower(Bot);
        }
    }

    [HarmonyPatch(typeof(ConfigurationManager.ConfigurationManager), "DisplayingWindow", MethodType.Setter)]
    public static class ConfigurationManagerPatch
    {
        public static void Prefix(ConfigurationManager.ConfigurationManager __instance, bool value)
        {
            friendlyPMC.Instance.GetEquipmentBuilds();
        }
    }

    [BepInPlugin("xyz.pit.companion", "friendlyPMC", "3.6.1")]
    [BepInDependency("xyz.drakia.bigbrain")]
    [BepInDependency("xyz.drakia.waypoints")]
    [BepInDependency("com.Arys.UnityToolkit")]
    public class friendlyPMC : BaseUnityPlugin
    {
        public static bool awaken;

        internal static friendlyPMC Instance { get; private set; }

        public static Dictionary<GameObject, HashSet<Material>> objectsMaterials = new Dictionary<GameObject, HashSet<Material>>();

        private static Dictionary<string, object> optionsLang = new Dictionary<string, object>
        {
            { "baseSettings", "Base Settings" },
            { "miscSettings", "Miscellaneous" },
            { "testSettings", "Testing"},
            {  
                "statusSound" , new Dictionary<string,string>{
                    { "Name", "Report Status Volume"},
                    { "Description", "Spawn with followers"}
                }
            },
            {
                "squadSpawn", new Dictionary<string,string>{
                    { "Name", "Squad Spawn"},
                    { "Description", "Set the volume of the report status sound"}
                }
            },
            {
                "squadSize", new Dictionary<string,string>{
                    { "Name", " Squad size"},
                    { "Description", "Number of followers to spawn with"}
                }
            }
        };

        public static ConfigEntry<bool> squadSpawn;
        public static ConfigEntry<int> squadSize;
        public static ConfigEntry<int> extraPickups;

        public static ConfigEntry<bool> squadSetup;

        public static Dictionary<int, List<ConfigEntry<string>>> squadMembers = new Dictionary<int, List<ConfigEntry<string>>>();

        public static ConfigEntry<bool> copyEquip;

        public static ConfigEntry<int> enemyRemember;

        public static ConfigEntry<float> heatlhMultiplier;

        public static ConfigEntry<int> scanDistance;

        public static ConfigEntry<int> returnChanceDeath;

        public static ConfigEntry<bool> knightSpawn;
        public static ConfigEntry<bool> bigPipeSpawn;
        public static ConfigEntry<bool> birdEyeSpawn;
        public static ConfigEntry<bool> justKnightSpawn;
        private string[] equipPresets = new string[] {
            "Default",
            "Player Equipment"
        };

        public static TarkovApplication application;

        private static ConfigurationManager.ConfigurationManager configurationManager;

        private static Dictionary<ConfigDefinition, string> savedConfigValues;

        private List<CancellationTokenSource> refreshTokens = new List<CancellationTokenSource>();
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

            new BotsControllerStopPatch().Enable();
            new LocalGameCleanupPatch().Enable();

            new AIDataContructPatch().Enable();
            new AIBossPlayerPatch().Enable();

            new QuickPanelPatch().Enable();
            new GestureMenuPatch().Enable();
            new GestureMenuAvailablePhrasesPatch().Enable();
            new EPhraseTriggerPatch().Enable();

            //new LookSensorPatch().Enable();

            var harmony = new Harmony("xyz.pit.companion");
            harmony.PatchAll(typeof(LocalGameVmethod4Patch).Assembly);

            harmony.PatchAll(typeof(GoalEnemyTracePatch).Assembly);

            harmony.PatchAll(typeof(LookSensorPatch).Assembly);

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
                    var followers = BossPlayers.GetFollowersByBoss(id);
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

            ConfigSet();

            harmony.PatchAll(typeof(ConfigurationManagerPatch).Assembly);

        }

        public void GetEquipmentBuilds()
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

                        BuildEquipmentPresets();
                    }
                }
            }
        }

        private void ConfigSet()
        {

            Config.SaveOnConfigSet = false;

            savedConfigValues = new Dictionary<ConfigDefinition, string>();

            var orphanedEntries = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries").GetValue(Config) as Dictionary<ConfigDefinition, string>;

            orphanedEntries.ExecuteForEach(it =>
            {
                savedConfigValues.Add(it.Key, it.Value);
            });


            squadSpawn = Config.Bind(
                (string)optionsLang["baseSettings"], "1 " + ((Dictionary<string, string>)optionsLang["squadSpawn"])["Name"], 
                true, 
                new ConfigDescription(((Dictionary<string, string>)optionsLang["squadSpawn"])["Description"])
            );
            squadSize = Config.Bind(
                (string)optionsLang["baseSettings"], 
                "1.2  -  " + ((Dictionary<string, string>)optionsLang["squadSize"])["Name"], 
                2, 
                new ConfigDescription(((Dictionary<string, string>)optionsLang["squadSize"])["Description"], new AcceptableValueRange<int>(1, 30))
            );

            returnChanceDeath = Config.Bind((string)optionsLang["baseSettings"], "1.3  -  Squadmate return chance after death", 50, new ConfigDescription("Chance your followers will return the items you gave them should you die. This applies only to members you spawned with.", new AcceptableValueRange<int>(1, 100)));

            squadSetup = Config.Bind((string)optionsLang["baseSettings"], "1.4  -  Use Squad setup", false, new ConfigDescription("Use specific setup for your squad"));

            extraPickups = Config.Bind((string)optionsLang["baseSettings"], "2 Maximum pickup followers", 1, new ConfigDescription("Maximum followers the player can pickup during raid. This is in addition to the squad.", new AcceptableValueRange<int>(0, 30)));

            scanDistance = Config.Bind((string)optionsLang["miscSettings"], "1 Maximum scan distance", 140, new ConfigDescription("Maximum distance to pick up any visible enemy that the player is signaling when issuing 'Contact' phrase", new AcceptableValueRange<int>(50, 300)));

            enemyRemember = Config.Bind((string)optionsLang["miscSettings"], "2 Time to forget about enemy (in sec.)", 20, new ConfigDescription("Maximum time a follower will remember an enemy. This is applied only at the begining of a raid", new AcceptableValueRange<int>(5, 60)));

            heatlhMultiplier = Config.Bind((string)optionsLang["miscSettings"], "3 Squad Health Multiplier", 1f, new ConfigDescription("Health multiplier for the followers you spawn with. This is applied per each body part. Does not apply to boss followers.", new AcceptableValueRange<float>(1, 5)));

            knightSpawn = Config.Bind((string)optionsLang["testSettings"], "1 Spawn with The Goons", false, new ConfigDescription("Experimental: Spawn with the goons squad. This works in combination with your own squad. Take note that a boss and his followers do not accept the same commands as your squad"));

            justKnightSpawn = Config.Bind((string)optionsLang["testSettings"], "1.1  -  Spawn with Knight", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));

            bigPipeSpawn = Config.Bind((string)optionsLang["testSettings"], "1.2  -  Spawn with BigPipe", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));

            birdEyeSpawn = Config.Bind((string)optionsLang["testSettings"], "1.3  -  Spawn with BirdEye", true, new ConfigDescription("Experimental: Only when Spawn with The Goons is active"));

            
            ConfigSquadMembersSet();

            Config.SettingChanged += (sender, args) =>
            {
                if (
                    args.ChangedSetting.Definition == squadSize.Definition ||
                    args.ChangedSetting.Definition == squadSetup.Definition ||
                    args.ChangedSetting.Definition.Key.Contains("Squad size") ||
                    args.ChangedSetting.Definition.Key.Contains("Squad setup")
                )
                {
                    RefreshManager().Forget();
                }
            };

            Config.SaveOnConfigSet = true;
            Config.Save();
        }

        private void ConfigSquadMembersSet()
        {
            
            if (squadSetup.Value)
            {
                for (int i = 0; i < squadSize.Value; i++)
                {

                    if (!squadMembers.ContainsKey(i))
                    {
                        string key = "1.4.1  -    -  Squad Member " + (i + 1) + " Tactic";
                        string value = "Default";

                        string seckey = "1.4.1  -    -  Squad Member " + (i + 1) + " Equipment";
                        string secvalue = "Default";

                        savedConfigValues.ExecuteForEach(saved =>
                        {
                            if(saved.Key.Key == key)
                            {
                                value = saved.Value;
                            } else if (saved.Key.Key == seckey)
                            {
                                secvalue = saved.Value;
                            }
                        });

                        List<ConfigEntry<string>> configEntries = new List<ConfigEntry<string>>
                        {
                            Config.Bind(
                                (string)optionsLang["miscSettings"],
                                key,
                                value,
                                new ConfigDescription("Set Squad member fight tactic. Default is a combination of Pusher and Holder. Pusher tries to push the enemy often. Holder will stay in place around the boss. Marksman will try to get a position from where he can shoot preferably from behind the player, at a distance and will not push even if ordered.",
                                    new AcceptableValueList<string>(new string[] {
                                        "Default",
                                        "Marksman",
                                        "Pusher",
                                        "Holder"
                                    })
                                )
                            ),
                            EquipmentOptions(seckey,secvalue)
                        };

                        squadMembers.Add(i, configEntries);
                    }
                }

                if (squadSize.Value < squadMembers.Count)
                {
                    int i = squadMembers.Count;

                    while (i > squadSize.Value)
                    {

                        if (squadMembers.TryGetValue(i-1, out var entries))
                        {

                            entries.ForEach(e =>
                            {
                                if (Config.TryGetEntry<string>(e.Definition, out var entry))
                                {
                                    Config.Remove(e.Definition);
                                }
                            });
                            squadMembers.Remove(i - 1);
                        }
                        i--;
                    }
                    Components.Logger.LogInfo("Reduce squad");
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

            savedConfigValues.Clear();
        }

        private ConfigEntry<string> EquipmentOptions(string name, string value)
        {

            string[] list = equipPresets;

            if(!equipPresets.Contains(value))
            {
                list = equipPresets.AddItem(value).ToArray();
            }

            ConfigEntry<string> entry = Config.Bind(
                (string)optionsLang["miscSettings"],
                name,
                value,
                new ConfigDescription("Set Squad member equipment. You can choose between default (which is SPT random equipment), user's current equipment or user created presets (recommended if using a tactic different than default.", new AcceptableValueList<string>(list))
             );

            return entry;
        }

        private void BuildEquipmentPresets()
        {

            var presets = Utils.Equipment.CustomPresets;

            var updatedPresets = new string[] {
                "Default",
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

            if (!wasUpdated)
            {
                foreach(var item in equipPresets)
                {
                    if (!updatedPresets.Contains(item))
                    {
                        wasUpdated = true;
                        break;
                    }
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
                            value = "Default";
                        }

                        Config.Remove(entry.Definition);

                        member.Value[1] = EquipmentOptions(name, value);
                    }
                }
            }
        }

        private async UniTask RefreshManager()
        {
            refreshTokens.ForEach(tk =>
            {
                tk.Cancel();
            });

            refreshTokens.Clear();

            var tokenSource = new CancellationTokenSource();
            refreshTokens.Add(tokenSource);
            try
            {
                await Task.Delay(100, tokenSource.Token).ContinueWith(t =>
                {
                    ConfigSquadMembersSet();
                });
                await Task.Delay(300, tokenSource.Token).ContinueWith(task =>
                {
                    configurationManager.BuildSettingList();
                });
            } catch { }
        }
    }
}
