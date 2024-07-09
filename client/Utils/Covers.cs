using EFT;
using friendlyPMC.Modules;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;

namespace friendlyPMC.Utils
{
    internal class Covers
    {
        /**
         *  Get closest cover point for the bot to given to the position, within the search radius and at a min distance from danger 
         */
        public static CustomNavigationPoint GetClosestCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, float safeDistance = 5f)
        {
            List<CustomNavigationPoint> customNavigationPoints = Utils.HasBoss(botOwner) ? Utils.GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                Vector3[] carePosition = new Vector3[] { };
                foreach (var item in botOwner.EnemiesController.EnemyInfos)
                {
                    try
                    {
                        carePosition = carePosition.AddItem(item.Value.CurrPosition).ToArray();
                    }
                    catch { }
                }

                CustomNavigationPoint closestPoint = GetClosestPoint(botOwner, customNavigationPoints, centerPosition, searchRadius, point => {
                    if (!point.IsDangerPositionFarEnough(carePosition, safeDistance * safeDistance)) return false;

                    if (point.IsFreeById(botOwner.Id) &&
                        !point.IsSpotted &&
                        (!botOwner.Memory.HaveEnemy || point.IsFreeById(botOwner.Memory.GoalEnemy.Owner.Id)))
                    {
                        return true;
                    }

                    return false;
                });

                return closestPoint;
            }

            return null;
        }
        /**
         *  Get closest cover point for the bot to pointA within the area between pointA and pointB, at a min safe distance from danger 
         */
        public static CustomNavigationPoint GetClosestCoverPointBetween(BotOwner botOwner, Vector3 pointA, Vector3 pointB, float safeDistance = 5f)
        {
            List<CustomNavigationPoint> customNavigationPoints = Utils.HasBoss(botOwner) ? Utils.GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                float distance = Vector3.Distance(pointA, pointB);

                Vector3[] carePosition = new Vector3[] { };
                foreach (var item in botOwner.EnemiesController.EnemyInfos)
                {
                    try
                    {
                        carePosition = carePosition.AddItem(item.Value.CurrPosition).ToArray();
                    }
                    catch { }
                }

                CustomNavigationPoint closestPoint = GetClosestPoint(botOwner, customNavigationPoints, pointA, distance, point =>
                {
                    if (IsPointBetween(point.Position, pointA, pointB) &&
                        point.IsFreeById(botOwner.Id) &&
                        !point.IsSpotted &&
                        (!botOwner.Memory.HaveEnemy || point.IsFreeById(botOwner.Memory.GoalEnemy.Owner.Id)))
                    {

                        return point.IsDangerPositionFarEnough(carePosition, safeDistance * safeDistance);
                    }

                    
                    return false;
                });


                return closestPoint;
            }

            return null;
        }


        /**
         *  Get a random cover point for the bot around the given position, within the specified radius
         */
        public static CustomNavigationPoint GetCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius)
        {
            List<CustomNavigationPoint> customNavigationPoints = Utils.HasBoss(botOwner) ? Utils.GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                List<CustomNavigationPoint> availablePoints = FilterEligiblePoints(botOwner,customNavigationPoints, searchRadius, point =>
                {
                    if (point.IsFreeById(botOwner.Id) && !point.IsSpotted)
                    {
                        float sqrDistance = (centerPosition - point.Position).sqrMagnitude;
                        return sqrDistance <= searchRadius * searchRadius;
                    }

                    return false;
                });

                if (availablePoints.Count > 0)
                {
                    return availablePoints.Random();
                }
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
                customNavigationPoint = GetCoverPoint(botOwner, botOwner.GetPlayer.Transform.position, searchRadius);
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
        public static CustomNavigationPoint GetClosestAttackCoverPoint(BotOwner botOwner, Vector3 centerPosition, float minDistance = 5f, float maxDistance = 120f, Vector3? dangerPosition = null)
        {
            List<CustomNavigationPoint> customNavigationPoints = Utils.HasBoss(botOwner) ? Utils.GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                NavMeshPath navMeshPath = new NavMeshPath();
                Vector3 botPosition = botOwner.Transform.position;

                Func<CustomNavigationPoint, bool> PointSet = (CustomNavigationPoint point) =>
                {
                    navMeshPath.ClearCorners();
                    bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                    if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                    {
                        float dist = navMeshPath.CalculatePathLength();
                        if (dist > (Utils.HasBoss(botOwner) ? 120f : Mathf.Infinity))
                        {
                            return false;
                        }
                    }

                    return true;
                };

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

                float closestSqrDistance = Mathf.Infinity;
                CustomNavigationPoint closestPoint = null;

                List<CustomNavigationPoint> eligiblePoints = new List<CustomNavigationPoint>();
                foreach (CustomNavigationPoint point in eligiblePoints)
                {
                    
                    if (point.IsDangerPositionFarEnough(carePosition, minDistance * minDistance) &&
                        point.IsFreeById(botOwner.Id) &&
                        botOwner.Memory.HaveEnemy &&
                        GClass301.CanShoot(point.Position, botOwner.Memory.GoalEnemy))
                    {
                        Vector3 vrange = centerPosition - point.Position;
                        float enemyRange = Vector3.Distance(botOwner.Memory.GoalEnemy.CurrPosition, point.Position);

                        if (enemyRange <= maxDistance)
                        {
                            if (
                                (
                                    dangerPosition == null ||
                                    Vector3.Dot(((Vector3)dangerPosition - botPosition).normalized, (point.Position - botPosition).normalized) <= 0
                                )
                            )
                            {
                                float dist = (centerPosition - point.Position).sqrMagnitude;
                                if(dist <= closestSqrDistance && PointSet(point))
                                {
                                    closestSqrDistance = dist;
                                    point.CanIShootToEnemy = true;
                                    closestPoint = point;
                                }
                            }
                        }
                    }
                };

                return closestPoint;
            }

            return null;
        }

        private static bool IsNavigablePoint(BotOwner botOwner, CustomNavigationPoint point, float maxDistance)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            Vector3 botPosition = botOwner.Transform.position;

            navMeshPath.ClearCorners();
            bool result = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
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
        private static bool IsPointBetween(Vector3 point, Vector3 start, Vector3 end)
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
        private static List<CustomNavigationPoint> FilterEligiblePoints(BotOwner botOwner, List<CustomNavigationPoint> customNavigationPoints, float maxDistance, Func<CustomNavigationPoint, bool> eligibilityCheck)
        {
            List<CustomNavigationPoint> eligiblePoints = new List<CustomNavigationPoint>();

            foreach (CustomNavigationPoint point in customNavigationPoints)
            {
                if (eligibilityCheck(point) && IsNavigablePoint(botOwner, point, maxDistance))
                {
                    eligiblePoints.Add(point);
                }
            }
            return eligiblePoints;
        }
        private static CustomNavigationPoint GetClosestPoint(BotOwner botOwner,List<CustomNavigationPoint> eligiblePoints, Vector3 centerPosition, float maxDistance, Func<CustomNavigationPoint, bool> eligibilityCheck)
        {
            CustomNavigationPoint closestPoint = null;
            float closestSqrDistance = Mathf.Infinity;

            foreach (CustomNavigationPoint point in eligiblePoints)
            {
                if (eligibilityCheck(point) && IsNavigablePoint(botOwner, point, maxDistance))
                {
                    float sqrDistance = (centerPosition - point.Position).sqrMagnitude;
                    if (sqrDistance < closestSqrDistance)
                    {
                        closestPoint = point;
                        closestSqrDistance = sqrDistance;
                    }
                }
            }

            return closestPoint;
        }

    }
}
