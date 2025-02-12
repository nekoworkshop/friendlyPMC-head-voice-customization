using EFT;

using System;
using System.Collections.Generic;

using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Components;
using friendlyPMC.Modules;

namespace friendlyPMC.Utils
{
    public class Covers
    {

        /**
         *  Get closest cover point for the bot to given to the position, within the search radius and at a min distance from danger 
         */
        public static CustomNavigationPoint GetClosestCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool> extraChecks = null)
        {
            NavMeshPath navMeshPath = new NavMeshPath();

            pitAIBossPlayer boss = botOwner.BotFollower.HaveBoss ? botOwner.BotFollower.BossToFollow as pitAIBossPlayer : null;
            List<CustomNavigationPoint> areaCovers = botOwner.Covers.GetClosePoints(centerPosition, searchRadius);

            Vector3[] bossPosition = boss != null ? new Vector3[] { boss.realPlayer.Transform.position } : new Vector3[] { };

            CustomNavigationPoint pt = ClosestPoint(botOwner.Id, botOwner.GetPlayer.Transform.position, centerPosition, areaCovers, (CustomNavigationPoint point) =>
            {
                // cover too far
                //if (Vector3.Distance(point.Position, centerPosition) > searchRadius) return false;

                if (boss != null && !GClass369.IsDangerPositionFarEnough(point.Position, bossPosition, 0.7f * 0.7f)) return false;


                if (extraChecks != null && !extraChecks(point)) return false;

                return true;

            }, safeDistance);

            botOwner.Memory.SetCoverPoints(pt);

            return pt;
        }
        /**
         *  Get closest cover point for the bot to pointA within the area between pointA and pointB, at a min safe distance from danger 
         */
        public static CustomNavigationPoint GetClosestCoverPointBetween(BotOwner botOwner, Vector3 pointA, Vector3 pointB, float safeDistance = 5f, Func<CustomNavigationPoint, bool> eligibilityCheck = null)
        {
            pitAIBossPlayer boss = botOwner.BotFollower.HaveBoss ? botOwner.BotFollower.BossToFollow as pitAIBossPlayer : null;
            List<CustomNavigationPoint> areaCovers = botOwner.Covers.GetClosePoints((pointA + pointB) / 2f, (pointA - pointB).magnitude);

            Vector3[] bossPosition = boss != null ? new Vector3[] { boss.realPlayer.Transform.position } : new Vector3[] { };

            CustomNavigationPoint pt = ClosestPoint(botOwner.Id, botOwner.GetPlayer.Transform.position, pointA, areaCovers, (CustomNavigationPoint point) =>
            {
                if (!IsPointBetween(point.Position, pointA, pointB)) return false;

                if (boss != null && !GClass369.IsDangerPositionFarEnough(point.Position, bossPosition, 0.7f * 0.7f)) return false;

                if (eligibilityCheck != null && !eligibilityCheck(point)) return false;

                return true;
            }, safeDistance);

            botOwner.Memory.SetCoverPoints(pt);

            return pt;
        }
        /**
         * Get all cover poins around the center positions
         */
        public static List<CustomNavigationPoint> GetCoverPoints(BotOwner botOwner, Vector3 centerPosition, float searchRadius, Func<CustomNavigationPoint, bool> eligibilityCheck = null)
        {
            List<CustomNavigationPoint> points = new List<CustomNavigationPoint>();

            List<CustomNavigationPoint> areaCovers = botOwner.Covers.GetClosePoints(centerPosition, searchRadius);

            foreach (CustomNavigationPoint point in areaCovers)
            {
                if (
                    !point.IsFreeById(botOwner.Id)
                )
                {
                    continue;
                }

                if (Vector3.Distance(centerPosition, point.Position) <= searchRadius)
                {
                    if (eligibilityCheck != null && !eligibilityCheck(point)) continue;
                    points.Add(point);
                }

            }

            return points;
        }
        /**
         *  Get a random cover point for the bot around the given position, within the specified radius
         */
        public static CustomNavigationPoint GetCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, Func<CustomNavigationPoint, bool> eligibilityCheck = null)
        {
            List<CustomNavigationPoint> points = botOwner.Covers.GetClosePoints(centerPosition, searchRadius);
            CustomNavigationPoint point = null;
            if (points.Count > 0)
            {
                if (eligibilityCheck != null)
                {
                    points = points.FindAll((CustomNavigationPoint p) => eligibilityCheck(p));
                }

                if (points.Count > 0)
                    point = points.Random();
            }

            botOwner.Memory.SetCoverPoints(point);
            return point;


        }
        /** Utility to use CustomNavigationPoint when searching for a point. It is meant to replace FindPoint in BaseLogicLayerSimpleClass of bots **/
        public static CustomNavigationPoint FindPoint(BotOwner botOwner, CustomNavigationPoint customNavigationPoint, float searchRadius = 50f)
        {
            if (customNavigationPoint != null && (!customNavigationPoint.IsFreeById(botOwner.Id) || customNavigationPoint.IsSpotted))
            {
                customNavigationPoint = null;
            }
            if (customNavigationPoint != null)
            {
                return customNavigationPoint;
            }
            else
            {
                NavMeshPath navMeshPath = new NavMeshPath();
                customNavigationPoint = GetCoverPoint(botOwner, botOwner.GetPlayer.Transform.position, searchRadius, (CustomNavigationPoint point) =>
                {
                    return IsNavigablePoint(botOwner.GetPlayer.Transform.position, point.Position, searchRadius, navMeshPath);
                });
            }


            return customNavigationPoint;
        }
        /** Get cover from which the bot can shoot that is closest to the middle of the distance between bot's position and specified position */
        public static CustomNavigationPoint GetCover(
            BotOwner botOwner,
            Vector3 desiredPostion,
            CoverSearchType coverSearchType,
            float? searchRadius = null
        )
        {

            if (!botOwner.Memory.HaveEnemy) return null;

            CoverShootType coverShootType = CoverShootType.shoot;
            ShootPointClass shootPointClass = botOwner.CurrentEnemyTargetPosition(true);

            float searchRadiusValue = searchRadius.HasValue ? searchRadius.Value * searchRadius.Value : GClass583.Core.START_DIST_TO_COV;

            botOwner.BotAttackManager.TryPointGetting(desiredPostion, coverShootType, searchRadiusValue, coverSearchType, shootPointClass, new Action<CustomNavigationPoint>((point) =>
            {
                botOwner.Memory.SetCoverPoints(point);
            }), null, false, false, true, null);

            if (botOwner.Memory.BotCurrentCoverInfo.CovPoint != null)
            {
                return botOwner.Memory.BotCurrentCoverInfo.CovPoint;

            }
            return null;
        }

        /** Get the point among the given ones that is the closest to centerPosition that meets the eligibility check **/
        public static CustomNavigationPoint ClosestPoint(
            int botOwnerId,
            Vector3 botPosition,
            Vector3 centerPosition,
            List<CustomNavigationPoint> areaPoints,
            Func<CustomNavigationPoint, bool> eligibleCheck,
            float safeDistance = 5f,
            Vector3[] dangerPositions = null
        )
        {
            if (dangerPositions == null) dangerPositions = new Vector3[0];
            CustomNavigationPoint closest = null;

            float lastsqr = Mathf.Infinity;

            foreach (CustomNavigationPoint point in areaPoints)
            {
                if (
                    !(point.CoverLevel == CoverLevel.Sit || point.CoverLevel == CoverLevel.Stay) ||
                    !point.IsFreeById(botOwnerId) ||
                    !GClass369.IsDangerPositionFarEnough(point.Position, dangerPositions, safeDistance * safeDistance) ||
                    Vector3.Distance(point.Position, botPosition) <= 1f ||
                    !eligibleCheck(point)
                )
                {
                    continue;
                }

                float dist = (centerPosition - point.Position).sqrMagnitude;
                if (dist <= lastsqr)
                {
                    closest = point;
                    lastsqr = dist;
                }
            }

            return closest;
        }

        public static bool IsPointBetween(Vector3 point, Vector3 start, Vector3 end)
        {
            return (point.x >= Math.Min(start.x, end.x) && point.x <= Math.Max(start.x, end.x)) &&
                       (point.y >= Math.Min(start.y, end.y) && point.y <= Math.Max(start.y, end.y)) &&
                       (point.z >= Math.Min(start.z, end.z) && point.z <= Math.Max(start.z, end.z));
        }
        public static bool IsNavigablePoint(Vector3 botPosition, Vector3 point, float maxDistance, NavMeshPath existingMesh = null)
        {
            NavMeshPath navMeshPath = existingMesh != null ? existingMesh : new NavMeshPath();

            navMeshPath.ClearCorners();
            bool result = NavMesh.CalculatePath(botPosition, point, -1, navMeshPath);
            if (result && navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                float dist = navMeshPath.CalculatePathLength();
                if (dist <= maxDistance)
                {
                    return true;
                }
            }

            return false;
        }
        /** Find a position from where the bot can shoot at the given target **/
        public static Vector3? FindShootPosition(BotOwner botOwner, float minDistance, float maxRadius, Func<Vector3, bool> eligibleCheck = null, Vector3? manualTarget = null)
        {
            if (!botOwner.Memory.HaveEnemy) return null;

            Vector3 botPosition = botOwner.GetPlayer.Transform.position;
            Vector3 botWeaponOffset = botOwner.ShootData.WeaponRootOffset;
            LayerMask Mask = botOwner.LookSensor.Mask;

            Vector3 targetPosition = botOwner.Memory.GoalEnemy.CurrPosition;

            if (manualTarget.HasValue) targetPosition = manualTarget.Value;

            NavMeshPath mesh = new NavMeshPath();

            List<Vector3> shootTarget = new List<Vector3>
            {
                botOwner.Memory.GoalEnemy.Person.MainParts[BodyPartType.head].Position,
                botOwner.Memory.GoalEnemy.Person.MainParts[BodyPartType.body].Position
            };

            // define the angular steps and the scan distance
            int numSteps = 72; // e.g., 36 steps for a 10° interval, adjust for precision/performance
            float angleStep = 360f / numSteps;
            float scanDistance = maxRadius;

            // evaluate positions around the bot in a circular pattern
            for (int i = 0; i < numSteps; i++)
            {
                float angle = i * angleStep;
                Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
                Vector3 scanPosition = targetPosition + direction * scanDistance;

                NavMeshHit navMeshHit;
                if (!NavMesh.SamplePosition(scanPosition, out navMeshHit, 10f, NavMesh.AllAreas)) continue;

                // - check if the position is valid based on conditions
                if (!GClass369.IsDangerPositionFarEnough(navMeshHit.position, new Vector3[] { targetPosition }, minDistance * minDistance)) continue;
                if (!IsNavigablePoint(botPosition, navMeshHit.position, 150f, mesh)) continue;

                // - check if position meets the eligibility requirements
                if (eligibleCheck != null && !eligibleCheck(navMeshHit.position)) continue;

                // - check if bot can shoot from this position to the target (head/torso of the enemy)
                bool canShoot = false;
                foreach (var target in shootTarget)
                {
                    ShootPointClass shootPoint = new ShootPointClass(target, 0.8f);
                    if (GClass344.CanShootToTarget(shootPoint, navMeshHit.position + botWeaponOffset, Mask, false) ||
                        GClass344.CanShootToTarget(shootPoint, navMeshHit.position + botWeaponOffset * 0.5f, Mask, false))
                    {
                        canShoot = true;
                        break;
                    }
                }

                if (canShoot) return navMeshHit.position;
            }

            return null;
        }
    }
}
