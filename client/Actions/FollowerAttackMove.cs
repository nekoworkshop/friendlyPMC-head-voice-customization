using EFT;

namespace friendlyPMC.Actions
{
    public class FollowerAttackMove : GClass162
    {

        public FollowerAttackMove(BotOwner bot) : base(bot)
        {
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
                botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
            }
        }
    }
}
