using EFT;
using JetBrains.Annotations;
using System;
using UnityEngine;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BigPipeArtilleryLayer : GClass51
    {

        private readonly float sprintDistance = 15f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        protected bool ordersChanged = false;

        private FollowerFightLayer followerFightLayer;
        public BigPipeArtilleryLayer([NotNull] BotOwner owner, int priority) : base(owner, priority)
        {
            followerFightLayer = new FollowerFightLayer(owner,priority);
        }

        private bool HasBoss()
        {
            return followerFightLayer.HasBoss();
        }

        private pitAIBossPlayer GetBoss()
        {
            return followerFightLayer.GetBoss();
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

            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;

            // is in dogfight?
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = followerFightLayer.DogFight();

            if (aicoreActionResultStruct != null)
            {
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }
            // needs healing?
            aicoreActionResultStruct = followerFightLayer.NeedHeal();
            if (aicoreActionResultStruct != null)
            {
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }

            // player needs help
            if (ordersChanged && request != null && request.BotRequestType == BotRequestType.warnPlayer)
            {
                if (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible)
                {
                    if (!botOwner_0.Memory.HaveEnemy)
                    {
                        GetClosestCoverPoint(bossPosition, friendlyPMC.fightOuterRadius.Value);
                    }
                    else
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

            // do not pursue a marksman
            if (botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman))
            {
                return followerFightLayer.DefendPosition();
            }

            Vector3 enemyPos = botOwner_0.Memory.HaveEnemy ? botOwner_0.Memory.GoalEnemy.CurrPosition : botPosition;


            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            if (ordersChanged && baseDecision.Action == BotLogicDecision.holdPosition && request != null && request.BotRequestType == BotRequestType.attackClose)
            {
                botOwner_0.BotRequestController.CurRequest.Complete();

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

            // do not let BigPipe run long distances to an enemy
            if(
                (baseDecision.Action == BotLogicDecision.runToEnemy || baseDecision.Action == BotLogicDecision.runToEnemyZigZag) &&
                (Utils.EnemyInfo.Distance(botOwner_0) > Utils.EnemyInfo.EnemyDistance.Mid || !botOwner_0.Memory.AttackImmediately)
             )
            {
                return followerFightLayer.DefendPosition();
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
            return followerFightLayer.EndGetInClose();
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            return followerFightLayer.EndHoldPosition();
        }

        public override AICoreActionEndStruct EndRunToEnemy()
        {
            return followerFightLayer.EndRunToEnemy();
        }

        public override AICoreActionEndStruct EndHeal()
        {
            return followerFightLayer.EndHeal();
        }


        private void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = followerFightLayer.GetClosestCoverPoint(centerPosition,searchRadius);
        }

        private void GetApproachablePoint()
        {
            customNavigationPoint_0 = followerFightLayer.GetApproachablePoint();
        }

        private void GetClosestAttackCoverPoint(Vector3 centerPosition, bool useFullCover = false, float minDistance = 5f)
        {
            customNavigationPoint_0 = followerFightLayer.GetClosestAttackCoverPoint(centerPosition,useFullCover,minDistance);
        }
    }
}
