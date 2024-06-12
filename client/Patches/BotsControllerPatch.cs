using Aki.PrePatch;
using Aki.Reflection.Patching;
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


using BotCacheClass = GClass591;
using IProfileData = GClass592;
using Comfort.Common;

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

        public BotsControllerPatch()
        {
            if (Instance == null) Instance = this;
        }


        private AICorePoint GetClosestCorePoint(BotsController _botsController,Vector3 position)
        {
            var botGame = Singleton<IBotGame>.Instance;
            var coversData = _botsController.CoversData;
            var groupPoint = coversData.GetClosest(position);
            return groupPoint.CorePointInGame;
        }


        private async UniTask SpawnGroupBots(BotsController _botsController,pitAIBossPlayer player)
        {
            float dist;

            CancelToken token = new CancelToken();

            var botSpawnerClass = _botsController.BotSpawner;
            var botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;

            var botGame = AccessTools.Field(typeof(BotSpawner), "_game").GetValue(botSpawnerClass) as IBotGame;
            var deadBodiesController = AccessTools.Field(typeof(BotSpawner), "_deadBodiesController").GetValue(botSpawnerClass) as DeadBodiesController;
            var allPlayers = AccessTools.Field(typeof(BotSpawner), "_allPlayers").GetValue(botSpawnerClass) as List<Player>;
            var spawnGroups = AccessTools.Field(typeof(BotSpawner), "_groups").GetValue(botSpawnerClass) as BotZoneGroupsDictionary;
            var allBotZones = AccessTools.Field(typeof(BotSpawner), "_allBotZones").GetValue(botSpawnerClass) as BotZone[];
            

            Vector3 position = player.Position;
            EPlayerSide side = player.Player().Side;

            BotZone zone = botSpawnerClass.GetClosestZone(position, out dist);

            WildSpawnType sptBear = (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
            WildSpawnType sptUsec = (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;

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

            BotCacheClass bot = await BotCacheClass.Create(botData, botCreator, memberCount, botSpawnerClass);
            
            // copy player equipment
            if (friendlyPMC.copyEquip.Value) bot.Profiles.ForEach(profile =>
            {
                if (profile != null)
                {
                    profile.Inventory.Equipment = player.Player().Profile.Inventory.Equipment.CloneItem(null);
                }
            });

            var closestCorePoint = GetClosestCorePoint(_botsController,position);
            bot.AddPosition(position, closestCorePoint.Id);

            Stopwatch stopWatch = new Stopwatch();


            Components.Logger.LogInfo("Spawn followers");


            BotsGroup followerGroup = null;

            botCreator.ActivateBot(bot, zone, true, new Func<BotOwner, BotZone, BotsGroup>((BotOwner bt, BotZone zn) =>
            {
                if (followerGroup == null)
                {
                    BotsGroup group = botSpawnerClass.GetGroupAndSetEnemies(bt, zn);
                    followerGroup = group;
                }

                return followerGroup;
            }), new Action<BotOwner>((BotOwner owner) =>
            {
                bool shallBeGroup = bot.SpawnParams?.ShallBeGroup != null;


                stopWatch.Start();
                botSpawnerClass.method_10(owner, bot, new Action<BotOwner>((BotOwner follower)=>
                {
                    Components.Logger.LogInfo("Follower " + follower.Profile.Nickname + " ready");
                    var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(2.0), false);

                    Timer.OnTimer += () =>
                    {
                        if(followerGroup != null && player.bossGroup == null)
                        {
                            player.bossGroup = followerGroup;
                            BossPlayers.Instance.AddFollowerGroup(followerGroup.Id);

                            player.bossGroup.AddAlly(player.realPlayer);
                            player.bossGroup.Lock();

                            player.bossGroup.AnyBodyShootImmediately = true;

                            player.bossGroup.OnEnemyAdd += (IPlayer pl, EBotEnemyCause cause)=>
                            {
                                if (pl != null && player.Player().ProfileId == pl.ProfileId)
                                {
                                    player.bossGroup.RemoveEnemy(player.Player());
                                    player.bossGroup.AddAlly(player.realPlayer);
                                }
                            };
                        }

                        BossPlayers.Instance.AddFollower(follower,player);

                        follower.BotTalk.TrySay(EPhraseTrigger.Ready,false);
                    };

                }) , shallBeGroup, stopWatch );

                Components.Logger.LogInfo("Activating followers");

            }), token.GetCancelToken());

        }



        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotsController), "AddActivePLayer");

        }
        [PatchPrefix]
        private static bool PatchPrefix(BotsController __instance, Player player)
        {
            new BossPlayers();
            new InteractableObjects();
            new Receivers();
            new FollowerPatrolInstances();
            Components.Logger.LogInfo("Raid Started");

            
            float dist;
            Vector3 position = player.Transform.position;

            BotZone zone = __instance.GetClosestZone(position, out dist);

            pitAIBossPlayer playerBoss = BossPlayers.Instance.AddBossPlayer(player);

            // spawn followers
            if (friendlyPMC.squadSpawn.Value)
            {
                var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(12.0), false);

                Timer.OnTimer += () =>
                {
                    Instance.SpawnGroupBots(__instance, playerBoss).Forget();
                };
            }

            return true;
        }
    }

    internal class LocalGamePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(LocalGame), "Stop");

        }
        [PatchPrefix]
        private static bool PatchPrefix(LocalGame __instance, string profileId, ExitStatus exitStatus, string exitName, float delay = 0f)
        {
            BossPlayers.Dispose();
            InteractableObjects.Dispose();
            Receivers.Dispose();
            FollowerPatrolInstances.Dispose();

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
            } catch (Exception ex)
            {
                Components.Logger.LogInfo("CleanUp Failed :" + ex.Message);
            }

            return true;
        }
    }
}