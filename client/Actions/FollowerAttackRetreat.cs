using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Actions
{
    internal class FollowerAttackRetreat : FollowerAttackMove
    {
        protected float float_1 = 0f;
        public FollowerAttackRetreat(BotOwner bot) : base(bot)
        {
            _autoCover = false;
        }

        public override void Update()
        {
            base.Update();
        }
    }
}
