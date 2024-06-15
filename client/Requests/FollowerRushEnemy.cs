using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Requests
{
    internal class FollowerRushEnemy : BotRequest
    {
        public FollowerRushEnemy(Player requester, BotRequestType request = BotRequestType.attackClose) : base(requester, request)
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
            if (Executor == null) return false;
            return true;
        }

        public override bool CanRequest(BotOwner requester)
        {
            return true;
        }
    }
}
