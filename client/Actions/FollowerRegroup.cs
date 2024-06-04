using EFT;

namespace friendlyPMC.Actions
{
    internal class FollowerRegroup : BotRequest
    {
        public FollowerRegroup(IPlayer requester) : base(requester, BotRequestType.warnPlayer)
        {
        }

        public override bool CanProceed()
        {
            if (Executor == null) return false;

            return (Executor.GetPlayer.Transform.position - Requester.Transform.position).magnitude > 10f;
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
