using EFT;
using EFT.Interactive;
using friendlyPMC.Modules;
using HarmonyLib;
using System.Collections.Generic;

namespace friendlyPMC.Components
{
    internal class FollowerLootLayer : GClass102
    {
        public FollowerLootLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        private bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        private pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }

        public override bool ShallUseNow()
        {
            return HasBoss() && InteractableObjects.IsToTake(botOwner_0);
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            // boss recall
            if (
                botOwner_0.BotRequestController.CurRequest != null && HasBoss() && 
                GetBoss().Player().ProfileId == botOwner_0.BotRequestController.CurRequest.Requester.ProfileId && 
                botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer
            )
            {
                return gstruct7_0; 
            }
            
            if (botOwner_0.ItemTaker.HaveItemToTake())
            {
                try
                {
                    var item = AccessTools.Field(typeof(BotItemTaker), "_itemToTake").GetValue(botOwner_0.ItemTaker) as LootItem;
                    if (item != null) { }

                    botOwner_0.ItemTaker.method_6(item);
                }
                catch { }

            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            
            if (this.botOwner_0.ItemTaker.HaveItemToTake())
            {
                Logger.LogInfo("Bot " + botOwner_0.Profile.Nickname + " tries to take item");

                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.botTakeItem, "Take Item");
            }
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "Stub logic");
        }

    }
}
