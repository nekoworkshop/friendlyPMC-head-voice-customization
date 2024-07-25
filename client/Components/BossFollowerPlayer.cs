
using EFT;

using HarmonyLib;

using System;

using friendlyPMC.Components.BossFollower;
using friendlyPMC.Components.FollowerBossFollower;
using friendlyPMC.Actions;

using System.Linq;

namespace friendlyPMC.Components
{
    internal class BossFollowerPlayer : BotFollowerPlayer
    {

        public BossFollowerPlayer(BotOwner bot, pitAIBossPlayer player, WildSpawnType bossRole) : base(bot, player, false, bossRole) {

            if (bossRole == WildSpawnType.followerBirdEye)
            {
                FollowerPatrol patrol = FollowerPatrolInstances.GetPatrol(bot);
                patrol.SetReachDist(17f);

                
            }
        }

        public override void SetFollowerSettings(BotOwner bot)
        {

            settingModif.PrecicingSpeedCoef = 1.35f;
            settingModif.AccuratySpeedCoef = 1.35f;
            settingModif.ScatteringCoef = 1.7f;

            base.SetFollowerSettings(bot);
            
            //bot.Settings.FileSettings.Core.HearingSense = 1.0f;


            if (bot.IsRole(WildSpawnType.followerBirdEye))
            {
                bot.Settings.FileSettings.Core.GainSightCoef = 0.5f;
                bot.Settings.FileSettings.Cover.SOUND_TO_GET_SPOTTED = 10f;
                bot.Settings.FileSettings.Cover.SPOTTED_COVERS_RADIUS = 12f;
                bot.Settings.FileSettings.Shoot.LOW_DIST_TO_CHANGE_WEAPON = 30f;
                bot.Settings.FileSettings.Shoot.FAR_DIST_TO_CHANGE_WEAPON = 68f;
                bot.Settings.FileSettings.Shoot.DIST_TO_CHANGE_TO_MAIN = 60f;
                bot.Settings.FileSettings.Aiming.SCATTERING_DIST_MODIF = 0.2f;
                bot.Settings.FileSettings.Aiming.COEF_FROM_COVER = 1f;
                bot.Settings.FileSettings.Aiming.HARD_AIM = 0.9f;
                // bird eye, aim for the head
                bot.Settings.FileSettings.Aiming.AIMING_TYPE = 6;
            }
            
            bot.Settings.FileSettings.Aiming.AIMING_TYPE = 3;

            EPlayerSide side = _player.Player().Side;

            WildSpawnType sptBear = WildSpawnType.pmcBEAR;
            WildSpawnType sptUsec = WildSpawnType.pmcUSEC;

            var _initialBot = AccessTools.Field(typeof(BotsGroup), "_initialBot").GetValue(_player.bossGroup) as BotOwner;

            if (side != EPlayerSide.Savage)
            {
                bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = new WildSpawnType[] { };
                bot.Settings.FileSettings.Mind.WARN_BOT_TYPES = new WildSpawnType[] { };
                bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = new WildSpawnType[] { WildSpawnType.shooterBTR };

                foreach (WildSpawnType botType in Enum.GetValues(typeof(WildSpawnType)))
                {
                    if (side == EPlayerSide.Bear && botType == sptBear)
                    {
                        if (!_initialBot.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack))
                        {
                            bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType).ToArray();
                        } else
                        {
                            bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.AddItem(botType).ToArray();
                        }
                        continue;
                    }
                    else if (side == EPlayerSide.Usec && botType == sptUsec)
                    {
                        if (!_initialBot.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack))
                        {
                            bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType).ToArray();
                        }
                        else
                        {
                            bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.AddItem(botType).ToArray();
                        }
                        continue;
                    } 
                    else if(botType != WildSpawnType.shooterBTR)
                    {
                        bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.AddItem(botType).ToArray();
                    }
                }
            } else
            {
                foreach (WildSpawnType botType in Enum.GetValues(typeof(WildSpawnType)))
                {
                    if (botType != sptBear && botType != sptUsec) 
                    {
                        if (!_initialBot.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.Attack))
                        {
                            bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType).ToArray();
                            bot.Settings.FileSettings.Mind.WARN_BOT_TYPES = bot.Settings.FileSettings.Mind.WARN_BOT_TYPES.AddItem(botType).ToArray();
                        }
                        else if (botType != WildSpawnType.shooterBTR)
                        {
                            bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.AddItem(botType).ToArray();
                        }
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