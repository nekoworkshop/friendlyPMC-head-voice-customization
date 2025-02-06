using EFT;
using HarmonyLib;

namespace friendlyPMC.Actions
{
    /**
     * Overwrite of dogFight decision to fix bot's aiming direction
     */
    public class FollowerDogFight : GClass183
    {
        private GClass163 gclass136_1;
        public FollowerDogFight(BotOwner bot) : base(bot)
        {

        }

        public override void Update()
        {
            if (gclass136_1 == null)
            {
                gclass136_1 = AccessTools.Field(typeof(GClass183), "gclass158_0").GetValue(this) as GClass163;
            }

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            botOwner_0.Mover.SetTargetMoveSpeed(1f);
            botOwner_0.DogFight.Fight();

            bool tense = false;

            if (goalEnemy != null && goalEnemy.IsVisible && goalEnemy.Distance < 15f)
            {
                botOwner_0.SetPose(0.7f);
                tense = true;
            }

            if (goalEnemy != null && goalEnemy.CanShoot && goalEnemy.IsVisible)
            {
                botOwner_0.Steering.LookToPoint(goalEnemy.CurrPosition);
                gclass136_1.Update();
                return;
            }
            else if (!tense)
            {
                botOwner_0.SetPose(1f);
            }

            botOwner_0.LookData.SetLookPointByHearing(null);
        }
    }

}
