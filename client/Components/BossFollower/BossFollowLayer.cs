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
            Vector3 bossPosition = GetBossPosition();

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