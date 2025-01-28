using EFT;
using System;
using friendlyPMC.Modules;
using UnityEngine;

namespace friendlyPMC.Components.Tactics
{
    /** 
     * This class is not meant to be used directly as a brain layer, but within one 
     * Enemy Push layer based on Boar follower fight logic
     * **/
    public class FollowerPusherLayer : GClass48
    {
        private FollowerCommonLayer commonLayer;

        public FollowerCommonLayer CommonLayer { get { return commonLayer; } }

        private bool existingCommon = false;

        public CustomNavigationPoint NavigationPoint
        {
            get
            {
                return customNavigationPoint_0;
            }
        }

        protected float holdTimer = 0f;

        public FollowerPusherLayer(BotOwner bot, int priority, FollowerCommonLayer commonLayer = null) : base(bot, priority)
        {
            if (commonLayer != null)
            {
                this.commonLayer = commonLayer;
                existingCommon = true;
            }
            else this.commonLayer = new FollowerCommonLayer(bot, priority);
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

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            commonLayer.DecisionChanged(prevDecision, nextDecision);
            base.DecisionChanged(prevDecision, nextDecision);
        }

        public AICoreActionResultStruct<BotLogicDecision> EngageEnemy(bool pushOrdered = false)
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            float lastEnemySeenTime = botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime;
            bool inCover = botOwner_0.Memory.IsInCover;

            Utils.Enemy.EnemyDistance distanceToEnemy = Utils.Enemy.Distance(botOwner_0);
            float enemiesAtLocation = 0;
            if (botOwner_0.Memory.GoalEnemy.ProfileId != null)
                enemiesAtLocation = Utils.Enemy.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, enemyPos);

            // PUSH CASE
            if (botOwner_0.Memory.AttackImmediately || pushOrdered)
            {
                if (
                    // - go for it if enemy is already close and if its low in numbers
                    (distanceToEnemy <= Utils.Enemy.EnemyDistance.Close && enemiesAtLocation < 2) ||
                    // - go for it if ordered
                    (pushOrdered && enemiesAtLocation < 4)
                )
                {
                    BotLogicDecision pushDecision;

                    if (pushOrdered) pushDecision = BotLogicDecision.runToEnemy;
                    else if (distanceToEnemy <= Utils.Enemy.EnemyDistance.Close) pushDecision = BotLogicDecision.goToEnemy;
                    else pushDecision = BotLogicDecision.runToEnemy;

                    // - check if the current enemy is the closest and do a slow approach if not
                    if (!Utils.Enemy.IsClosestEnemy(botOwner_0))
                    {
                        pushDecision = BotLogicDecision.goToEnemy;
                    }

                    // -- push if not visible or ordered
                    if (!enemyVisible || pushOrdered)
                        return new AICoreActionResultStruct<BotLogicDecision>(pushDecision, "pushEnemy");
                    else
                    {
                        if (distanceToEnemy >= Utils.Enemy.EnemyDistance.Mid)
                        {
                            botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Attack);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                        }
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToCoverPointTactical, "getInCloseSlow");
                    }
                }

                // - enemy visible and push conditions not met
                if (enemyVisible)
                {
                    if (distanceToEnemy <= Utils.Enemy.EnemyDistance.Mid)
                    {
                        // -- in cover
                        if (inCover)
                        {
                            // --- try to shot enemy
                            if (enemiesAtLocation < 3 || (botOwner_0.Memory.CurCustomCoverPoint != null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy))
                            {
                                if (botOwner_0.Memory.CurCustomCoverPoint != null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                                // --- else find better spot
                                else
                                {
                                    botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Attack);
                                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMovingWithSuppress, "relocate");
                                }
                            }
                            // --- too many enemies, find better spot or hold out
                            else
                            {
                                if (holdTimer < Time.time)
                                {

                                    botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Ambush);
                                    float timer = GClass824.Random(2f, 5f);
                                    holdTimer = Time.time + timer + GClass824.Random(2f, 3f);
                                    return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                                }
                                else
                                {
                                    GetApproachablePoint();

                                    botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Protect);
                                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToCoverPointTactical, "relocate");
                                }
                            }


                        }
                        // -- not in cover
                        else
                        {
                            GetApproachablePoint();
                            botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Ambush);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "findCover");
                        }
                    }
                    // -- enemy is distant but visible
                    else
                    {
                        GetClosestCoverPointBetween(botPosition, enemyPos);
                        if (customNavigationPoint_0 != null)
                        {
                            botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Attack);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "approachEnemyFast");
                        }
                        else
                        {
                            // No cover point found, hold position temporarily
                            if (holdTimer < Time.time)
                            {
                                botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Ambush);
                                float timer = GClass824.Random(2f, 5f);
                                holdTimer = Time.time + timer + GClass824.Random(2f, 3f);
                                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                            }
                        }
                    }
                }
                // - enemy not visible and push conditions not met
                else
                {

                    GetClosestCoverPointBetween(enemyPos, botPosition);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "approachEnemyFast");
                    }

                    return EnemySearch();
                }
                // play the intimidation game 
            }
            else
            {
                // - enemy is visible
                if (enemyVisible)
                {
                    // -- if the bot is in cover
                    if (inCover)
                    {
                        // --- find an approachable point towards the enemy
                        GetApproachablePoint();
                        if (customNavigationPoint_0 != null)
                        {
                            botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Attack);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToCoverPointTactical, "approachEnemy");
                        }
                        else
                        {
                            // --- no approachable point found, hold position temporarily
                            if (holdTimer < Time.time)
                            {

                                botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Ambush);
                                float timer = GClass824.Random(2f, 5f);
                                holdTimer = Time.time + timer + GClass824.Random(2f, 3f);
                                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                            }
                        }
                    }
                    // -- if the bot is not in cover
                    else
                    {
                        // --- find a cover point closer to the enemy
                        GetClosestCoverPoint(enemyPos, 120f);

                        if (customNavigationPoint_0 != null)
                        {

                            botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Attack);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                        }
                        else
                        {
                            // --- no cover point found, hold position temporarily
                            if (holdTimer < Time.time)
                            {

                                botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Ambush);
                                float timer = GClass824.Random(2f, 5f);
                                holdTimer = Time.time + timer + GClass824.Random(2f, 3f);
                                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                            }
                            else
                                return EnemySearch();
                        }
                    }
                }
                // - if the enemy is not visible
                else if (Time.time - lastEnemySeenTime < GClass824.Random(2f, 5f))
                {
                    // -- find a cover point closer to the enemy's last known position
                    GetApproachablePoint();

                    AICoreActionResultStruct<BotLogicDecision>? previousDecision = commonLayer.CurrentDecision;

                    if (customNavigationPoint_0 != null && previousDecision?.Reason != "getInCloseFast" && previousDecision?.Reason != "waitAbit")
                    {

                        botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Attack);
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }
                    else
                    {
                        // -- no cover point found, approach the enemy
                        return EnemySearch();
                    }
                }
            }

            // --  fallback If no conditions are met
            if (holdTimer < Time.time)
            {

                botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Ambush);
                float timer = GClass824.Random(2f, 5f);
                holdTimer = Time.time + timer + GClass824.Random(2f, 3f);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "waitAbit");
            }
            else
            {
                if (enemyVisible)
                {
                    //botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dgf");
                }
                else
                {
                    AICoreActionResultStruct<BotLogicDecision>? previousDecision = commonLayer.CurrentDecision;

                    if (Utils.Enemy.Distance(botOwner_0) > Utils.Enemy.EnemyDistance.Mid && previousDecision?.Reason != "getInCloseFast" && previousDecision?.Reason != "waitAbit")
                    {
                        GetClosestCoverPointBetween(botPosition, enemyPos);
                        if (customNavigationPoint_0 != null)
                        {

                            botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Attack);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                        }
                    }

                    return EnemySearch();
                }

            }
        }

        public AICoreActionResultStruct<BotLogicDecision> EnemySearch(string reason = null)
        {

            botOwner_0.Tactic.SetTactic(BotsGroup.BotCurrentTactic.Ambush);
            return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.EnemySearch, reason != null ? reason : "enemy.Search");
        }

        public AICoreActionEndStruct? ShallEndDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (
                curDecision.Action == (BotLogicDecision)CustomBotDecisions.EnemySearch
            )
            {
                return EndEnemySearch();
            }

            return null;
        }

        public override AICoreActionEndStruct EndRunToEnemy()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            if (botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            return base.EndRunToEnemy();
        }


        public AICoreActionEndStruct EndEnemySearch()
        {
            if (commonLayer.OrderHasChangedRecently)
                return new AICoreActionEndStruct("search.End", true);

            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            if (botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if (Time.time - commonLayer.LastTimeHit <= 0.5f)
            {
                return new AICoreActionEndStruct("enemy.ShotMe", true);
            }

            if (Utils.Enemy.Distance(botOwner_0) <= Utils.Enemy.EnemyDistance.VeryClose)
            {
                return new AICoreActionEndStruct("enemy.Close", true);
            }

            return aICoreActionEndStruct;
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            try
            {
                if (commonLayer.OrderHasChangedRecently)
                {
                    return new AICoreActionEndStruct("EndHol", true);
                }


                AIBossPlayerLogic gclass363_0 = commonLayer.HasBoss() ? commonLayer.GetBoss().GetBossLogic() : null;

                string text;
                if (botOwner_0.Memory.HaveEnemy && botOwner_0.Memory.GoalEnemy.Person.HealthController.IsAlive && base.method_5(out text))
                {
                    return new AICoreActionEndStruct("cst", true);
                }

                if (commonLayer.TimeToHeal())
                {
                    return new AICoreActionEndStruct("wntHeal", true);
                }

                if (this.customNavigationPoint_0 != null && !this.customNavigationPoint_0.IsFreeById(this.botOwner_0.Id))
                {
                    this.customNavigationPoint_0 = null;
                }
                if (this.customNavigationPoint_0 != null && this.customNavigationPoint_0.CanIShootToEnemy && this.botOwner_0.Memory.IsInCover && this.botOwner_0.Memory.BotCurrentCoverInfo.CovPoint.Id != this.customNavigationPoint_0.Id && Time.time - this.botOwner_0.Memory.ComeToCoverTime > 3f && gclass363_0 != null)
                {
                    this.botOwner_0.Memory.Spotted(false, null, null);
                    this.botOwner_0.Memory.BotCurrentCoverInfo.SetCover(this.customNavigationPoint_0, true);
                    GClass405 gclass = gclass363_0;
                    if (gclass != null)
                    {
                        gclass.StartMoveToAttackPoint(botOwner_0.Id);
                    }
                    return new AICoreActionEndStruct("betterCover", true);
                }


                if (commonLayer.ShallGoNearBoss()) return new AICoreActionEndStruct("goNearBoss", true);

                if (base.method_6())
                {
                    return new AICoreActionEndStruct("EndHol", true);
                }
                if (!this.botOwner_0.Memory.IsInCover)
                {
                    return new AICoreActionEndStruct("notInCover", true);
                }
                EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
                if (goalEnemy != null && goalEnemy.IsVisible && goalEnemy.CanShoot)
                {
                    return new AICoreActionEndStruct("CanShoot", true);
                }
                if (gclass363_0.IsHitted && commonLayer.coverType == "close")
                {
                    return new AICoreActionEndStruct("bossHit", true);
                }


                //GetCoverPoint(botOwner_0.GetPlayer.Transform.position, nearSearchRadius);

                return aICoreActionEndStruct_1;
            }
            catch (Exception e)
            {
                Modules.Logger.LogError("EndHoldPosition Error");
                Modules.Logger.LogError(e);
                return new AICoreActionEndStruct("hpError", true);
            }
        }

        public CustomNavigationPoint GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = commonLayer.GetClosestCoverPoint(centerPosition, searchRadius);

            return customNavigationPoint_0;
        }

        public CustomNavigationPoint GetClosestAttackCoverPoint(Vector3 centerPosition, float maxDistance = 150f, CoverSearchType coverSearchType = CoverSearchType.shoot_toCover_toBot_Distances)
        {
            customNavigationPoint_0 = Utils.Covers.GetCover(botOwner_0, centerPosition, coverSearchType, maxDistance);
            return customNavigationPoint_0;
        }

        public CustomNavigationPoint GetApproachablePoint()
        {
            customNavigationPoint_0 = commonLayer.GetApproachableCover();
            return customNavigationPoint_0;

        }

        public CustomNavigationPoint GetClosestCoverPointBetween(Vector3 pointA, Vector3 pointB)
        {
            customNavigationPoint_0 = Utils.Covers.GetCover(botOwner_0, (pointA + pointB) / 2f, CoverSearchType.closerToSelectedPoint);//commonLayer.GetClosestCoverPointBetween(pointA, pointB, safeDistance);
            return customNavigationPoint_0;
        }
    }
}
