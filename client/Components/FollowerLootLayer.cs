using EFT;
using friendlyPMC.Modules;

namespace friendlyPMC.Components
{
    internal class FollowerLootLayer : GClass102
    {

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
            
            return aICoreActionEndStruct;
        }
        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            if (!InteractableObjects.IsTaker(botOwner_0))
                return new AICoreActionEndStruct("item.None", true);

            return aICoreActionEndStruct;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            
            if (!InteractableObjects.IsTaker(botOwner_0))
            {
                InteractableObjects.RemoveTaker(botOwner_0);
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "loot.Error");
            }
            

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.botTakeItem, "takeItem");
        }

    }
}
