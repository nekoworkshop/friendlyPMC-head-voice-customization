
using Aki.Reflection.Patching;
using Aki.PrePatch;

using HarmonyLib;

using UnityEngine;
using Cysharp.Threading.Tasks;

using EFT;

using System.Threading;
using System;
using System.Reflection;
using System.Diagnostics;


using friendlyPMC.Components;
using friendlyPMC.Modules;

using BotCacheClass = GClass591;
using IProfileData = GClass592;
using Comfort.Common;
using System.Collections.Generic;
using EFT.InventoryLogic;




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

    internal class BotSpawnerAddPlayerPatch : ModulePatch
    {

        public static BotSpawnerAddPlayerPatch Instance;

        public BotSpawnerAddPlayerPatch()
        {
            if (Instance == null) Instance = this;
        }

        private AICorePoint GetClosestCorePoint(Vector3 position)
        {
            var botGame = Singleton<IBotGame>.Instance;
            var coversData = botGame.BotsController.CoversData;
            var groupPoint = coversData.GetClosest(position);
            return groupPoint.CorePointInGame;
        }


        private async UniTask SpawnGroupBots(pitAIBossPlayer player)
        {
            float dist;

            var botSpawnerClass = Singleton<IBotGame>.Instance.BotsController.BotSpawner;
            var botCreator = AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(botSpawnerClass) as IBotCreator;
            var cancellationTokenSource = AccessTools.Field(typeof(BotSpawner), "_cancellationTokenSource").GetValue(botSpawnerClass) as CancellationTokenSource;
            var botGame = AccessTools.Field(typeof(BotSpawner), "_game").GetValue(botSpawnerClass) as IBotGame;
            var deadBodiesController = AccessTools.Field(typeof(BotSpawner), "_deadBodiesController").GetValue(botSpawnerClass) as DeadBodiesController;
            var allPlayers = AccessTools.Field(typeof(BotSpawner), "_allPlayers").GetValue(botSpawnerClass) as List<Player>;
            var spawnGroups = AccessTools.Field(typeof(BotSpawner), "_groups").GetValue(botSpawnerClass) as BotZoneGroupsDictionary;
            var allBotZones = AccessTools.Field(typeof(BotSpawner), "_allBotZones").GetValue(botSpawnerClass) as BotZone[];

            var method10 = AccessTools.Method(typeof(BotSpawner), "method_10");

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
            @params.ShallBeGroup = new ShallBeGroupParams(true,false, memberCount);

            IProfileData botData = new IProfileData(side, type, BotDifficulty.hard, 0f, @params);

            BotCacheClass bot = await BotCacheClass.Create(botData, botCreator, memberCount, botSpawnerClass);
            // copy player equipment
            if(friendlyPMC.copyEquip.Value) bot.Profiles.ForEach(profile =>
            {
                if (profile != null)
                {
                    profile.Inventory.Equipment = player.Player().Profile.Inventory.Equipment.CloneItem(null);
                }
            });

            var closestCorePoint = GetClosestCorePoint(position);
            bot.AddPosition(position, closestCorePoint.Id);

            Stopwatch stopWatch = new Stopwatch();


            Components.Logger.LogInfo("Spawn followers");


            BotsGroup followerGroup = null;

            botCreator.ActivateBot(bot, zone, true, new Func<BotOwner, BotZone, BotsGroup>((BotOwner bt, BotZone zn) =>
            {
                if(followerGroup == null)
                {
                    BotsGroup group = botSpawnerClass.GetGroupAndSetEnemies(bt, zn);
                    followerGroup = group;
                }

                return followerGroup;
            }), new Action<BotOwner>((BotOwner owner) =>
            {
                bool shallBeGroup = bot.SpawnParams?.ShallBeGroup != null;


                stopWatch.Start();
                method10.Invoke(botSpawnerClass, new object[] { owner, bot, new Action<BotOwner>((BotOwner follower)=>
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
                    
                }) , shallBeGroup, stopWatch });
                
                Components.Logger.LogInfo("Activating followers");

            }), cancellationTokenSource.Token);

        }
        

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "AddPlayer");
        }

        [PatchPostfix]
        private static void PatchPostfix(BotSpawner __instance, Player player)
        {
            float dist;
            Vector3 position = player.Transform.position;

            BotZone zone = __instance.GetClosestZone(position, out dist);

            pitAIBossPlayer playerBoss = BossPlayers.Instance.AddBossPlayer(player);

            // spawn a friendly bot
            if (friendlyPMC.squadSpawn.Value)
            {
                var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(10.0), false);

                Timer.OnTimer += () =>
                {
                    Instance.SpawnGroupBots(playerBoss).Forget();
                };
            }
            
        }
    }
}
