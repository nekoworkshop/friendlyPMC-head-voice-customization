
using EFT;

using HarmonyLib;

using System;

using friendlyPMC.Components.BossFollower;
using friendlyPMC.Components.FollowerBossFollower;
using friendlyPMC.Actions;

using System.Linq;
using friendlyPMC.Modules;
using System.Collections.Generic;

namespace friendlyPMC.Components
{
    public class BossFollowerPlayer : BotFollowerPlayer
    {

        public BossFollowerPlayer(BotOwner bot, pitAIBossPlayer player, WildSpawnType bossRole) : base(bot, player, false, bossRole) {

            NpcMessage.RemoveNpc(bot.ProfileId);

            // when questing with bosses, there will not be any messages from them
            if(!Utils.Props.BossFollowersType.Contains(bossRole) || !Utils.Utils.FlagGet("questGoons"))
                NpcMessage.AddNpc(bot, false, true);
        }

        protected override void SetFollowerSettings(BotOwner bot)
        {

            settingModif.PrecicingSpeedCoef = 1.35f;
            settingModif.AccuratySpeedCoef = 1.35f;
            settingModif.ScatteringCoef = 1.7f;

            settingModif.VisibleDistCoef = 0.8f;
            if (bot.IsRole(WildSpawnType.followerBirdEye))
            {
                settingModif.VisibleDistCoef = 0.9f;
            }

            //bot.Settings.FileSettings.Look.FULL_SECTOR_VIEW = true;

            base.SetFollowerSettings(bot);

            
            bot.Settings.FileSettings.Look.LOOK_THROUGH_GRASS = false;

            bot.Settings.FileSettings.Boss.EFFECT_REGENERATION_PER_MIN = 60f;
            if (bot.IsRole(WildSpawnType.followerBirdEye))
            {
                //bot.Settings.FileSettings.Core.GainSightCoef = 0.1f;
                bot.Settings.FileSettings.Cover.SOUND_TO_GET_SPOTTED = 10f;
                bot.Settings.FileSettings.Cover.SPOTTED_COVERS_RADIUS = 12f;
                bot.Settings.FileSettings.Shoot.LOW_DIST_TO_CHANGE_WEAPON = 30f;
                bot.Settings.FileSettings.Shoot.FAR_DIST_TO_CHANGE_WEAPON = 68f;
                bot.Settings.FileSettings.Shoot.DIST_TO_CHANGE_TO_MAIN = 60f;
                bot.Settings.FileSettings.Aiming.SCATTERING_DIST_MODIF = 0.2f;
                bot.Settings.FileSettings.Aiming.COEF_FROM_COVER = 1f;
                bot.Settings.FileSettings.Aiming.HARD_AIM = 0.9f;
                bot.Settings.FileSettings.Mind.MAX_AGGRO_BOT_DIST = 200f;
                bot.Settings.FileSettings.Look.MAX_VISION_GRASS_METERS = 1.5f;
            }

            EPlayerSide side = _player.Player().Side;

            WildSpawnType sptBear = WildSpawnType.pmcBEAR;
            WildSpawnType sptUsec = WildSpawnType.pmcUSEC;

            var _initialBot = AccessTools.Field(typeof(BotsGroup), "_initialBot").GetValue(_player.bossGroup) as BotOwner;

            if (side != EPlayerSide.Savage)
            {
                bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = new WildSpawnType[] { };
                bot.Settings.FileSettings.Mind.WARN_BOT_TYPES = new WildSpawnType[] { };
                bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = new WildSpawnType[] { 
                    WildSpawnType.shooterBTR,
                    WildSpawnType.peacefullZryachiyEvent,
                    WildSpawnType.gifter
                };

                foreach (WildSpawnType botType in Enum.GetValues(typeof(WildSpawnType)))
                {
                    if (side == EPlayerSide.Bear && botType == sptBear)
                    {
                        if (!_initialBot.Settings.FileSettings.Mind.DEFAULT_BEAR_BEHAVIOUR.HasFlag(EWarnBehaviour.AlwaysEnemies))
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
                        if (!_initialBot.Settings.FileSettings.Mind.DEFAULT_USEC_BEHAVIOUR.HasFlag(EWarnBehaviour.AlwaysEnemies))
                        {
                            bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType).ToArray();
                        }
                        else
                        {
                            bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES = bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.AddItem(botType).ToArray();
                        }
                        continue;
                    } 
                    else if(!Utils.Props.friendlyBotTypes.Contains(botType))
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
                        if (!_initialBot.Settings.FileSettings.Mind.DEFAULT_SAVAGE_BEHAVIOUR.HasFlag(EWarnBehaviour.AlwaysEnemies))
                        {
                            bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES = bot.Settings.FileSettings.Mind.FRIENDLY_BOT_TYPES.AddItem(botType).ToArray();
                            bot.Settings.FileSettings.Mind.WARN_BOT_TYPES = bot.Settings.FileSettings.Mind.WARN_BOT_TYPES.AddItem(botType).ToArray();
                        }
                        else if (botType != WildSpawnType.shooterBTR && botType != WildSpawnType.peacefullZryachiyEvent && botType != WildSpawnType.gifter)
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