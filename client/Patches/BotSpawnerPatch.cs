
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
using Comfort.Common;
using System.Collections.Generic;
using static BoxFracture;
using UnityEngine.Profiling;
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

    internal class BotSpawnerAddPlayerPatch : ModulePatch
    {

        public static BotSpawnerAddPlayerPatch Instance;

        public BotSpawnerAddPlayerPatch()
        {
            if (Instance == null) Instance = this;
        }


        public void SpawnBossFollowers(BotSpawner __instance, pitAIBossPlayer player)
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

            IProfileData botData = new IProfileData(side, type, BotDifficulty.hard, 0f, null);

            CancelToken token = new CancelToken();

            var boCreator = (IBotCreator)AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(__instance);
            var spawnSystem = (ISpawnSystem)AccessTools.Field(typeof(BotSpawner), "_spawnSystem").GetValue(__instance); 
            if(boCreator != null && spawnSystem != null) {
                BossSpawnerClass BossSpawner = new BossSpawnerClass(spawnSystem, __instance, boCreator, new BotZone[] { zone });

                Components.Logger.LogInfo("Preparing to spawn followers");


                if (SynchronizationContext.Current == null)
                {

                    SynchronizationContext context = new SynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(context);

                }

                Task<BotCacheClass> botCreate = BotCacheClass.Create(botData, boCreator, 1, token);

                botCreate.ConfigureAwait(false);

                botCreate.ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Components.Logger.LogInfo($"Create Task failed with exception: {task.Exception}");
                        return null;
                    }
                    else if (task.IsCanceled)
                    {
                        Components.Logger.LogInfo("Create Task was canceled");
                        return null;
                    }
                    else
                    {
                        BotCacheClass bot = task.Result;
                        bot.AddPosition(position, closestCorePoint.Id);
                        Components.Logger.LogInfo("Activating followers");

                        Task newTask = BossSpawner.method_6(bot, null, zone, 1, botData, (BotOwner bt) =>
                        {
                            Components.Logger.LogInfo("Followers activated");
                        });

                        return newTask;
                    }
                }).Unwrap().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Components.Logger.LogInfo($"Spawn Task failed with exception: {task.Exception}");
                    }
                    else if (task.IsCanceled)
                    {
                        Components.Logger.LogInfo("Spawn Task was canceled");
                    }
                });

            }

            
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

            pitAIBossPlayer playerBoss = BossPlayers.Instance.AddBossPlayer(player,zone, __instance.BotGame);

            // spawn a friendly bot
            //Instance.SpawnBossFollowers(__instance, playerBoss);
        }
    }
}
