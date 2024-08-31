using EFT;

namespace friendlyPMC.Requests
{
    internal class FollowerHold : BotRequest
    {
        public FollowerHold(Player requester) : base(requester, BotRequestType.wait)
        {
            this.EndIfCantExecute = true;
        }

        public override EBotRequestMode RequestMode
        {
            get
            {
                return EBotRequestMode.Fight;
            }
        }
        public override bool CanRequest(BotOwner requester)
        {
            return true;
        }

        public override bool CanProceed()
        {
            return true;
        }

        public override bool CanStartExecute(BotOwner executor)
        {
            if(executor.BotRequestController.CurRequest?.BotRequestType == BotRequestType.goToPoint)
            {
                return false;
            }

            return base.CanStartExecute(executor);
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            return new AICoreActionEndStruct(false);
        }
    }
}
