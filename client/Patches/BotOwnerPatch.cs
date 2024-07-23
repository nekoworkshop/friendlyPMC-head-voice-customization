using SPT.Reflection.Patching;
using EFT;

using friendlyPMC.Modules;

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using friendlyPMC.Components;


namespace friendlyPMC.Patches
{

    internal class BotOwnerDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "method_9");

        }
        /** if BOT is getting hit by a player BOSS of which it is a follower of, do not turn hostile **/
        [PatchPrefix]
        private static bool PatchPrefix(BotOwner __instance, DamageInfo damageInfo, EBodyPart bodyType, float damageReducedByArmor)
        {
            if (__instance != null && __instance.BotFollower != null && __instance.BotFollower.HaveBoss && BossPlayers.Instance.IsFollower(__instance) && damageInfo.Player != null)
            {

                AIBossPlayer player = BossPlayers.Instance.GetBossPlayer(damageInfo.Player.iPlayer.ProfileId);

                if (player != null && BossPlayers.Instance.IsFollower(__instance, player))
                {
                    // - yell "friendly fire"
                    __instance.BotTalk.TrySay(EPhraseTrigger.FriendlyFire);

                    __instance.StandBy.GetHit();
                    __instance.BotPersonalStats.GetHit(damageInfo, bodyType);
                    __instance.Memory.GetHit(damageInfo);

                    return false;
                }
            }

            return true;
        }
    }

    internal class BotOwnerIsFolowerPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "IsFollower");

        }
        /** Skip checking bot's role if we have made this bot a follower of a boss player **/
        [PatchPrefix]
        private static bool PatchPrefix(BotOwner __instance, ref bool __result)
        {   

            if (BossPlayers.Instance.IsFollower(__instance))
            {
                __result = true;
                return false;
            }
            return true;
        }
    }

    internal class BotOwnerManualUpdatePatch : ModulePatch
    {

        public static Dictionary<string, Action<BotOwner>> BotOwnerUpdate = new Dictionary<string, Action<BotOwner>>();
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "UpdateManual");
        }

        /** Prefix patch UpdateManual so that followers will not have goals and have enemies auto assigned **/
        /*[PatchPrefix]
        private static bool PatchPrefix(BotOwner __instance)
        {
            // followers should not have goals
            try
            {
                float _nextGetGoalTime = (float)AccessTools.Field(typeof(BotOwner), "_nextGetGoalTime").GetValue(__instance);

                if (_nextGetGoalTime <= Time.time)
                {

                    if (BossPlayers.Instance.IsFollower(__instance))
                    {

                        if (__instance.Memory.DangerData.HaveCloseDanger)
                        {
                            __instance.Memory.GoalTarget.Clear();
                            __instance.Memory.GoalEnemy = null;

                        }
                        else if (!__instance.Memory.HaveEnemy)
                        {
                            // check if any enemy is close enough for bot to hear and get next to it
                            EnemyInfo potentialEnemy = __instance.EnemyChooser.FindDangerEnemy();

                            if (
                                potentialEnemy != null &&
                                (
                                    Utils.Utils.GetNavDistance(__instance.GetPlayer.Transform.position, potentialEnemy.Person.Transform.position) < 35f
                                )
                            )
                            {
                                __instance.Memory.GoalEnemy = potentialEnemy; // needs testing
                            }
                        }

                        AccessTools.Field(typeof(BotOwner), "_nextGetGoalTime").SetValue(__instance, GClass531.Core.UPDATE_GOAL_TIMER_SEC + Time.time);
                    }
                }
            }
            catch (Exception e)
            {
                Components.Logger.LogInfo("Exception on BotOwner UpdateManual PatchPrefix: " + e.Message);
            }

            return true;
        }*/
        /** Patch on botOwner UpdateManual to allow us to execute custom code **/
        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            try
            {
                if (
                    __instance != null &&
                    __instance.BotState == EBotState.Active &&
                    __instance.GetPlayer != null &&
                    __instance.GetPlayer.HealthController != null &&
                    __instance.ProfileId != null &&
                    __instance.GetPlayer.HealthController.IsAlive
                )
                {
                    Action<BotOwner> OnUpdate;
                    BotOwnerUpdate.TryGetValue(__instance.ProfileId, out OnUpdate);
                    if (OnUpdate != null) OnUpdate(__instance);

                }
            }
            catch (Exception e)
            {
                Components.Logger.LogInfo("Exception on BotOwner UpdateManual PatchPostfix: " + e.Message);
            }
        }
    }
   
    internal class BotOwnerActivatePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BotOwner), "method_10");

        }
        /** Fix having followers be enemy of same side just because their roles where under ENEMY_BOT_TYPES **/
        [PatchPostfix]
        private static void PatchPostfix(BotOwner __instance)
        {
            if (BossPlayers.Instance.IsFollower(__instance)) return;

            Dictionary<string, pitAIBossPlayer> playerBosses = BossPlayers.Instance.GetBossPlayers();

            foreach (pitAIBossPlayer boss in playerBosses.Values)
            {
                var followers = BossPlayers.GetFollowersByBoss(boss.Player().ProfileId);

                if (followers.Count > 0)
                {
                    EPlayerSide bossSide = boss.Player().Side;
                    if (bossSide == __instance.Side)
                    {
                        var sett = __instance.Settings.FileSettings;
                        if (
                            (bossSide == EPlayerSide.Bear && !sett.Mind.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                            (bossSide == EPlayerSide.Usec && !sett.Mind.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack)) ||
                            (bossSide == EPlayerSide.Savage && !sett.Mind.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack))
                        )
                        {
                            foreach (var follower in followers)
                            {
                                var botPlayer = follower.GetBot().GetPlayer;

                                if (__instance.EnemiesController.IsEnemy(botPlayer))
                                {
                                    __instance.EnemiesController.Remove(botPlayer);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
