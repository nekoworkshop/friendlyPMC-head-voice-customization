using EFT;
using UnityEngine;

namespace friendlyPMC.Actions
{
    public class FollowerHoldPosition : GClass221
    {
        private float timer = 0f;
        public FollowerHoldPosition (BotOwner bot) : base(bot) { }

        public override void Look()
        {
            if (botOwner_0.Memory.HaveEnemy && !botOwner_0.Memory.IsInCover && botOwner_0.Memory.GoalEnemy.CurrPosition != null)
            {
                botOwner_0.Steering.LookToDirection(botOwner_0.Memory.GoalEnemy.CurrPosition - botOwner_0.GetPlayer.Transform.position);
            }

            if(!botOwner_0.Memory.HaveEnemy && timer < Time.time)
            {
                timer = Time.time + GClass761.Random(3f, 6f);
                botOwner_0.LookData.SetLookPointByHearing(null);
            }
        }
    }
}
