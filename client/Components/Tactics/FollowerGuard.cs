using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components.Tactics
{
    /** This class is not meant to be used directly as a brain layer, but within one **/
    internal class FollowerGuard : GClass61
    {
        protected float coverTimer = 0f;
        protected float holdTimer = 0f;

        protected readonly float fightRange = 10f;

        private bool existingCommon = false;

        private FollowerCommonLayer commonLayer;

        public CustomNavigationPoint NavigationPoint
        {
            get
            {
                return customNavigationPoint_0;
            }
        }

        public FollowerCommonLayer CommonLayer { get { return commonLayer; } }
        public FollowerGuard(BotOwner bot, int priority, FollowerCommonLayer commonLayer = null) : base(bot, priority)
        {
            if (commonLayer != null)
            {
                this.commonLayer = commonLayer;
                existingCommon = true;
            }
            else this.commonLayer = new FollowerCommonLayer(bot, priority);
        }

        public override string Name()
        {
            return "FBGuard";
        }
        // dummy 
        public override bool ShallUseNow()
        {
            return true;
        }

        public override void OnActivate()
        {
            base.OnActivate();
            if (!existingCommon) commonLayer?.OnActivate();
        }
        public override void Dispose()
        {
            base.Dispose();
            if (!existingCommon) commonLayer?.Dispose();
        }
        public void OrdersChanged()
        {
            commonLayer.OrdersChanged();
        }

        public bool ShallGoNearBoss()
        {
            return commonLayer.ShallGoNearBoss();
        }

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            commonLayer.DecisionChanged(prevDecision, nextDecision);
            base.DecisionChanged(prevDecision, nextDecision);
        }

        public override ShootPointClass GetShootPoint()
        {
            return botOwner_0.CurrentEnemyTargetPosition(true);
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;

            if (enemyVisible)
            {
                // enemy visible (can't shoot) and we are not in cover
                if (!botOwner_0.Memory.IsInCover)
                {
                    // - find cover to shoot from
                    GetClosestAttackCoverPoint(botPosition);
                    // - no attack cover, just find a cover 
                    if (customNavigationPoint_0 == null) GetClosestCoverPoint(botPosition, fightRange);

                    if (customNavigationPoint_0 != null && coverTimer < Time.time)
                    {
                        if (commonLayer.GetNavDistance(customNavigationPoint_0.Position) < 25f)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "relocate");
                        }
                        coverTimer = Time.time + GClass761.Random(3f, 5f);
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "relocateFast");
                    }

                    // - fallback
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dgf");

                } else
                {
                    // - try to shot enemy
                    if (botOwner_0.Memory.CurCustomCoverPoint != null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                    // - else find better spot
                    else
                    {
                        bool getClose = false;
                        if (
                            Utils.Enemy.Distance(botOwner_0) <= Utils.Enemy.EnemyDistance.Close &&
                            botOwner_0.Memory.AttackImmediately && Utils.Enemy.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, enemyPos) < 3)
                        {
                            GetClosestAttackCoverPoint(enemyPos, fightRange);
                            getClose = true;
                        } else
                        {
                            GetClosestAttackCoverPoint(botPosition, fightRange);
                        }
                            
                        if (customNavigationPoint_0 != null && coverTimer < Time.time)
                        {
                            coverTimer = Time.time + GClass761.Random(3f, 5f);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, getClose ? "getInCloseSlow" : "relocate");
                        }
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
                }
            }
            // enemy not visible
            else
            {
                // - approach enemy if close enough
                if(
                    Utils.Enemy.Distance(botOwner_0) <= Utils.Enemy.EnemyDistance.Close &&
                    botOwner_0.Memory.AttackImmediately && Utils.Enemy.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, enemyPos) < 3)
                {
                    GetClosestAttackCoverPoint(enemyPos);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToEnemy, "pushEnemy");
                } 
                else if (Utils.Enemy.Distance(botOwner_0) == Utils.Enemy.EnemyDistance.Mid)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.EnemySearch, "enemy.Search");
                } else if(!(botOwner_0.Memory.AttackImmediately && Utils.Enemy.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, enemyPos) < 3))
                {
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
                }

                // - look for a shooting spot
                GetClosestAttackCoverPoint(commonLayer.GetBoss().realPlayer.Transform.position);

                if (customNavigationPoint_0 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "relocateFast");
                }

                if(commonLayer.ShallGoNearBoss())
                {
                    customNavigationPoint_0 = commonLayer.GetClosestCoverPointGroup(commonLayer.GetBoss().realPlayer.Transform.position, commonLayer.coverSearchRadius);

                    if (customNavigationPoint_0 != null)
                    {
                        if (commonLayer.GetNavDistance(customNavigationPoint_0.Position) < commonLayer.sprintDistance)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToBoss");
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "regroupToBossFast");
                    }
                }

                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
            }
        }


        protected virtual void GetClosestAttackCoverPoint(Vector3 centerPosition, float minDistance = 15f)
        {
            customNavigationPoint_0 = commonLayer.GetClosestAttackCoverPoint(centerPosition, minDistance);
        }

        protected virtual void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool> extraChecks = null)
        {
            customNavigationPoint_0 = commonLayer.GetClosestCoverPoint(centerPosition, searchRadius, safeDistance, extraChecks);
        }
    }
}
