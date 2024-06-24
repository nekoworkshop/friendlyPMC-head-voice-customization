using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BirdEyeFightLayer : GClass62
    {
        protected bool ordersChanged = false;

        protected readonly float sprintDistance = 15f;

        protected float coverTimer = 0f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        public BirdEyeFightLayer(BotOwner bot, int priority) : base(bot, priority)
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
                    if (!botOwner_0.Memory.HaveEnemy)
                    {
                        GetClosestCoverPoint(bossPosition, friendlyPMC.fightOuterRadius.Value);
                    } else
                    {
                        GetClosestAttackCoverPoint(bossPosition);
                    }

                    if (customNavigationPoint_0 != null)
                    {

                        if (Utils.Utils.GetNavDistance(botPosition, customNavigationPoint_0.Position) > sprintDistance)
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

            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;

            if (baseDecision.Action == BotLogicDecision.holdPosition && request != null && request.BotRequestType == BotRequestType.attackClose)
            {

                GetApproachablePoint();

                if (customNavigationPoint_0 == null)
                {
                    GetCoverPoint(bossPosition, friendlyPMC.fightInnerRadius.Value);
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
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

            if (
                baseDecision.Action == BotLogicDecision.runToCover && 
                baseDecision.Reason != "еtoCov" && 
                baseDecision.Reason != "tc1" && 
                baseDecision.Reason != "run2Hold"
            )
            {

                GetClosestAttackCoverPoint(enemyPos, false, 10f);

                if (customNavigationPoint_0 == null)
                {
                    GetClosestAttackCoverPoint(bossPosition);
                }

                if (customNavigationPoint_0 == null)
                    GetClosestCoverPoint(bossPosition, friendlyPMC.fightOuterRadius.Value);

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }

            return baseDecision;
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            List<string> getInClose = new List<string>
            {
                "regroupToBossFast",
                "regroupToBossSlow",
                "getInCloseFast",
                "getInCloseSlow"
            };

            if (getInClose.Contains(curDecision.Reason))
            {
                return EndGetInClose();
            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        public override bool ShallUseNow()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                if (
                        botOwner_0.BotRequestController.CurRequest != null &&
                        (botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer ||
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.attackClose)
                    )
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                }

                return false;
            }

            return true;
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
            if (!HasBoss()) return false;
            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            float bossDist = Vector3.Distance(botOwner_0.Position, GetBoss().Position);

            return bossDist > Mathf.Min(friendlyPMC.maximumCoverDistance.Value, friendlyPMC.regroupMinDistance.Value) && (goalEnemy == null || !goalEnemy.HaveSeen || (goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime > friendlyPMC.maximumCover.Value));
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
            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0, 100f);


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
            CustomNavigationPoint point1 = Utils.Utils.GetCoverPoint(botOwner_0, centerPosition, searchRadius, true);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);
        }

        protected virtual void GetClosestAttackCoverPoint(Vector3 centerPosition, bool useFullCover = false, float minDistance = 5f)
        {
            CustomNavigationPoint cover = Utils.Utils.GetClosestAttackCoverPoint(botOwner_0,centerPosition,useFullCover, minDistance);
            customNavigationPoint_0 = cover;
            botOwner_0.Memory.SetCoverPoints(cover);
            if(cover != null)
            {
                botOwner_0.Memory.BotCurrentCoverInfo.SetCover(customNavigationPoint_0, true);
            }
        }

        protected virtual void GetApproachablePoint()
        {
            customNavigationPoint_0 = Utils.Utils.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition);
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);
        }
    }
}
