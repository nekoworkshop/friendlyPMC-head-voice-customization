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
            return this.botOwner_0.ItemTaker.HaveItemToTake() && InteractableObjects.IsToTake(botOwner_0) && !botOwner_0.Memory.HaveEnemy;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            Logger.LogInfo("Bot " + botOwner_0.Profile.Nickname +" tries to take item");
            if (this.botOwner_0.ItemTaker.HaveItemToTake())
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.botTakeItem, "Take Item");
            }
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "Stub logic");
        }
    }
}
