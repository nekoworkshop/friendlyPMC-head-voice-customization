using EFT;
namespace friendlyPMC.Actions
{
    internal class FollowerGoCheck : BotRequest
    {
        public FollowerGoCheck(IPlayer requester) : base(requester, BotRequestType.goToPoint)
        {
            FollowerGoCheck _me = this;
        }

        public override bool CanProceed()
        {
            if (Executor == null) return false;

            return true;
        }

        public override bool CanRequest(BotOwner requester)
        {
            return true;
        }

        public override EBotRequestMode RequestMode
        {
            get
            {
                return EBotRequestMode.Fight;
            }
        }
    }
}
