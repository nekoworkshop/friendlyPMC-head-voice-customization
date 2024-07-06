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
using Aki.Common.Http;
using friendlyPMC.Utils;

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

            try
            {
                return KnightFight();
            } catch (Exception ex)
            {
                Components.Logger.LogInfo("Error: " + ex.Message);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass760.Random(1f, 2f)), "decision.Error");
            }

        }

        public AICoreActionResultStruct<BotLogicDecision> KnightAssault()
        {
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            Utils.EnemyInfo.EnemyDistance distanceToEnemy = Utils.EnemyInfo.Distance(botOwner_0);

            // If the enemy is visible
            if (enemyVisible)
            {
                // If the enemy is close or mid-range
                if (distanceToEnemy <= Utils.EnemyInfo.EnemyDistance.Mid)
                {
                    // Rush towards the enemy while suppressing
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "assaultRush");
                }
                else
                {
                    // Find a cover point closer to the enemy
                    GetClosestAttackCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, false, 5f);
                    if (customNavigationPoint_0 != null)
                    {
                        // Move towards the cover point while suppressing the enemy
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMovingWithSuppress, "assaultApproach");
                    }
                    else
                    {
                        // No cover point found, move towards the enemy while suppressing
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "assaultRush");
                    }
                }
            }
            // If the enemy is not visible
            else
            {
                // Find a cover point closer to the enemy's last known position
                GetClosestAttackCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, false, 5f);
                if (customNavigationPoint_0 != null)
                {
                    // Move towards the cover point while suppressing
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMovingWithSuppress, "assaultApproach");
                }
                else
                {
                    // No cover point found, move towards the enemy's last known position while suppressing
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "assaultRush");
                }
            }
        }

        public AICoreActionResultStruct<BotLogicDecision> KnightFight()
        {
            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = request != null ? botOwner_0.BotRequestController.CurRequest.Requester.Position : botPosition;

            // player needs help or has call for a regroup
            if (
                ordersChanged && request != null &&
                request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup &&
                Utils.Utils.GetNavDistance(botPosition, bossPosition) > friendlyPMC.regroupMinDistance.Value
            )
            {
                if (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible)
                {
                    followerFightLayer.GetCloserToBoss();

                    
                }
                else
                {
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.OnFight, true);
                    request.Complete();
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

            if (HasBoss())
            {
                if (
                    (baseDecision.Action == BotLogicDecision.runToCover && baseDecision.Reason == "loseTarget") ||
                    (baseDecision.Action == BotLogicDecision.runToCover && baseDecision.Reason == "EnoughtHave") ||
                    (baseDecision.Action == BotLogicDecision.runToCover && baseDecision.Reason == "run3")

                )
                {
                    GetClosestCoverPoint(GetBoss().Position, friendlyPMC.fightOuterRadius.Value);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, baseDecision.Reason);
                    }
                }
            }

            if (baseDecision.Reason == "assault2" || baseDecision.Reason == "assault1")
            {
                return KnightAssault();
            }

            if (baseDecision.Reason == "IsDamaged" || baseDecision.Reason == "EnoughtHave")
            {
                GetCoverPoint(botPosition, fightLongRange);
                if (customNavigationPoint_0 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, baseDecision.Reason);
                }
            }

            return followerFightLayer.EngageEnemy();
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return gstruct7_0;
            }

            List<string> regroup = new List<string>
            {
                "regroupToBossFast",
                "regroupToBoss",
            };

            if (
                curDecision.Reason == "getInCloseFast" || 
                curDecision.Reason == "getInCloseSlow" || 
                curDecision.Reason == "am" || 
                curDecision.Reason == "repositionFast" || 
                curDecision.Reason == "reposition" ||
                (regroup.Contains(curDecision.Reason) && curDecision.Action == BotLogicDecision.goToPoint)
            )
            {
                return EndGetInClose();
            }

            List<string> ordersIgnoreDecisions = new List<string>
            {
                "healInCover",
                "DogFight",
                "heal"
            };

            List<BotLogicDecision> ordersAllowActions = new List<BotLogicDecision>
            {
                BotLogicDecision.holdPosition,
                BotLogicDecision.lay,
            };

            // orders changed
            if (ordersChanged &&
                !ordersIgnoreDecisions.Contains(curDecision.Reason) &&
                ordersAllowActions.Contains(curDecision.Action) &&
                (
                    !botOwner_0.Memory.HaveEnemy ||
                    !botOwner_0.Memory.GoalEnemy.HaveSeen ||
                    (!botOwner_0.Memory.GoalEnemy.IsVisible &&
                        Time.time - botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime < 3f &&
                        Utils.EnemyInfo.DistanceProxy(botOwner_0, botOwner_0.GetPlayer.Transform.position) > Utils.EnemyInfo.ProxyDistance.VeryClose
                    )
                )
            )
            {
                return gstruct7_0;
            }

            return base.ShallEndCurrentDecision(curDecision);
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
            if(botOwner_0.Memory.HaveEnemy) return new AICoreActionEndStruct("enemy.Present", true);
            return base.EndFollowerPatrolItem();
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
