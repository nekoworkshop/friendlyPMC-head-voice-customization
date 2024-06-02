using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            if (Executor == null) return false;

            EnemyInfo goalEnemy = Executor.Memory.HaveEnemy ? Executor.Memory.GoalEnemy : null;
            if (goalEnemy != null && ((Executor.Memory.GoalEnemy.EnemyLastPosition - Executor.GetPlayer.Transform.position).sqrMagnitude > 15f || !Executor.Memory.GoalEnemy.IsVisible))
            {
                return true;
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
