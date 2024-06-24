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

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightFightLayer : GClass65
    {

        protected float coverTimer = 0f;

        protected bool ordersChanged = false;

        protected readonly float sprintDistance = 15f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        private bool bool_14;
        public KnightFightLayer(BotOwner bot, int priority) : base(bot, priority)
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

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = base.InFightLogic();
            if (aicoreActionResultStruct != null)
            {
                return aicoreActionResultStruct.Value;
            }

            if (this.botOwner_0.Medecine.FirstAid.Have2Do || this.botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                if (!this.botOwner_0.Memory.HaveEnemy)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal2");
                }
                if (!BaseLogicLayerSimpleClass.CheckMedsToStop(this.botOwner_0))
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal1");
                }
            }

            var baseDecision = base.GetDecision();

            Vector3 botPosition = botOwner_0.GetPlayer.Position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;
            Vector3 enemyPos = botOwner_0.Memory.HaveEnemy ? botOwner_0.Memory.GoalEnemy.CurrPosition : botPosition;

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
                        GetClosestAttackCoverPoint(bossPosition,false,1f);
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
                GetClosestCoverPoint(GetBoss().Position, friendlyPMC.fightOuterRadius.Value);
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

            if(baseDecision.Action == BotLogicDecision.runToEnemy && baseDecision.Reason == "runToEnemy")
            {
                if(Utils.Utils.GetNavDistance(botPosition, enemyPos) > 25f)
                {
                    GetApproachablePoint();

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
            }

            return baseDecision;
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return gstruct7_0;
            }

            if (curDecision.Reason == "getInCloseFast" || curDecision.Reason == "getInCloseSlow" || curDecision.Reason == "am")
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

            if (!this.bool_14)
            {
                this.bool_14 = true;
                this.botOwner_0.Brain.BaseBrain.OnLayerChangedTo += this.OnLayerChanged;
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

        private void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {
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

        protected virtual void GetApproachablePoint()
        {
            customNavigationPoint_0 = Utils.Utils.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition,1f);
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
