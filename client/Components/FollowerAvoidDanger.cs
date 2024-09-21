using EFT;
using friendlyPMC.Components.Tactics;

namespace friendlyPMC.Components
{
    internal class FollowerAvoidDanger : GClass36
    {

        private FollowerCommonLayer commonLayer;
        public FollowerAvoidDanger(BotOwner bot, int priority) : base(bot, priority)
        {
            commonLayer = new FollowerCommonLayer(bot, priority);
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            if(request != null && request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup)
            {
                return commonLayer.GetCloserToBoss(out var customNavigationPoint_0);
            }

            return base.GetDecision();
        }
    }
}
