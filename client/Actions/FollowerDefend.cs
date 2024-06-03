using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Actions
{
    internal class FollowerDefend : BotRequest
    {
        public FollowerDefend(BotOwner bot, BotRequestType request = BotRequestType.wait) : base(bot, request) { 
            
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

        public override bool CanProceed()
        {
            return Executor != null;
        }
    }
}
