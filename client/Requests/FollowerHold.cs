using EFT;
using friendlyPMC.Actions;
using HarmonyLib;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace friendlyPMC.Requests
{
    internal class FollowerHold : BotRequest
    {
        private List<BotRequest> botRequests = null;
        public FollowerHold(Player requester) : base(requester, BotRequestType.wait)
        {
            
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
            if(botRequests == null)
            {
                botRequests = AccessTools.Field(typeof(BotGroupRequestController), "_listOfRequests").GetValue(executor.BotsGroup.RequestsController) as List<BotRequest>;
            }

            if (botRequests != null)
            {
                var req = botRequests.Find(request => (request is FollowerGoCheck));
                if(req != null)
                {
                    var reqExecutor = AccessTools.Field(typeof(BotRequest), "Executor").GetValue(req) as BotOwner;
                    if(reqExecutor == executor)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
