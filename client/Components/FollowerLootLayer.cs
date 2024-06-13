using EFT;
using EFT.Interactive;
using friendlyPMC.Modules;

using LootingBots.Brain.Logics;
using System;
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
            return InteractableObjects.IsTaker(botOwner_0);
        }

        public override string Name()
        {
            return "FBPLooting";
        }
        public override AICoreActionEndStruct EndTakeItem()
        {

            return gstruct7_0;
        }
        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            return gstruct7_0;
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (
                    botOwner_0.BotRequestController.CurRequest != null && HasBoss() &&
                    GetBoss().Player().ProfileId == botOwner_0.BotRequestController.CurRequest.Requester.ProfileId &&
                    (
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer ||
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.followMe
                    )
                )
            {
                // stop everything
                if (_follower != null && _follower.LootingBrain != null)
                {

                    _follower.LootingBrain.DisableTransactions();
                    _follower.LootingBrain.UpdateGridStats();
                    _follower.LootingBrain.StopAllCoroutines();
                    _follower.LootingBrain.ActiveItem = null;
                    _follower.LootingBrain.ActiveCorpse = null;
                }
                return gstruct7_0;
            }
                

            return base.ShallEndCurrentDecision(curDecision);
        }
        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            _follower = BossPlayers.Instance.GetFollower(botOwner_0);
            
            if (!InteractableObjects.IsTaker(botOwner_0) || (_follower.LootingBrain.ActiveItem == null && _follower.LootingBrain.ActiveCorpse == null))
            {
                
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "backToFLB");
            }

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.botTakeItem, "takeItem");
        }

    }
}
