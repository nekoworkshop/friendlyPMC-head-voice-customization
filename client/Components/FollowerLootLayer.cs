using EFT;
using EFT.Interactive;
using friendlyPMC.Modules;

using LootingBots.Brain.Logics;

namespace friendlyPMC.Components
{
    internal class FollowerLootLayer : GClass102
    {

        private BotFollowerPlayer _follower;
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
            
            if (
                // not active item to pickup
                _follower == null ||
                ( _follower.LootingBrain.ActiveItem == null &&
                _follower.LootingBrain.ActiveCorpse == null ) ||
                // boss recall
                botOwner_0.BotRequestController.CurRequest != null && HasBoss() && 
                GetBoss().Player().ProfileId == botOwner_0.BotRequestController.CurRequest.Requester.ProfileId && 
                botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer
            )
            {
                return gstruct7_0; 
            }

            return gstruct7_1;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            _follower = BossPlayers.Instance.GetFollower(botOwner_0);

            if(_follower == null || (_follower.LootingBrain.ActiveItem == null && _follower.LootingBrain.ActiveCorpse == null))
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "Stub logic");
            }

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.botTakeItem, "Take Item");
        }

    }
}
