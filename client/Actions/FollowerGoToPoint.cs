using EFT;
using UnityEngine;

namespace friendlyPMC.Actions
{
    /**
     * Overwrite of goToPoint decision to include sprinting
     */
    public class FollowerGoToPoint : GClass196
    {
        private bool _shouldSprint = true;

        public FollowerGoToPoint(BotOwner bot) : base(bot)
        {

            Vector3 point = botOwner_0.GoToSomePointData.Point;
            if (point != null)
                _shouldSprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, point) > 15f;
        }

        public override void Update()
        {
            base.method_0();
            botOwner_0.GoToSomePointData.UpdateToGo(_shouldSprint);
        }
    }
}