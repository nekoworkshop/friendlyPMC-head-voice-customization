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


using System.Threading.Tasks;
using System.Linq;
using System;
using EFT.Builds;
using BepInEx.Bootstrap;
using System.Threading;
using Cysharp.Threading.Tasks;

using friendlyPMC.Utils;
using friendlyPMC.Components;

using SPT.Common.Http;

using SPT.Common.Utils;

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
        MoveToPoint = 103,
        RunToCover = 104,
        GuardToCover = 105,
        FollowBoss = 106,
    }

    public enum CustomPhrases
    {
        TeamStatus = 200,
        OverThere = 201,
    }

    public enum CustomGestures
    {
        OverThere = 201,
    }



    [HarmonyPatch(typeof(ConfigurationManager.ConfigurationManager), "DisplayingWindow", MethodType.Setter)]
    public static class ConfigurationManagerPatch
    {
        public static void Prefix(ConfigurationManager.ConfigurationManager __instance, bool value)
        {
            try
            {
                friendlyPMC.Instance.GetEquipmentBuilds();
            }
            catch (Exception ex)
            {
                Modules.Logger.LogError(ex);
            }
        }
    }

    public class LanguageOptions
    {
        public string baseSettings { get; set; }
        public string miscSettings { get; set; }
        public string testSettings { get; set; }
        public string raidSettings { get; set; }
        public string[] equipOptions { get; set; }
        public string[] tacticOptions { get; set; }
        public string[] clothesOptions { get; set; }

        public Dictionary<string, string> statusSound { get; set; }
        public Dictionary<string, string> enemyMarker { get; set; }
        public Dictionary<string, string> squadSpawn { get; set; }
        public Dictionary<string, string> squadSize { get; set; }
        public Dictionary<string, string> extraPickups { get; set; }
        public Dictionary<string, string> returnChanceDeath { get; set; }
        public Dictionary<string, string> squadSetup { get; set; }
        public Dictionary<string, string> scanDistance { get; set; }
        public Dictionary<string, string> enemyRemember { get; set; }
        public Dictionary<string, string> healthMultiplier { get; set; }

        public Dictionary<string, string> memberTactic { get; set; }
        public Dictionary<string, string> memberEquipment { get; set; }
        public Dictionary<string, string> memberName { get; set; }
        public Dictionary<string, string> memberVoice { get; set; }
        public Dictionary<string, string> memberUniformTop { get; set; }
        public Dictionary<string, string> memberUniformBottom { get; set; }

        public Dictionary<string, string> equipmentLock { get; set; }
        public Dictionary<string, string> npcSendMessage { get; set; }

        public Dictionary<string, string> friendlyPMC { get; set; }
        public Dictionary<string, string> badGuy { get; set; }
        public Dictionary<string, string> pmcArmbands { get; set; }
        public Dictionary<string, string> englishBear { get; set; }

        public Dictionary<string, string> pingSquad { get; set; }
        public Dictionary<string, string> enemyContact { get; set; }

        public Dictionary<string, string> gestures { get; set; }

        public Dictionary<string, string> botStatus { get; set; }

        public Dictionary<string, string> patrolRadius { get; set; }

        public Dictionary<string, string> botTeleport { get; set; }
        public Dictionary<string, string> botHeal { get; set; }

        public Dictionary<string, string> botPrefetch { get; set; }

        public Dictionary<string, string> botGrenades { get; set; }

        // used only by BE
        public string[] returnItems { get; set; }
        public string[] returnItemsDeath { get; set; }
        public string[] teamEscaped { get; set; }
        public string[] teamSomeEscaped { get; set; }
        public string[] friendlyEscaped { get; set; }
    }

    [BepInPlugin("xyz.pit.friendlypmc", "friendlyPMC", "3.9.5")]
    [BepInDependency("xyz.drakia.bigbrain")]
    [BepInDependency("com.Arys.UnityToolkit")]
    public class friendlyPMC : BaseUnityPlugin
    {
        public static bool awaken;

        internal static friendlyPMC Instance { get; private set; }

        public static Dictionary<GameObject, HashSet<Material>> objectsMaterials = new Dictionary<GameObject, HashSet<Material>>();

        public static LanguageOptions optionsLang;

        public static ConfigEntry<bool> squadSpawn;
        public static ConfigEntry<int> squadSize;
        public static ConfigEntry<int> extraPickups;

        public static ConfigEntry<bool> squadSetup;

        public static Dictionary<int, List<ConfigEntry<string>>> squadMembers = new Dictionary<int, List<ConfigEntry<string>>>();

        public static ConfigEntry<int> enemyRemember;

        public static ConfigEntry<int> heatlhMultiplier;

        public static ConfigEntry<int> scanDistance;

        public static ConfigEntry<int> returnChanceDeath;

        public static ConfigEntry<int> statusSound;
        public static ConfigEntry<bool> enemyMarker;
        public static ConfigEntry<bool> npcSendMessage;

        public static ConfigEntry<bool> friendlyPMCFLAG;
        public static ConfigEntry<bool> badGuy;

        public static ConfigEntry<bool> pmcArmbands;
        public static ConfigEntry<bool> englishBear;

        public static ConfigEntry<bool> botPrefetch;

        public static ConfigEntry<bool> botGrenades;

        public static ConfigEntry<int> patrolRadius;


        public static ConfigEntry<KeyboardShortcut> pingKey;
        public static ConfigEntry<KeyboardShortcut> contactKey;

        public static ConfigEntry<KeyboardShortcut> teleportKey;
        public static ConfigEntry<KeyboardShortcut> healKey;

        private string[] equipPresets = new string[] {};

        private string[] UniformTop = new string[] {};
        private Dictionary<int,string> UniformTopPair = new Dictionary<int,string>();

        private string[] UniformBottom = new string[] {};
        private Dictionary<int, string> UniformBottomPair = new Dictionary<int, string>();

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
                new Modules.Logger();
            }


            var harmony = new Harmony("xyz.pit.friendlypmc");
            // configuration manager patch to help with keeping equipment builds up to date
            harmony.PatchAll(typeof(ConfigurationManagerPatch).Assembly);
            
            // bot patches to help with various scenarios while being a follower of the player
            new BotGroupAddEnemyPatch().Enable();
            //new BotMemoryAddEnemyPatch().Enable();

            new BotMemoryDamagePatch().Enable();
            new BotGroupUsecEnemyPatch().Enable();
            new ExUsecBrainHitPatch().Enable();

            new BotOwnerIsFolowerPatch().Enable();
            new BotOwnerManualUpdatePatch().Enable();
            new BotOwnerActivatePatch().Enable();

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

            // spawn patches
            harmony.PatchAll(typeof(LocalGameCtorPatch).Assembly);
            harmony.PatchAll(typeof(BaseLocalGameVmethod4Patch).Assembly);
            new BossSpawnWaveManagerClassPatch().Enable();

            // attempt to patch some sain methods
            SAINPatch.PatchSAINIfInstalled(harmony);
            // some error catchers here - they do not seem related to this mod but causing conflicts
            new GClass1069Patch().Enable();
            harmony.PatchAll(typeof(LookSensorPatch).Assembly);
            // patch hearing
            new HearingSensorPatch().Enable();
            new FootstepSoundPatch().Enable();
            new BulletImpactPatch().Enable();
            new PlayerSayPatch().Enable();
            new GamePlayerOwnerPatch().Enable();
            // patch bot equipment to prevent looting companions
            new UnlootableComponentPatch().Enable();
            new ModRaidModdablePatch().Enable();
            new ItemSpecificationPanelPatch().Enable();
            // raid patches to help with questing, having bots as being friends and part of the same group, and sending config changes to the server
            new RaidStartPatch().Enable();
            new MainMenuControllerPatch().Enable();
            new TarkovApplicationPatch().Enable();
            // quests related patches
            new PlayerKilledPatch().Enable();
            new ConditionCounterPatch().Enable();
            // social related patches to help with refreshing the list of friends when a quest is completed
            new SocialNetworkClassPatch().Enable();
            new QuestClassPatch().Enable();
            harmony.PatchAll(typeof(SendInvitePatch).Assembly);

            // set configuration manager
            SetConfiguration();
            // this is used for debug purposes that is why it stays disabled
            //harmony.PatchAll(typeof(GoalEnemyTracePatch).Assembly);
        }


        private void GetLanguage()
        {
            string json = RequestHandler.GetJson("/singleplayer/pitlang");
            

            var language = Json.Deserialize<LanguageOptions>(json);

            optionsLang = language;
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

                        equipBuilds.Values.Where((GClass3582 build) =>
                        {
                            return build.BuildType == EEquipmentBuildType.Custom;
                        }).ExecuteForEach((GClass3582 build) =>
                        {
                            Utils.Equipment.CustomPresets.Add(build);
                        });

                        BuildEquipmentPresets();
                    }
                }

                UniformTop = new string[] {
                    optionsLang.clothesOptions[0],
                    optionsLang.clothesOptions[1]
                };
                UniformBottom = new string[] {
                    optionsLang.clothesOptions[0],
                    optionsLang.clothesOptions[1]
                };

                UniformTopPair.Clear();
                UniformBottomPair.Clear();

                var playerEquip = Singleton<GClass1597>.Instance;
                if(playerEquip == null || playerEquip.AvailableSuites == null) return;

                foreach (var suit in playerEquip.AvailableSuites)
                {
                    if(suit == null || suit.Clothings == null || suit.Clothings.Length == 0) continue;

                    if (suit.MainBodyPart == EBodyModelPart.Body || suit.MainBodyPart == EBodyModelPart.Feet)
                    {
                        string id = suit.Clothings[0];
                        string nm = suit.NameLocalizationKey.Localized(null);
                        if(suit.MainBodyPart == EBodyModelPart.Body)
                        {
                            UniformTop = UniformTop.AddItem(nm).ToArray();
                            UniformTopPair[UniformTop.Length -1] = id;
                        }
                        else
                        {
                            UniformBottom = UniformBottom.AddItem(nm).ToArray();
                            UniformBottomPair[UniformBottom.Length - 1] = id;
                        }
                    }
                }

                BuildUniformOptions();
            }
        }

        private void ConfigSet()
        {
            equipPresets = new string[]
            {
                optionsLang.equipOptions[0]
            };

            UniformTop = new string[] {
                optionsLang.clothesOptions[0],
                optionsLang.clothesOptions[1]
            };
            UniformBottom = new string[] {
                optionsLang.clothesOptions[0],
                optionsLang.clothesOptions[1]
            };

            Config.SaveOnConfigSet = false;

            savedConfigValues = new Dictionary<ConfigDefinition, string>();

            var orphanedEntries = AccessTools.Property(typeof(ConfigFile), "OrphanedEntries").GetValue(Config) as Dictionary<ConfigDefinition, string>;

            orphanedEntries.ExecuteForEach(it =>
            {
                savedConfigValues.Add(it.Key, it.Value);
            });

            
            squadSpawn = Config.Bind(
                "I " + optionsLang.baseSettings, "1 " + optionsLang.squadSpawn["Name"], 
                true, 
                new ConfigDescription(optionsLang.squadSpawn["Description"], null, new ConfigurationManagerAttributes { Order = -100 })
            );
            
            squadSize = Config.Bind(
                "I " + optionsLang.baseSettings, 
                "1.2  -  " + optionsLang.squadSize["Name"], 
                2, 
                new ConfigDescription(optionsLang.squadSize["Description"], new AcceptableValueRange<int>(1, 30), new ConfigurationManagerAttributes { Order = -200 })
            );
            
            returnChanceDeath = Config.Bind("I " + optionsLang.baseSettings, "1.3  -  " + optionsLang.returnChanceDeath["Name"], 50, new ConfigDescription(optionsLang.returnChanceDeath["Description"], new AcceptableValueRange<int>(1, 100), new ConfigurationManagerAttributes { Order = -300 }));

            squadSetup = Config.Bind("I " + optionsLang.baseSettings, "1.4  -  " + optionsLang.squadSetup["Name"], false, new ConfigDescription(optionsLang.squadSpawn["Description"],null, new ConfigurationManagerAttributes { Order = -400 }));
            
            extraPickups = Config.Bind("I " + optionsLang.baseSettings, "2 " + optionsLang.extraPickups["Name"], 1, new ConfigDescription(optionsLang.extraPickups["Description"], new AcceptableValueRange<int>(0, 30), new ConfigurationManagerAttributes { Order = -500 }));
            
            scanDistance = Config.Bind("II " + optionsLang.miscSettings, "1 " + optionsLang.scanDistance["Name"], 140, new ConfigDescription(optionsLang.scanDistance["Description"], new AcceptableValueRange<int>(50, 300), new ConfigurationManagerAttributes { Order = -100 }));

            patrolRadius = Config.Bind("II " + optionsLang.miscSettings, "2 " + optionsLang.patrolRadius["Name"], 50, new ConfigDescription(optionsLang.patrolRadius["Description"], new AcceptableValueRange<int>(30, 100), new ConfigurationManagerAttributes { Order = -200 }));

            enemyRemember = Config.Bind("II " + optionsLang.miscSettings, "3 " + optionsLang.enemyRemember["Name"], 20, new ConfigDescription(optionsLang.enemyRemember["Description"], new AcceptableValueRange<int>(5, 60), new ConfigurationManagerAttributes { Order = -300 }));
            
            heatlhMultiplier = Config.Bind("II " + optionsLang.miscSettings, "4 " + optionsLang.healthMultiplier["Name"], 1, new ConfigDescription(optionsLang.healthMultiplier["Description"], new AcceptableValueRange<int>(1, 5), new ConfigurationManagerAttributes { Order = -400 }));

            statusSound = Config.Bind("II " + optionsLang.miscSettings, "5 " + optionsLang.statusSound["Name"], 100, new ConfigDescription(optionsLang.statusSound["Description"], new AcceptableValueRange<int>(0, 100), new ConfigurationManagerAttributes { Order = -500 }));
            
            enemyMarker = Config.Bind("II " + optionsLang.miscSettings, "6 " + optionsLang.enemyMarker["Name"], true, new ConfigDescription(optionsLang.enemyMarker["Description"],null, new ConfigurationManagerAttributes { Order = -600 }));

            npcSendMessage = Config.Bind("II " + optionsLang.miscSettings, "7 " + optionsLang.npcSendMessage["Name"], true, new ConfigDescription(optionsLang.npcSendMessage["Description"],null, new ConfigurationManagerAttributes { Order = -700 }));

            friendlyPMCFLAG = Config.Bind("II " + optionsLang.miscSettings, "8 " + optionsLang.friendlyPMC["Name"], true, new ConfigDescription(optionsLang.friendlyPMC["Description"],null, new ConfigurationManagerAttributes { Order = -800 }));

            badGuy = Config.Bind("II " + optionsLang.miscSettings, "9 " + optionsLang.badGuy["Name"], false, new ConfigDescription(optionsLang.badGuy["Description"], null, new ConfigurationManagerAttributes { Order = -900 }));

            pmcArmbands = Config.Bind("II " + optionsLang.miscSettings, "10 " + optionsLang.pmcArmbands["Name"], true, new ConfigDescription(optionsLang.pmcArmbands["Description"],null, new ConfigurationManagerAttributes { Order = -1000 }));

            englishBear = Config.Bind("II " + optionsLang.miscSettings, "11 " + optionsLang.englishBear["Name"], true, new ConfigDescription(optionsLang.englishBear["Description"],null, new ConfigurationManagerAttributes { Order = -1100 }));

            botGrenades = Config.Bind("II " + optionsLang.miscSettings, "12 " + optionsLang.botGrenades["Name"], true, new ConfigDescription(optionsLang.botGrenades["Description"], null, new ConfigurationManagerAttributes { Order = -1105 }));

            pingKey = Config.Bind("II " + optionsLang.miscSettings, "13 " + optionsLang.pingSquad["Name"], new KeyboardShortcut(KeyCode.None), new ConfigDescription(optionsLang.pingSquad["Description"],null, new ConfigurationManagerAttributes { Order = -1101 }));

            contactKey = Config.Bind("II " + optionsLang.miscSettings, "14 " + optionsLang.enemyContact["Name"], new KeyboardShortcut(KeyCode.None), new ConfigDescription(optionsLang.enemyContact["Description"],null, new ConfigurationManagerAttributes { Order = -1102 }));

            teleportKey = Config.Bind("II " + optionsLang.miscSettings, "15 " + optionsLang.botTeleport["Name"], new KeyboardShortcut(KeyCode.None), new ConfigDescription(optionsLang.botTeleport["Description"], null, new ConfigurationManagerAttributes { Order = -1103 }));
            healKey = Config.Bind("II " + optionsLang.miscSettings, "16 " + optionsLang.botHeal["Name"], new KeyboardShortcut(KeyCode.None), new ConfigDescription(optionsLang.botHeal["Description"], null, new ConfigurationManagerAttributes { Order = -1104 }));

            botPrefetch = Config.Bind("II " + optionsLang.miscSettings, "17 " + optionsLang.botPrefetch["Name"], true, new ConfigDescription(optionsLang.botPrefetch["Description"], null, new ConfigurationManagerAttributes { Order = -1105 }));
             
            ConfigSquadMembersSet();
            

            Config.SettingChanged += (sender, args) =>
            {
                if (
                    args.ChangedSetting.Definition == squadSize.Definition ||
                    args.ChangedSetting.Definition == squadSetup.Definition ||
                    args.ChangedSetting.Definition.Key.Contains(optionsLang.squadSize["Name"]) ||
                    args.ChangedSetting.Definition.Key.Contains(optionsLang.squadSetup["Name"])
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
                        string key = $"1.4.1.{i + 1}.4  -  -  " + String.Format(optionsLang.memberTactic["Name"],i+1);
                        string value = optionsLang.tacticOptions[0];

                        string seckey = $"1.4.1.{i + 1}.2  -  -  " + String.Format(optionsLang.memberEquipment["Name"], i + 1);
                        string secvalue = optionsLang.equipOptions[0];

                        string trdkey = $"1.4.1.{i + 1}.1  -  -  " + String.Format(optionsLang.memberName["Name"], i + 1);
                        string trdvalue = "";

                        string frtkey = $"1.4.1.{i + 1}.3.1  -  -  " + String.Format(optionsLang.memberUniformTop["Name"], i + 1);
                        string frtvalue = "";

                        string fiftkey = $"1.4.1.{i + 1}.3.2  -  -  " + String.Format(optionsLang.memberUniformBottom["Name"], i + 1);
                        string fiftvalue = "";

                        savedConfigValues.ExecuteForEach(saved =>
                        {
                            if(saved.Key.Key == key)
                            {
                                value = saved.Value;
                            } 
                            else if (saved.Key.Key == seckey)
                            {
                                secvalue = saved.Value;
                            }
                            else if (saved.Key.Key == trdkey)
                            {
                                trdvalue = saved.Value;
                            }
                            else if (saved.Key.Key == frtkey)
                            {
                                frtvalue = saved.Value;
                            }
                            else if (saved.Key.Key == fiftkey)
                            {
                                fiftvalue = saved.Value;
                            }
                        });

                        List<string> tactics = new List<string>();
                        for (int j = 0; j < optionsLang.tacticOptions.Length - 1; j++)
                        {
                            tactics.Add(optionsLang.tacticOptions[j]);
                        }


                        List<ConfigEntry<string>> configEntries = new List<ConfigEntry<string>>
                        {
                            Config.Bind(
                                "I " +optionsLang.baseSettings,
                                key,
                                value,
                                new ConfigDescription(optionsLang.memberTactic["Description"],
                                    new AcceptableValueList<string>(tactics.ToArray()),
                                    new ConfigurationManagerAttributes { Order = -400 + ((i + 1) * -1) }
                                )
                            ),
                            EquipmentOptions(seckey,secvalue,-400 + ((i + 1) * -1)),
                            UniformOptions(frtkey,frtvalue,"top",true,-400 + ((i + 1) * -1)),
                            UniformOptions(fiftkey,fiftvalue,"bottom",true,-400 + ((i + 1) * -1)),
                            Config.Bind(
                                "I " +optionsLang.baseSettings,
                                trdkey,
                                trdvalue,
                                new ConfigDescription(optionsLang.memberName["Description"],null,
                                new ConfigurationManagerAttributes { Order = -400 + ((i + 1) * -1) }
                                )
                            )
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

        private ConfigEntry<string> EquipmentOptions(string name, string value, int order = 0)
        {

            string[] list = equipPresets;

            if(value != "" && !equipPresets.Contains(value))
            {
                list = equipPresets.AddItem(value).ToArray();
            }

            ConfigEntry<string> entry = Config.Bind(
                "I " + optionsLang.baseSettings,
                name,
                value,
                new ConfigDescription(optionsLang.memberEquipment["Description"], 
                new AcceptableValueList<string>(list),
                new ConfigurationManagerAttributes { Order = order }
                )
             );

            return entry;
        }

        private void BuildEquipmentPresets()
        {
            var presets = Utils.Equipment.CustomPresets;

            var updatedPresets = new string[] {
                optionsLang.equipOptions[0]
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
                int i = 0;
                foreach (var member in squadMembers)
                {
                    var entry = member.Value[1];
     
                    if(Config.TryGetEntry<string>(entry.Definition, out var e))
                    {
                        string name = entry.Definition.Key;
                        string value = entry.Value;

                        if(!equipPresets.Contains(value))
                        {
                            value = optionsLang.equipOptions[0];
                        }

                        Config.Remove(entry.Definition);

                        member.Value[1] = EquipmentOptions(name, value, -400 + ((i + 1) * -1));
                    }
                    i++;
                }

            }

        }

        private ConfigEntry<string> UniformOptions(string name, string value, string bodyPart = "top", bool addval = false, int order = 0)
        {
            string[] list;
            if (bodyPart == "top") list = UniformTop.ToArray();
            else list = UniformBottom.ToArray();
            
            if (value == "" || !list.Contains(value))
            {
                if(addval && value != "") list = list.AddItem(value).ToArray();
                else value = list[0];
            }

            string description;
            if(bodyPart == "top")
                description = optionsLang.memberUniformTop["Description"];
            else 
                description = optionsLang.memberUniformBottom["Description"];

            ConfigEntry<string> entry = Config.Bind(
                "I " + optionsLang.baseSettings,
                name,
                value,
                new ConfigDescription(description, new AcceptableValueList<string>(list), new ConfigurationManagerAttributes { Order = order })
             );

            return entry;

        }

        private void BuildUniformOptions()
        {
            int i = 0;
            foreach (var member in squadMembers)
            {
                var entryTop = member.Value[2];
                var entryBottom = member.Value[3];

                if (Config.TryGetEntry<string>(entryTop.Definition, out var e))
                {
                    string name = entryTop.Definition.Key;
                    string value = entryTop.Value;

                    Config.Remove(entryTop.Definition);

                    member.Value[2] = UniformOptions(name, value,"top",false, -400 + ((i + 1) * -1));
                }

                if (Config.TryGetEntry<string>(entryBottom.Definition, out var ex))
                {
                    string name = entryBottom.Definition.Key;
                    string value = entryBottom.Value;

                    Config.Remove(entryBottom.Definition);

                    member.Value[3] = UniformOptions(name, value,"bottom",false, -400 + ((i + 1) * -1));
                }

                i++;
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
            } catch(Exception ex) {
                Modules.Logger.LogError(ex);
            }
        }

        public static string[] GetEquipOptions()
        {
            return Instance.equipPresets;
        }

        public static List<string[]> GetUniformOptions()
        {
            return new List<string[]>
            {
                Instance.UniformTop,
                Instance.UniformBottom
            };

        }

        public static List<Dictionary<int,string>> GetUniformPairs()
        {
            return new List<Dictionary<int, string>>
            {
                Instance.UniformTopPair,
                Instance.UniformBottomPair
            };
        }

        public static string[] GetTacticOptions()
        {
            return optionsLang.tacticOptions;
        }

        private void _BotTeleport()
        {
            Modules.Logger.LogInfo("Trigger followers telepor");

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
        }

        private void _BotHeal()
        {

            Modules.Logger.LogInfo("Trigger followers fix heal");

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

                foreach (var follower in followers)
                {
                    var bot = follower.GetBot();
                    if (follower != null && bot.HealthController.IsAlive)
                    {

                        bot.AIData.Player.ActiveHealthController.RestoreFullHealth();

                        (bot.Brain.BaseBrain as FollowerBrain).HandsReset();
                        bot.WeaponManager.Selector.TakePrevWeapon();
                    }
                }
            }

        }

        private void SetConfiguration()
        {
            configurationManager = Chainloader.PluginInfos
            .Values
            .FirstOrDefault(x => x.Instance.GetType().Name == "ConfigurationManager")
            ?.Instance as ConfigurationManager.ConfigurationManager;
            // - get config language
            GetLanguage();
            // - set config
            ConfigSet();
        }

        void Update()
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld == null) return;

            if (GamePlayerOwner.MyPlayer == null || GamePlayerOwner.MyPlayer.HealthController == null || !GamePlayerOwner.MyPlayer.HealthController.IsAlive)
            {
                return;
            }

            if (pingKey.Value.IsPressed() || contactKey.Value.IsPressed())
            {

                string id = GamePlayerOwner.MyPlayer.ProfileId;

                if (BossPlayers.Instance != null && PingTeamates.Instance != null)
                {
                    var boss = BossPlayers.Instance.GetBossPlayer(id);
                    if (boss != null)
                    {
                        if(pingKey.Value.IsPressed())
                            boss.realPlayer.Say((EPhraseTrigger)CustomPhrases.TeamStatus,true);
                        else
                            boss.realPlayer.Say(EPhraseTrigger.OnRepeatedContact,true);
                    }
                }
            }

            else if (teleportKey.Value.IsPressed())
            {
                _BotTeleport();
            }

            else if (healKey.Value.IsPressed())
            {
                _BotHeal();
            }

        }
    }
}
