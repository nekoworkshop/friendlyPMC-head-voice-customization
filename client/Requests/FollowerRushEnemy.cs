using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Requests
{
    internal class FollowerRushEnemy : GClass506
    {
        public FollowerRushEnemy(Player requester, Vector3 pos, Action completeCallback, Action disposeCallback, BotRequestType request = BotRequestType.attackClose) : base(requester,pos,completeCallback,disposeCallback)
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
            return Executor != null;
        }
    }
}
