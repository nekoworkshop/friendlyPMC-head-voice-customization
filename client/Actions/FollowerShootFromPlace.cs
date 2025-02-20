using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Actions
{
    internal class FollowerShootFromPlace : GClass254
    {
        public FollowerShootFromPlace(BotOwner bot) : base(bot)
        {
        }

        public override void Update()
        {
            method_1();
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            if (botOwner_0.ShootFromPlace.CanShootSit && goalEnemy != null && goalEnemy.IsVisible && goalEnemy.Distance < 40f)
            {
                botOwner_0.Mover.SetPose(0.5f);
            }
            method_0();
        }
    }
}
