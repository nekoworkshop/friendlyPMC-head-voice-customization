
using Aki.Reflection.Patching;
using Comfort.Common;

using friendlyPMC.Components;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

using BotCacheClass = GClass591;
using IProfileData = GClass592;


using EFT;
using Aki.PrePatch;
using System.Threading;
using System;
using UnityEngine.AI;
using System.Threading.Tasks;


namespace friendlyPMC.Patches
{


    internal class BotSpawnerAddPlayerPatch : ModulePatch
    {

        public static BotSpawnerAddPlayerPatch Instance;

        public BotSpawnerAddPlayerPatch()
        {
            if (Instance == null) Instance = this;
        }

        private void ActivateBot(BotSpawner __instance,IBotCreator botCreator, BotZone botZone, BotCacheClass botData, CancellationToken cancelToken, Vector3 moveTo)
        {
 
            botCreator.ActivateBot(botData, botZone, false, new Func<BotOwner, BotZone, BotsGroup>((BotOwner bot, BotZone zone) =>
            {
                return __instance.GetGroupAndSetEnemies(bot, zone);

            }), new Action<BotOwner>((BotOwner bot) => {
                
                // send the friendly to the user, for now
                NavMeshPath path = new NavMeshPath();
                if (NavMesh.CalculatePath(moveTo, bot.Position, -1, path))
                {
                    bot.GetPlayer.Teleport(moveTo, true);
                }

                Logger.LogInfo("Spawned bot " + bot.Profile.Nickname);

            }), cancelToken);

        }

        public async void SpawnBot(BotSpawner __instance, pitAIBossPlayer player)
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

            if(boCreator == null)
            {
                Logger.LogInfo("boCreator is NULL");
            }

            IProfileData botData = new IProfileData(side, side == EPlayerSide.Bear ? sptBear : sptUsec, BotDifficulty.hard, 0f, null);
            BotCacheClass bot = await BotCacheClass.Create(botData, boCreator, 1,__instance);

           
            bot.AddPosition(position, closestCorePoint.Id);
            
            Logger.LogInfo("Activate Bot");

            new CancellationTokenSource();

            ActivateBot(__instance,boCreator, zone, bot, __instance.GetCancelToken(), position);
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "AddPlayer");
        }

        [PatchPostfix]
        private static void PatchPostfix(BotSpawner __instance, Player player)
        {
            pitAIBossPlayer playerBoss = BossPlayer.Instance.AddBossPlayer(player);
            // spawn a friendly bot
            Task.Delay(5000).ContinueWith(t =>
            {
                Components.Logger.LogInfo("Spawn a friendly");
                Instance.SpawnBot(__instance, playerBoss);
            });

        }
    }
}
