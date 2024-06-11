using EFT;
using EFT.Interactive;
using friendlyPMC.Modules;

using LootingBots.Brain.Logics;
using UnityEngine;

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
            return (
                _follower != null && _follower.LootingBrain != null && 
                (_follower.LootingBrain.ActiveItem != null || _follower.LootingBrain.ActiveCorpse != null)
            );
        }

        public override string Name()
        {
            return "FBPLooting";
        }

        private bool ShouldEnd()
        {
            if(
                // not active item to pickup
                _follower == null || _follower.LootingBrain == null ||
                (
                    _follower.LootingBrain.ActiveItem == null &&
                    _follower.LootingBrain.ActiveCorpse == null
                ) ||
                // boss recall
                (
                    botOwner_0.BotRequestController.CurRequest != null && HasBoss() &&
                    GetBoss().Player().ProfileId == botOwner_0.BotRequestController.CurRequest.Requester.ProfileId &&
                    (
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer ||
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.followMe
                    )
                ) ||
                // enemy
                botOwner_0.Memory.HaveEnemy
            )
            {
                // stop everything
                if(_follower.LootingBrain != null)
                {
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                }

                return true;
            }

            return false;
        }

        public override AICoreActionEndStruct EndTakeItem()
        {

            if (
                // not active item to pickup
                _follower == null || _follower.LootingBrain == null ||
                (
                    _follower.LootingBrain.ActiveItem == null &&
                    _follower.LootingBrain.ActiveCorpse == null
                ) ||
                // boss recall
                (
                    botOwner_0.BotRequestController.CurRequest != null && HasBoss() &&
                    GetBoss().Player().ProfileId == botOwner_0.BotRequestController.CurRequest.Requester.ProfileId &&
                    (
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer ||
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.followMe
                    )
                ) ||
                // enemy
                botOwner_0.Memory.HaveEnemy
            )
            {
                // stop everything
                if (_follower.LootingBrain != null)
                {
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                }

                return gstruct7_0;
            }

            return gstruct7_1;
        }
        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            return gstruct7_0;
        }
        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            _follower = BossPlayers.Instance.GetFollower(botOwner_0);
            
            if (_follower == null || _follower.LootingBrain == null || (_follower.LootingBrain.ActiveItem == null && _follower.LootingBrain.ActiveCorpse == null))
            {
                
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "backToFLB");
            }

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.botTakeItem, "takeItem");
        }

    }
}
