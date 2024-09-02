using EFT;
using friendlyPMC.Modules;
using System;

namespace friendlyPMC.Actions
{
    internal class FollowerEnemyCheck
    {

        public static void CheckBossReport(BotOwner bot)
        {

            Player closest = InteractableObjects.GetClosestSeenEnemy();

            if (closest != null && bot.BotState == EBotState.Active && !bot.IsDead)
            {
                Components.Logger.LogInfo("Player has seen " + closest.Profile.Nickname);
                try
                {
                    if (bot.Memory.HaveEnemy && bot.Memory.GoalEnemy.ProfileId == closest.Profile.ProfileId) return;

                    EnemyInfo info = Utils.Enemy.MakeEnemy(bot, closest);

                    if (info != null && !bot.Memory.HaveEnemy && !bot.Medecine.FirstAid.Using && !bot.Medecine.SurgicalKit.Using)
                    {
                        info.PriorityIndex = 0;
                        info.SetVisible(true);
                        bot.Memory.GoalEnemy = info;

                        Components.Logger.LogInfo("Made " + closest.Profile.Nickname + " an active enemy to " + bot.Profile.Nickname);
                    }
                    else if (info == null)
                    {
                        Components.Logger.LogInfo("Cannot make " + bot.Profile.Nickname + " an active enemy");
                    }
                } catch(Exception e)
                {
                    Components.Logger.LogInfo("Failed to accquire reported enemy:");
                    Components.Logger.LogInfo(e.StackTrace);
                }
            }
        }
    }
}
