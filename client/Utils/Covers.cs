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
        public static CustomNavigationPoint GetClosestCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool>extraChecks = null)
        {
            NavMeshPath navMeshPath = new NavMeshPath();

            pitAIBossPlayer boss = botOwner.BotFollower.HaveBoss ?  botOwner.BotFollower.BossToFollow as pitAIBossPlayer : null;
            List<CustomNavigationPoint> areaCovers = boss != null ? boss.GetAreaCovers() : BossPlayers.GetAICovers();

            Vector3[] bossPosition = boss != null ? new Vector3[]{ boss.realPlayer.Transform.position } : new Vector3[]{};

            CustomNavigationPoint pt =  ClosestPoint(botOwner.Id, botOwner.GetPlayer.Transform.position, centerPosition, areaCovers, (CustomNavigationPoint point) =>
            {
                // cover too far
                if(Vector3.Distance(point.Position, centerPosition) > searchRadius) return false;

                if(boss != null && !GClass326.IsDangerPositionFarEnough(point.Position, bossPosition, 0.7f * 0.7f)) return false;

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
         *  Get closest cover point for the bot to pointA within the area between pointA and pointB, at a min safe distance from danger 
         */
        public static CustomNavigationPoint GetClosestCoverPointBetween(BotOwner botOwner, Vector3 pointA, Vector3 pointB, float safeDistance = 5f, Func<CustomNavigationPoint, bool>  eligibilityCheck = null)
        {
            pitAIBossPlayer boss = botOwner.BotFollower.HaveBoss ?  botOwner.BotFollower.BossToFollow as pitAIBossPlayer : null;
            List<CustomNavigationPoint> areaCovers = boss != null ? boss.GetAreaCovers() : BossPlayers.GetAICovers();

            Vector3[] bossPosition = boss != null ? new Vector3[]{ boss.realPlayer.Transform.position } : new Vector3[]{};

            CustomNavigationPoint pt =  ClosestPoint(botOwner.Id, botOwner.GetPlayer.Transform.position, pointA, areaCovers, (CustomNavigationPoint point) =>
            {
                if(!IsPointBetween(point.Position, pointA, pointB)) return false;

                 if(boss != null && !GClass326.IsDangerPositionFarEnough(point.Position, bossPosition, 0.7f * 0.7f)) return false;

                if (eligibilityCheck != null && !eligibilityCheck(point)) return false;

                return true;

            }, safeDistance);

            return pt;
        }
        /**
         * Get all cover poins around the center positions
         */
        public static List<CustomNavigationPoint> GetCoverPoints(BotOwner botOwner, Vector3 centerPosition, float searchRadius, Func<CustomNavigationPoint, bool> eligibilityCheck = null)
        {
            List<CustomNavigationPoint> points = new List<CustomNavigationPoint>();

            if (!botOwner.BotFollower.HaveBoss) return points;

            pitAIBossPlayer boss = botOwner.BotFollower.HaveBoss ?  botOwner.BotFollower.BossToFollow as pitAIBossPlayer : null;

            if (boss == null) return points;

            List<CustomNavigationPoint> areaCovers = boss != null ? boss.GetAreaCovers() : BossPlayers.GetAICovers();
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
            List<CustomNavigationPoint> points = GetCoverPoints(botOwner,centerPosition,searchRadius,eligibilityCheck);
            
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
        /** Get cover from which the bot can shoot at the enemy that is closest to the specified position and that is at min and max distance from danger and optionally that is not towards the direction of danger */
        public static CustomNavigationPoint GetClosestShootCover(
            BotOwner botOwner,
            Vector3 desiredPostion,
            float minDistance = 5f,
            float maxDistance = 200f,
            Func<CustomNavigationPoint, bool> eligibleCheck = null
        )
        {

            NavMeshPath path =  new NavMeshPath();

            Vector3 botPosition = botOwner.Transform.position;

            Vector3 targetPosition = botOwner.Memory.GoalEnemy.CurrPosition;
            List<Vector3> shootTarget = new List<Vector3>
            {
                botOwner.Memory.GoalEnemy.Person.MainParts[BodyPartType.head].Position,
                botOwner.Memory.GoalEnemy.Person.MainParts[BodyPartType.body].Position
            };

            pitAIBossPlayer boss = botOwner.BotFollower.HaveBoss ? botOwner.BotFollower.BossToFollow as pitAIBossPlayer : null;
            Vector3[] bossPosition = boss != null ? new Vector3[] { boss.realPlayer.Transform.position } : new Vector3[] { };

            List<CustomNavigationPoint> areaPoints = boss != null ? boss.GetAreaCovers() : BossPlayers.GetAICovers();

            // extend the bot position in the opposite direction
            Vector3 direction = (botPosition - targetPosition).normalized;
            Vector3 extendedBotPosition = botPosition + direction * 30;

            CustomNavigationPoint pt = ClosestPoint(botOwner.Id, botPosition, desiredPostion, areaPoints,
            (CustomNavigationPoint point) =>
            {
                float enemyRange = Vector3.Distance(desiredPostion, point.Position);
                if (enemyRange > maxDistance)
                {
                    return false;
                }

                bool cansh = true;
                // check if bot can shoot either the head or torso of the enemy from this position
                foreach (var target in shootTarget)
                {
                    ShootPointClass shootPoint = new ShootPointClass(target, 0.8f);
                    if (!GClass301.CanShootToTarget(shootPoint, point, LayerMaskClass.HighPolyWithTerrainMask, false))
                    {
                        cansh = false;
                        break;
                    }
                }

                if (!cansh) return false;

                if (!IsPointBetween(point.Position, extendedBotPosition, targetPosition)) return false;

                if (!IsNavigablePoint(botPosition, point.Position, 100f, path))
                {
                    return false;
                }

                if (eligibleCheck != null && !eligibleCheck(point)) return false;

                return true;

            }, minDistance);

            return pt;
        }
        /** Get cover from which the bot can shoot that is closest to the middle of the distance between bot's position and specified position */
        public static CustomNavigationPoint GetApproachableCover(BotOwner botOwner, Vector3 point, float minDistance = 5f)
        {
            Vector3 midpoint = Vector3.Lerp(botOwner.GetPlayer.Transform.position, point, 0.5f);

            pitAIBossPlayer boss = botOwner.BotFollower.HaveBoss ? botOwner.BotFollower.BossToFollow as pitAIBossPlayer : null;
            Vector3[] bossPosition = boss != null ? new Vector3[] { boss.realPlayer.Transform.position } : new Vector3[] { };

            return GetClosestShootCover(botOwner, midpoint, minDistance, 200f, (cover) =>
            {
                if (boss != null && !GClass326.IsDangerPositionFarEnough(cover.Position, bossPosition, 0.7f * 0.7f)) return false;

                return true;
            });
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
            if(dangerPositions == null) dangerPositions = new Vector3[0];
            CustomNavigationPoint closest = null;

            float lastsqr = Mathf.Infinity;
                
            foreach (CustomNavigationPoint point in areaPoints)
            {
                if (
                    !(point.CoverLevel == CoverLevel.Sit || point.CoverLevel == CoverLevel.Stay) ||
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
            if(!botOwner.Memory.HaveEnemy) return null;

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
                if (!GClass326.IsDangerPositionFarEnough(navMeshHit.position, new Vector3[] { targetPosition }, minDistance * minDistance)) continue;
                if (!IsNavigablePoint(botPosition, navMeshHit.position, 150f, mesh)) continue;

                // - check if position meets the eligibility requirements
                if (eligibleCheck != null && !eligibleCheck(navMeshHit.position)) continue;

                // - check if bot can shoot from this position to the target (head/torso of the enemy)
                bool canShoot = false;
                foreach (var target in shootTarget)
                {
                    ShootPointClass shootPoint = new ShootPointClass(target, 0.8f);
                    if (GClass301.CanShootToTarget(shootPoint, navMeshHit.position + botWeaponOffset, Mask, false) ||
                        GClass301.CanShootToTarget(shootPoint, navMeshHit.position + botWeaponOffset * 0.5f, Mask, false))
                    {
                        canShoot = true;
                        break;
                    }
                }

                if (canShoot) return navMeshHit.position;
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

        public static List<Vector3> CheckAndCorrectPath(NavMeshPath path)
        {
            List<Vector3> correctedPath = new List<Vector3>(path.corners);

            for (int i = 0; i < correctedPath.Count - 1; i++)
            {
                Vector3 start = correctedPath[i];
                Vector3 end = correctedPath[i + 1];
                Vector3 direction = end - start;

                if (NavMesh.Raycast(start, end, out NavMeshHit hit, NavMesh.AllAreas))
                {
                    // Obstacle detected, find a valid point on NavMesh
                    Vector3 newPoint;
                    if (NavMesh.SamplePosition(hit.position, out NavMeshHit navHit, 10f, NavMesh.AllAreas))
                    {
                        newPoint = navHit.position;
                        correctedPath.Insert(i + 1, newPoint);

                        // Recalculate affected path segments
                        NavMeshPath newSegment = new NavMeshPath();
                        if (NavMesh.CalculatePath(start, newPoint, NavMesh.AllAreas, newSegment))
                        {
                            correctedPath.InsertRange(i + 1, newSegment.corners);
                        }
                        if (NavMesh.CalculatePath(newPoint, end, NavMesh.AllAreas, newSegment))
                        {
                            correctedPath.InsertRange(i + 2, newSegment.corners);
                        }

                        i++; // Skip the newly inserted point in the next iteration
                    }
                }
            }


            return correctedPath;
        }
    }
}
