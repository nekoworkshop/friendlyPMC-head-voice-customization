using Aki.Common.Http;
using EFT;
using friendlyPMC.Actions;
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

        protected bool requestComeHere = false;
        protected bool requestGoThere = false;
        public BossFollowLayer(BotOwner bot, int priority) : base(bot, priority)
        {
        }

        public override bool ShallUseNow()
        {

            if (!HasBoss() || botOwner_0.Memory.HaveEnemy) return false;

            botOwner_0.PriorityAxeTarget.FindTarget();

            return !InteractableObjects.IsTaker(botOwner_0);
        }

        public override string Name()
        {
            return "BossFLP";
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            BotRequest request = botOwner_0.BotRequestController.CurRequest != null ? botOwner_0.BotRequestController.CurRequest : null;

            bool ordersAreReqroup = request != null && request.BotRequestType == BotRequestType.warnPlayer;

            float regroupMinDistance = friendlyPMC.regroupMinDistance.Value;
            float nearSearchRadius = friendlyPMC.fightInnerRadius.Value;
            float sprintDistance = 10f;
            Vector3 bossPosition = GetBossPosition();

            if(request == null)
            {
                requestGoThere = false;
                requestComeHere = false;
            } 
            else if (request.BotRequestType != BotRequestType.goToPoint)
            {
                requestGoThere = false;
            }
            else if (request.BotRequestType != BotRequestType.followMe && request.BotRequestType != BotRequestType.warnPlayer)
            {
                requestComeHere = false;
            }

            if (!botOwner_0.Memory.HaveEnemy && request != null)
            {
                if (request.BotRequestType == BotRequestType.goToPoint && !requestGoThere)
                {
                    requestGoThere = true;

                    botOwner_0.Gesture.TryGestus(EGesture.Good, false);

                    IPlayer requester = botOwner_0.BotRequestController.CurRequest.Requester;

                    Vector3 dir = requester.LookDirection;
                    float forwardDistance = GClass760.Random(3f, 5f);

                    Vector3 forwardPosition = requester.Position + dir.normalized * forwardDistance;
                    float lateralOffset = GClass760.RandomSing() * GClass760.Random(0.5f, 1.5f);
                    Vector3 lateralDirection = Vector3.Cross(Vector3.up, dir).normalized;

                    Vector3 finalPosition = forwardPosition + lateralDirection * lateralOffset;

                    botOwner_0.GoToSomePointData.SetPoint(finalPosition);
                    botOwner_0.Steering.LookToPoint(finalPosition);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, "req:moveThere");
                }

                if ((request.BotRequestType == BotRequestType.followMe || request.BotRequestType == BotRequestType.warnPlayer) && !requestComeHere)
                {
                    requestComeHere = true;
                    botOwner_0.Gesture.TryGestus(EGesture.Good, false);

                    IPlayer requester = botOwner_0.BotRequestController.CurRequest.Requester;

                    Vector3 requestPos = botOwner_0.BotRequestController.CurRequest.Requester.Position;
                    Vector3 dir = requester.LookDirection;

                    float offset = GClass760.RandomSing() * GClass760.Random(1f, 2f);
                    Vector3 direction = Vector3.Cross(Vector3.up, dir).normalized;

                    Vector3 finPos = requestPos + direction * offset;

                    botOwner_0.GoToSomePointData.SetPoint(new Vector3(finPos.x, requestPos.y, finPos.z));
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, "req:comeHere");
                }

                if (request.BotRequestType == BotRequestType.wait)
                {
                    botOwner_0.Gesture.TryGestus(EGesture.Good, false);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:holdPos");
                }
            }

            

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

        protected float GetNavDistance(Vector3 point)
        {
            return Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position,point);
        }

        protected virtual void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1f + Time.time;

            CustomNavigationPoint point = Utils.Utils.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);
        }

    }
}