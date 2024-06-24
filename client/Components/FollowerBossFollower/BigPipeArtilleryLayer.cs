using Aki.Common.Http;
using EFT;
using friendlyPMC.Modules;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BigPipeArtilleryLayer : GClass51
    {
        private float coverTimer = 0f;

        private readonly float sprintDistance = 15f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        protected bool ordersChanged = false;
        public BigPipeArtilleryLayer([NotNull] BotOwner owner, int priority) : base(owner, priority)
        {
        }

        private bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        private pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
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

            return base.ShallUseNow();
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            Vector3 botPosition = botOwner_0.GetPlayer.Position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;
            Vector3 enemyPos  = botOwner_0.Memory.HaveEnemy ? botOwner_0.Memory.GoalEnemy.CurrPosition : botPosition;

            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            if (ordersChanged && baseDecision.Action == BotLogicDecision.holdPosition && request != null && request.BotRequestType == BotRequestType.attackClose)
            {
                GetApproachablePoint();

                if (customNavigationPoint_0 == null)
                {
                    GetClosestCoverPoint(enemyPos, fightRange);
                }

                if (customNavigationPoint_0 == null)
                {
                    GetClosestCoverPoint(enemyPos, fightLongRange);
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "runToEnemy");
                }
                else
                {
                    if (Utils.Utils.GetNavDistance(botPosition, customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }
            }

            if (ordersChanged && request != null && request.BotRequestType == BotRequestType.warnPlayer)
            {
                if (Utils.Utils.GetNavDistance(botPosition, bossPosition) > friendlyPMC.regroupMinDistance.Value && (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible))
                {
                    if (!botOwner_0.Memory.HaveEnemy)
                    {
                        GetClosestCoverPoint(bossPosition, fightRange);
                    }
                    else
                    {
                        GetClosestAttackCoverPoint(bossPosition, false, 1f);
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



            if (baseDecision.Action == BotLogicDecision.search)
            {
                GetApproachablePoint();
                if (customNavigationPoint_0 == null)
                {
                    GetClosestCoverPoint(botPosition, fightLongRange);
                }
                
                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "followerFallback");
                } else
                {
                    if (Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Position, customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }

            }
            else if(baseDecision.Action == BotLogicDecision.runToCover && baseDecision.Reason != "toSFC") {
                if (baseDecision.Reason == "InSmoke")
                {
                    GetClosestCoverPoint(bossPosition, fightRange);
                }

                if(baseDecision.Reason == "Wannashoot5")
                {
                    GetApproachablePoint();
                }

                if(customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
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

            if (curDecision.Reason == "getInCloseFast" || curDecision.Reason == "getInCloseSlow")
            {
                return EndGetInClose();
            }

            return base.ShallEndCurrentDecision(curDecision);
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


        private void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1f + Time.time;

            CustomNavigationPoint point = Utils.Utils.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);
        }

        protected virtual void GetApproachablePoint()
        {
            customNavigationPoint_0 = Utils.Utils.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition, 5f);
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);
        }

        protected virtual void GetClosestAttackCoverPoint(Vector3 centerPosition, bool useFullCover = false, float minDistance = 5f)
        {
            CustomNavigationPoint cover = Utils.Utils.GetClosestAttackCoverPoint(botOwner_0, centerPosition, useFullCover, minDistance);
            customNavigationPoint_0 = cover;
            botOwner_0.Memory.SetCoverPoints(cover);
            if (cover != null)
            {
                botOwner_0.Memory.BotCurrentCoverInfo.SetCover(customNavigationPoint_0, true);
            }
        }
    }
}
