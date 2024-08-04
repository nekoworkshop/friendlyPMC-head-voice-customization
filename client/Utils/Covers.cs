using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Components;
using static RootMotion.FinalIK.IKSolver;
using TMPro;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading;

namespace friendlyPMC.Utils
{
    internal class Covers
    {

        /**
         *  Get closest cover point for the bot to given to the position, within the search radius and at a min distance from danger 
         */
        public static CustomNavigationPoint GetClosestCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool>extraChecks = null)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            CustomNavigationPoint pt =  ClosestPoint(botOwner, centerPosition, (CustomNavigationPoint point) =>
            {
                // cover too far
                if(Vector3.Distance(point.Position, centerPosition) > searchRadius) return false;

                navMeshPath.ClearCorners();
                bool result = NavMesh.CalculatePath(centerPosition, point.Position, -1, navMeshPath);
                if (result && navMeshPath.status == NavMeshPathStatus.PathComplete)
                {
                    float dist = navMeshPath.CalculatePathLength();
                    // cover far to reach
                    if (dist > searchRadius)
                    {
                        return false;
                    }
                }
                if (extraChecks != null && !extraChecks(point)) return false;

                return true;

            }, safeDistance);

            return pt;
        }
        /**
         *  (V2) Get closest cover point for the bot to given to the position, within the search radius and at a min distance from danger 
         */
        public static CustomNavigationPoint GetClosestCoverPoint(
            int botOwnerId,
            Vector3 botPosition,
            Vector3 centerPosition,
            List<CustomNavigationPoint> areaPoints,
            float searchRadius, 
            float safeDistance = 5f,
            Vector3[] dangerPositions = null,
            Func<CustomNavigationPoint, bool>extraChecks = null
        )
        {
            NavMeshPath navMeshPath = new NavMeshPath();

            CustomNavigationPoint pt = ClosestPoint(botOwnerId, botPosition, centerPosition, areaPoints, 
            (CustomNavigationPoint point) =>
            {
                // cover too far
                if (Vector3.Distance(point.Position, centerPosition) > searchRadius) return false;

                navMeshPath.ClearCorners();
                bool result = NavMesh.CalculatePath(centerPosition, point.Position, -1, navMeshPath);

                if (result && navMeshPath.status == NavMeshPathStatus.PathComplete)
                {
                    float dist = navMeshPath.CalculatePathLength();
                    // cover far to reach
                    if (dist > searchRadius)
                    {
                        return false;
                    }
                }
                // no nav mesh to it
                else
                {
                    return false;
                }
                // did not pass extra checks
                if (extraChecks != null && !extraChecks(point)) return false;

                return true;

            }, safeDistance, dangerPositions);

            return pt;
        }

        /**
         *  Get closest cover point for the bot to pointA within the area between pointA and pointB, at a min safe distance from danger 
         */
        public static CustomNavigationPoint GetClosestCoverPointBetween(BotOwner botOwner, Vector3 pointA, Vector3 pointB, float safeDistance = 5f)
        {
            CustomNavigationPoint pt = ClosestPoint(botOwner, pointA, (CustomNavigationPoint point) =>
            {
                if(!IsPointBetween(point.Position, pointA, pointB)) return false;

                return true;

            }, safeDistance);

            return pt;
        }
        /**
         *  Get a random cover point for the bot around the given position, within the specified radius
         */
        public static CustomNavigationPoint GetCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, Func<CustomNavigationPoint, bool> eligibilityCheck = null)
        {
            if (!botOwner.BotFollower.HaveBoss) return null;

            pitAIBossPlayer boss = botOwner.BotFollower.BossToFollow as pitAIBossPlayer;

            //NavMeshPath navMeshPath = new NavMeshPath();

            List<CustomNavigationPoint> points = new List<CustomNavigationPoint>();
            List<CustomNavigationPoint> areaCovers = boss.GetAreaCovers();
            foreach (CustomNavigationPoint point in areaCovers)
            {
                if (
                    !point.IsFreeById(botOwner.Id)
                )
                {
                    continue;
                }


                if (Vector3.Distance(centerPosition,point.Position) <= searchRadius)
                {
                    if (eligibilityCheck != null && !eligibilityCheck(point)) continue;
                    points.Add(point);
                }

            }

            if (points.Count > 0)
            {
                return points.Random();
            }

            return null;


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
        /** Get cover point from which the bot can shoot that is closest to the middle of the distance between bot's position and specified position */
        public static CustomNavigationPoint GetApproachableCoverPoint(BotOwner botOwner, Vector3 point, float minDistance = 5f)
        {
            Vector3 midpoint = Vector3.Lerp(botOwner.GetPlayer.Transform.position, point, 0.5f);

            return GetClosestAttackCoverPoint(botOwner, midpoint, minDistance);
        }
        /** Get cover point from which the bot can shoot that is closest to the specified position, that is at min and max distance from danger and optionally that is not towards the direction of danger */
        public static CustomNavigationPoint GetClosestAttackCoverPoint(BotOwner botOwner, Vector3 centerPosition, float minDistance = 5f, float maxDistance = 200f, Vector3? dangerPosition = null)
        {
            if (!botOwner.Memory.HaveEnemy) return null;

            NavMeshPath navMeshPath = new NavMeshPath();
            Vector3 botPosition = botOwner.Transform.position;
            Vector3 enemyPos = botOwner.Memory.GoalEnemy.CurrPosition;

            ShootPointClass shootTarget = new ShootPointClass(enemyPos, 1f);

            CustomNavigationPoint pt = ClosestPoint(botOwner, centerPosition, (CustomNavigationPoint point) =>
            {
                float enemyRange = Vector3.Distance(enemyPos, point.Position);
                if (enemyRange > maxDistance)
                {
                    return false;
                }
                // not currently in use
                if (
                    dangerPosition != null &&
                    Vector3.Dot(((Vector3)dangerPosition - botPosition).normalized, (point.Position - botPosition).normalized) > 0
                )
                {
                    return false;
                }

                if (!IsNavigablePoint(botOwner.GetPlayer.Transform.position, point.Position, 100f, navMeshPath))
                {
                    return false;
                }

                if (!GClass301.CanShootToTarget(shootTarget, point.Position, LayerMaskClass.HighPolyWithTerrainMask, false)) return false;

                
                return true;

            }, minDistance);

            return pt;
        }
        /** (V2) Get cover point from which the bot can shoot that is closest to the specified position, that is at min and max distance from danger and optionally that is not towards the direction of danger */
        public static CustomNavigationPoint GetClosestAttackCoverPoint(
            int botOwnerId,
            Vector3 botPosition,
            Vector3 enemyPosition,

            List<CustomNavigationPoint> areaPoints,

            float minDistance = 5f, 
            float maxDistance = 200f,
            Vector3[] dangerPositions = null,
            bool behindEnemy = true
        )
        {


            ShootPointClass shootTarget = new ShootPointClass(enemyPosition, 1f);

            CustomNavigationPoint pt = ClosestPoint(botOwnerId, botPosition, enemyPosition, areaPoints, 
            (CustomNavigationPoint point) =>
            {
                float enemyRange = Vector3.Distance(enemyPosition, point.Position);
                if (enemyRange > maxDistance)
                {
                    return false;
                }

                if (
                    behindEnemy &&
                    Vector3.Dot((enemyPosition - botPosition).normalized, (point.Position - botPosition).normalized) > 0
                )
                {
                    return false;
                }

                if (!IsNavigablePoint(botPosition, point.Position, 100f))
                {
                    return false;
                }

                if (!GClass301.CanShootToTarget(shootTarget, point.Position, LayerMaskClass.HighPolyWithTerrainMask, false)) return false;

                
                return true;

            }, minDistance,dangerPositions);

            return pt;
        }
        /** Get the point among the relative ones to the boss player that is the closest to centerPosition that meets the eligibility check **/
        public static CustomNavigationPoint ClosestPoint(BotOwner botOwner, Vector3 centerPosition, Func<CustomNavigationPoint, bool> eligibleCheck,  float safeDistance = 5f)
        {
            
            if (!botOwner.BotFollower.HaveBoss) return null;

            Vector3[] carePosition = new Vector3[] { };

            foreach (var item in botOwner.EnemiesController.EnemyInfos)
            {
                try
                {
                    carePosition = carePosition.AddItem(item.Value.CurrPosition).ToArray();
                }
                catch
                {
                }
            }

            pitAIBossPlayer boss = botOwner.BotFollower.BossToFollow as pitAIBossPlayer;

            float lastsqr = Mathf.Infinity;
            CustomNavigationPoint closest = null;
            
            List<CustomNavigationPoint> areaCovers = boss.GetAreaCovers();
            
            foreach (CustomNavigationPoint point in areaCovers)
            {
                if (
                    !point.IsFreeById(botOwner.Id) ||
                    !GClass326.IsDangerPositionFarEnough(point.Position, carePosition, safeDistance * safeDistance) ||
                    Vector3.Distance(point.Position, botOwner.GetPlayer.Transform.position) <= 1f ||
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
        /** (V2) Get the point among the given ones that is the closest to centerPosition that meets the eligibility check **/
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
            CustomNavigationPoint closest = null;

            float lastsqr = Mathf.Infinity;
                
            foreach (CustomNavigationPoint point in areaPoints)
            {
                if (
                        !point.IsFreeById(botOwnerId) ||
                        !GClass326.IsDangerPositionFarEnough(point.Position, dangerPositions, safeDistance * safeDistance) ||
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
            if (point.x >= start.x && point.y >= start.y && point.z >= start.z)
            {
                if (point.x <= end.x && point.y <= end.y && point.z <= end.z)
                {
                    return true;
                }
            }

            return false;
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
        public static Vector3? FindShootPosition(BotOwner botOwner, ShootPointClass shootTarget, float minDistance, float maxRadius)
        {
            Vector3 targetPosition = shootTarget.Point;
            
            NavMeshPath mesh = new NavMeshPath();

            // Try to find a valid position within the sphere
            for (int i = 0; i < 70; i++) // Adjust the number of attempts as needed
            {
                Vector3 randomPosition = targetPosition + UnityEngine.Random.insideUnitSphere * maxRadius;

                NavMeshHit navMeshHit;

                if (!NavMesh.SamplePosition(randomPosition, out navMeshHit, 10f, -1)) continue;

                if (!GClass326.IsDangerPositionFarEnough(navMeshHit.position, new Vector3[] { targetPosition }, minDistance * minDistance)) continue;

                if (!IsNavigablePoint(botOwner.GetPlayer.Transform.position, navMeshHit.position, 150f, mesh)) continue;

                // Check if the bot can shoot from the random position to the target
                if (GClass301.CanShootToTarget(shootTarget, navMeshHit.position, botOwner.LookSensor.Mask, false))
                {
                    return navMeshHit.position;
                }
            }

            // If no valid position is found, return Vector3.zero
            return null;
        }
        /** (V2) Find a position from where the bot can shoot at the given target **/
        public static Vector3? FindShootPosition(Vector3 botPosition, ShootPointClass shootTarget, LayerMask Mask, float minDistance, float maxRadius, Func<Vector3, bool> eligibleCheck = null)
        {
            Vector3 targetPosition = shootTarget.Point;

            NavMeshPath mesh = new NavMeshPath();

            // Try to find a valid position within the sphere
            List<Vector3> positions = new List<Vector3>();

            for (int i = 0; i < 100; i++) // Adjust the number of attempts as needed
            {
                Vector3 randomPosition = targetPosition + UnityEngine.Random.insideUnitSphere * maxRadius;
                if (randomPosition == Vector3.zero) continue;
                if (positions.Contains(randomPosition)) continue;

                NavMeshHit navMeshHit;

                if (!NavMesh.SamplePosition(randomPosition, out navMeshHit, 10f, -1)) continue;

                if (!GClass326.IsDangerPositionFarEnough(navMeshHit.position, new Vector3[] { targetPosition }, minDistance * minDistance)) continue;

                if (!IsNavigablePoint(botPosition, navMeshHit.position, 150f, mesh)) continue;

                if (eligibleCheck != null && !eligibleCheck(navMeshHit.position)) continue;

                if (GClass301.CanShootToTarget(shootTarget, navMeshHit.position, Mask, false))
                {
                    return navMeshHit.position;
                }
            }

            return null;
        }

        // taken from SAIN 
        private static bool CheckRayCast(Vector3 point, Vector3 target, float distance = 3f)
        {
            point.y += 0.5f;
            target.y += 1.25f;
            Vector3 direction = target - point;
            return Physics.Raycast(point, direction, distance, LayerMaskClass.HighPolyWithTerrainMask);
        }
        //taken from SAIN
        private static float RaycastAlongDirection(Vector3 pointA, Vector3 pointB, Vector3 rayOrigin, int SegmentCount = 5)
        {
            const float RayHeight = 1.1f;
            const float MinSegLength = 1f;
            const float MaxSegLength = 5f;

            LayerMask mask = LayerMaskClass.HighPolyWithTerrainMask;

            Vector3 direction = pointB - pointA;

            // Make sure we aren't raycasting too often, set to MinSegLength for each raycast along a path
            float segmentLength = GetSegmentLength(SegmentCount, direction, MinSegLength, MaxSegLength, out float dirMagnitude, out int testCount);

            if (segmentLength <= 0 || testCount <= 0)
            {
                return 1f;
            }

            Vector3 dirNormal = direction.normalized;
            Vector3 dirSegment = dirNormal * segmentLength;

            Vector3 testPoint = pointA + (Vector3.up * RayHeight);

            int hits = 0;
            int i;

            for (i = 0; i < testCount; i++)
            {
                testPoint += dirSegment;

                Vector3 enemyDir = testPoint - rayOrigin;
                float rayLength = enemyDir.magnitude;

                if (Physics.Raycast(rayOrigin, enemyDir, rayLength, mask))
                {
                    hits++;
                }
            }

            float result = (float)hits / (float)i;
            return result;
        }
        // taken from SAIN
        private static float GetSegmentLength(int segmentCount, Vector3 direction, float minLength, float maxLength, out float dirMagnitude, out int countResult, int maxIterations = 10)
        {
            dirMagnitude = direction.magnitude;
            countResult = 0;
            if (dirMagnitude < minLength)
            {
                return 0f;
            }

            float segmentLength = 0f;
            for (int i = 0; i < maxIterations; i++)
            {
                if (segmentCount > 0)
                {
                    segmentLength = dirMagnitude / segmentCount;
                }
                if (segmentLength > maxLength)
                {
                    segmentCount++;
                }
                if (segmentLength < minLength)
                {
                    segmentCount--;
                }
                if (segmentLength <= maxLength && segmentLength >= minLength)
                {
                    break;
                }
                if (segmentCount <= 0)
                {
                    break;
                }
            }
            countResult = segmentCount;
            return segmentLength;
        }

        // taken from SAIN 
        public static bool CheckCoverVisibility(Vector3 position, Vector3 target)
        {
            const float offset = 0.1f;

            if (CheckRayCast(position, target, 3f))
            {
                Vector3 enemyDirection = target - position;
                enemyDirection = enemyDirection.normalized * offset;

                Quaternion right = Quaternion.Euler(0f, 90f, 0f);
                Vector3 rightPoint = right * enemyDirection;
                rightPoint += position;

                if (CheckRayCast(rightPoint, target, 3f))
                {
                    Quaternion left = Quaternion.Euler(0f, -90f, 0f);
                    Vector3 leftPoint = left * enemyDirection;
                    leftPoint += position;

                    if (CheckRayCast(leftPoint, target, 3f))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // taken from SAIN 
        public static bool CheckCoverDirection(Vector3 coverPosition, Vector3 targetPosition, Vector3 botPosition)
        {

            Vector3 directionToTarget = targetPosition - botPosition;
            float targetDist = directionToTarget.magnitude;

            Vector3 directionToCollider = coverPosition - botPosition;
            float colliderDist = directionToCollider.magnitude;

            float dot = Vector3.Dot(directionToTarget.normalized, directionToCollider.normalized);

            if (dot <= 0.33f)
            {
                return true;
            }
            if (dot <= 0.6f)
            {
                return colliderDist < targetDist * 0.75f;
            }
            if (dot <= 0.8f)
            {
                return colliderDist < targetDist * 0.5f;
            }
            return colliderDist < targetDist * 0.25f;
        }
        // taken from SAIN
        public static bool CheckPathSafety(NavMeshPath path, Vector3 enemyHeadPos, float ratio = 0.5f)
        {
            Vector3[] corners = path.corners;
            int max = corners.Length - 1;

            for (int i = 0; i < max; i++)
            {
                Vector3 pointA = corners[i];
                Vector3 pointB = corners[i + 1];

                float ratioResult = RaycastAlongDirection(pointA, pointB, enemyHeadPos);

                if (ratioResult < ratio)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
