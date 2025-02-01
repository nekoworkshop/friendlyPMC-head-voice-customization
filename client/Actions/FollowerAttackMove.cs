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
        protected bool _withSuppress = false;

        private float float_3 = 0f;
        private bool bool_0 = false;

        public FollowerAttackMove(BotOwner bot,bool withSuppress = false) : base(bot)
        {
            _withSuppress = withSuppress;
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
            if (float_3 < Time.time)
            {
                float_3 = Time.time + GClass824.Random(2f, 4f);
                bool_0 = !bool_0;
            }

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            if ((bool_0 && _withSuppress) ||  (goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible))
            {
                gclass158_0.Update();
                return;
            }

            if (goalEnemy != null)
            {
                botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.EnemyLastPosition + new Vector3(0,0.5f, 0));
            }
        }
    }
}
