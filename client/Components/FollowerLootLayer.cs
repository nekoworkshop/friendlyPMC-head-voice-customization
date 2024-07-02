using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using friendlyPMC.Modules;

using LootingBots.Brain.Logics;
using System;
using System.Reflection;
using UnityEngine;

namespace friendlyPMC.Components
{
    internal class FollowerLootLayer : GClass102
    {

        private BotFollowerPlayer _follower;
        public FollowerLootLayer(BotOwner bot, int priority) : base(bot, priority)
        {

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
            if (!InteractableObjects.IsTaker(botOwner_0))
                return new AICoreActionEndStruct("item.None", true);
            
            return gstruct7_0;
        }
        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            if (!InteractableObjects.IsTaker(botOwner_0))
                return new AICoreActionEndStruct("item.None", true);

            return gstruct7_0;
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            return base.ShallEndCurrentDecision(curDecision);
        }
        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            _follower = BossPlayers.Instance.GetFollower(botOwner_0);
            
            if (!InteractableObjects.IsTaker(botOwner_0) || (_follower.LootingBrain.ActiveItem == null && _follower.LootingBrain.ActiveCorpse == null))
            {
                InteractableObjects.RemoveTaker(botOwner_0);
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "loot.Error");
            }

            if (_follower.LootingBrain.ActiveItem.Item != null && _follower.LootingBrain.IsLootIgnored(_follower.LootingBrain.ActiveItem.Item.Id))
            {
                botOwner_0.Gesture.TryGestus(EGesture.Bad, false);
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "item.ignore");
            }
            

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.botTakeItem, "takeItem");
        }

    }
}
