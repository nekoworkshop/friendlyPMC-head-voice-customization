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

        public static CustomNavigationPoint GetClosestCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, bool useFullCover = false)
        {
            List<CustomNavigationPoint> customNavigationPoints = HasBoss(botOwner) && !useFullCover ? GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;

                NavMeshPath navMeshPath = new NavMeshPath();
                Vector3 botPosition = botOwner.Transform.position;

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (
                            point.IsFreeById(botOwner.Id) &&
                            !point.IsSpotted &&
                            (
                                !botOwner.Memory.HaveEnemy ||
                                (
                                    point.IsFreeById(botOwner.Memory.GoalEnemy.Owner.Id) &&
                                    point.IsDangerPositionFarEnough(new Vector3[] { botOwner.Memory.GoalEnemy.CurrPosition }, 3f * 3f)
                                )
                            )
                        )
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
                            point1 = point;
                            distance = range;
                        }
                    }
                }

                return point1;
            }

            return null;
        }

        public static CustomNavigationPoint GetCoverPoint(BotOwner botOwner, Vector3 centerPosition, float searchRadius, bool useFullCover = false)
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

        public static CustomNavigationPoint GetApproachableCoverPoint(BotOwner botOwner, Vector3 point, float mindDistance = 5f)
        {
            Vector3 midpoint = Vector3.Lerp(botOwner.GetPlayer.Transform.position, point, 0.5f);
            float distance = Vector3.Distance(midpoint, point);

            return GetClosestAttackCoverPoint(botOwner, midpoint, false, mindDistance);
        }

        public static CustomNavigationPoint GetClosestAttackCoverPoint(BotOwner botOwner, Vector3 centerPosition, bool useFullCover = false, float minDistance = 5f)
        {
            List<CustomNavigationPoint> customNavigationPoints = HasBoss(botOwner) && !useFullCover ? GetBoss(botOwner).GetAreaCovers() : BossPlayers.GetAICovers();

            if (customNavigationPoints.Count > 0)
            {
                if (customNavigationPoints.Count > 0)
                {
                    CustomNavigationPoint point1 = null;
                    float distance = !useFullCover && HasBoss(botOwner) ?  friendlyPMC.maximumRadius.Value : Mathf.Infinity;

                    NavMeshPath navMeshPath = new NavMeshPath();
                    Vector3 botPosition = botOwner.Transform.position;

                    foreach (CustomNavigationPoint point in customNavigationPoints)
                    {
                        if (
                                point.IsFreeById(botOwner.Id) &&
                                botOwner.Memory.HaveEnemy &&
                                GClass301.CanShoot(point.Position, botOwner.Memory.GoalEnemy) &&
                                point.IsDangerPositionFarEnough(new Vector3[] { botOwner.Memory.GoalEnemy.CurrPosition }, minDistance * minDistance)
                            )
                        {
                            float range = Vector3.Distance(centerPosition, point.Position);
                            if (range < distance)
                            {
                                navMeshPath.ClearCorners();
                                bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                                if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                                {

                                    float dist = navMeshPath.CalculatePathLength();
                                    if (dist > distance)
                                    {
                                        continue;
                                    }
                                }
                                point1 = point;
                                distance = range;
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
