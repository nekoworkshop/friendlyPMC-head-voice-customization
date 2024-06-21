using EFT;
using System.Threading.Tasks;

namespace friendlyPMC.Requests
{
    internal class FollowerRushEnemy : BotRequest
    {
        private BotOwner botOwner_0;
        public FollowerRushEnemy(BotOwner bot, Player requester, BotRequestType request = BotRequestType.attackClose) : base(requester, request)
        {
            botOwner_0 = bot;

            Task.Delay(1000).ContinueWith(t =>
            {
                if(botOwner_0.BotRequestController.CurRequest !=null && botOwner_0.BotRequestController.CurRequest.BotRequestType == request)
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                }
            });
        }

        public override EBotRequestMode RequestMode
        {
            get
            {
                return EBotRequestMode.Fight;
            }
        }

        public override bool CanProceed()
        {
            return Executor != null && Executor.Memory.HaveEnemy;
        }

        public override bool CanRequest(BotOwner requester)
        {
            return true;
        }
    }
}
