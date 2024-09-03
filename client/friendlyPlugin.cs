using BepInEx;
using BepInEx.Configuration;
using Comfort.Common;
using EFT;
using EFT.UI;

using friendlyPMC.Modules;
using friendlyPMC.Patches;
using HarmonyLib;

using System.Collections.Generic;
using UnityEngine;

using Logger = friendlyPMC.Components.Logger;

using System.Threading.Tasks;
using System.Linq;
using System;
using EFT.Builds;
using BepInEx.Bootstrap;
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
        EnemySearch = 102,
        MoveToPoint = 103
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

    [BepInPlugin("xyz.pit.companion", "friendlyPMC", "3.7.0")]
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
            { "equipOptions", new string[]
                {
                    "Default",
                    "Player Equipment"
                } 
            },
            {
                "tacticOptions", new string[]
                {
                    "Default",
                    "Marksman",
                    "Pusher",
                    "Holder"
                }
            },
            {  
                "statusSound" , new Dictionary<string,string>{
                    { "Name", "Report Status Volume"},
                    { "Description", "Volume of the radio sound when triggering report status"}
                }
            },
            {
                "enemyMarker" , new Dictionary<string,string>{
                    { "Name", "Enemy Marker"},
                    { "Description", "Show enemy position when reporting status. If disabled, the enemy marker sound will also be disabled."}
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
                    { "Name", "Squad size"},
                    { "Description", "Number of followers to spawn with"}
                }
            },
            {
                "extraPickups",  new Dictionary<string,string>{
                    { "Name", "Maximum pickup followers"},
                    { "Description", "Maximum followers the player can pickup during raid. This is in addition to the squad"}
                }
            },
            {
                "returnChanceDeath", new Dictionary<string,string>{
                    { "Name", "Squadmate return chance after death"},
                    { "Description", "Chance your followers will return the items you gave them should you die. This applies only to members you spawned with"}
                }
            },
            {
                "squadSetup", new Dictionary<string,string>{
                    { "Name", "Use Squad setup"},
                    { "Description", "Use specific setup for your squad"}
                }
            },
            {
                "squadUniform", new Dictionary<string,string>{
                    { "Name", "Squad player uniform"},
                    { "Description", "Use player clothes for all squad members"}
                }
            },
            {
                "scanDistance", new Dictionary<string,string>{
                    { "Name", "Maximum scan distance"},
                    { "Description", "Maximum distance to pick up any visible enemy that the player is signaling when issuing 'Contact' phrase"}
                }
            },
            {
                "enemyRemember", new Dictionary<string,string>{
                    { "Name", "Time to forget about enemy (in sec.)"},
                    { "Description", "Maximum time a follower will remember an enemy. This is applied only at the begining of a raid"}
                }
            },
            {
                "heatlhMultiplier", new Dictionary<string,string>{
                    { "Name", "Squad Health Multiplier"},
                    { "Description", "Health multiplier for the followers you spawn with. This is applied per each body part. Does not apply to boss followers"}
                }
            },
            {
                "memberTactic", new Dictionary<string,string>{
                    { "Name", "Squad Member {0} Tactic"},
                    { "Description", "Set Squad member fight tactic. Default is a combination of Pusher and Holder. Pusher tries to push the enemy often. Holder will stay in place around the boss. Marksman will try to get a position from where he can shoot preferably from behind the player, at a distance and will not push even if ordered"}
                }
            },
            {
                "memberEquipment", new Dictionary<string,string>{
                    { "Name", "Squad Member {0} Equipment"},
                    { "Description", "Set Squad member equipment. You can choose between default (which is SPT random equipment), user's current equipment or user created presets (recommended if using a tactic different than default"}
                }
            },
            {
                "equipmentLock", new Dictionary<string,string>{
                    { "Name", "Lock Squad Equipment"},
                    { "Description", "Locks the equipment of the squad members. This is useful if you want to use your own equipment presets and do not wish to loose the equipment if you or them die."}
                }
            }
        };

        public static ConfigEntry<bool> squadSpawn;
        public static ConfigEntry<int> squadSize;
        public static ConfigEntry<int> extraPickups;

        public static ConfigEntry<bool> squadSetup;
        public static ConfigEntry<bool> squadUniform;

        public static Dictionary<int, List<ConfigEntry<string>>> squadMembers = new Dictionary<int, List<ConfigEntry<string>>>();

        public static ConfigEntry<int> enemyRemember;

        public static ConfigEntry<float> heatlhMultiplier;

        public static ConfigEntry<int> scanDistance;

        public static ConfigEntry<int> returnChanceDeath;

        public static ConfigEntry<int> statusSound;
        public static ConfigEntry<bool> enemyMarker;

        public static ConfigEntry<bool> knightSpawn;
        public static ConfigEntry<bool> bigPipeSpawn;
        public static ConfigEntry<bool> birdEyeSpawn;
        public static ConfigEntry<bool> justKnightSpawn;



        private string[] equipPresets = new string[] {};

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
            new PhraseSpeakerClassPatch().Enable();
            new EPhraseTriggerPatch().Enable();

            var harmony = new Harmony("xyz.pit.companion");

            harmony.PatchAll(typeof(LocalGameCtorPatch).Assembly);
            harmony.PatchAll(typeof(LocalGameVmethod4Patch).Assembly); // backup spawn patch
            new BossSpawnWaveManagerClassPatch().Enable(); // normal spawn patch

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

            ConsoleScreen.Processor.RegisterCommand("followersfixheal", delegate ()
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
                            follower.GetBot().WeaponManager.Selector.TakePrevWeapon();
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
            
            // patch sain in regards to Squad 
            SAINPatch.PatchSAINIfInstalled(harmony);
            // some error catchers here - they do not seem related to this mod
            new GClass974Patch().Enable();

            harmony.PatchAll(typeof(LookSensorPatch).Assembly);

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
            equipPresets = new string[]
            {
                ((string[])optionsLang["equipOptions"])[0],
                ((string[])optionsLang["equipOptions"])[1]
            };

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

            returnChanceDeath = Config.Bind((string)optionsLang["baseSettings"], "1.3  -  " + ((Dictionary<string, string>)optionsLang["returnChanceDeath"])["Name"], 50, new ConfigDescription(((Dictionary<string, string>)optionsLang["returnChanceDeath"])["Description"], new AcceptableValueRange<int>(1, 100)));

            squadSetup = Config.Bind((string)optionsLang["baseSettings"], "1.4  -  " + ((Dictionary<string, string>)optionsLang["squadSetup"])["Name"], false, new ConfigDescription(((Dictionary<string, string>)optionsLang["squadSetup"])["Description"]));

            squadUniform = Config.Bind((string)optionsLang["baseSettings"], "1.5  -  " + ((Dictionary<string, string>)optionsLang["squadUniform"])["Name"], false, new ConfigDescription(((Dictionary<string, string>)optionsLang["squadUniform"])["Description"]));

            extraPickups = Config.Bind((string)optionsLang["baseSettings"], "2 " + ((Dictionary<string, string>)optionsLang["extraPickups"])["Name"], 1, new ConfigDescription(((Dictionary<string, string>)optionsLang["extraPickups"])["Description"], new AcceptableValueRange<int>(0, 30)));

            scanDistance = Config.Bind((string)optionsLang["miscSettings"], "1 " + ((Dictionary<string, string>)optionsLang["scanDistance"])["Name"], 140, new ConfigDescription(((Dictionary<string, string>)optionsLang["scanDistance"])["Description"], new AcceptableValueRange<int>(50, 300)));

            enemyRemember = Config.Bind((string)optionsLang["miscSettings"], "2 " + ((Dictionary<string, string>)optionsLang["enemyRemember"])["Name"], 20, new ConfigDescription(((Dictionary<string, string>)optionsLang["enemyRemember"])["Description"], new AcceptableValueRange<int>(5, 60)));

            heatlhMultiplier = Config.Bind((string)optionsLang["miscSettings"], "3 " + ((Dictionary<string, string>)optionsLang["heatlhMultiplier"])["Name"], 1f, new ConfigDescription(((Dictionary<string, string>)optionsLang["heatlhMultiplier"])["Description"], new AcceptableValueRange<float>(1, 5)));

            statusSound = Config.Bind((string)optionsLang["miscSettings"], "4 " + ((Dictionary<string, string>)optionsLang["statusSound"])["Name"], 100, new ConfigDescription(((Dictionary<string, string>)optionsLang["statusSound"])["Description"], new AcceptableValueRange<int>(0, 100)));
            enemyMarker = Config.Bind((string)optionsLang["miscSettings"], "5 " + ((Dictionary<string, string>)optionsLang["enemyMarker"])["Name"], true, new ConfigDescription(((Dictionary<string, string>)optionsLang["enemyMarker"])["Description"]));

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
                    args.ChangedSetting.Definition.Key.Contains(((Dictionary<string, string>)optionsLang["squadSize"])["Name"]) ||
                    args.ChangedSetting.Definition.Key.Contains(((Dictionary<string, string>)optionsLang["squadSetup"])["Name"])
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
                        string key = "1.4.1  -    -  " + String.Format(((Dictionary<string, string>)optionsLang["memberTactic"])["Name"],i+1);
                        string value = ((string[])optionsLang["tacticOptions"])[0];

                        string seckey = "1.4.1  -    -  " + String.Format(((Dictionary<string, string>)optionsLang["memberEquipment"])["Name"], i + 1);
                        string secvalue = ((string[])optionsLang["tacticOptions"])[0];

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
                                (string)optionsLang["baseSettings"],
                                key,
                                value,
                                new ConfigDescription(((Dictionary<string, string>)optionsLang["memberTactic"])["Description"],
                                    new AcceptableValueList<string>((string[])optionsLang["tacticOptions"])
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
                (string)optionsLang["baseSettings"],
                name,
                value,
                new ConfigDescription(((Dictionary<string, string>)optionsLang["memberEquipment"])["Description"], new AcceptableValueList<string>(list))
             );

            return entry;
        }

        private void BuildEquipmentPresets()
        {

            var presets = Utils.Equipment.CustomPresets;

            var updatedPresets = new string[] {
                ((string[])optionsLang["equipOptions"])[0],
                ((string[])optionsLang["equipOptions"])[1]
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
                            value = ((string[])optionsLang["equipOptions"])[0];
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

        public static string[] GetEquipOptions()
        {
            return Instance.equipPresets;
        }

        public static string[] GetTacticOptions()
        {
            return (string[])optionsLang["tacticOptions"];
        }
    }
}
