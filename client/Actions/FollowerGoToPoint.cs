using EFT;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    internal class FollowerGoToPoint : GClass159
    {
        private bool _shouldSprint = true;
        public FollowerGoToPoint(BotOwner bot) : base(bot)
        {

            Vector3 point = botOwner_0.GoToSomePointData.Point;
            if(point != null)
            _shouldSprint = GetNavDistance(point) > 15f;
        }

        private float GetNavDistance(Vector3 point)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            navMeshPath.ClearCorners();
            bool resut = NavMesh.CalculatePath(botOwner_0.Transform.position, point, -1, navMeshPath);

            if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                return navMeshPath.CalculatePathLength();
            }
            else
            {
                return Vector3.Distance(point, botOwner_0.Transform.position);
            }
        }

        public override void Update()
        {
            base.method_0();
            botOwner_0.GoToSomePointData.UpdateToGo(_shouldSprint);
        }
    }
}