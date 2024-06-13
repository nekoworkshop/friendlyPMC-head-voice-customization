using EFT;
using System.Threading;
using UnityEngine;

namespace friendlyPMC.Actions
{
    internal class FollowerAttackMove : GClass162
    {
        private float timer = 0f;
        public FollowerAttackMove(BotOwner bot) : base(bot)
        {
        }

        public override void Update()
        {
            if(timer < Time.time)
            {
                timer = Time.time + GClass760.Random(1.5f, 3.5f);
                EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

                if (goalEnemy != null && !goalEnemy.IsVisible)
                {
                    botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.CurrPosition);
                }
            }
            
            base.Update();
        }

        public override void AimingAndShoot()
        {
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible)
            {
                gclass136_0.Update();
                return;
            }

            if (goalEnemy != null)
            {
                if(goalEnemy.IsVisible) botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.CurrPosition);
                else
                    botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.EnemyLastPosition);
            }
        }
    }
}
