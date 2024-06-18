using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Components.BossFollower
{
    internal class BossFollowLayer : FollowerLayer
    {

        protected float coverTimer = 0f;
        public BossFollowLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }


        public override bool ShallUseNow()
        {

            if (!HasBoss()) return false;

            botOwner_0.PriorityAxeTarget.FindTarget();

            if (!botOwner_0.Memory.HaveEnemy) return !InteractableObjects.IsTaker(botOwner_0);

            List<BotRequestType> fightdRequests = new List<BotRequestType>
            {
                BotRequestType.warnPlayer // this is need help or regroup from the player
            };

            return botOwner_0.BotRequestController.CurRequest != null && fightdRequests.Contains(botOwner_0.BotRequestController.CurRequest.BotRequestType);
        }

        public override string Name()
        {
            return "KnightFLP";
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            if (!HasBoss() || !botOwner_0.Memory.HaveEnemy) return base.GetDecision();

            bool ordersAreReqroup = botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer;

            float regroupMinDistance = friendlyPMC.regroupMinDistance.Value;
            float nearSearchRadius = friendlyPMC.fightInnerRadius.Value;
            float sprintDistance = 7f;
            Vector3 bossPosition = GetBoss().Position;

            if (ordersAreReqroup && GetNavDistance(bossPosition) > regroupMinDistance && (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible))
            {
                GetClosestCoverPoint(bossPosition, nearSearchRadius);

                if (customNavigationPoint_0 != null)
                {

                    if (GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "regroupToPlayerFast");
                    }
                    else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToPlayerSlow");
                    }
                }
                else
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "regroupFallback");
                }
            }

            return base.GetDecision();
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

        private void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1f + Time.time;

            List<CustomNavigationPoint> customNavigationPoints = GetBoss().GetAreaCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;

                NavMeshPath navMeshPath = new NavMeshPath();
                Vector3 botPosition = botOwner_0.Transform.position;

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (
                            point.IsFreeById(botOwner_0.Id) &&
                            !point.IsSpotted &&
                            (
                                !botOwner_0.Memory.HaveEnemy ||
                                (
                                    point.IsFreeById(botOwner_0.Memory.GoalEnemy.Owner.Id) &&
                                    point.IsDangerPositionFarEnough(new Vector3[] { botOwner_0.Memory.GoalEnemy.CurrPosition }, 5f)
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
                if (point1 != null)
                {
                    customNavigationPoint_0 = point1;
                    botOwner_0.Memory.SetCoverPoints(point1);
                }
                else
                {
                    customNavigationPoint_0 = null;
                }
            }
        }

    }
}