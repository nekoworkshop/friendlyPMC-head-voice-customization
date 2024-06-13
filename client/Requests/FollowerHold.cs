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

        public override AICoreActionEndStruct EndHoldPosition()
        {
            return new AICoreActionEndStruct(false);
        }
    }
}
