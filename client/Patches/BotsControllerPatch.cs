using SPT.Reflection.Patching;
using Cysharp.Threading.Tasks;
using EFT;

using friendlyPMC.Actions;
using friendlyPMC.Components;
using friendlyPMC.Modules;

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using UnityEngine;
using friendlyPMC.Utils;
using Comfort.Common;
using EFT.InventoryLogic;
using EFT.Bots;
using System.Collections;
using System.Linq;

using IProfileData = GClass592;
using ProfileEndPoint = ProfileEndpointFactoryAbstractClass;
using BotCreator = GClass814;
using UnityEngine.UI;
using System.Security.Policy;
using System.Threading.Tasks;
using SPT.Common.Http;
using Newtonsoft.Json;

namespace friendlyPMC.Patches
{
    internal class CancelToken : GInterface19
    {
        CancellationTokenSource cancelSource;
        public CancelToken()
        {
            cancelSource = new CancellationTokenSource();
        }

        public CancellationToken GetCancelToken()
        {
            return cancelSource.Token;
        }

        public void Cancel()
        {
            cancelSource.Cancel();
        }
    }

    internal class BotsControllerPatch : ModulePatch
    {
        public static BotsControllerPatch Instance;

        public static BotsController Controller = null;

        public static List<pitAIBossPlayer> spawnedPlayers = new List<pitAIBossPlayer>();

        public static Dictionary<string, UniTask<Dictionary<int, Profile>>> followerCreationTask;
        public static Dictionary<string, Task<BotCreationDataClass>> alliesCreationTask;
        public static Dictionary<string, Task<BotCreationDataClass>> pmcCreationTask;
        public static Dictionary<WildSpawnType, Task<BotCreationDataClass>> bossCreationTask;

        public BotsControllerPatch()
        {
            if (Instance == null) Instance = this;
            followerCreationTask = new Dictionary<string, UniTask<Dictionary<int, Profile>>>();
            alliesCreationTask = new Dictionary<string, Task<BotCreationDataClass>>();
            bossCreationTask = new Dictionary<WildSpawnType, Task<BotCreationDataClass>>();
            pmcCreationTask = new Dictionary<string, Task<BotCreationDataClass>>();
        }

        private AICorePoint GetClosestCorePoint(BotsController _botsController,Vector3 position)
        {
            var coversData = _botsController.CoversData;
            var groupPoint = coversData.GetClosest(position);
            return groupPoint.CorePointInGame;
        }

        private BotsGroup GetPlayerGroup(pitAIBossPlayer player, BotOwner bt, BotZone zn, int groupSize = 0, bool badGuyGroup = false)
        {
            if(player.bossGroup != null) return player.bossGroup;

            BotSpawner botSpawnerClass = Controller.BotSpawner;

            var botGame = AccessTools.Field(typeof(BotSpawner), "_game").GetValue(botSpawnerClass) as IBotGame;

            var spawnGroups = AccessTools.Field(typeof(BotSpawner), "_groups").GetValue(botSpawnerClass) as BotZoneGroupsDictionary;
            var deadBodiesController = AccessTools.Field(typeof(BotSpawner), "_deadBodiesController").GetValue(botSpawnerClass) as DeadBodiesController;
            var allPlayers = AccessTools.Field(typeof(BotSpawner), "_allPlayers").GetValue(botSpawnerClass) as List<Player>;

            bool _freeForAll = true;

            WildSpawnType sptBear = WildSpawnType.pmcBEAR;
            WildSpawnType sptUsec = WildSpawnType.pmcUSEC;

            WildSpawnType roleh;
            bool sameSideHostile = badGuyGroup;

            if (player.realPlayer.Side == EPlayerSide.Bear)
            {
                roleh = sptBear;
            }
            else if (player.realPlayer.Side == EPlayerSide.Usec)
            {
                roleh = sptUsec;
            }
            else
            {
                roleh = WildSpawnType.assault;
            }

            if(!badGuyGroup) GetSameSideHostile(roleh, player.realPlayer.Side, out sameSideHostile);

            EPlayerSide side = player.realPlayer.Side;

            BotsGroup botsGroup;
            List<BotOwner> list = new List<BotOwner>();

            if (side != EPlayerSide.Savage)
            {
                // botsGroup take on the values of the inital bot, attempt to prevent the group from being hostile to the player
                bt.Settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = side != EPlayerSide.Savage ? false : true;
                bt.Settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = side == EPlayerSide.Savage ? false : true;

                var oldBehaviourBear = bt.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR;
                var oldBehaviorUsec = bt.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR;
                var oldBehaviorSavage = bt.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR;

                var old_reasons = bt.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY;

                bt.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = true;
                bt.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = new EBotEnemyCause[] { };

                if (side == EPlayerSide.Bear)
                {
                    bt.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = sameSideHostile ? EWarnBehaviour.Attack : EWarnBehaviour.Ignore;
                    bt.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = EWarnBehaviour.Attack;
                }
                else
                {
                    bt.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = sameSideHostile ? EWarnBehaviour.Attack : EWarnBehaviour.Ignore;
                    bt.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = EWarnBehaviour.Attack;
                }

                foreach (BotOwner item2 in botSpawnerClass.method_4(bt))
                {
                    list.Add(item2);
                }
                
                botsGroup = new BotsGroupPlayer(zn, botGame, bt, list, deadBodiesController, allPlayers, player);
                if(groupSize != 0) botsGroup.TargetMembersCount = groupSize;

                if (_freeForAll)
                {
                    spawnGroups.AddNoKey(botsGroup, zn);
                }
                else
                {
                    spawnGroups.Add(zn, side, botsGroup, false);
                }

                BossPlayers.AddGroupToBoss(player, botsGroup);

                // revert changes
                bt.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = false;
                bt.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = old_reasons;
                bt.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = oldBehaviourBear;
                bt.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = oldBehaviorUsec;
                bt.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = oldBehaviorSavage;

            } 
            else
            {
                foreach (BotOwner item2 in botSpawnerClass.method_4(bt))
                {
                    list.Add(item2);
                }

                botsGroup = new BotsGroupPlayer(zn, botGame, bt, list, deadBodiesController, allPlayers, player);
                if (groupSize != 0) botsGroup.TargetMembersCount = groupSize;

                if (_freeForAll)
                {
                    spawnGroups.AddNoKey(botsGroup, zn);
                }
                else
                {
                    spawnGroups.Add(zn, side, botsGroup, false);
                }

                BossPlayers.AddGroupToBoss(player, botsGroup);
            }

            return botsGroup;
        }

        private void GetSameSideHostile(WildSpawnType role, EPlayerSide side, out bool isHostile)
        {
            isHostile = false;

            if(friendlyPMC.badGuy.Value)
            {
                isHostile = true;
                return;
            }

            BotSettingsComponents botSettingsComponents = GClass531.smethod_1(BotDifficulty.normal, role, false);
            if(botSettingsComponents != null)
            {
                if (side == EPlayerSide.Bear) isHostile = botSettingsComponents.Mind.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack);
                else if (side == EPlayerSide.Usec) isHostile = botSettingsComponents.Mind.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack);
                else isHostile = botSettingsComponents.Mind.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack);
            }
        }

        private async UniTask ActivateBotFollower(BotCreator botCreator, Profile profile, GClass590 position, BotZone zone,bool shallBeGroup, Func<BotOwner, BotZone, BotsGroup> GroupAction, Action<BotOwner> OnActivate,CancellationToken token)
        {

            await botCreator.ActivateBot(
                profile,
                position,
                zone, shallBeGroup,
                GroupAction,
                OnActivate,
                token
            );
        }

        /** Fetch Follower Profile data along with custom appearance from server */
        private async UniTask<Profile> FetchMemberProfile(KeyValuePair<int,List<BepInEx.Configuration.ConfigEntry<string>>> member, Profile boss, BotCreator botCreator, EPlayerSide side, WildSpawnType role, BotSpawnParams spawnParams)
        {
            IProfileData data = new IProfileData(side, role, BotDifficulty.hard, 0f, spawnParams);

            Dictionary<string, dynamic> customization = new Dictionary<string, dynamic>();

            // assign custom clothes, if se
            List<string[]> uniforms = friendlyPMC.GetUniformOptions();
            string top = member.Value[2].Value;
            int idxt = uniforms[0].IndexOf(top);

            string btm = member.Value[3].Value;
            int idxb = uniforms[1].IndexOf(btm);

            if (top != uniforms[0][0])
            {
                // use player clothes if we selected the second option
                if (top == uniforms[0][1])
                {
                    customization["Body"] = boss.Customization[EBodyModelPart.Body];
                }
                else if (friendlyPMC.GetUniformPairs()[0].ContainsKey(idxt))
                {
                    string id = friendlyPMC.GetUniformPairs()[0][idxt];
                    customization["Body"] = id;
                }
            }

            if (btm != uniforms[1][0])
            {
                // use player pants if we selected the second option
                if (btm == uniforms[1][1])
                {
                    customization["Feet"] = boss.Customization[EBodyModelPart.Feet];
                }
                else if (friendlyPMC.GetUniformPairs()[1].ContainsKey(idxb))
                {
                    string id = friendlyPMC.GetUniformPairs()[1][idxb];
                    customization["Feet"] = id;
                }
            }

            // assign custom nickname, if set
            string nickname = member.Value[4].Value;
            if (nickname != null && nickname.Length > 0)
            {
                customization["Nickname"] = nickname;
            }

            var botPresets = AccessTools.Field(typeof(BotCreator), "ginterface18_0").GetValue(botCreator) as BotsPresets;
            var profileEndpoint = AccessTools.Field(typeof(BotsPresets), "iSession").GetValue(botPresets) as ProfileEndPoint;
            var gclass1200_0 = AccessTools.Field(typeof(ProfileEndPoint), "gclass1200_0").GetValue(profileEndpoint) as GClass1200;

            List<WaveInfo> limit = botPresets.method_1(data.PrepareToLoadBackend(1).ToList(), out var list3); ;

            customization["English"] = friendlyPMC.englishBear.Value;
            // call backend
            var result = await profileEndpoint.method_3<Profile[]>(new LegacyParamsStruct
            {
                Url = gclass1200_0.Main + "/client/game/bot/followergenerate",
                Params = new Dictionary<string, object>
                    {
                        { "Info",  new Class17<List<WaveInfo>>(limit) },
                        { "Custom", customization }
                    },
                Retries = new byte?(LegacyParamsStruct.DefaultRetries)
            });

            Profile profile = result.ToList().Random();
            // process backend result
            await Singleton<PoolManager>.Instance.LoadBundlesAndCreatePools(PoolManager.PoolsCategory.Raid, PoolManager.AssemblyType.Local, profile.GetAllPrefabPaths(false).ToArray<ResourceKey>(), JobPriority.General, null, PoolManager.DefaultCancellationToken);

            Modules.Logger.LogInfo("Generated Follower Profile " + profile.Nickname + " with level " + profile.Info.Level);

            return profile;
        }
        /** 
         * Task for creating Follower Profiles along with applying custom equipment (if specified) to them 
         * **/
        private async UniTask<Dictionary<int, Profile>> CreateProfilesJob(pitAIBossPlayer player)
        {
            Dictionary<int,Profile> profiles = new Dictionary<int,Profile>();

            string[] equipOptions = friendlyPMC.GetEquipOptions();

            var botSpawnerClass = Controller.BotSpawner;
            var botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as BotCreator;

            EPlayerSide side = player.realPlayer.Side;
            Vector3 position = player.Position;

            WildSpawnType sptBear = WildSpawnType.pmcBEAR;
            WildSpawnType sptUsec = WildSpawnType.pmcUSEC;

            WildSpawnType type;
            if (side == EPlayerSide.Bear)
            {
                type = sptBear;
            }
            else
            {
                type = sptUsec;
            }

            int memberCount = friendlyPMC.squadSize.Value;

            BotSpawnParams @params = new BotSpawnParams();
            @params.ShallBeGroup = new ShallBeGroupParams(true, false, memberCount + 1);

            Profile playerProfile = player.realPlayer.Profile;
            
            List<UniTask> profileTasks = new List<UniTask>();

            if (friendlyPMC.squadSetup.Value)
            {
                foreach (var member in friendlyPMC.squadMembers)
                {
                    // generate profile only for bots with equipment other than default or player's equipment
                    if (member.Value[1].Value != equipOptions[0])
                    {
                        // fetch profile from server
                        profileTasks.Add(FetchMemberProfile(member,playerProfile,botCreator,side,type,@params).ContinueWith(dt =>
                        {
                            profiles[member.Key] = dt;
                        }));
                    }
                }
            }


            await UniTask.WhenAll(profileTasks);

            Dictionary<string, Item> secureContainers = new Dictionary<string, Item>();


            Dictionary<string, List<DependencyGraph<IEasyBundle>.GClass3415>> bundleTokens = new Dictionary<string, List<DependencyGraph<IEasyBundle>.GClass3415>>();

            Dictionary<string, EquipmentClass> profileEquipment = new Dictionary<string, EquipmentClass>();

            Dictionary<string, string> bundleJobs = new Dictionary<string, string>();

            // change bot equipment based on custom presets
            List<GClass3205> presets = new List<GClass3205>();
            Utils.Equipment.CustomPresets.ForEach(preset =>
            {
                presets.Add(preset);
            });

            try
            {
                foreach (var pr in profiles)
                {
                    Profile profile = pr.Value;

                    int pid = pr.Key;

                    string eq = friendlyPMC.squadMembers[pid][1].Value;
                    // when using custom preset - prepare what bundles will need to be prefected
                    if (eq != null && eq != friendlyPMC.GetEquipOptions()[0])
                    {
                        // - remember the original secure container to put it back later as custom presets might overwrite it
                        var secureContainer = profile.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer).ContainedItem;
                        // - store the original secure container to use it later, prepare what bundles must be downloaded and associate the bot's profile with the specific preset
                        secureContainers.Add(profile.Id, secureContainer.CloneItem());

                        foreach (var preset in presets)
                        {
                            if (eq == preset.Name)
                            {
                                var equipment = preset.Equipment.CloneItem(null);
                                // - use bundleJobs to detect when more than 1 bot is using the same equipment
                                if (!bundleJobs.ContainsKey(preset.Name))
                                {
                                    // - start collecting all the items used by this preset in order to do bundle load for each
                                    foreach (var item in equipment.GetAllItems())
                                    {
                                        if (!bundleTokens.ContainsKey(profile.Id))
                                        {
                                            bundleTokens.Add(profile.Id, new List<DependencyGraph<IEasyBundle>.GClass3415>());
                                        }
                                        bundleTokens[profile.Id].Add(item.GetAllBundleTokens());
                                    };

                                    bundleJobs[preset.Name] = profile.Id;

                                }
                                else
                                {
                                    bundleTokens[profile.Id] = bundleTokens[bundleJobs[preset.Name]];
                                }

                                profileEquipment.Add(profile.Id, equipment);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Modules.Logger.LogError("Failed to set squad equipment for a bot");
                Modules.Logger.LogError(ex);
            }

            // gather what equipment bundles this bot needs to wait for
            List<UniTask> bundleTasks = new List<UniTask>();

            foreach( var item in profiles)
            {
                Profile profile = item.Value;

                if (bundleTokens.ContainsKey(profile.Id))
                {
                    bundleTokens[profile.Id].ForEach(bundle =>
                    {
                        bundleTasks.Add(GClass1458.WaitForAllBundlesJob(bundle, new Action(GClass1947.Class1694.class1694_0.method_0), default(CancellationToken), null).AsUniTask());
                    });

                }
            }

            Modules.Logger.LogInfo("Fetching preset assets...");
            try
            {
                if(bundleTasks.Count > 0) 
                    await UniTask.WhenAll(bundleTasks);
            }
            catch (Exception ex)
            {
                Modules.Logger.LogError("Failed to use custom preset, will fall back to default loadout");
                Modules.Logger.LogError(ex);
            }


            Modules.Logger.LogInfo("Preset assets fetched");

            
            foreach (var item in profiles)
            {
                Profile profile = item.Value;
                // apply the custom preset for the bot as bundles are ready
                if (profileEquipment.ContainsKey(profile.Id))
                {
                    foreach (EquipmentSlot slotType in Enum.GetValues(typeof(EquipmentSlot)))
                    {
                        if (slotType == EquipmentSlot.Dogtag) continue;


                        Slot botSlot = profile.Inventory.Equipment.GetSlot(slotType);

                        if(botSlot.IsSpecial) continue;

                        Slot cloneSlot = profileEquipment[profile.Id].GetSlot(slotType);

                        if(cloneSlot.IsSpecial) continue;

                        Item contained = cloneSlot.ContainedItem;

                        botSlot.RemoveItem();

                        if (contained != null)
                        {
                            contained.CurrentAddress = null;

                            botSlot.AddWithoutRestrictions(contained);

                        }
                    }
                    // - restore original secure container
                    if (secureContainers.ContainsKey(profile.Id))
                    {
                        Slot secCon = profile.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer);
                        secCon.RemoveItem();
                        secureContainers[profile.Id].CurrentAddress = null;
                        secCon.AddWithoutRestrictions(secureContainers[profile.Id]);
                    }

                }

                // followers should use the same groupID as the player
                profile.Info.GroupId = player.realPlayer.GroupId;
                profile.Info.TeamId = player.Player().Profile.Info.TeamId;

                // spawned followers will have a different health than the rest
                foreach (EBodyPart part in Enum.GetValues(typeof(EBodyPart)))
                {
                    profile.Health.BodyParts.TryGetValue(part, out var bodyPart);
                    if (bodyPart != null)
                    {
                        switch (part)
                        {
                            case EBodyPart.Head:
                                bodyPart.Health.Minimum = bodyPart.Health.Minimum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = bodyPart.Health.Maximum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = bodyPart.Health.Maximum * friendlyPMC.heatlhMultiplier.Value;
                                break;
                            case EBodyPart.Chest:
                                bodyPart.Health.Minimum = bodyPart.Health.Minimum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = bodyPart.Health.Maximum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = bodyPart.Health.Current * friendlyPMC.heatlhMultiplier.Value;
                                break;
                            case EBodyPart.Stomach:
                                bodyPart.Health.Minimum = bodyPart.Health.Minimum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = bodyPart.Health.Maximum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = bodyPart.Health.Current * friendlyPMC.heatlhMultiplier.Value;
                                break;
                            case EBodyPart.RightArm:
                            case EBodyPart.LeftArm:
                                bodyPart.Health.Minimum = bodyPart.Health.Minimum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = bodyPart.Health.Maximum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = bodyPart.Health.Current * friendlyPMC.heatlhMultiplier.Value;
                                break;
                            case EBodyPart.RightLeg:
                            case EBodyPart.LeftLeg:
                                bodyPart.Health.Minimum = bodyPart.Health.Minimum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = bodyPart.Health.Maximum * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = bodyPart.Health.Current * friendlyPMC.heatlhMultiplier.Value;
                                break;

                            default:
                                break;
                        }
                    }
                }

                // adjust follower's skills based on level
                float maxHealth = 2700f;
                float maxVitality = 2500f;
                float maxRecoil = 4500f;
                float maxHeavy = 1500f;
                float maxLight = 1500f;
                float maxStress = 2500f;

                float healthIncrement = 40f;
                float vitalityIncrement = 30f;
                float recoilIncrement = 50f;
                float heavyIncrement = 20f;
                float lightIncrement = 20f;

                float stressIncrement = 20f;

                int botLevel = profile.Info.Level;

                // --- health
                float scaledHealth = Utils.Utils.GetScaledValue(0f, healthIncrement, botLevel, maxHealth);
                if (profile.Skills.Health.Current < scaledHealth)
                    profile.Skills.Health.SetCurrent(scaledHealth, true);

                // --- vitality
                float scaledVitality = Utils.Utils.GetScaledValue(0f, vitalityIncrement, botLevel, maxVitality);
                if (profile.Skills.Vitality.Current < scaledVitality)
                    profile.Skills.Vitality.SetCurrent(scaledVitality, true);

                // --- recoil
                float scaledRecoil = Utils.Utils.GetScaledValue(0f, recoilIncrement, botLevel, maxRecoil);
                if (profile.Skills.RecoilControl.Current < scaledRecoil)
                    profile.Skills.RecoilControl.SetCurrent(scaledRecoil, true);

                // --- heavy vests
                float scaledHeavy = Utils.Utils.GetScaledValue(0f, heavyIncrement, botLevel, maxHeavy);
                if (profile.Skills.HeavyVests.Current < scaledHeavy)
                    profile.Skills.HeavyVests.SetCurrent(scaledHeavy, true);

                // --- light vests
                float scaledLight = Utils.Utils.GetScaledValue(0f, lightIncrement, botLevel, maxLight);
                if (profile.Skills.LightVests.Current < scaledLight)
                    profile.Skills.LightVests.SetCurrent(scaledLight, true);

                // --- stress
                float scaledStrees = Utils.Utils.GetScaledValue(0f, stressIncrement, botLevel, maxStress);
                if(profile.Skills.StressResistance.Current < scaledStrees)
                    profile.Skills.StressResistance.SetCurrent(scaledStrees, true);

                // -- grenade launcher
                profile.Skills.Launcher.SetCurrent(scaledRecoil, true);
                // -- grenade throwing
                profile.Skills.Throwing.SetCurrent(scaledRecoil, true);
            }

            Modules.Logger.LogInfo("Return follower profile data");

            return profiles;

        }
        /** Promise of Follower Profiles that will be cached for faster use **/
        public UniTask<Dictionary<int, Profile>>? CreateFollowerProfiles(pitAIBossPlayer player)
        {
            if (Controller == null) return null;
            if (followerCreationTask.ContainsKey(player.realPlayer.ProfileId))
            {
                return followerCreationTask[player.realPlayer.ProfileId];
            }

            followerCreationTask[player.realPlayer.ProfileId] = CreateProfilesJob(player);


            return followerCreationTask[player.realPlayer.ProfileId];
        }

        public void PreFetchBossProfiles(pitAIBossPlayer player)
        {
            if (Controller == null) return;

            EPlayerSide side = player.Player().Side;

            var botSpawnerClass = Controller.BotSpawner;
            BotCreator botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as BotCreator;

            WildSpawnType[] bosses = new WildSpawnType[] { WildSpawnType.bossKnight, WildSpawnType.followerBigPipe, WildSpawnType.followerBirdEye };

            BotSpawnParams @params = new BotSpawnParams();
            @params.ShallBeGroup = new ShallBeGroupParams(true, false, 4);

            

            foreach (var boss in bosses)
            {
                IProfileData botData = new IProfileData(side, boss, BotDifficulty.hard, 0f, @params);

                bossCreationTask[boss] = BotCreationDataClass.Create(botData, botCreator, 1, botSpawnerClass);
            }

        }

        private Task<BotCreationDataClass> GetBossProfile(WildSpawnType boss)
        {
            if (bossCreationTask.ContainsKey(boss))
            {
                return bossCreationTask[boss];
            }

            return null;
        }

        public void PreFetchScavProfiles(pitAIBossPlayer player)
        {
            if (Controller == null) return;

            EPlayerSide side = player.Player().Side;

            var botSpawnerClass = Controller.BotSpawner;
            BotCreator botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as BotCreator;

            int memberCount = friendlyPMC.squadSize.Value;

            BotSpawnParams @params = new BotSpawnParams();
            @params.ShallBeGroup = new ShallBeGroupParams(true, false, memberCount + 1);

            IProfileData data = new IProfileData(side, WildSpawnType.assault, BotDifficulty.hard, 5f, @params);

            alliesCreationTask[player.realPlayer.ProfileId] = BotCreationDataClass.Create(data, botCreator, memberCount, botSpawnerClass);
        }

        public Task<BotCreationDataClass> GetScavProfiles(pitAIBossPlayer player)
        {
            if (alliesCreationTask.ContainsKey(player.realPlayer.ProfileId))
            {
                return alliesCreationTask[player.realPlayer.ProfileId];
            }

            return null;
        }

        public void PreFetchPMCProfiles(pitAIBossPlayer player)
        {
            EPlayerSide side = player.Player().Side;;

            // get what type the bot will be 
            WildSpawnType sptBear = WildSpawnType.pmcBEAR;
            WildSpawnType sptUsec = WildSpawnType.pmcUSEC;

            WildSpawnType type;
            if (side == EPlayerSide.Bear)
            {
                type = sptBear;
            }
            else
            {
                type = sptUsec;
            }

            var botSpawnerClass = Controller.BotSpawner;
            BotCreator botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as BotCreator;

            int memberCount = friendlyPMC.squadSize.Value;

            BotSpawnParams @params = new BotSpawnParams();
            @params.ShallBeGroup = new ShallBeGroupParams(true, false, memberCount + 1);

            IProfileData data = new IProfileData(side, type, BotDifficulty.hard, 0f, @params);

            pmcCreationTask[player.realPlayer.ProfileId] = BotCreationDataClass.Create(data, botCreator, memberCount, botSpawnerClass);
        }

        public Task<BotCreationDataClass> GetPMCProfiles(pitAIBossPlayer player)
        {
            if (pmcCreationTask.ContainsKey(player.realPlayer.ProfileId))
            {
                return pmcCreationTask[player.realPlayer.ProfileId];
            }

            return null;
        }

        public static void PreventPMCConvert(bool state)
        {
            var converterClass = typeof(AbstractGame).Assembly.GetTypes()
                .First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

            var _defaultJsonConverters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;

            // ensure no PMC bots are generated due to convertIntoPmcChance value of the server
            RequestHandler.PutJson("/client/game/bot/preventpmcgenerate", new
            {
                State = state

            }.ToJson(_defaultJsonConverters));
        }

        private static bool HasFika()
        {
            return Type.GetType("Fika.Core.Coop.GameMode.CoopGame, Fika.Core") != null;
        }

        public async UniTask SpawnBossFollower(pitAIBossPlayer player, WildSpawnType boss = WildSpawnType.bossKnight, CancelToken cancelToken = null)
        {
            float dist;

            CancelToken token = cancelToken != null ? cancelToken :  new CancelToken();

            var botSpawnerClass = Controller.BotSpawner;
            var botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as BotCreator;

            Vector3 position = player.Position;
            EPlayerSide side = player.Player().Side;


            BotZone zone = botSpawnerClass.GetClosestZone(position, out dist);

            BotSpawnParams @params = new BotSpawnParams();
            @params.ShallBeGroup = new ShallBeGroupParams(true, false, 4);

            IProfileData botData = new IProfileData(side, boss, BotDifficulty.hard, 0f, @params);
            List<IProfileData> bossFollowers = new List<IProfileData> { };

            BotCreationDataClass bossAlly = null;

            if (boss == WildSpawnType.bossKnight)
            {
                bossAlly = await GetBossProfile(WildSpawnType.followerBirdEye);
                //bossFollowers.Add(new IProfileData(side, WildSpawnType.followerBigPipe, BotDifficulty.hard, 0f, @params));
                //bossFollowers.Add(new IProfileData(side, WildSpawnType.followerBirdEye, BotDifficulty.impossible, 0f, @params));

            }

            if (bossAlly == null) return;

            var closestCorePoint = GetClosestCorePoint(Controller, position);
            bossAlly.AddPosition(position, closestCorePoint.Id);


            List<Action> spanwers = new List<Action>();


            bossAlly.Profiles.ForEach(profile =>
            {
                
                InteractableObjects.StoreEquipment(profile);

                // normalize boss followers health
                foreach (EBodyPart part in Enum.GetValues(typeof(EBodyPart)))
                {
                    profile.Health.BodyParts.TryGetValue(part, out var bodyPart);
                    if(bodyPart != null)
                    {
                        switch (part)
                        {
                            case EBodyPart.Head:
                                bodyPart.Health.Minimum = 120;
                                bodyPart.Health.Maximum = 120;
                                bodyPart.Health.Current = 120;
                                break;
                            case EBodyPart.Chest:
                            case EBodyPart.Stomach:
                                bodyPart.Health.Minimum = 220;
                                bodyPart.Health.Maximum = 220;
                                bodyPart.Health.Current = 220;
                                break;
                            case EBodyPart.RightArm:
                            case EBodyPart.LeftArm:
                                bodyPart.Health.Minimum = 150;
                                bodyPart.Health.Maximum = 150;
                                bodyPart.Health.Current = 150;
                                break;
                            case EBodyPart.RightLeg:
                            case EBodyPart.LeftLeg:
                                bodyPart.Health.Minimum = 170;
                                bodyPart.Health.Maximum = 170;
                                bodyPart.Health.Current = 170;
                                break;

                            default:
                                break;
                        }
                    }
                }

                WildSpawnType botRole = profile.Info.Settings.Role;

                profile.Info.Side = side;
                profile.Info.GroupId = player.realPlayer.GroupId;
                profile.Info.TeamId = player.Player().Profile.Info.TeamId;

                // profile adjustment for each boss
                if (botRole == WildSpawnType.followerBirdEye)
                {
                    profile.Skills.BotSoundGoef.SetCurrent(3100f, true);
                    profile.Skills.AimMasterElite.Value = true;
                    profile.Skills.Sniper.SetCurrent(5100f, true);
                    profile.Skills.RecoilControl.SetCurrent(4800f, true);
                }
                else if (botRole == WildSpawnType.followerBigPipe)
                {
                    profile.Skills.RecoilControl.SetCurrent(4800f, true);
                    profile.Skills.SMG.SetCurrent(5000f, true);
                }
                else if (botRole == WildSpawnType.bossKnight)
                {
                    profile.Skills.RecoilControl.SetCurrent(4800f, true);
                    profile.Skills.Assault.SetCurrent(5000f, true);
                }
                // - common
                float maxVitality = 2500f;
                float maxHeavy = 1500f;
                float maxLight = 1500f;
                float maxStress = 2500f;

                float vitalityIncrement = 30f;
                float heavyIncrement = 20f;
                float lightIncrement = 20f;

                float stressIncrement = 20f;
                // --- vitality
                float scaledVitality = Utils.Utils.GetScaledValue(0f, vitalityIncrement, profile.Info.Level, maxVitality);
                if (profile.Skills.Vitality.Current < scaledVitality)
                    profile.Skills.Vitality.SetCurrent(scaledVitality, true);

                // --- heavy vests
                float scaledHeavy = Utils.Utils.GetScaledValue(0f, heavyIncrement, profile.Info.Level, maxHeavy);
                if (profile.Skills.HeavyVests.Current < scaledHeavy)
                    profile.Skills.HeavyVests.SetCurrent(scaledHeavy, true);

                // --- light vests
                float scaledLight = Utils.Utils.GetScaledValue(0f, lightIncrement, profile.Info.Level, maxLight);
                if (profile.Skills.LightVests.Current < scaledLight)
                    profile.Skills.LightVests.SetCurrent(scaledLight, true);

                // --- stress
                float scaledStrees = Utils.Utils.GetScaledValue(0f, stressIncrement, profile.Info.Level, maxStress);
                if (profile.Skills.StressResistance.Current < scaledStrees)
                    profile.Skills.StressResistance.SetCurrent(scaledStrees, true);


                spanwers.Add(() => {
                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    // switch role on spawning as original one glitches out
                    if (botRole == WildSpawnType.followerBirdEye)
                    {
                        if (side == EPlayerSide.Bear)
                        {
                            profile.Info.Settings.Role = WildSpawnType.pmcBEAR;
                        }
                        else if (side == EPlayerSide.Usec)
                        {
                            profile.Info.Settings.Role = WildSpawnType.pmcUSEC;
                        }
                        else
                            profile.Info.Settings.Role = WildSpawnType.assault;

                    }

                    Action<BotOwner> OnActivate = new Action<BotOwner>((BotOwner owner) =>
                    {
                        Action<BotOwner> OnBotState = new Action<BotOwner>((BotOwner me) =>
                        {
                            BotOwnerManualUpdatePatch.BotOwnerUpdate.Remove(me.ProfileId); // clear watcher

                            try
                            {
                                // prevent attack of player on spawn
                                me.Memory.DeleteInfoAboutEnemy(player.Player());

                                me.GetPlayer.ActiveHealthController.RestoreFullHealth(); // ensure bot has full health

                                me.Memory.IsPeace = true;

                                // force player side on the bot
                                if (me.Side != side)
                                {
                                    me.GetPlayer.Profile.Info.Side = side;
                                }

                                if (!me.IsRole(botRole))
                                {
                                    me.GetPlayer.Profile.Info.Settings.Role = botRole;
                                }

                                // our Pipe needs the same boss logic as knight due to their shared fighting logic
                                if (me.IsRole(WildSpawnType.followerBigPipe))
                                {
                                    if (me.Boss != null)
                                    {
                                        if (me.Boss.BossLogic != null)
                                            me.Boss.BossLogic.Dispose();

                                        me.Boss.BossLogic = new GClass371(me, me.Boss);
                                        me.Boss.NeedProtection = false;
                                    }

                                }

                                BossPlayers.AddFollower(me, player, false, botRole); // make bot a follower

                                Utils.Utils.SetTimeout(() =>
                                {
                                    if (spanwers.Count > 0)
                                    {
                                        spanwers[spanwers.Count - 1].Invoke();
                                        spanwers.RemoveAt(spanwers.Count - 1);
                                    }
                                    else
                                    {
                                        token.Cancel();
                                        bossAlly.StopSpawn();
                                    }
                                }, 1000);
                            }
                            catch (Exception ex)
                            {
                                Modules.Logger.LogError("Failed to add " + me.Profile.Nickname + " as ally");
                                Modules.Logger.LogError(ex);
                            }
                        });

                        BotOwnerManualUpdatePatch.BotOwnerUpdate.Add(owner.ProfileId, OnBotState);

                        // force player side on the bot
                        if (owner.Side != side)
                        {
                            owner.GetPlayer.Profile.Info.Side = side;
                        }
                        
                        BossPlayers.ShallBeFollower(owner);

                        botSpawnerClass.method_10(owner, bossAlly, new Action<BotOwner>((BotOwner follower) =>
                        {
                            Modules.Logger.LogInfo("Ally " + follower.Profile.Nickname + " spawned");

                            Utils.Utils.SetTimeout(() =>
                            {
                                follower.BotTalk.TrySay(EPhraseTrigger.Ready, false);
                            }, 2000);

                        }), true, stopWatch);

                    });

                    Func<BotOwner, BotZone, BotsGroup> GroupAction = new Func<BotOwner, BotZone, BotsGroup>((BotOwner bt, BotZone zn) =>
                    {
                        return GetPlayerGroup(player, bt, zn,0,true);
                    });

                    ActivateBotFollower(
                        botCreator,
                        profile,
                        new GClass590(position, closestCorePoint.Id, false),
                        zone, true,
                        GroupAction,
                        OnActivate,
                        token.GetCancelToken()
                    ).Forget();

                });   
            });

            spanwers.Reverse();
            spanwers[spanwers.Count - 1].Invoke();
            spanwers.RemoveAt(spanwers.Count - 1);
        }

        public async UniTask SpawnGroupBots(pitAIBossPlayer player)
        {

            CancelToken token = new CancelToken();

            var botSpawnerClass = Controller.BotSpawner;
            var botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as BotCreator;

            EPlayerSide side = player.Player().Side;
            Vector3 position = player.Position;
            BotZone zone = botSpawnerClass.GetClosestZone(position, out var dist);

            // get what type the bot will be 
            WildSpawnType sptBear = WildSpawnType.pmcBEAR;
            WildSpawnType sptUsec = WildSpawnType.pmcUSEC;

            WildSpawnType type;
            if (side == EPlayerSide.Bear)
            {
                type = sptBear;
            }
            else if (side == EPlayerSide.Usec)
            {
                type = sptUsec;
            }
            else
            {
                type = WildSpawnType.assault;
            }

            Modules.Logger.LogInfo("Spawn Followers");

            int memberCount = friendlyPMC.squadSize.Value;


            BotCreationDataClass botsData;

            // remember what tactic this bot is associated with as the tactic will be set after spawn
            Dictionary<string, string> profileTactic = new Dictionary<string, string>();

            // scav players will have random followers that cannot be customized
            if (side == EPlayerSide.Savage)
            {
                var converterClass = typeof(AbstractGame).Assembly.GetTypes()
                .First(t => t.GetField("Converters", BindingFlags.Static | BindingFlags.Public) != null);

                var _defaultJsonConverters = Traverse.Create(converterClass).Field<JsonConverter[]>("Converters").Value;
                

                botsData = await GetScavProfiles(player);

                botsData.Profiles.ForEach(profile =>
                {
                    profileTactic[profile.Id] = "Assist";
                });

                PreventPMCConvert(false);
            }
            else 
            {
                // 0 squadMembers means no squadSetup, so just use default
                if (friendlyPMC.squadMembers.Count < 1)
                {
                    botsData = await GetPMCProfiles(player);
                }

                // else use our own profile generator
                else
                {
                    BotSpawnParams @params = new BotSpawnParams();
                    @params.ShallBeGroup = new ShallBeGroupParams(true, false, memberCount + 1);

                    IProfileData data = new IProfileData(side, type, BotDifficulty.hard, side == EPlayerSide.Savage ? 5f : 0f, @params);

                    Dictionary<int, Profile> botsProfile = await CreateFollowerProfiles(player).Value;

                    botsData = await BotCreationDataClass.Create(data, botCreator, 0, botSpawnerClass);

                    followerCreationTask.Remove(player.realPlayer.ProfileId);

                    foreach (var member in friendlyPMC.squadMembers)
                    {
                        string tactic = member.Value[0].Value;
                        string[] availableTactics = friendlyPMC.GetTacticOptions();

                        string eq = member.Value[1].Value;

                        // first to try see if this profile has any custom equipment
                        if (botsProfile.TryGetValue(member.Key, out Profile profile))
                        {
                            // - set what tactic this follower will have
                            if (tactic != null && tactic != availableTactics[0])
                            {
 
                                if (tactic == availableTactics[3])
                                {
                                    tactic = "Push";
                                }
                                else if (tactic == availableTactics[4])
                                {
                                    tactic = "Defend";
                                }
                                else if (tactic == availableTactics[2])
                                {
                                    tactic = "Marksman";
                                    
                                    // -- adjust sniper skill only for marskman
                                    float maxSniper = 5500f;
                                    float sniperIncrement = 102f;
                                    int botLevel = profile.Info.Level;

                                    float scaledSniper = Utils.Utils.GetScaledValue(0f, sniperIncrement, botLevel, maxSniper);
                                    if (profile.Skills.Sniper.Current < scaledSniper)
                                        profile.Skills.Sniper.SetCurrent(scaledSniper, true);
                                }
                                else if (tactic == availableTactics[1])
                                {
                                    tactic = "Guard";
                                }

                                profileTactic.Add(profile.ProfileId, tactic);
                            }

                            botsData.AddProfile(profile);
                        }
                        else
                        {
                            // else just use the profile as it is
                            profile = await FetchMemberProfile(member, player.realPlayer.Profile, botCreator, side, type, @params);

                            botsData.AddProfile(profile);
                        }
                    }

                }
                
            }

            Func<BotOwner, BotZone, BotsGroup> GroupAction = new Func<BotOwner, BotZone, BotsGroup>((BotOwner bt, BotZone zn) =>
            {
                return GetPlayerGroup(player, bt, zn,memberCount);
            });

            
            int spawnedFollowers = 0;

            List<UniTask> activateTasks = new List<UniTask>();

            var closestCorePoint = GetClosestCorePoint(Controller, position);
            botsData.AddPosition(position, closestCorePoint.Id);

            botsData.Profiles.ForEach(profile =>
            {
                InteractableObjects.StoreEquipment(profile);

                Action<BotOwner> OnActivate = new Action<BotOwner>((BotOwner owner) =>
                {
                    
                    bool shallBeGroup = botsData.SpawnParams?.ShallBeGroup != null;

                    Stopwatch stopWatch = new Stopwatch();
                    stopWatch.Start();

                    Action<BotOwner> OnBotState = new Action<BotOwner>((BotOwner me) =>
                    {
                        BotOwnerManualUpdatePatch.BotOwnerUpdate.Remove(me.ProfileId); // clear watcher
                        try
                        {
                            me.Memory.DeleteInfoAboutEnemy(player.Player()); // prevent attack of player on spawn

                            me.GetPlayer.ActiveHealthController.RestoreFullHealth(); // ensure bot has full health

                            string tactic = null;
                            profileTactic.TryGetValue(me.Profile.ProfileId, out tactic);

                            if (tactic == null) tactic = "Default";

                            WildSpawnType botType = type;

                            if(me.Profile.Info?.Settings?.Role != null)
                            {
                                botType = me.Profile.Info.Settings.Role;
                            }

                            Modules.Logger.LogInfo("Tactic is " + tactic);

                            BossPlayers.AddFollower(me, player, true, botType, tactic);

                            Utils.Utils.SetTimeout(() =>
                            {
                                me.BotTalk.TrySay(EPhraseTrigger.Ready, true);
                            }, 2000);

                        }
                        catch (Exception ex)
                        {
                            Modules.Logger.LogError("Failed to add " + me.Profile.Nickname + " as follower");
                            Modules.Logger.LogError(ex);
                        }
                    });

                    BotOwnerManualUpdatePatch.BotOwnerUpdate.Add(owner.ProfileId, OnBotState);

                    // force player side on the bot
                    if (owner.Side != side)
                    {
                        owner.GetPlayer.Profile.Info.Side = side;
                    }

                    BossPlayers.ShallBeFollower(owner);
 
                    botSpawnerClass.method_10(owner, botsData, new Action<BotOwner>((BotOwner follower) =>
                    {

                        Modules.Logger.LogInfo("Follower " + follower.Profile.Nickname + " spawned");

                        spawnedFollowers++;

                        if (spawnedFollowers >= memberCount)
                        {
                            token.Cancel();
                            botsData.StopSpawn();
                        }

                    }), shallBeGroup, stopWatch);

                });


                Modules.Logger.LogInfo("Trying to spawn " + profile.Nickname + " follower");

                var _inSpawnProcess = (int)AccessTools.Field(typeof(BotSpawner), "_inSpawnProcess").GetValue(botSpawnerClass);
                AccessTools.Field(typeof(BotSpawner), "_inSpawnProcess").SetValue(botSpawnerClass, _inSpawnProcess + 1);

                // activate the bot
                activateTasks.Add( ActivateBotFollower(
                    botCreator,
                    profile,
                    new GClass590(position, botsData.GetPosition().CorePointId, false),
                    zone, true,
                    GroupAction,
                    OnActivate,
                    token.GetCancelToken()
                ));

            });
            try
            {
                await UniTask.WhenAll(activateTasks.ToArray());
            }
            catch (Exception ex)
            {
                Modules.Logger.LogError(ex);
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "AddActivePLayer");

        }
        [PatchPostfix]
        private static void PatchPostfix(BotsController __instance, Player player)
        {
            try
            {
                if (Controller == null)
                {
                    new BossPlayers();
                    new InteractableObjects();
                    new NpcMessage();
                    new Receivers();
                    new FollowerPatrolInstances();

                    PingTeamates.Enable();

                    friendlyPMC.Instance.GetEquipmentBuilds(); // ensure equipment is gathered

                    Props.Reset();

                    BaseLocalGameVmethod4Patch.squadSpawned = false;

                    Controller = __instance;

                    string locationId = Singleton<GameWorld>.Instance.LocationId;

                    if (locationId == "factory4_day" || locationId == "factory4_night")
                    {
                        Props.FactoryMapSett();
                    }

                    Modules.Logger.LogInfo("Raid Started");
                }


                pitAIBossPlayer playerBoss = BossPlayers.AddPlayerAsBoss(player);

                spawnedPlayers.Add(playerBoss);

                // prefetch follower profile data
                if (playerBoss.Player().Side != EPlayerSide.Savage)
                {
                    if (!friendlyPMC.squadSpawn.Value)
                        Instance?.PreFetchBossProfiles(playerBoss);

                    else if (friendlyPMC.squadSetup.Value)
                    {
                        if(friendlyPMC.squadMembers.Count < 1) 
                            Instance?.PreFetchPMCProfiles(playerBoss);
                        else
                            Instance?.CreateFollowerProfiles(playerBoss);
                    }
                }
                else if (playerBoss.Player().Side == EPlayerSide.Savage && friendlyPMC.squadSpawn.Value)
                {
                    Instance?.PreFetchScavProfiles(playerBoss);
                }
            }
            catch (Exception e)
            {
                Modules.Logger.LogError(e);
            }

        }
    }
    
    [HarmonyPatch(typeof(LocalGame),MethodType.Constructor)]
    internal class LocalGameCtorPatch
    {
        public static LocalGame Instance;
        public static void Postfix(LocalGame __instance)
        {
            Instance = __instance;
        }
    }

    [HarmonyPatch(typeof(BaseLocalGame<EftGamePlayerOwner>))]
    [HarmonyPatch("vmethod_4")]
    internal class BaseLocalGameVmethod4Patch
    {
        [HarmonyPostfix]
        public static IEnumerator Postfix(IEnumerator __result, BaseLocalGame<EftGamePlayerOwner> __instance, BotControllerSettings controllerSettings, ISpawnSystem spawnSystem, Callback runCallback)
        {
            yield return __result;
            try
            {
                SpawnFollowers();
            }
            catch (Exception e)
            {
                Modules.Logger.LogError(e);
            }
            yield break;
        }

        public static bool squadSpawned = false;

        public static void SpawnFollowers()
        {

            if (squadSpawned || BotsControllerPatch.Controller == null) return;

            squadSpawned = true;

            List<UniTask> squadSpawners = new List<UniTask>();

            if (friendlyPMC.squadSpawn.Value)
            {
                Modules.Logger.LogInfo("Start Squad Spawn");

                BotsControllerPatch.spawnedPlayers.ForEach(playerBoss =>
                {
                    UniTask squadSpanner = BotsControllerPatch.Instance.SpawnGroupBots(playerBoss);
                    squadSpawners.Add(squadSpanner);
                });

                UniTask.WhenAll(squadSpawners).ContinueWith(() => {
                    BotsControllerPatch.alliesCreationTask.Clear();
                    BotsControllerPatch.pmcCreationTask.Clear();
                }).Forget();

            } else 
            {
                Modules.Logger.LogInfo("Start Boss Ally Spawn");
                
                BotsControllerPatch.Controller.BotSpawner.SetBlockedRoles(new string[] { "bossKnight", "followerBirdEye", "followerBigPipe" });

                UniTask.Void(async () =>
                {

                    BotsControllerPatch.alliesCreationTask.Clear();
                    BotsControllerPatch.pmcCreationTask.Clear();

                    List<UniTask> bossSpawners = new List<UniTask>();

                    BotsControllerPatch.spawnedPlayers.ForEach(playerBoss =>
                    {
                        try
                        {
                            bossSpawners.Add(BotsControllerPatch.Instance.SpawnBossFollower(playerBoss));
                        }
                        catch (Exception e)
                        {
                            Modules.Logger.LogError("Failed to spawn Boss Ally");
                            Modules.Logger.LogError(e);
                        }
                    });

                    await UniTask.WhenAll(bossSpawners);
                    BotsControllerPatch.bossCreationTask.Clear();

                });
            }
        }
    }

    internal class BotsControllerStopPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "Stop");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotsController __instance)
        {
            InteractableObjects.Dispose();
            NpcMessage.Dispose();

            BossPlayers.Dispose();
            Receivers.Dispose();
            FollowerPatrolInstances.Dispose();

            BotsControllerPatch.spawnedPlayers.Clear();
            BotsControllerPatch.followerCreationTask.Clear();
            BotsControllerPatch.alliesCreationTask.Clear();
            BotsControllerPatch.pmcCreationTask.Clear();
            BotsControllerPatch.bossCreationTask.Clear();

            BotsControllerPatch.Controller = null;

            BotOwnerManualUpdatePatch.BotOwnerUpdate.Clear();

            PingTeamates.Disable();

            Enemy.ClearEnemiesLocations();
            Utils.Utils.FlagsClear();

            AIDataContructPatch.playerAIData.Clear();

            BaseLocalGameVmethod4Patch.squadSpawned = false;

            LocalGameCtorPatch.Instance = null;

            LookSensorPatch.FlushSwitches();

            Modules.Logger.LogInfo("Raid Ended");

            return true;
        }
    }

    internal class LocalGameCleanupPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalGame), "CleanUp");

        }
        // fix errors during cleanup because of NULL players
        [PatchPrefix]
        private static bool PatchPrefix(LocalGame __instance)
        {
            try
            {
                var dictionary_2 = AccessTools.Field(typeof(LocalGame), "dictionary_2").GetValue(__instance) as Dictionary<string, Player>;
                if (dictionary_2 != null)
                {
                    List<string> keysToRemove = new List<string>();

                    // Iterate through the dictionary to find null values
                    foreach (var kvp in dictionary_2)
                    {
                        if (kvp.Value == null)
                        {
                            keysToRemove.Add(kvp.Key);
                        }
                    }

                    // Remove the keys with null values
                    foreach (var key in keysToRemove)
                    {
                        dictionary_2.Remove(key);
                    }
                }

                Modules.Logger.LogInfo("Raid CleanUp Finished");

            } catch (Exception ex)
            {
                Modules.Logger.LogError("Raid CleanUp Failed");
                Modules.Logger.LogError(ex);
            }

            return true;
        }
    }
}
