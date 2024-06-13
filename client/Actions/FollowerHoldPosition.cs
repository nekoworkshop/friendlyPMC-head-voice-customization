using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Actions
{
    internal class FollowerHoldPosition : GClass221
    {
        public FollowerHoldPosition (BotOwner bot) : base(bot) { }

        public override void Look()
        {
            if (botOwner_0.Memory.HaveEnemy && !botOwner_0.Memory.IsInCover && botOwner_0.Memory.GoalEnemy.CurrPosition != null) botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.CurrPosition);
        }
    }
}
