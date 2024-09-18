using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Actions
{
    internal class FollowerDogFight : GClass160
    {
        private GClass141 gclass136_1;
        public FollowerDogFight(BotOwner bot) : base(bot) {
           
        }

        public override void Update()
        {
            if(gclass136_1 == null)
            {
                gclass136_1 = AccessTools.Field(typeof(GClass160), "gclass136_0").GetValue(this) as GClass141;
            }

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            botOwner_0.Mover.SetTargetMoveSpeed(1f);
            botOwner_0.DogFight.Fight();

            bool tense = false;

            if (goalEnemy != null && goalEnemy.IsVisible && goalEnemy.Distance < 15f)
            {
                botOwner_0.SetPose(0.5f);
                tense = true;
            }

            if (goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible)
            {
                botOwner_0.Steering.LookToPoint(goalEnemy.CurrPosition);
                gclass136_1.Update();
                return;
            } else if(!tense)
            {
                botOwner_0.SetPose(1f);
            }

            botOwner_0.LookData.SetLookPointByHearing(null);
        }
    }

}
