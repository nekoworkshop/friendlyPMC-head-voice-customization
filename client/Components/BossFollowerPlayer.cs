using Aki.PrePatch;
using EFT;

using HarmonyLib;
using LootingBots.Patch.Components;
using System;

using friendlyPMC.Components.BossFollower;
using friendlyPMC.Components.FollowerBossFollower;

namespace friendlyPMC.Components
{
    internal class BossFollowerPlayer : BotFollowerPlayer
    {
        public BossFollowerPlayer(BotOwner bot, pitAIBossPlayer player) : base(bot, player) {
            Components.Logger.LogInfo("Create boss follower");
        }

        public override void SetlFollowerSettings(BotOwner bot)
        {
            base.SetlFollowerSettings(bot);

            EPlayerSide side = _player.Player().Side;

            WildSpawnType sptBear = (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
            WildSpawnType sptUsec = (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;

            if (side != EPlayerSide.Savage && !bot.IsRole(sptBear) && !bot.IsRole(sptUsec))
            {
                bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = new WildSpawnType[] { };
                bot.Settings.FileSettings.Mind.WARN_BOT_TYPES = new WildSpawnType[] { };
                bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = new WildSpawnType[] { };

                foreach (WildSpawnType botType in Enum.GetValues(typeof(WildSpawnType)))
                {
                    if (side == EPlayerSide.Bear && botType == sptBear)
                    {
                        bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType);
                        continue;
                    }
                    else if (side == EPlayerSide.Usec && botType == sptUsec)
                    {
                        bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType);
                        continue;
                    } 
                    else
                    {
                        bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.AddItem(botType);
                    }
                }
            }

            bot.Tactic.AggressionChange(-1f);

            Components.Logger.LogInfo("Set boss settings");
        }

        public override FollowerBrain GetFollowerBrain(BotOwner bot, pitAIBossPlayer boss)
        {
            Components.Logger.LogInfo("Get boss brain");
            if(bot.IsRole(WildSpawnType.bossKnight))
            {
                return new KnightFollowerBrain(bot, boss);
            }

            if (bot.IsRole(WildSpawnType.followerBigPipe))
                return new BigPipeFollowerBrain(bot, boss);

            if (bot.IsRole(WildSpawnType.followerBirdEye))
                return new BirdEyeFollowerBrain(bot, boss);

            return new FollowerBrain(bot, boss);
        }
    }
}