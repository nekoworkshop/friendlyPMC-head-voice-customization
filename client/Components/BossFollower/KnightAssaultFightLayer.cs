using EFT;
using friendlyPMC.Modules;
using System.Collections.Generic;
using System;
using UnityEngine.AI;
using UnityEngine;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightAssaultFightLayer : GClass34
    {
        protected CustomNavigationPoint customNavigationPoint_0 = null;
        protected float coverTimer = 0f;

        protected bool ordersChanged = false;

        protected readonly float sprintDistance = 15f;
        public KnightAssaultFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;

            if (ordersChanged && request != null && request.BotRequestType == BotRequestType.warnPlayer)
            {
                if (Utils.Utils.GetNavDistance(botPosition, bossPosition) > friendlyPMC.regroupMinDistance.Value && (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible))
                {
                    GetClosestCoverPoint(bossPosition, friendlyPMC.fightInnerRadius.Value);
                    if(customNavigationPoint_0 == null)
                    {
                        GetClosestCoverPoint(bossPosition, friendlyPMC.fightOuterRadius.Value);
                    }

                    if (customNavigationPoint_0 != null)
                    {

                        if (Utils.Utils.GetNavDistance(botPosition,customNavigationPoint_0.Position) > sprintDistance)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "regroupToBossFast");
                        }
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToBossSlow");
                        }
                    }
                    else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "regroupFallback");
                    }
                }
            }

            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            if (baseDecision.Action == BotLogicDecision.runToCover)
            {
                Vector3 center = botOwner_0.GetPlayer.Transform.position;

                if (baseDecision.Reason == "runToNtg")
                {
                    GetApproachablePoint();
                }
                else if (baseDecision.Reason == "runIfCoverF" || baseDecision.Reason == "Ambush")
                {
                    GetClosestCoverPoint(center, friendlyPMC.fightOuterRadius.Value);
                }
                else if (baseDecision.Reason == "ShallRunIfNoAmmo" || baseDecision.Reason == "LastDamageDataActive")
                {
                    GetClosestCoverPoint(center, friendlyPMC.fightOuterRadius.Value);
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }
            else if (baseDecision.Action == BotLogicDecision.attackMoving)
            {
                if (baseDecision.Reason == "attackNtg" || baseDecision.Reason == "Last")
                {
                    GetApproachablePoint();
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }
            else if (baseDecision.Action == BotLogicDecision.holdPosition && baseDecision.Reason == "hold2")
            {
                HoldFor(GClass760.Random(1f, 3f));
            }
            else if (baseDecision.Action == BotLogicDecision.search)
            {
                GetApproachablePoint();

                if (customNavigationPoint_0 == null)
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, friendlyPMC.fightInnerRadius.Value);
                }

                if (customNavigationPoint_0 == null)
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, friendlyPMC.fightOuterRadius.Value);
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "nowhereToGo");
                }
                else
                {
                    if (Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Position, customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }

            }

            return baseDecision;
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return gstruct7_0;
            }
            if (
                botOwner_0.BotRequestController.CurRequest != null &&
                botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer &&
                botOwner_0.Memory.HaveEnemy && !botOwner_0.Memory.GoalEnemy.IsVisible
            )
            {
                return gstruct7_0;
            }

            if (curDecision.Reason == "getInCloseFast" || curDecision.Reason == "getInCloseSlow")
            {
                return EndGetInClose();
            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            if (ordersChanged)
            {
                return new AICoreActionEndStruct("EndHol", true);
            }



            if (ShallGoNearBoss()) return new AICoreActionEndStruct("goNearPlayer", true);
            return base.EndHoldPosition();
        }

        public AICoreActionEndStruct EndGetInClose()
        {
            if (botOwner_0.Memory.HaveEnemy && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            return base.EndRunToCover();
        }

        public override bool CanSearchEnemy()
        {
            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            return goalEnemy == null || (!this.method_11(7f) && (!goalEnemy.IsVisible && !goalEnemy.CanShoot));
        }

        protected virtual bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        protected virtual pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }

        private bool ShallGoNearBoss()
        {
            if(!HasBoss()) return false;
            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            float bossDist = Vector3.Distance(botOwner_0.Position, GetBoss().Position);

            return bossDist > Mathf.Min(friendlyPMC.maximumCoverDistance.Value,friendlyPMC.regroupMinDistance.Value) && (goalEnemy == null || !goalEnemy.HaveSeen || (goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime > friendlyPMC.maximumCover.Value));
        }


        public void OrdersChanged()
        {
            ordersChanged = true;
            var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(1), false);
            Timer.OnTimer += () =>
            {
                ordersChanged = false;
            };
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0);


            return customNavigationPoint_0;
        }

        protected virtual void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1f + Time.time;

            CustomNavigationPoint point = Utils.Utils.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);
        }

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            CustomNavigationPoint point1 = Utils.Utils.GetCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);
        }

        protected virtual void GetApproachablePoint()
        {
            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            customNavigationPoint_0 = Utils.Utils.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition);
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);
        }
    }
}
