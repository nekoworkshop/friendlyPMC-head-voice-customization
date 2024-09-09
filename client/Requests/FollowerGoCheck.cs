using EFT;
namespace friendlyPMC.Actions
{
    internal class FollowerGoCheck : BotRequest
    {
        private bool _fromWait = false;

        public bool FromWait
        {
            get
            {
                return _fromWait;
            }
        }
        public FollowerGoCheck(IPlayer requester, BotRequestType request = BotRequestType.goToPoint, bool fromWait = false) : base(requester, request)
        {
            _fromWait = fromWait;
        }

        public override bool CanProceed()
        {
            if (Executor == null) return false;

            return true;
        }

        public override bool CanRequest(BotOwner owner)
        {

            if (owner.Memory.HaveEnemy) return false;

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
