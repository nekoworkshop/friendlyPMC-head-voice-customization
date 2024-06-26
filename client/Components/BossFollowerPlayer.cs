using Aki.PrePatch;
using EFT;

using HarmonyLib;
using LootingBots.Patch.Components;
using System;

using friendlyPMC.Components.BossFollower;
using friendlyPMC.Components.FollowerBossFollower;
using friendlyPMC.Actions;

namespace friendlyPMC.Components
{
    internal class BossFollowerPlayer : BotFollowerPlayer
    {

        public BossFollowerPlayer(BotOwner bot, pitAIBossPlayer player, WildSpawnType bossRole) : base(bot, player, false, bossRole) {

            if (bossRole == WildSpawnType.followerBirdEye)
            {
                FollowerPatrol patrol = FollowerPatrolInstances.GetPatrol(bot);
                patrol.SetReachDist(20f);
            }
        }

        public override void SetlFollowerSettings(BotOwner bot)
        {

            base.SetlFollowerSettings(bot);
            
            bot.Settings.FileSettings.Core.HearingSense = 1.0f;

            if (bot.IsRole(WildSpawnType.followerBirdEye))
            {
                bot.Settings.FileSettings.Core.GainSightCoef = 0.1f;
                bot.Settings.FileSettings.Cover.SOUND_TO_GET_SPOTTED = 10f;
                bot.Settings.FileSettings.Cover.SPOTTED_COVERS_RADIUS = 12f;
                bot.Settings.FileSettings.Shoot.LOW_DIST_TO_CHANGE_WEAPON = 30f;
                bot.Settings.FileSettings.Shoot.FAR_DIST_TO_CHANGE_WEAPON = 68f;
                bot.Settings.FileSettings.Shoot.DIST_TO_CHANGE_TO_MAIN = 60f;
                bot.Settings.FileSettings.Aiming.BAD_SHOOTS_MIN = 0;
                bot.Settings.FileSettings.Aiming.BAD_SHOOTS_MAX = 1;
                bot.Settings.FileSettings.Aiming.BAD_SHOOTS_OFFSET = 0.5f;
                bot.Settings.FileSettings.Aiming.BAD_SHOOTS_MAIN_COEF = 0.5f;
                bot.Settings.FileSettings.Aiming.SCATTERING_DIST_MODIF = 0.4f;
                bot.Settings.FileSettings.Aiming.COEF_FROM_COVER = 0.6f;

            }

            EPlayerSide side = bot.Side;

            WildSpawnType sptBear = (WildSpawnType)AkiBotsPrePatcher.sptBearValue;
            WildSpawnType sptUsec = (WildSpawnType)AkiBotsPrePatcher.sptUsecValue;

            if (side != EPlayerSide.Savage)
            {
                bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = new WildSpawnType[] { };
                bot.Settings.FileSettings.Mind.WARN_BOT_TYPES = new WildSpawnType[] { };
                bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = new WildSpawnType[] { };

                foreach (WildSpawnType botType in Enum.GetValues(typeof(WildSpawnType)))
                {
                    if (side == EPlayerSide.Bear && botType == sptBear)
                    {
                        bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType);
                        bot.Settings.FileSettings.Mind.WARN_BOT_TYPES.AddItem(botType);
                        continue;
                    }
                    else if (side == EPlayerSide.Usec && botType == sptUsec)
                    {
                        bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType);
                        bot.Settings.FileSettings.Mind.WARN_BOT_TYPES.AddItem(botType);
                        continue;
                    } 
                    else
                    {
                        bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.AddItem(botType);
                    }
                }
            }

            bot.Tactic.AggressionChange(-1f);
        }

        public override FollowerBrain GetFollowerBrain(BotOwner bot, pitAIBossPlayer boss)
        {

            if(_botRole == WildSpawnType.bossKnight)
            {
                return new KnightFollowerBrain(bot, boss);
            }

            if (_botRole == WildSpawnType.followerBigPipe)
                return new BigPipeFollowerBrain(bot, boss);

            if (_botRole == WildSpawnType.followerBirdEye)
                return new BirdEyeFollowerBrain(bot, boss);

            return new FollowerBrain(bot, boss);
        }
    }
}