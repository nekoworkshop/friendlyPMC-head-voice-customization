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

        public BotsControllerPatch()
        {
            if (Instance == null) Instance = this;
        }


        private AICorePoint GetClosestCorePoint(BotsController _botsController,Vector3 position)
        {
            var coversData = _botsController.CoversData;
            var groupPoint = coversData.GetClosest(position);
            return groupPoint.CorePointInGame;
        }

        private BotsGroup GetPlayerGroup(pitAIBossPlayer player, BotOwner bt, BotZone zn)
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
            bool sameSideHostile;

            if (player.Player().Side == EPlayerSide.Bear)
            {
                roleh = sptBear;
            }
            else if (player.Player().Side == EPlayerSide.Usec)
            {
                roleh = sptUsec;
            }
            else
            {
                roleh = WildSpawnType.assault;
            }

            GetSameSideHostile(roleh, player.Player().Side, out sameSideHostile);

            EPlayerSide side = player.realPlayer.Side;

            BotsGroup botsGroup;
            List<BotOwner> list = new List<BotOwner>();

            // botsGroup take on the values of the inital bot, attempt to prevent the group from being hostile to the player
            bt.Settings.FileSettings.Mind.ENEMY_BY_GROUPS_PMC_PLAYERS = side != EPlayerSide.Savage ? false : true;
            bt.Settings.FileSettings.Mind.ENEMY_BY_GROUPS_SAVAGE_PLAYERS = side == EPlayerSide.Savage ? false : true;

            var oldBehaviourBear = bt.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR;
            var oldBehaviorUsec = bt.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR;
            var oldBehaviorSavage = bt.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR;

            var old_reasons = bt.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY;

            bt.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = true;
            bt.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = new EBotEnemyCause[] { };

            if (side == EPlayerSide.Savage)
            {
                bt.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = sameSideHostile ? EWarnBehaviour.Attack : EWarnBehaviour.Ignore;
                bt.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = EWarnBehaviour.Attack;
                bt.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = EWarnBehaviour.Attack;
            }
            else if (side == EPlayerSide.Bear)
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
            if (_freeForAll)
            {
                spawnGroups.AddNoKey(botsGroup, zn);
            }
            else
            {
                spawnGroups.Add(zn, side, botsGroup, false);
            }

            BossPlayers.AddGroupToBoss(player,botsGroup);

            // revert changes
            bt.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = false;
            bt.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = old_reasons;
            bt.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = oldBehaviourBear;
            bt.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = oldBehaviorUsec;
            bt.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = oldBehaviorSavage;

            return botsGroup;
        }

        public void GetSameSideHostile(WildSpawnType role, EPlayerSide side, out bool isHostile)
        {
            isHostile = false;

            BotSettingsComponents botSettingsComponents = GClass531.smethod_1(BotDifficulty.normal, role, false);
            if(botSettingsComponents != null)
            {
                if (side == EPlayerSide.Bear) isHostile = botSettingsComponents.Mind.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack);
                else if (side == EPlayerSide.Usec) isHostile = botSettingsComponents.Mind.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack);
                else isHostile = botSettingsComponents.Mind.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack);
            }
        }

        public async UniTask ActivateBotFollower(BotCreator botCreator, Profile profile, GClass590 position, BotZone zone,bool shallBeGroup, Func<BotOwner, BotZone, BotsGroup> GroupAction, Action<BotOwner> OnActivate,CancellationToken token)
        {
            
            LocalGame game = LocalGameCtorPatch.Instance;
            Type fikaType = Type.GetType("Fika.Core.Coop.GameMode.CoopGame, Fika.Core");

            if (game != null  && fikaType == null) 
            {

                BotSpawner botSpawnerClass = Controller.BotSpawner;

                IBotGame botGame = AccessTools.Field(typeof(BotSpawner), "_game").GetValue(botSpawnerClass) as IBotGame;

                Dictionary<string, Player> dictionary_2 = null;

                dictionary_2 = AccessTools.Field(typeof(LocalGame), "dictionary_2").GetValue(game) as Dictionary<string, Player>;

                // recreation of ActivateBot from GClass814
                BotCreator.Class509 @class = new BotCreator.Class509();
                @class.gclass814_0 = botCreator;
                @class.zone = zone;

                @class.callback = OnActivate;

                @class.groupAction = GroupAction;


                GClass590 bornInfo = position;
                // this is part of method_17 from LocalGame
                int playerId = game.method_12();
                profile.SetSpawnedInSession(profile.Info.Side == EPlayerSide.Savage);

                LocalPlayer localPlayer = await LocalPlayer.Create(playerId, bornInfo.position, Quaternion.identity, "Player", "", EPointOfView.ThirdPerson, profile, true, game.UpdateQueue, Player.EUpdateMode.Auto, Player.EUpdateMode.Auto, BackendConfigAbstractClass.Config.CharacterController.BotPlayerMode, new Func<float>(LocalGame.Class1394.class1394_0.method_4), new Func<float>(LocalGame.Class1394.class1394_0.method_5), new GClass1800(), GClass1457.Default, null, null, false);
                localPlayer.Location = game.Location_0.Id;

                dictionary_2.Add(localPlayer.ProfileId, localPlayer);

                // method_2 of GClass814
                AICorePoint corePoint = Controller.CoversData.AICorePointsHolder.GetCorePoint(bornInfo.CorePointId);
                BotOwner botOwner = BotOwner.Create(localPlayer, null, botGame.GameDateTime, Controller, true, corePoint);
                botCreator.method_4(botOwner.GetPlayer);
                botCreator.method_5(botOwner, false);
                botOwner.GetComponentsInChildren<Collider>();
                botOwner.GetPlayer.CharacterController.isEnabled = false;

                @class.method_0(botOwner);
            } 
            else 
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
        }

        private async UniTask<Profile> GenerateFollowerProfile(
            BotCreator botCreator,
            WaveInfo[] source
        )
        {

            var botPresets = AccessTools.Field(typeof(BotCreator), "ginterface18_0").GetValue(botCreator) as BotsPresets;
            var profileEndpoint = AccessTools.Field(typeof(BotsPresets), "iSession").GetValue(botPresets) as ProfileEndPoint;
            var gclass1200_0 = AccessTools.Field(typeof(ProfileEndPoint), "gclass1200_0").GetValue(profileEndpoint) as GClass1200;

            List<WaveInfo> limit = botPresets.method_1(source.ToList(), out var list3); ;


            var result = await profileEndpoint.method_3<Profile[]>(new LegacyParamsStruct
            {
                    Url = gclass1200_0.Main + "/client/game/bot/followergenerate",
                    Params = new Dictionary<string, object>
                    {
                        { "Info",  new Class17<List<WaveInfo>>(limit) }
                    },
                    Retries = new byte?(LegacyParamsStruct.DefaultRetries)
            });

            Profile profile = result.ToList().Random();

            await Singleton<PoolManager>.Instance.LoadBundlesAndCreatePools(PoolManager.PoolsCategory.Raid, PoolManager.AssemblyType.Local, profile.GetAllPrefabPaths(false).ToArray<ResourceKey>(), JobPriority.General, null, PoolManager.DefaultCancellationToken);

            Components.Logger.LogInfo("Generated Follower Profile " + profile.Nickname + " with level " + profile.Info.Level);

            return profile;
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
            List<IProfileData> bossFollowers = new List<IProfileData> { };;

            if (boss == WildSpawnType.bossKnight)
            {

                Utils.Utils.FlagSet("withKnight", true);

                if (friendlyPMC.bigPipeSpawn.Value)
                {
                    bossFollowers.Add(new IProfileData(side, WildSpawnType.followerBigPipe, BotDifficulty.hard, 0f, @params));
                    Utils.Utils.FlagSet("withBigPipe", true);
                }

                if (friendlyPMC.birdEyeSpawn.Value)
                {
                    bossFollowers.Add(new IProfileData(side, WildSpawnType.followerBirdEye, BotDifficulty.impossible, 0f, @params));
                    Utils.Utils.FlagSet("withBirdEye", true);
                }
                
                if(friendlyPMC.birdEyeSpawn.Value && friendlyPMC.birdEyeSpawn.Value)
                {
                    Utils.Utils.FlagSet("withGoons", true);
                }

            } else
            {
                if(boss == WildSpawnType.followerBigPipe)
                {
                    Utils.Utils.FlagSet("withBigPipe", true);
                }

                if (boss == WildSpawnType.followerBirdEye)
                {
                    Utils.Utils.FlagSet("withBirdEye", true);
                }
            }

            BotCreationDataClass bot = await BotCreationDataClass.Create(botData, botCreator, 1, botSpawnerClass);


            if (bossFollowers.Count > 0)
            {
                foreach (var item in bossFollowers)
                {
                    BotCreationDataClass flw = await BotCreationDataClass.Create(item, botCreator, 1, botSpawnerClass);
                    bot.AddProfiles(flw.Profiles);
                }

                if (!friendlyPMC.justKnightSpawn.Value)
                {
                    Profile knightProfile = bot.Profiles.Find(pr=>pr.Info.Settings.Role == WildSpawnType.bossKnight);
                    bot.RemoveProfile(knightProfile);
                }
            }

            var closestCorePoint = GetClosestCorePoint(Controller, position);
            bot.AddPosition(position, closestCorePoint.Id);


            List<Action> spanwers = new List<Action>();


            bot.Profiles.ForEach(profile =>
            {
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
                                        bot.StopSpawn();
                                    }
                                }, 1000);
                            }
                            catch (Exception ex)
                            {
                                Components.Logger.LogError("Failed to add " + me.Profile.Nickname + " as ally");
                                Components.Logger.LogError(ex);
                            }
                        });

                        BotOwnerManualUpdatePatch.BotOwnerUpdate.Add(owner.ProfileId, OnBotState);

                        // force player side on the bot
                        if (owner.Side != side)
                        {
                            owner.GetPlayer.Profile.Info.Side = side;
                        }
                        
                        BossPlayers.ShallBeFollower(owner);

                        botSpawnerClass.method_10(owner, bot, new Action<BotOwner>((BotOwner follower) =>
                        {
                            Components.Logger.LogInfo("Ally " + follower.Profile.Nickname + " spawned");

                            Utils.Utils.SetTimeout(() =>
                            {
                                follower.BotTalk.TrySay(EPhraseTrigger.Ready, false);
                            }, 2000);

                        }), true, stopWatch);

                    });

                    Func<BotOwner, BotZone, BotsGroup> GroupAction = new Func<BotOwner, BotZone, BotsGroup>((BotOwner bt, BotZone zn) =>
                    {
                        return GetPlayerGroup(player, bt, zn);
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

            float dist;

            CancelToken token = new CancelToken();

            var botSpawnerClass = Controller.BotSpawner;
            var botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as BotCreator;
           

            Vector3 position = player.Position;
            EPlayerSide side = player.Player().Side;


            BotZone zone = botSpawnerClass.GetClosestZone(position, out dist);

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

            int memberCount = friendlyPMC.squadSize.Value;

            BotSpawnParams @params = new BotSpawnParams();
            @params.ShallBeGroup = new ShallBeGroupParams(true, false, memberCount + 1);

            IProfileData botData = new IProfileData(side, type, BotDifficulty.hard, 0f, @params);

            BotCreationDataClass botCreationData = new BotCreationDataClass(botData);
            AccessTools.Field(typeof(BotCreationDataClass), "ginterface19_0").SetValue(botCreationData, botSpawnerClass);
            AccessTools.Field(typeof(BotCreationDataClass), "iBotCreator").SetValue(botCreationData, botCreator);

            Profile playerProfile = player.Player().Profile;
            
            BotCreationDataClass bot;

            if (side != EPlayerSide.Savage)
            {
                for (int i = 0; i < memberCount; i++)
                {
                    var pr = await GenerateFollowerProfile(botCreator, botData.PrepareToLoadBackend(1));
                    botCreationData.AddProfile(pr);
                    // take player's clothes if flag is turned on
                    if (friendlyPMC.squadUniform.Value)
                    {
                        pr.Customization[EBodyModelPart.Body] = playerProfile.Customization[EBodyModelPart.Body];
                        pr.Customization[EBodyModelPart.Feet] = playerProfile.Customization[EBodyModelPart.Feet];
                        pr.Customization[EBodyModelPart.Hands] = playerProfile.Customization[EBodyModelPart.Hands];
                    }
                }

                bot = botCreationData;
            }
            else
            {
                bot = await BotCreationDataClass.Create(botData, botCreator, memberCount, botSpawnerClass);
            }


            List<DependencyGraph<IEasyBundle>.GClass3415> bundleTokens = new List<DependencyGraph<IEasyBundle>.GClass3415>();
            Dictionary<string,EquipmentClass> profileEquipment = new Dictionary<string,EquipmentClass>();

            Dictionary<string,string> profileTactic = new Dictionary<string,string>();
            Dictionary<string, Item> secureContainers = new Dictionary<string, Item>();

            if (side != EPlayerSide.Savage)
            {
                List<GClass3205> presets = new List<GClass3205>();
                Utils.Equipment.CustomPresets.ForEach(preset =>
                {
                    presets.Add(preset);
                });

                Dictionary<string,int> usedPresets = new Dictionary<string,int>();
                List<string> bundleJobs = new List<string>();

                // change bot equipment based on preferences
                try
                {
                    int pid = 0;
                    bot.Profiles.ForEach(profile =>
                    {
                        if (profile != null)
                        {   
                            if (friendlyPMC.squadSetup.Value && friendlyPMC.squadMembers.ContainsKey(pid))
                            {
                                string eq = friendlyPMC.squadMembers[pid][1].Value;
                                if (eq != null && eq != friendlyPMC.GetEquipOptions()[0])
                                {
                                    var secureContainer = profile.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer).ContainedItem;

                                    if (eq == friendlyPMC.GetEquipOptions()[1])
                                    {
                                        EquipmentClass equipClone = playerProfile.Inventory.Equipment.CloneItem(null);

                                        foreach (EquipmentSlot slotType in Enum.GetValues(typeof(EquipmentSlot)))
                                        {
                                            if (slotType == EquipmentSlot.SecuredContainer) continue;

                                            Slot cloneSlot = equipClone.GetSlot(slotType);
                                            Item contained = cloneSlot.ContainedItem;

                                            Slot botSlot = profile.Inventory.Equipment.GetSlot(slotType);

                                            botSlot.RemoveItem();

                                            if (contained != null)
                                            {
                                                contained.CurrentAddress = null;
                                                botSlot.AddWithoutRestrictions(contained);
                                            }
                                        }

                                        profile.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer).ChangeContainedItemDirectly(secureContainer);
                                        profile.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer).ApplyContainedItem();
                                    }
                                    else
                                    {
                                        secureContainers.Add(profile.Id, secureContainer.CloneItem());

                                        foreach (var preset in presets)
                                        {
                                            if (eq == preset.Name)
                                            {
                                                var equipment = preset.Equipment.CloneItem(null);

                                                if (!bundleJobs.Contains(preset.Name))
                                                {
                                                    foreach (var item in equipment.GetAllItems())
                                                    {
                                                        bundleTokens.Add(item.GetAllBundleTokens());
                                                    };
                                                    bundleJobs.Add(preset.Name);
                                                }

                                                profileEquipment.Add(profile.Id, equipment);
                                            }
                                        }
                                    }
                                }

                                string tactic = friendlyPMC.squadMembers[pid][0].Value;
                                string[] availableTactics = friendlyPMC.GetTacticOptions();
                                if (tactic != null && tactic != availableTactics[0])
                                {
                                    if(tactic == availableTactics[2])
                                    {
                                        tactic = "Push";
                                    } else if (tactic == availableTactics[3])
                                    {
                                        tactic = "Defend";
                                    } else if (tactic == availableTactics[1])
                                    {
                                        tactic = "Marksman";
                                        // some cheating here, making our marskman good
                                        profile.Skills.Sniper.SetCurrent(5100f, true);
                                        profile.Skills.RecoilControl.SetCurrent(4800f, true);
                                    }

                                    profileTactic.Add(profile.ProfileId, tactic);
                                }
                            }
                            // - else leave it random
                            pid++;
                        }
                    });
                } catch(Exception ex)
                {
                    Components.Logger.LogError("Failed to set squad equipment for a bot");
                    Components.Logger.LogError(ex);
                }
            }
            
            // if we have build presets we must ensure we load all their assets
            if(bundleTokens.Count > 0)
            {
                Components.Logger.LogInfo("Fetching presets assets");
                try
                {
                    List<UniTask> bundleTasks = new List<UniTask>();
                    foreach (var bundleToken in bundleTokens)
                    {
                        bundleTasks.Add(GClass1458.WaitForAllBundlesJob(bundleToken, new Action(GClass1947.Class1694.class1694_0.method_0), default(CancellationToken), null).AsUniTask());
                    }
                    await UniTask.WhenAll(bundleTasks);

                    bot.Profiles.ForEach(profile =>
                    {
                        if (profile != null)
                        {
                            if (profileEquipment.ContainsKey(profile.Id))
                            {
                                foreach (EquipmentSlot slotType in Enum.GetValues(typeof(EquipmentSlot)))
                                {
                                    Slot cloneSlot = profileEquipment[profile.Id].GetSlot(slotType);
                                    Item contained = cloneSlot.ContainedItem;

                                    Slot botSlot = profile.Inventory.Equipment.GetSlot(slotType);

                                    botSlot.RemoveItem();

                                    if (contained != null)
                                    {
                                        contained.CurrentAddress = null;
                                        botSlot.AddWithoutRestrictions(contained);
                                    }
                                }

                                if (secureContainers.ContainsKey(profile.Id))
                                {
                                    Slot secCon = profile.Inventory.Equipment.GetSlot(EquipmentSlot.SecuredContainer);
                                    secCon.RemoveItem();
                                    secureContainers[profile.Id].CurrentAddress = null;
                                    secCon.AddWithoutRestrictions(secureContainers[profile.Id]);
                                }

                            }
                        }
                    });

                } catch (Exception ex)
                {
                    Components.Logger.LogError("Failed to use custom presets, will fall back to default loadout");
                    Components.Logger.LogError(ex);
                }
            }

            Components.Logger.LogInfo("Spawn Followers");

            var closestCorePoint = GetClosestCorePoint(Controller, position);
            bot.AddPosition(position, closestCorePoint.Id);

            float spawnedFollowers = 0;

            Func<BotOwner, BotZone, BotsGroup> GroupAction = new Func<BotOwner, BotZone, BotsGroup>((BotOwner bt, BotZone zn) =>
            {
                return GetPlayerGroup(player, bt, zn);
            });

            bot.Profiles.ForEach(async profile =>
            {
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
                                bodyPart.Health.Minimum = 35 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = 35 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = 35 * friendlyPMC.heatlhMultiplier.Value;
                                break;
                            case EBodyPart.Chest:
                                bodyPart.Health.Minimum = 85 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = 85 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = 85 * friendlyPMC.heatlhMultiplier.Value;
                                break;
                            case EBodyPart.Stomach:
                                bodyPart.Health.Minimum = 70 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = 70 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = 70 * friendlyPMC.heatlhMultiplier.Value;
                                break;
                            case EBodyPart.RightArm:
                            case EBodyPart.LeftArm:
                                bodyPart.Health.Minimum = 60 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = 60 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = 60 * friendlyPMC.heatlhMultiplier.Value;
                                break;
                            case EBodyPart.RightLeg:
                            case EBodyPart.LeftLeg:
                                bodyPart.Health.Minimum = 65 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Maximum = 65 * friendlyPMC.heatlhMultiplier.Value;
                                bodyPart.Health.Current = 65 * friendlyPMC.heatlhMultiplier.Value;
                                break;

                            default:
                                break;
                        }
                    }
                }

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                Action<BotOwner> OnActivate = new Action<BotOwner>((BotOwner owner) =>
                {

                    bool shallBeGroup = bot.SpawnParams?.ShallBeGroup != null;

                    stopWatch.Start();

                    Action<BotOwner> OnBotState = new Action<BotOwner>((BotOwner me) =>
                    {
                        BotOwnerManualUpdatePatch.BotOwnerUpdate.Remove(me.ProfileId); // clear watcher
                        try
                        {
                            me.Memory.DeleteInfoAboutEnemy(player.Player()); // prevent attack of player on spawn
                            me.GetPlayer.ActiveHealthController.RestoreFullHealth(); // ensure bot has full health

                            string tactic = null;
                            profileTactic.TryGetValue(profile.ProfileId, out tactic);

                            if (tactic == null) tactic = "Default";

                            WildSpawnType botType = type;

                            if(profile.Info?.Settings?.Role != null)
                            {
                                botType = profile.Info.Settings.Role;
                            }

                            Components.Logger.LogInfo("Tactic is " + tactic);

                            BossPlayers.AddFollower(me, player, true, botType, tactic);

                        }
                        catch (Exception ex)
                        {
                            Components.Logger.LogError("Failed to add " + me.Profile.Nickname + " as follower");
                            Components.Logger.LogError(ex);
                        }
                    });

                    BotOwnerManualUpdatePatch.BotOwnerUpdate.Add(owner.ProfileId, OnBotState);

                    // force player side on the bot
                    if (owner.Side != side)
                    {
                        owner.GetPlayer.Profile.Info.Side = side;
                    }

                    BossPlayers.ShallBeFollower(owner);
                    botSpawnerClass.method_10(owner, bot, new Action<BotOwner>((BotOwner follower) =>
                    {

                        Components.Logger.LogInfo("Follower " + follower.Profile.Nickname + " spawned");

                        spawnedFollowers++;

                        if (spawnedFollowers >= memberCount)
                        {
                            token.Cancel();
                            bot.StopSpawn();
                        }


                        Utils.Utils.SetTimeout(() =>
                        {
                            follower.BotTalk.TrySay(EPhraseTrigger.Ready, false);
                        }, 2000);

                    }), shallBeGroup, stopWatch);

                });

                await ActivateBotFollower(
                    botCreator,
                    profile,
                    new GClass590(position, closestCorePoint.Id, false),
                    zone, true,
                    GroupAction,
                    OnActivate,
                    token.GetCancelToken()
                );

            });
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "AddActivePLayer");

        }
        [PatchPostfix]
        private static void PatchPostfix(BotsController __instance, Player player)
        {
            if(Controller == null) 
            {
                new BossPlayers();
                new InteractableObjects();
                new NpcMessage();
                new Receivers();
                new FollowerPatrolInstances();

                PingTeamates.Enable();

                friendlyPMC.Instance.GetEquipmentBuilds(); // ensure equipment is gathered
                
                Props.Reset();

                LocalGameVmethod4Patch.squadSpawned = false;

                Controller = __instance;

                string locationId = Singleton<GameWorld>.Instance.LocationId;

                if(locationId == "factory4_day" || locationId == "factory4_night")
                {
                    Props.FactoryMapSett();
                }

                Components.Logger.LogInfo("Raid Started");
            }

           
            pitAIBossPlayer playerBoss = BossPlayers.AddPlayerAsBoss(player);

            spawnedPlayers.Add(playerBoss);

            if (friendlyPMC.knightSpawn.Value)
            {
                if (friendlyPMC.justKnightSpawn.Value || friendlyPMC.birdEyeSpawn.Value || friendlyPMC.bigPipeSpawn.Value)
                {
                    Controller.BotSpawner.SetBlockedRoles(new string[] { "bossKnight", "followerBirdEye", "followerBigPipe" });
                }
                
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
    internal class LocalGameVmethod4Patch
    {
        [HarmonyPostfix]
        public static IEnumerator Postfix(IEnumerator __result, BaseLocalGame<EftGamePlayerOwner> __instance, BotControllerSettings controllerSettings, ISpawnSystem spawnSystem, Callback runCallback)
        {
            yield return __result;

            SpawnFollowers();

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
                Components.Logger.LogInfo("Start Squad Spawn");
                BotsControllerPatch.spawnedPlayers.ForEach(playerBoss =>
                {

                    UniTask squadSpanner = BotsControllerPatch.Instance.SpawnGroupBots(playerBoss);
                    if (!friendlyPMC.knightSpawn.Value)
                    {
                        squadSpanner.Forget();
                    }
                    else
                    {
                        squadSpawners.Add(squadSpanner);
                    }
                });
            }

            if (friendlyPMC.knightSpawn.Value)
            {
                Components.Logger.LogInfo("Start Boss Ally Spawn");

                UniTask.WhenAll(squadSpawners).ContinueWith(() =>
                {
                    BotsControllerPatch.spawnedPlayers.ForEach(playerBoss =>
                    {
                        try
                        {
                            BotsControllerPatch.Instance.SpawnBossFollower(playerBoss).Forget();
                        }
                        catch (Exception e) 
                        {  
                            Components.Logger.LogError("Failed to spawn Boss Ally");
                            Components.Logger.LogError(e);
                        }
                    });

                }).Forget();
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
            BotsControllerPatch.Controller = null;

            BotOwnerManualUpdatePatch.BotOwnerUpdate.Clear();

            PingTeamates.Disable();

            Enemy.ClearEnemiesLocations();
            Utils.Utils.FlagsClear();

            AIDataContructPatch.playerAIData.Clear();

            LocalGameVmethod4Patch.squadSpawned = false;

            LocalGameCtorPatch.Instance = null;

            Components.Logger.LogInfo("Raid Ended");

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
                
                Components.Logger.LogInfo("Raid CleanUp Finished");

            } catch (Exception ex)
            {
                Components.Logger.LogError("Raid CleanUp Failed");
                Components.Logger.LogError(ex);
            }

            return true;
        }
    }
}
