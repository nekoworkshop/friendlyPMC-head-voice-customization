
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

            BotWaveDataClass followerWave = new BotWaveDataClass();
            followerWave.BotsCount = 2;
            followerWave.Side = side;
            followerWave.Difficulty = BotDifficulty.hard;
            followerWave.WildSpawnType = type;
            followerWave.IsPlayers = false;
            followerWave.SpawnAreaName = zone.NameZone;
            followerWave.Time = 10f;
            followerWave.WithCheckMinMax = false;

            __instance.ActivateBotsByWave(followerWave);
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
