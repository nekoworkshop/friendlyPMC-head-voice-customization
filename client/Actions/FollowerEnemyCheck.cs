using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using System;
using UnityEngine;
namespace friendlyPMC.Actions
{
    public class FollowerEnemyCheck
    {

        public static void CheckBossReport(BotOwner bot)
        {

            Player closest = InteractableObjects.GetClosestSeenEnemy();

            if (bot.BotState == EBotState.Active && !bot.IsDead)
            {
                // if boss did not see anyone, look in the direction he was looking
                if (closest == null)
                {
                    FollowerBrain brain = bot.Brain.BaseBrain as FollowerBrain;
                    if (brain != null && bot.BotFollower.HaveBoss)
                    {
                        Vector3 bossPosition = bot.BotFollower.BossToFollow.Player().Transform.position;
                        Vector3 bossLookDirection = bot.BotFollower.BossToFollow.Player().LookDirection;
                        
                        brain.FakeShot(bossPosition + bossLookDirection.normalized * 50f);
                    }
                }
                else
                {
                    Modules.Logger.LogInfo("Player has seen " + closest.Profile.Nickname);
                    try
                    {
                        if (bot.Memory.HaveEnemy && bot.Memory.GoalEnemy.ProfileId == closest.Profile.ProfileId) return;

                        EnemyInfo info = Utils.Enemy.MakeEnemy(bot, closest);

                        if (info != null && !bot.Memory.HaveEnemy && !bot.Medecine.FirstAid.Using && !bot.Medecine.SurgicalKit.Using)
                        {
                            info.PriorityIndex = 0;
                            info.SetVisible(true);
                            bot.Memory.GoalEnemy = info;

                            Modules.Logger.LogInfo("Made " + closest.Profile.Nickname + " an active enemy to " + bot.Profile.Nickname);
                        }
                        else if (info == null)
                        {
                            Modules.Logger.LogInfo("Cannot make " + bot.Profile.Nickname + " an active enemy");
                        }
                    }
                    catch (Exception e)
                    {
                        Modules.Logger.LogInfo("Failed to accquire reported enemy:");
                        Modules.Logger.LogInfo(e.StackTrace);
                    }
                }
            }

            bot.CalcGoal();
        }
    }
}
