using EFT;

using friendlyPMC.Modules;

namespace friendlyPMC.Components
{
    internal class FollowerLootTaker : GClass102
    {
        public FollowerLootTaker(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        public override bool ShallUseNow()
        {
            this.botOwner_0.ItemTaker.RefreshClosestItems();
            return this.botOwner_0.ItemTaker.HaveItemToTake() || InteractableObjects.IsToTake(botOwner_0);
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            bool isme = InteractableObjects.IsToTake(botOwner_0);
            Logger.LogInfo("Bot " + botOwner_0.Profile.Nickname +" tries to take item");
            if (this.botOwner_0.ItemTaker.HaveItemToTake() || isme)
            {
                InteractableObjects.SetTaker(null,null);
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.botTakeItem, "Take Item");
            }
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "Stub logic");
        }
    }
}
