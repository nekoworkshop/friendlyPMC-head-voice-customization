using EFT;
using friendlyPMC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static RootMotion.FinalIK.IKSolver;

namespace friendlyPMC.Components.Tactics
{
    internal class FollowerCommonLayer
    {
        private BotOwner botOwner_0;


        private CustomNavigationPoint customNavigationPoint_0;
        private CustomNavigationPoint customNavigationPoint_1;
        private CustomNavigationPoint customNavigationPoint_2;
        private CustomNavigationPoint customNavigationPoint_3;

        private float coverTimer_0 = 0f;
        private float coverTimer_1 = 0f;
        private float coverTimer_2 = 0f;
        private float holdTimer = 0f;
        public FollowerCommonLayer(BotOwner bot)
        {
            botOwner_0 = bot;
        }

        public bool HasBoss()
        {
            return Utils.Utils.HasBoss(botOwner_0);
        }

        public pitAIBossPlayer GetBoss()
        {
            return Utils.Utils.GetBoss(botOwner_0);
        }

        public void ResetTimer(string timer)
        {
            if (timer == "coverTimer_0")
                coverTimer_0 = 0f;
            else if (timer == "coverTimer_1")
                coverTimer_1 = 0f;
            else if (timer == "coverTimer_2")
                coverTimer_2 = 0f;
            else if (timer == "holdTimer")
                holdTimer = 0f;
        }

        /** Find a shoot positionm that is closest to the enemy but at a minimum distance and maximum from the enemy **/
        public CustomNavigationPoint GetClosestAttackCoverPoint(Vector3 centerPosition, float minDistance = 5f, float maxDistance = 150f)
        {
            if (coverTimer_1 > Time.time) return customNavigationPoint_1;

            coverTimer_1 = 1f + Time.time;

            customNavigationPoint_1 = Covers.GetClosestAttackCoverPoint(botOwner_0, centerPosition, minDistance, maxDistance);
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_1);
            return customNavigationPoint_1;
        }
        /** Find a shoot position between bot and enemy, that is the closest to the middle point between bot and enemy **/
        public CustomNavigationPoint GetApproachablePoint()
        {
            if (coverTimer_1 > Time.time) return customNavigationPoint_1;

            coverTimer_1 = 1f + Time.time;

            customNavigationPoint_1 = Covers.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition);

            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_1);
            return customNavigationPoint_1;
        }
        /** Find the closest cover point to the given position, within the given radius and ensuring it is at minimum safeDistance from danger **/
        public CustomNavigationPoint GetClosestCoverPoint(Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool> extraChecks = null)
        {
            if (coverTimer_2 > Time.time) return customNavigationPoint_2;

            coverTimer_2 = 1f + Time.time;

            CustomNavigationPoint point = Covers.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius, safeDistance, extraChecks);

            customNavigationPoint_2 = point;
            
            botOwner_0.Memory.SetCoverPoints(point);

            return customNavigationPoint_2;
        }
        /** Find closest cover point at the given position taking into cosideration the rest of the followers **/
        public CustomNavigationPoint GetClosestCoverPointGroup(Vector3 centerPosition, float searchRadius)
        {
            if (!HasBoss() || GetBoss().Followers.Count < 2)
            {
                return GetClosestCoverPoint(centerPosition, searchRadius);
            }

            if (this.coverTimer_2 > Time.time) return customNavigationPoint_2;

            this.coverTimer_2 = 1.5f + Time.time;

            float maxInnerRadius = searchRadius;

            Vector3 botPosition = botOwner_0.Transform.position;

            NavMeshPath _navMeshPath = new NavMeshPath();

            customNavigationPoint_2 = Covers.ClosestPoint(botOwner_0, centerPosition, (CustomNavigationPoint point) =>
            {
                if (IsPointFreeGroup(point)) return false;

                float range = Vector3.Distance(centerPosition, point.Position);
                if (range < maxInnerRadius)
                {
                    _navMeshPath.ClearCorners();
                    bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, _navMeshPath);
                    if (resut && _navMeshPath.status == NavMeshPathStatus.PathComplete)
                    {

                        float dist = _navMeshPath.CalculatePathLength();
                        if (dist > maxInnerRadius)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return true;
            }, 10f);

            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_2);

            return customNavigationPoint_2;

        }
        /** Find a random cover point at the given position, within the given radius **/
        public CustomNavigationPoint GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (coverTimer_0 > Time.time) return customNavigationPoint_0;

            coverTimer_0 = 1f + Time.time;

            CustomNavigationPoint point1 = Covers.GetCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);
            return customNavigationPoint_0;

        }
        /** Find the closest safe cover point to the given position, within the given radius **/
        public CustomNavigationPoint GetClosestSafeCoverPoint(Vector3 centerPosition, float safeDistance = 10f)
        {
            Vector3 dangerPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            NavMeshPath navMeshPath = new NavMeshPath();

            CustomNavigationPoint point = Covers.ClosestPoint(botOwner_0, centerPosition, (CustomNavigationPoint pt) => {
                bool good = true;
                // should not be seen by any enemy
                foreach (var enemy in botOwner_0.EnemiesController.EnemyInfos)
                {
                    if (enemy.Value.Person.HealthController.IsAlive && !Covers.CheckCoverVisibility(pt.Position, enemy.Value.Person.Transform.position))
                    {
                        good = false;
                    }
                }

                if (good)
                {
                    navMeshPath.ClearCorners();
                    bool result = NavMesh.CalculatePath(centerPosition, pt.Position, -1, navMeshPath);
                    if (result && navMeshPath.status == NavMeshPathStatus.PathComplete)
                    {
                        float dist = navMeshPath.CalculatePathLength();

                        if (dist > Vector3.Distance(botPosition, pt.Position) + 20f) good = false;
                    }
                    else good = false;
                }

                return good;

            }, safeDistance);

            customNavigationPoint_3 = point;
            botOwner_0.Memory.SetCoverPoints(point);
  
            return customNavigationPoint_3;
        }

        /** Find closest cover point to pointA between pointA and pointB ensuring it is at minimum safeDistance from danger **/
        public CustomNavigationPoint GetClosestCoverPointBetween(Vector3 pointA, Vector3 pointB, float safeDistance = 5f)
        {
            CustomNavigationPoint point = Covers.GetClosestCoverPointBetween(botOwner_0, pointA, pointB, safeDistance);

            customNavigationPoint_3 = point;
            botOwner_0.Memory.SetCoverPoints(point);

            return customNavigationPoint_3;
        }
        /** Is point free by the followers group **/
        public bool IsPointFreeGroup(CustomNavigationPoint point)
        {
            if (!HasBoss()) return point.IsFreeById(botOwner_0.Id);

            bool isfree = true;

            foreach (var follower in GetBoss().Followers)
            {
                if (follower.Id != botOwner_0.Id && !point.IsFreeById(follower.Id))
                {
                    isfree = false;
                    break;
                }
            }
            return isfree;

        }
    }
}
