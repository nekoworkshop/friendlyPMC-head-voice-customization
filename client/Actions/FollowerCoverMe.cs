using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Actions
{
    internal class FollowerCoverMe : BotRequest
    {
 
        public FollowerCoverMe(IPlayer requester) : base(requester, BotRequestType.goToPoint)
        {
        }

        public override bool CanProceed()
        {
            if (Executor == null) return false;

            return (Executor.GetPlayer.Transform.position - Requester.Transform.position).magnitude > 5f;
        }

        public override bool CanRequest(BotOwner requester)
        {
            if (Executor == null) return false;

            EnemyInfo goalEnemy = Executor.Memory.HaveEnemy ? Executor.Memory.GoalEnemy : null;
            if (goalEnemy != null && (Executor.Memory.LastTimeHit < 2.5f || (Executor.Memory.GoalEnemy.EnemyLastPosition - Executor.GetPlayer.Transform.position).magnitude < 7f))
            {
                return false;
            }

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
