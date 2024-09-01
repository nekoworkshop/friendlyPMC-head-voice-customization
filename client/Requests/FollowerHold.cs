using EFT;
using System.CodeDom.Compiler;

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
            if(
                executor.BotRequestController.CurRequest?.BotRequestType == BotRequestType.followMe ||
                executor.BotRequestController.CurRequest?.BotRequestType == BotRequestType.goToPoint
            )
            {
                return false;
            }

            return base.CanStartExecute(executor);
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            if (Executor != null && 
                (Executor.BotRequestController.CurRequest?.BotRequestType == BotRequestType.followMe ||
                Executor.BotRequestController.CurRequest?.BotRequestType == BotRequestType.goToPoint)
            )
            {
                return new AICoreActionEndStruct(true);
            }

            return new AICoreActionEndStruct(false);
        }
    }
}
