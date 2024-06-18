using Aki.PrePatch;
using EFT;
using friendlyPMC.Components.BossFollower;
using HarmonyLib;
using LootingBots.Patch.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Components
{
    internal class BossFollowerPlayer : BotFollowerPlayer
    {
        public BossFollowerPlayer(BotOwner bot, pitAIBossPlayer player) : base(bot, player) {

            if (_lootingBrain == null)
            {
                _lootingBrain = _bot.GetPlayer.gameObject.AddComponent<LootingBrain>();
                _lootingBrain.Init(bot);
                _transactionController = AccessTools.Field(typeof(InventoryController), "_transactionController").GetValue(_lootingBrain.InventoryController) as TransactionController;

                Components.Logger.LogInfo("Loot Brain was NULL");
            }

            if (_lootFinder == null)
            {
                _lootFinder = _bot.GetPlayer.gameObject.AddComponent<LootFinder>();
                _lootFinder.Init(bot);
                Components.Logger.LogInfo("Loot Frinder was NULL");
            }

            if(_transactionController == null)
            {
                Components.Logger.LogInfo("_transactionController is NULL");
            }
           
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
        }

        public override FollowerBrain GetFollowerBrain(BotOwner bot, pitAIBossPlayer boss)
        {
            if(bot.IsRole(WildSpawnType.bossKnight))
            {
                return new KnightFollowerBrain(bot, boss);
            }

            return new FollowerBrain(bot, boss);
        }
    }
}