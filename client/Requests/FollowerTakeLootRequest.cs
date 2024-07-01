using EFT;
using friendlyPMC.Modules;

namespace friendlyPMC.Requests
{
    internal class FollowerTakeLootRequest : BotRequest
    {
        public FollowerTakeLootRequest(IPlayer requester) : base(requester, (BotRequestType)CustomBotRequestType.TakeLoot)
        {
            
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

        public new void AddPossibleExecutors(BotOwner bot)
        {
            InteractableObjects.SetTaker(bot);
            base.AddPossibleExecutors(bot);
        }
    }
}