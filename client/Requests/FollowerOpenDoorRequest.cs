using EFT;
using EFT.Interactive;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Requests
{
    internal class FollowerOpenDoorRequest : GClass508
    {
        public FollowerOpenDoorRequest(Door door, Player requester, Action completeCallback = null) : base(door,requester,completeCallback) 
        {
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
            
            if(!InteractableObjects.IsOpener(Executor))
            {
                Complete();
                return true;
            }

            return base.CanProceed();
        }

        public new void AddPossibleExecutors(BotOwner bot)
        {
            InteractableObjects.SetOpener(bot);
            base.AddPossibleExecutors(bot);
        }
    }
}
