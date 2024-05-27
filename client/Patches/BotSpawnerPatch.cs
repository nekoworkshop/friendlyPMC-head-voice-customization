using Aki.Reflection.Patching;
using EFT;
using System;
using System.Threading.Tasks;
using System.Reflection;
using Comfort.Common;
using BepInEx.Logging;
using friendlyPMC.Components;
using System.Threading;
using UnityEngine;
using Aki.Common.Http;
using static RootMotion.FinalIK.IKSolver;
using UnityEngine.AI;
using HarmonyLib;
using EFT.Game.Spawning;
using EFT.Bots;
using System.Data;

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

            await __instance.method_1(side, zone,DebugBotProfileChooser.Auto,true);
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
            Task.Delay(8000).ContinueWith(t =>
            {
                Components.Logger.LogInfo("Spawn a friendly");
                Instance.SpawnBot(__instance, player);
            });

        }
    }
}
