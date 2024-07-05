using EFT;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using Newtonsoft.Json.Linq;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightFightLayer : GClass65
    {

        protected float coverTimer = 0f;

        protected readonly float sprintDistance = 15f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        protected bool ordersChanged = false;

        protected FollowerFightLayer followerFightLayer;

        protected bool bool_14;
        public KnightFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            followerFightLayer = new FollowerFightLayer(bot, priority);
        }

        protected bool HasBoss()
        {
            return followerFightLayer.HasBoss();
        }

        protected pitAIBossPlayer GetBoss()
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
            if (ordersChanged && request != null && request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup)
            {
                if (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible)
                {
                    GetClosestCoverPoint(bossPosition, friendlyPMC.fightOuterRadius.Value);

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
                return followerFightLayer.MarksManFight();
            }

            // player suggested to do a push
            if (ordersChanged && request != null && request.BotRequestType == BotRequestType.attackClose)
            {
                var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(2), false);

                Timer.OnTimer += () =>
                {
                    try
                    {
                        botOwner_0.BotRequestController.CurRequest.Complete();
                    }
                    catch { }
                };

                return followerFightLayer.EngageEnemy(true);
            }

            if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.Close)
            {
                return followerFightLayer.CloseFight();
            }

            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            Vector3 enemyPos = botOwner_0.Memory.HaveEnemy ? botOwner_0.Memory.GoalEnemy.CurrPosition : botPosition;

            // do not let Knight run long distances to an enemy
            if (
                (baseDecision.Action == BotLogicDecision.runToEnemy || baseDecision.Action == BotLogicDecision.runToEnemyZigZag) &&
                (Utils.EnemyInfo.Distance(botOwner_0) > Utils.EnemyInfo.EnemyDistance.Mid)
            )
            {
                return followerFightLayer.EngageEnemy();
            }

            if (HasBoss())
            {
                if (
                    (baseDecision.Action == BotLogicDecision.runToCover && baseDecision.Reason == "loseTarget") ||
                    (baseDecision.Action == BotLogicDecision.runToCover && baseDecision.Reason == "EnoughtHave") ||
                    (baseDecision.Action == BotLogicDecision.runToCover && baseDecision.Reason == "run3")

                )
                {
                    GetClosestCoverPoint(GetBoss().Position, friendlyPMC.fightOuterRadius.Value);
                    if (customNavigationPoint_0 == null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                    }
                }
            }

            if (baseDecision.Reason == "assault2")
            {
                GetClosestCoverPoint(botPosition, fightRange);
                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                } else
                {
                    if (Utils.Utils.GetNavDistance(botPosition, customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "repositionFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "reposition");
                }
            }

            if (baseDecision.Reason == "assault1")
            {
                GetApproachablePoint();
                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                } else
                {
                    if (Utils.Utils.GetNavDistance(botPosition, customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }
            }

            if ( baseDecision.Action == BotLogicDecision.runToCover && baseDecision.Reason == "nextPosible" && HasBoss())
            {
                GetClosestCoverPoint(bossPosition, followerFightLayer.bossOuterRadius);
                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }

            if (baseDecision.Reason == "IsDamaged" || baseDecision.Reason == "EnoughtHave") 
            {
                GetCoverPoint(botPosition, fightLongRange);
                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }

            if (baseDecision.Action == BotLogicDecision.attackMoving && baseDecision.Reason == "am")
            {
                if(!botOwner_0.Memory.HaveEnemy)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "regroupToBoss");
                }

                GetApproachablePoint();

                if (customNavigationPoint_0 == null)
                {
                    GetClosestCoverPoint(enemyPos, fightRange);
                }

                if (customNavigationPoint_0 == null)
                { 
                    GetCoverPoint(botPosition, fightRange);
                }

                if (customNavigationPoint_0 == null)
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

            if (
                curDecision.Reason == "getInCloseFast" || 
                curDecision.Reason == "getInCloseSlow" || 
                curDecision.Reason == "am" || 
                curDecision.Reason == "repositionFast" || 
                curDecision.Reason == "reposition"
            )
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
                        (botOwner_0.BotRequestController.CurRequest.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup ||
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.attackClose)
                    )
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                }

                return false;
            }

            if (!this.bool_14)
            {
                this.bool_14 = true;
                this.botOwner_0.Brain.BaseBrain.OnLayerChangedTo += this.OnLayerChanged;
            }

            return true;
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0);
            return customNavigationPoint_0;
        }

        public AICoreActionEndStruct EndGetInClose()
        {
            return followerFightLayer.EndGetInClose();
        }
        public override AICoreActionEndStruct EndGoToPoint()
        {
            return followerFightLayer.EndGoToPoint();
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
        public override AICoreActionEndStruct EndTakeItem()
        {
            return followerFightLayer.EndTakeItem();
        }
        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            return followerFightLayer.EndFollowerPatrolItem();
        }

        protected void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = followerFightLayer.GetClosestCoverPoint(centerPosition, searchRadius);
        }
        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = followerFightLayer.GetCoverPoint(centerPosition, searchRadius);
        }

        protected void GetApproachablePoint()
        {
            customNavigationPoint_0 = followerFightLayer.GetApproachablePoint();
        }

        protected void GetClosestAttackCoverPoint(Vector3 centerPosition, bool useFullCover = false, float minDistance = 5f)
        {
            customNavigationPoint_0 = followerFightLayer.GetClosestAttackCoverPoint(centerPosition, useFullCover, minDistance);
        }
    }
}
