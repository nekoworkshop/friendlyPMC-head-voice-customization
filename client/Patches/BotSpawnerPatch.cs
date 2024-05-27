using Aki.Reflection.Patching;
using EFT;
using friendlyPMC.Components;
using HarmonyLib;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Patches
{


    internal class BotSpawnerAddPlayerPatch : ModulePatch
    {

        public static BotSpawnerAddPlayerPatch Instance;

        public BotSpawnerAddPlayerPatch()
        {
            if (Instance == null) Instance = this;
        }

        public async void SpawnBot(BotSpawner __instance, Player player)
        {
            Vector3 position = player.Position;
            EPlayerSide side = player.Side;

            float dist;
            BotZone zone = __instance.GetClosestZone(position, out dist);

            BossLocationSpawn bossLocationSpawn = new BossLocationSpawn();
            bossLocationSpawn.BossZone = zone.NameZone;
            bossLocationSpawn.Time = -1f;
            bossLocationSpawn.Delay = 0f;
            bossLocationSpawn.TriggerId = "";
            bossLocationSpawn.TriggerName = "";
            bossLocationSpawn.BossChance = 100f;
            bossLocationSpawn.BossName = "sptBear";
            bossLocationSpawn.BossDifficult = BotDifficulty.normal.ToString();
            bossLocationSpawn.BossEscortAmount = 0.ToString();
            bossLocationSpawn.BossEscortDifficult = BotDifficulty.normal.ToString();
            bossLocationSpawn.BossEscortType = WildSpawnType.followerBully.ToString();
            bossLocationSpawn.ParseMainTypesTypes();
            bossLocationSpawn.ForceSpawn = true;
            bossLocationSpawn.IgnoreMaxBots = true;

            await __instance.BossSpawner.Spawn(bossLocationSpawn, new BotSpawnParams());
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotSpawner), "AddPlayer");
        }

        [PatchPostfix]
        private static void PatchPostfix(BotSpawner __instance, Player player)
        {
            BossPlayer.Instance.AddBossPlayer(player.ProfileId, player);

            // spawn a friendly bot
            Task.Delay(2000).ContinueWith(t =>
            {
                Components.Logger.LogInfo("Spawn a friendly");
                Instance.SpawnBot(__instance, player);
            });

        }
    }
}
