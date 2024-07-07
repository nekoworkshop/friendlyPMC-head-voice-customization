using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Modules;
using friendlyPMC.Components;
using EFT.InventoryLogic;
using HarmonyLib;
using TMPro;

namespace friendlyPMC.Utils
{
    internal class Utils
    {
        public static List<WildSpawnType> BossFollowersRoles = new List<WildSpawnType> { 
            WildSpawnType.bossKnight, 
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye
        };

        
        public static float GetNavDistance(Vector3 point1, Vector3 point2)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
            navMeshPath.ClearCorners();
            bool resut = NavMesh.CalculatePath(point1, point2, -1, navMeshPath);

            if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                return navMeshPath.CalculatePathLength();
            }
            else
            {
                return Vector3.Distance(point2, point1);
            }
        }

        private static bool HasBoss(BotOwner botOwner)
        {
            return botOwner.BotFollower.HaveBoss;
        }

        private static pitAIBossPlayer GetBoss(BotOwner botOwner)
        {
            return (pitAIBossPlayer)botOwner.BotFollower.BossToFollow;
        }

        public static CustomNavigationPoint GetClosestCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, float safeDistance = 5f)
        {
            List<CustomNavigationPoint> customNavigationPoints = HasBoss(botOwner) ? GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;
                float sqrDistance = 0f;

                NavMeshPath navMeshPath = new NavMeshPath();
                Vector3 botPosition = botOwner.Transform.position;

                Func<CustomNavigationPoint,bool> PointSet = (CustomNavigationPoint point) =>
                {
                    navMeshPath.ClearCorners();
                    bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                    if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                    {

                        float dist = navMeshPath.CalculatePathLength();
                        if (dist > searchRadius)
                        {
                            return false;
                        }
                    }

                    return true;
                };

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    Vector3[] carePosition = new Vector3[] { };
                    foreach (var item in botOwner.EnemiesController.EnemyInfos)
                    {
                        carePosition = carePosition.AddItem(item.Value.CurrPosition).ToArray();
                    }

                    if (!point.IsDangerPositionFarEnough(carePosition, safeDistance * safeDistance)) continue;

                    if (
                            point.IsFreeById(botOwner.Id) &&
                            !point.IsSpotted &&
                            (
                                !botOwner.Memory.HaveEnemy ||
                                (
                                    point.IsFreeById(botOwner.Memory.GoalEnemy.Owner.Id)
                                )
                            )
                        )
                    {
                        float sqrrange = (centerPosition - point.Position).sqrMagnitude;

                        if(sqrrange <= sqrDistance)
                        {
                            if (PointSet(point))
                            {
                                point1 = point;
                                sqrDistance = sqrrange;
                                continue;
                            }
                        }

                        float range = Vector3.Distance(centerPosition, point.Position);
                        if (range < distance)
                        {
                            if (PointSet(point))
                            {
                                point1 = point;
                                distance = range;
                            }
                        }
                    }
                }

                return point1;
            }

            return null;
        }

        public static CustomNavigationPoint GetClosestCoverPointBetween(BotOwner botOwner, Vector3 pointA, Vector3 pointB, float safeDistance = 5f)
        {
            List<CustomNavigationPoint> customNavigationPoints = HasBoss(botOwner) ? GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float searchRadius = Vector3.Distance(pointA, pointB);

                float distance = searchRadius;
                float sqrDistance = 0f;

                NavMeshPath navMeshPath = new NavMeshPath();
                Vector3 botPosition = botOwner.Transform.position;

                Func<CustomNavigationPoint, bool> PointSet = (CustomNavigationPoint point) =>
                {
                    navMeshPath.ClearCorners();
                    bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                    if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                    {

                        float dist = navMeshPath.CalculatePathLength();
                        if (dist > searchRadius)
                        {
                            return false;
                        }
                    }

                    return true;
                };

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (!IsPointBetween(point.Position, pointA, pointB)) continue;

                    Vector3[] carePosition = new Vector3[] { };
                    foreach (var item in botOwner.EnemiesController.EnemyInfos)
                    {
                        carePosition = carePosition.AddItem(item.Value.CurrPosition).ToArray();
                    }

                    if (!point.IsDangerPositionFarEnough(carePosition, safeDistance * safeDistance)) continue;

                    if (
                            point.IsFreeById(botOwner.Id) &&
                            !point.IsSpotted &&
                            (
                                !botOwner.Memory.HaveEnemy ||
                                (
                                    point.IsFreeById(botOwner.Memory.GoalEnemy.Owner.Id)
                                )
                            )
                        )
                    {
                        float sqrrange = (pointA - point.Position).sqrMagnitude;

                        if (sqrrange <= sqrDistance && PointSet(point))
                        {
                            point1 = point;
                            sqrDistance = sqrrange;
                            continue;
                        }

                        float range = Vector3.Distance(pointA, point.Position);
                        if (range <= distance && PointSet(point))
                        {
                            point1 = point;
                            distance = range;
                        }
                    }
                }

            }

            return null;
        }
        public static CustomNavigationPoint GetCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius)
        {

            List<CustomNavigationPoint> customNavigationPoints = HasBoss(botOwner) ? GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;

                List<CustomNavigationPoint> availablePoints = new List<CustomNavigationPoint>();

                NavMeshPath navMeshPath = new NavMeshPath();
                Vector3 botPosition = botOwner.Transform.position;

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (point.IsFreeById(botOwner.Id) && !point.IsSpotted)
                    {

                        float range = Vector3.Distance(centerPosition, point.Position);
                        if (range < distance)
                        {
                            navMeshPath.ClearCorners();
                            bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                            if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                            {

                                float dist = navMeshPath.CalculatePathLength();
                                if (dist > searchRadius)
                                {
                                    continue;
                                }
                            }
                            distance = range;
                            availablePoints.Add(point);

                        }
                    }
                }
                // get a random point
                if (availablePoints.Count > 0)
                {
                    point1 = availablePoints.Random();
                }


                return point1;
            }

            return null;

        }

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
                customNavigationPoint = GetCoverPoint(botOwner,botOwner.GetPlayer.Transform.position, searchRadius);
            }


            return customNavigationPoint;
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

        public static CustomNavigationPoint GetApproachableCoverPoint(BotOwner botOwner, Vector3 point, float minDistance = 5f)
        {
            Vector3 midpoint = Vector3.Lerp(botOwner.GetPlayer.Transform.position, point, 0.5f);

            return GetClosestAttackCoverPoint(botOwner, midpoint, false, minDistance);
        }

        public static CustomNavigationPoint GetClosestAttackCoverPoint(BotOwner botOwner, Vector3 centerPosition, bool useFullCover = false, float minDistance = 5f, float maxDistance = 120f)
        {
            List<CustomNavigationPoint> customNavigationPoints = HasBoss(botOwner) && !useFullCover ? GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                if (customNavigationPoints.Count > 0)
                {
                    CustomNavigationPoint point1 = null;
                    float distance = !useFullCover && HasBoss(botOwner) ?  friendlyPMC.maximumRadius.Value : Mathf.Infinity;
                    float sqrdistance = 0f;

                    NavMeshPath navMeshPath = new NavMeshPath();
                    Vector3 botPosition = botOwner.Transform.position;

                    Func<CustomNavigationPoint, bool> PointSet = (CustomNavigationPoint point) =>
                    {
                        navMeshPath.ClearCorners();
                        bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                        if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                        {

                            float dist = navMeshPath.CalculatePathLength();
                            if (dist > distance)
                            {
                                return false;
                            }
                        }

                        return true;
                    };

                    foreach (CustomNavigationPoint point in customNavigationPoints)
                    {

                        Vector3[] carePosition = new Vector3[] { };

                        foreach (var item in botOwner.EnemiesController.EnemyInfos)
                        {
                            carePosition = carePosition.AddItem(item.Value.CurrPosition).ToArray();
                        }
                        
                        if (!point.IsDangerPositionFarEnough(carePosition, minDistance * minDistance)) continue;

                        if (
                                point.IsFreeById(botOwner.Id) &&
                                botOwner.Memory.HaveEnemy &&
                                GClass301.CanShoot(point.Position, botOwner.Memory.GoalEnemy)
                            )
                        {

                            Vector3 vrange = centerPosition - point.Position;
                            float enemyRange = Vector3.Distance(botOwner.Memory.GoalEnemy.CurrPosition, point.Position);

                            if(enemyRange > maxDistance)
                            {
                                continue;
                            }

                            float sqrrange = vrange.sqrMagnitude;

                            if(sqrrange <= sqrdistance )
                            {
                                if(PointSet(point))
                                {
                                    point.CanIShootToEnemy = true;
                                    point1 = point;
                                    sqrdistance = sqrrange;
                                    continue;
                                }
                            }

                            float range = vrange.magnitude;
                            if (range < distance)
                            {
                                if (PointSet(point))
                                {

                                    point.CanIShootToEnemy = true;
                                    point1 = point;
                                    distance = range;
                                }
                            }
                        }
                    }

                    return point1;
                }
            }

            return null;
        }
    }

    
}
