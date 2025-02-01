using EFT;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEngine.AI;
using static RootMotion.FinalIK.IKSolver;
namespace friendlyPMC.Actions
{
    internal class FollowerGoCheck : BotRequest
    {
        private bool _fromWait = false;

        public bool FromWait
        {
            get
            {
                return _fromWait;
            }
        }

        private bool _hasPoint = false;
        public bool HasPoint
        {
            get
            {
                return _hasPoint;
            }
        }

        public FollowerGoCheck(IPlayer requester, BotRequestType request = BotRequestType.goToPoint, bool fromWait = false) : base(requester, request)
        {
            _fromWait = fromWait;


        }

        public override void Activate()
        {
            base.Activate();
            if (BotRequestType == BotRequestType.goToPoint)
            {
                SetSearchPoint();
            }
        }
        public void SetSearchPoint()
        {
            Vector3 playerPosition = Requester.WeaponRoot.position;
            Vector3 playerLookDirection = Requester.LookDirection;
            Ray visionRay = new Ray(playerPosition, playerLookDirection);

            RaycastHit[] hits = new RaycastHit[10];
            float distance = 50f;
            int numHits = Physics.SphereCastNonAlloc(
                    visionRay,
                    0.3f,
                    hits,
                    distance,
                    LayerMaskClass.HighPolyWithTerrainNoGrassMask
                );

            List<Vector3> points = new List<Vector3>();
            for (int i = 0; i < numHits; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.collider != null && hit.collider.gameObject != null)
                {
                    if (Vector3.Distance(hit.point, playerPosition) < distance)
                        points.Add(hit.point);
                }
            }

            points.Sort((a, b) => Vector3.Distance(a, playerPosition).CompareTo(Vector3.Distance(b, playerPosition)));

            Vector3 place = points[0];

            if (NavMesh.SamplePosition(place, out var navMeshHit, 5f, -1))
            {
                if (Executor.GoToPoint(navMeshHit.position, true, 0.5f) == NavMeshPathStatus.PathComplete)
                {
                    _hasPoint = true;
                }
            }

            if (!_hasPoint)
            {

                Vector3 dir02 = Requester.LookDirection;
                float forwardDistance = GClass824.Random(3f, 5f);

                Vector3 forwardPosition = Requester.Position + dir02.normalized * forwardDistance;
                float lateralOffset = GClass824.RandomSing() * GClass824.Random(0.5f, 1.5f);
                Vector3 lateralDirection = Vector3.Cross(Vector3.up, dir02).normalized;

                Vector3 finalPosition = forwardPosition + lateralDirection * lateralOffset;

                if (Executor.GoToPoint(finalPosition, true, 0.5f) == NavMeshPathStatus.PathComplete)
                {
                    _hasPoint = true;
                }
            }
        }

        public override bool CanProceed()
        {
            if (Executor == null) return false;

            return true;
        }

        public override bool CanRequest(BotOwner owner)
        {

            return true;
        }

        public override EBotRequestMode RequestMode
        {
            get
            {
                return EBotRequestMode.Fight;
            }
        }
    }
}
