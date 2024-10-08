using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Actions
{
    internal class FollowerSuppressionFire : GClass224
    {
        public FollowerSuppressionFire(BotOwner bot) : base(bot)
        {
        }

        public override void Update()
        {
            if (!botOwner_0.WeaponManager.Selector.IsWeaponReady) return;
            base.Update();
        }
    }
}
