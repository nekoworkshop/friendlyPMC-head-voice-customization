
using Aki.Reflection.Patching;
using Aki.PrePatch;

using HarmonyLib;

using UnityEngine;
using UnityEngine.AI;

using EFT;

using System.Threading;
using System;
using System.Reflection;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

using friendlyPMC.Components;
using friendlyPMC.Modules;

using BotCacheClass = GClass591;
using IProfileData = GClass592;
using System.Collections.Generic;


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

        private void ActivateBot(BotSpawner __instance,IBotCreator botCreator, BotZone botZone, BotCacheClass botData, CancellationToken cancelToken, pitAIBossPlayer player)
        {

            BotSpawner.Class934 data = new BotSpawner.Class934();
  
            data.botSpawner_0 = __instance;
            data.data = botData;
            data.stopWatch = new Stopwatch();
            data.callback = new Action<BotOwner>((BotOwner bot) => {

                BossPlayers.Instance.AddFollower(bot, player);

                Components.Logger.LogInfo("Spawn: spawned bot " + bot.Profile.Nickname);

            });

            data.stopWatch.Start();

            data.shallBeGroup = (data.data.SpawnParams != null && data.data.SpawnParams.ShallBeGroup != null && data.data.SpawnParams.ShallBeGroup.Group && data.data.SpawnParams.ShallBeGroup.RemainCount > 0);

            if (data.shallBeGroup)
            {
                data.data.SpawnParams.ShallBeGroup.DescreaseCount();
            }

            botCreator.ActivateBot(botData, botZone, data.shallBeGroup, __instance.GetGroupAndSetEnemies, data.method_0, cancelToken);

        }

        public void SpawnBot(BotSpawner __instance, pitAIBossPlayer player)
        {

            try
            {
                float dist;

                Vector3 position = player.Position;
                EPlayerSide side = player.Player().Side;

                BotZone zone = __instance.GetClosestZone(position, out dist);
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

                IProfileData botData = new IProfileData(side, type, BotDifficulty.hard, 5f, null);

                var task = __instance.ActivateBotsWithoutWave(2, botData);
                task.GetAwaiter().OnCompleted(() =>
                {
                    if (task.IsFaulted)
                    {
                        Components.Logger.LogInfo($"SpawnError: Task failed with exception: {task.Exception}");
                    }
                    else if (task.IsCanceled)
                    {
                        Components.Logger.LogInfo("SpawnError: Task was canceled");
                    }
                });

            } catch (Exception ex)
            {
                Components.Logger.LogInfo($"SpawnError : {ex.Message}");
            }
            /*try
            {


                float dist;

                Vector3 position = player.Position;
                EPlayerSide side = player.Player().Side;

                BotZone zone = __instance.GetClosestZone(position, out dist);
                WildSpawnType sptBear = (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
                WildSpawnType sptUsec = (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;

                var coversData = __instance.BotGame.BotsController.CoversData;
                var groupPoint = coversData.GetClosest(position);
                var closestCorePoint = groupPoint.CorePointInGame;

                var boCreator = (IBotCreator)AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(__instance);
                if (boCreator == null)
                {
                    Components.Logger.LogInfo("SpawnError: boCreator is null");
                    return;
                }

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

                IProfileData botData = new IProfileData(side, type, BotDifficulty.hard, 10f, null);

                CancelToken token = new CancelToken();

                Components.Logger.LogInfo("Spawn: Preparing to spawn");

                var syncContext = new SynchronizationContext();

                syncContext.Post(_ =>
                {
                    Task<BotCacheClass> botCreate = BotCacheClass.Create(botData, boCreator, 1, token);
                    
                    botCreate.ConfigureAwait(false);

                    botCreate.ContinueWith(task =>
                    {
                        if (task.IsFaulted)
                        {
                            Components.Logger.LogInfo($"SpawnError: Task failed with exception: {task.Exception}");
                        }
                        else if (task.IsCanceled)
                        {
                            Components.Logger.LogInfo("SpawnError: Task was canceled");
                        }
                        else
                        {
                            BotCacheClass bot = task.Result;
                            bot.AddPosition(position, closestCorePoint.Id);
                            Components.Logger.LogInfo("Spawn: Activate Bot");

                            ActivateBot(__instance, boCreator, zone, bot, token.GetCancelToken(), player);
                        }
                    });

                },null);
                


            } catch (Exception ex)
            {
                Components.Logger.LogInfo($"SpawnError : {ex.Message}");
            }*/
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "AddPlayer");
        }

        [PatchPostfix]
        private static void PatchPostfix(BotSpawner __instance, Player player)
        {
            pitAIBossPlayer playerBoss = BossPlayers.Instance.AddBossPlayer(player);
            // spawn a friendly bot
            Components.Logger.LogInfo("Spawn a friendly");

            Instance.SpawnBot(__instance, playerBoss);
        }
    }
}
