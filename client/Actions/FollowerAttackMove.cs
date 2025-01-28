using EFT;
using HarmonyLib;
using UnityEngine;

namespace friendlyPMC.Actions
{
    /**
     * Overwrite of attackMoving decision to fix bot's aiming direction
     */
    public class FollowerAttackMove : GClass185
    {

        protected bool _autoCover = true;

        public FollowerAttackMove(BotOwner bot) : base(bot)
        {
        }

        public override void Update()
        {
            if (!_autoCover)
            {
                AccessTools.Field(typeof(GClass185), "float_0").SetValue(this, Time.time);
                GClass185.Class115 @class = new GClass185.Class115();
                @class.gclass185_0 = this;
                botOwner_0.SetTargetMoveSpeed(1f);
                botOwner_0.Sprint(false, true);
                botOwner_0.SetPose(1f);
                @class.recalcTime = 0f;
                @class.withShoot = botOwner_0.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Attack) || botOwner_0.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Protect);

                if(botOwner_0.Memory.CurCustomCoverPoint != null)
                {
                    @class.method_0(botOwner_0.Memory.CurCustomCoverPoint);
                }
            }

            base.Update();
        }

        public override void AimingAndShoot()
        {
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible)
            {
                gclass158_0.Update();
                return;
            }

            if (goalEnemy != null)
            {
                botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
            }
        }
    }
}
