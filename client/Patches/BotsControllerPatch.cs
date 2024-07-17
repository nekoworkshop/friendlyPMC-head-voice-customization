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


using IProfileData = GClass592;

using friendlyPMC.Utils;
using Comfort.Common;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Security.Policy;



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

            var allBotZones = AccessTools.Field(typeof(BotSpawner), "_allBotZones").GetValue(botSpawnerClass) as BotZone[];
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
            WildSpawnType role = bt.Profile.Info.Settings.Role;
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
            player.bossGroup = botsGroup;

            BossPlayers.Instance.AddFollowerGroup(player.bossGroup.Id);
            player.bossGroup.Lock();

            player.bossGroup.OnEnemyAdd += (IPlayer pl, EBotEnemyCause cause) =>
            {
                if (pl != null)
                {
                    if (player.Player().ProfileId == pl.ProfileId)
                    {
                        player.bossGroup.RemoveEnemy(player.Player());
                        player.bossGroup.AddAlly(player.realPlayer);
                    }
                    else if (pl.IsAI && player.bossGroup.Contains(pl.AIData.BotOwner))
                    {
                        player.bossGroup.RemoveEnemy(pl);
                        player.bossGroup.AddAlly(pl.AIData.Player);
                    }
                }
            };

            // revert changes
            bt.Settings.FileSettings.Mind.USE_ADD_TO_ENEMY_VALIDATION = false;
            bt.Settings.FileSettings.Mind.VALID_REASONS_TO_ADD_ENEMY = old_reasons;
            bt.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR = oldBehaviourBear;
            bt.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR = oldBehaviorUsec;
            bt.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR = oldBehaviorSavage;

            return player.bossGroup;
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

        public async UniTask ActivateBot(GClass814 botCreator, Profile profile, Vector3 position, int pointId,  BotZone zone, Func<BotOwner, BotZone, BotsGroup>groupAction, Action<BotOwner> callback)
        {
            LocalGame game = LocalGameCtorPatch.Instance;

            var botSpawnerClass = Controller.BotSpawner;

            var botGame = AccessTools.Field(typeof(BotSpawner), "_game").GetValue(botSpawnerClass) as IBotGame;

            var dictionary_2 = AccessTools.Field(typeof(LocalGame), "dictionary_2").GetValue(game) as Dictionary<string, Player>;

            // recreation of ActivateBot from GClass814
            GClass814.Class509 @class = new GClass814.Class509();
            @class.gclass814_0 = botCreator;
            @class.zone = zone;

            @class.callback = callback;

            @class.groupAction = groupAction;



            GClass590 bornInfo = new GClass590(position, pointId, false);
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
        public async UniTask SpawnBossFollower(pitAIBossPlayer player, WildSpawnType boss = WildSpawnType.bossKnight, CancelToken cancelToken = null)
        {
            
            float dist;

            CancelToken token = cancelToken != null ? cancelToken :  new CancelToken();

            var botSpawnerClass = Controller.BotSpawner;
            var botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as GClass814;

            Vector3 position = player.Position;
            EPlayerSide side = player.Player().Side;


            BotZone zone = botSpawnerClass.GetClosestZone(position, out dist);

            BotSpawnParams @params = new BotSpawnParams();
            @params.ShallBeGroup = new ShallBeGroupParams(true, false, 4);

            IProfileData botData = new IProfileData(side, boss, BotDifficulty.hard, 0f, @params);
            List<IProfileData> bossFollowers = new List<IProfileData> { };;

            if (boss == WildSpawnType.bossKnight)
            {
                
                if(friendlyPMC.bigPipeSpawn.Value) bossFollowers.Add(new IProfileData(side,WildSpawnType.followerBigPipe,BotDifficulty.hard,0f,@params));
                if (friendlyPMC.birdEyeSpawn.Value) bossFollowers.Add(new IProfileData(side, WildSpawnType.followerBirdEye, BotDifficulty.impossible, 0f, @params));
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

                    ActivateBot(botCreator, profile, position, closestCorePoint.Id, zone,
                        new Func<BotOwner, BotZone, BotsGroup>((BotOwner bt, BotZone zn) =>
                        {
                            return GetPlayerGroup(player, bt, zn);
                        }),
                        new Action<BotOwner>((BotOwner owner) =>
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

                                    // restore original boss logic
                                    /*if(me.Boss != null && me.Boss.BossLogic != null)
                                        me.Boss.BossLogic.Dispose();*/

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

                                    BossPlayers.Instance.AddFollower(me, player, false, botRole); // make bot a follower

                                    var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(1), false);

                                    Timer.OnTimer += () =>
                                    {
                                        if (spanwers.Count > 0)
                                        {
                                            spanwers[spanwers.Count - 1].Invoke();
                                            spanwers.RemoveAt(spanwers.Count - 1);
                                        }
                                        else
                                        {
                                            token.Cancel();
                                        }
                                    };
                                }
                                catch (Exception ex)
                                {
                                    Components.Logger.LogInfo("Failed to add " + me.Profile.Nickname + " as ally: " + ex.Message);
                                    Components.Logger.LogInfo("Trace : " + ex.StackTrace);
                                }
                            });

                            BotOwnerManualUpdatePatch.BotOwnerUpdate.Add(owner.ProfileId, OnBotState);

                            // force player side on the bot
                            if (owner.Side != side)
                            {
                                owner.GetPlayer.Profile.Info.Side = side;
                            }

                            botSpawnerClass.method_10(owner, bot, new Action<BotOwner>((BotOwner follower) =>
                            {
                                Components.Logger.LogInfo("Ally " + follower.Profile.Nickname + " spawned");

                                var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(2), false);

                                Timer.OnTimer += () =>
                                {
                                    follower.BotTalk.TrySay(EPhraseTrigger.Ready, false);
                                };

                            }), true, stopWatch);

                        })
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
            var botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as GClass814;
           

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
            @params.ShallBeGroup = new ShallBeGroupParams(true, false, memberCount);

            IProfileData botData = new IProfileData(side, type, BotDifficulty.hard, 0f, @params);

            BotCreationDataClass bot = await BotCreationDataClass.Create(botData, botCreator, memberCount, botSpawnerClass);

            // copy player equipment
            if (friendlyPMC.copyEquip.Value && side != EPlayerSide.Savage) bot.Profiles.ForEach(profile =>
            {
                if (profile != null)
                {
                    profile.Inventory.Equipment = player.Player().Profile.Inventory.Equipment.CloneItem(null);
                }
            });

            List<Action> spanwers = new List<Action>();

            Components.Logger.LogInfo("Spawn Followers");

            var closestCorePoint = GetClosestCorePoint(Controller, position);
            bot.AddPosition(position, closestCorePoint.Id);

            float spawnedFollowers = 0;

            bot.Profiles.ForEach(async profile =>
            {
                // followers should use the same groupID as the player
                profile.Info.GroupId = player.realPlayer.GroupId;
                // spawned followers will have a different health than the rest
                foreach (EBodyPart part in Enum.GetValues(typeof(EBodyPart)))
                {
                    profile.Health.BodyParts.TryGetValue(part, out var bodyPart);
                    if (bodyPart != null)
                    {
                        switch (part)
                        {
                            case EBodyPart.Head:
                                bodyPart.Health.Minimum = 42;
                                bodyPart.Health.Maximum = 42;
                                bodyPart.Health.Current = 42;
                                break;
                            case EBodyPart.Chest:
                            case EBodyPart.Stomach:
                                bodyPart.Health.Minimum = 150;
                                bodyPart.Health.Maximum = 150;
                                bodyPart.Health.Current = 150;
                                break;
                            case EBodyPart.RightArm:
                            case EBodyPart.LeftArm:
                                bodyPart.Health.Minimum = 100;
                                bodyPart.Health.Maximum = 100;
                                bodyPart.Health.Current = 100;
                                break;
                            case EBodyPart.RightLeg:
                            case EBodyPart.LeftLeg:
                                bodyPart.Health.Minimum = 110;
                                bodyPart.Health.Maximum = 110;
                                bodyPart.Health.Current = 110;
                                break;

                            default:
                                break;
                        }
                    }
                }

                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();

                await ActivateBot(
                    botCreator, profile, position, closestCorePoint.Id, zone,
                    new Func<BotOwner, BotZone, BotsGroup>((BotOwner bt, BotZone zn) =>
                    {
                        Components.Logger.LogInfo("Followers group get");
                        return GetPlayerGroup(player, bt, zn);

                    }), new Action<BotOwner>((BotOwner owner) =>
                    {
                        Components.Logger.LogInfo("Folowers callback");

                        bool shallBeGroup = bot.SpawnParams?.ShallBeGroup != null;

                        stopWatch.Start();

                        Action<BotOwner> OnBotState = new Action<BotOwner>((BotOwner me) =>
                        {
                            try
                            {
                                BotOwnerManualUpdatePatch.BotOwnerUpdate.Remove(me.ProfileId); // clear watcher

                                me.Memory.DeleteInfoAboutEnemy(player.Player()); // prevent attack of player on spawn

                                BossPlayers.Instance.AddFollower(me, player, true); // make bot a follower

                                me.GetPlayer.ActiveHealthController.RestoreFullHealth(); // ensure bot has full health

                                if (side == EPlayerSide.Savage)
                                {
                                    (me.Brain.BaseBrain as FollowerBrain).SetBossTactic("ally");
                                }
                            }
                            catch (Exception ex)
                            {
                                Components.Logger.LogInfo("Failed to add " + me.Profile.Nickname + " as follower : " + ex.Message);
                                Components.Logger.LogInfo("Trace: " + ex.StackTrace);
                            }
                        });

                        BotOwnerManualUpdatePatch.BotOwnerUpdate.Add(owner.ProfileId, OnBotState);

                        // force player side on the bot
                        if (owner.Side != side)
                        {
                            owner.GetPlayer.Profile.Info.Side = side;
                        }


                        botSpawnerClass.method_10(owner, bot, new Action<BotOwner>((BotOwner follower) =>
                        {

                            Components.Logger.LogInfo("Follower " + follower.Profile.Nickname + " spawned");

                            spawnedFollowers++;
                            if (spawnedFollowers >= memberCount)
                            {
                                token.Cancel();
                            }

                            var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(2), false);

                            Timer.OnTimer += () =>
                            {
                                follower.BotTalk.TrySay(EPhraseTrigger.Ready, false);
                            };

                        }), false, stopWatch);

                    })
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
                new Receivers();
                new FollowerPatrolInstances();

                PingTeamates.Enable();

                Components.Logger.LogInfo("Raid Started");

                Controller = __instance;
            }

           
            pitAIBossPlayer playerBoss = BossPlayers.AddBoss(player);
            spawnedPlayers.Add(playerBoss);

            if (friendlyPMC.knightSpawn.Value)
            {
                if (friendlyPMC.justKnightSpawn.Value || friendlyPMC.birdEyeSpawn.Value || friendlyPMC.bigPipeSpawn.Value)
                {
                    Controller.BotSpawner.SetBlockedRoles(new string[] { "bossKnight", "followerBirdEye", "followerBigPipe" });
                }
                
            }

            if (friendlyPMC.alternativeSpawn.Value == true)
            {
                var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(friendlyPMC.squadDelay.Value), false);
                Timer.OnTimer += () =>
                {
                    try
                    {
                        Components.Logger.LogInfo("Start Squad Spawn");
                        Instance.SpawnGroupBots(playerBoss).Forget();
                    }
                    catch (Exception e) { Components.Logger.LogInfo("Failed Alternative Spawn Process #1: " + e.Message); }
                };


                if (friendlyPMC.knightSpawn.Value)
                {
                    var Timer2 = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(friendlyPMC.squadDelay.Value + 1), false);
                    Timer2.OnTimer += () =>
                    {
                        try
                        {
                            Components.Logger.LogInfo("Start Boss Ally Spawn");
                            Instance.SpawnBossFollower(playerBoss).Forget();
                        }
                        catch (Exception e) { Components.Logger.LogInfo("Failed Alternative Spawn Process #2: " + e.Message); }
                    };
                    
                }
            }

        }
    }

    internal class WavesSpawnScenarioRunPatch : ModulePatch
    {
        public static bool spawnRan = false;

        public static void SpawnFollowers()
        {
            
            if (friendlyPMC.alternativeSpawn.Value == true) return;

            if (spawnRan) return;

            spawnRan = true;
            
            if (friendlyPMC.squadSpawn.Value)
            {
                Components.Logger.LogInfo("Start Squad Spawn");
                BotsControllerPatch.spawnedPlayers.ForEach(playerBoss =>
                {

                    if (BotsControllerPatch.Controller != null)
                    {
                        if (friendlyPMC.squadDelay.Value <= 0)
                        {
                            BotsControllerPatch.Instance.SpawnGroupBots(playerBoss).Forget();
                        } else
                        {
                            var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(friendlyPMC.squadDelay.Value), false);
                            Timer.OnTimer += () =>
                            {
                                try
                                {
                                    BotsControllerPatch.Instance.SpawnGroupBots(playerBoss).Forget();
                                }
                                catch(Exception e) { Components.Logger.LogInfo("Failed Delayed Squad Spawn Process " + e.Message); }
                            };
                        }
                    }
                });
            }

            if (friendlyPMC.knightSpawn.Value)
            {
                Components.Logger.LogInfo("Start Boss Ally Spawn");

                BotsControllerPatch.spawnedPlayers.ForEach(playerBoss =>
                {
                    var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(friendlyPMC.squadDelay.Value + 1), false);
                    Timer.OnTimer += () =>
                    {
                        try
                        {

                            BotsControllerPatch.Instance.SpawnBossFollower(playerBoss).Forget();
                        }
                        catch (Exception e) { Components.Logger.LogInfo("Failed Delayed Boss Ally Process " + e.Message); }
                    };
                });
            }
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(WavesSpawnScenario), "Run");
        }
        [PatchPostfix]
        private static void PatchPostfix(WavesSpawnScenario __instance, EBotsSpawnMode spawnMode = EBotsSpawnMode.Anyway)
        {
            SpawnFollowers();
        }
    }

    internal class NonWavesSpawnScenarioRunPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(NonWavesSpawnScenario), "Run");
        }
        [PatchPostfix]
        private static void PatchPostfix(NonWavesSpawnScenario __instance)
        {
            WavesSpawnScenarioRunPatch.SpawnFollowers();
        }
    }


    internal class Glass579RunPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BossSpawnWaveManagerClass), "Run");

        }
        [PatchPostfix]
        private static void PatchPostfix(BossSpawnWaveManagerClass __instance, EBotsSpawnMode spawnMode = EBotsSpawnMode.Anyway)
        {
            WavesSpawnScenarioRunPatch.SpawnFollowers();
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
            BossPlayers.Dispose();
            InteractableObjects.Dispose();
            Receivers.Dispose();
            FollowerPatrolInstances.Dispose();

            BotsControllerPatch.spawnedPlayers.Clear();
            BotsControllerPatch.Controller = null;

            WavesSpawnScenarioRunPatch.spawnRan = false;

            BotOwnerManualUpdatePatch.BotOwnerUpdate.Clear();

            PingTeamates.Disable();

            Utils.EnemyInfo.ClearEnemiesLocations();

            if (LocalGameCtorPatch.Instance != null) LocalGameCtorPatch.Instance = null;

            Components.Logger.LogInfo("Raid Ended");

            return true;
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
            } catch (Exception ex)
            {
                Components.Logger.LogInfo("CleanUp Failed :" + ex.Message);
            }

            Components.Logger.LogInfo("Raid CleanUp Finished");

            return true;
        }
    }
}