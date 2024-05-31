
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
using DG.Tweening.Core.Easing;
using EFT.Game.Spawning;
using System.Linq;


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

            Components.Logger.LogInfo(zone.NameZone + " ; " + zone.ShortName);
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

            CancelToken token = new CancelToken();

            IProfileData botData = new IProfileData(side, type, BotDifficulty.hard, 0f, null);

            var boCreator = (IBotCreator)AccessTools.Field(typeof(BotSpawner), "_botCreator").GetValue(__instance);
            var spawnSystem = (ISpawnSystem)AccessTools.Field(typeof(BotSpawner), "_spawnSystem").GetValue(__instance);
            if (boCreator != null && spawnSystem != null)
            {
                if (SynchronizationContext.Current == null)
                {
                    SynchronizationContext context = new SynchronizationContext();
                    SynchronizationContext.SetSynchronizationContext(context);
                }
                
                Task<BotCacheClass> botCreate = BotCacheClass.Create(botData, boCreator, 2, token);

                botCreate.ConfigureAwait(false);
                botCreate.ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Components.Logger.LogInfo($"Create Task failed with exception: {task.Exception}");
                    }
                    else if (task.IsCanceled)
                    {
                        Components.Logger.LogInfo("Create Task was canceled");
                    }
                    else
                    {
                        var data = task.Result;
                        Components.Logger.LogInfo("Continuing creation");
                        ISpawnPoint[] array = spawnSystem.SelectAISpawnPoints(ESpawnCategory.Bot, data, zone, 2, null, ActionIfNotEnoughPoints.DuplicateIfAtLeastOne);
                        __instance.method_6(array.ToList<ISpawnPoint>(), zone, data, (BotOwner bot)=>{
                            Components.Logger.LogInfo("Bots created");
                        },token.GetCancelToken());
                        new GClass583(zone, 2, data);
                    }
                });

            }
                BotWaveDataClass followerWave = new BotWaveDataClass();
            followerWave.BotsCount = 2;
            followerWave.Side = side;
            followerWave.Difficulty = BotDifficulty.hard;
            followerWave.WildSpawnType = type;
            followerWave.IsPlayers = false;
            followerWave.SpawnAreaName = zone.NameZone;
            followerWave.Time = 11f;
            followerWave.WithCheckMinMax = false;

            try
            {
                __instance.BotGame.BotsController.ActivateBotsByWave(followerWave);
            } catch (Exception ex)
            {
                Components.Logger.LogInfo($"SpawnError: {ex.Message}");
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
            Instance.SpawnBossFollowers(__instance, playerBoss);
        }
    }
}
