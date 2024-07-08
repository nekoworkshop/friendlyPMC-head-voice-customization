using EFT;
using EFT.InventoryLogic;
using friendlyPMC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BirdEyeFightLayer : GClass61
    {
        protected bool ordersChanged = false;

        protected readonly float sprintDistance = 15f;

        protected float coverTimer = 0f;
        protected int coverTries = 0;
        protected float holdTimer = 0f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        private FollowerFightLayer followerFightLayer;

        private AICoreActionResultStruct<BotLogicDecision>? previousDecision = null;
        public BirdEyeFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {

            followerFightLayer = new FollowerFightLayer(bot, priority);
        }

        public override string Name()
        {
            return "BirdEyeFight";
        }

        private AICoreActionResultStruct<BotLogicDecision>  SniperFight()
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;

            if (enemyVisible)
            {
                // enemy visible (can't shoot) and we are not in cover
                if (!botOwner_0.Memory.IsInCover)
                {
                    if(previousDecision != null && previousDecision.Value.Action == BotLogicDecision.runToCover)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                    }
                    // - find cover to shoot from
                    GetClosestAttackCoverPoint(botPosition);
                    // - no attack cover, just find a cover 
                    if (customNavigationPoint_0 == null) GetClosestCoverPoint(botPosition, fightRange);

                    if (customNavigationPoint_0 != null)
                    {
                        if (Utils.Utils.GetNavDistance(botPosition, customNavigationPoint_0.Position) < 25f)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "relocate");
                        }

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "relocateFast");
                    }
                    // - found nothing, fallback
                    if (holdTimer < Time.time)
                    {
                        float timer = GClass760.Random(2f, 5f);
                        holdTimer = Time.time + timer + GClass760.Random(2f, 3f);
                        return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                    }


                    botOwner_0.SearchData.SearchPoint = new BotSearchPoint(enemyPosition, EBotSearchPoint.playerPosition);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.search, "enemy.Search");

                }
                // enemy visible and in cover
                else
                {
                    // - try to shot enemy
                    if (botOwner_0.Memory.CurCustomCoverPoint != null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy) 
                    {
                        if (botOwner_0.Memory.CurCustomCoverPoint != null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                        // - else find better spot
                        else
                        {
                            GetClosestAttackCoverPoint(botPosition, 25f);
                            if (customNavigationPoint_0 != null)
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "relocate");
                            }
                        }
                    }
                    // - no new cover found, wait 4 it
                    if (holdTimer < Time.time)
                    {
                        float timer = GClass760.Random(2f, 5f);
                        holdTimer = Time.time + timer + GClass760.Random(2f, 3f);
                        return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                    }
                    // - else just find some cover 
                    GetClosestCoverPoint(botPosition, fightRange);
                    if (customNavigationPoint_0 != null)
                    {
                        if (Utils.Utils.GetNavDistance(botPosition, customNavigationPoint_0.Position) < 25f)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "relocate");
                        }

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "relocateFast");
                    }
                        if (botOwner_0.Memory.GoalEnemy.CanShoot)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                    }

                    // - fallback
                    return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass760.Random(2f, 5f)), "wait4it");
                }
            }
            // enemy not visible
            else
            {   
                
                // - wait a bit
                if (holdTimer < Time.time)
                {
                    float timer = GClass760.Random(2f, 5f);
                    holdTimer = Time.time + timer + GClass760.Random(2f, 3f);
                    return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                }

                // - look for a shooting spot
                GetClosestAttackCoverPoint(botPosition);

                // - no attack cover, just find a cover 
                if (customNavigationPoint_0 == null) GetClosestCoverPoint(botPosition, fightLongRange);

                if (customNavigationPoint_0 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "repositionFast");
                }



                // - fallback
                botOwner_0.SearchData.SearchPoint = new BotSearchPoint(enemyPosition, EBotSearchPoint.playerPosition);
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.search, "enemy.Search");
            }
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            // is in dogfight?
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = followerFightLayer.DogFight();

            if (aicoreActionResultStruct != null)
            {
                coverTries = 0;
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }
            // needs healing?
            aicoreActionResultStruct = followerFightLayer.NeedHeal();
            if (aicoreActionResultStruct != null)
            {
                coverTries = 0;
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }

            // player needs help or has call for a regroup
            if (
                ordersChanged && request != null &&
                request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup &&
                Utils.Utils.GetNavDistance(botPosition, bossPosition) > friendlyPMC.regroupMinDistance.Value
            )
            {
                Components.Logger.LogInfo("Player asked for help");
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

            AICoreActionResultStruct<BotLogicDecision> decision;

            // default to hold tactic if enemy is too close
            if (Utils.EnemyInfo.Distance(botOwner_0) < Utils.EnemyInfo.EnemyDistance.Mid)
            {
                decision = followerFightLayer.DefendPosition();
            }
            else 
            { 
                decision = SniperFight();
            }

            
            if(goalEnemy != null)
            {
                // switch to secondary weapon if we are getting closer
                var proxydist = Utils.EnemyInfo.DistanceProxy(botOwner_0, customNavigationPoint_0.Position);
                if (
                    decision.Action == BotLogicDecision.runToCover &&
                    decision.Reason != "runToHeal" &&
                    (decision.Action != BotLogicDecision.search || !botOwner_0.Memory.GoalEnemy.IsVisible) &&
                    botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.SecondPrimaryWeapon &&
                    botOwner_0.WeaponManager.Selector.CanChangeToSecondWeapons &&
                    proxydist < Utils.EnemyInfo.ProxyDistance.Mid && proxydist > Utils.EnemyInfo.ProxyDistance.VeryClose
                )
                {
                    botOwner_0.WeaponManager.Selector.TryChangeWeapon(true);

                } 
                // switch back to sniper if we are moving for a sniper shot
                else if(
                    (decision.Action == BotLogicDecision.goToPoint ||
                    decision.Action == BotLogicDecision.runToCover) &&
                    decision.Reason != "runToHeal" &&
                    botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.FirstPrimaryWeapon &&
                    Utils.EnemyInfo.DistanceProxy(botOwner_0, customNavigationPoint_0.Position) >= Utils.EnemyInfo.ProxyDistance.Mid
                )
                {
                    botOwner_0.WeaponManager.Selector.TryChangeToMain();
                }
            }

            return decision;

        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            AICoreActionEndStruct? common = followerFightLayer.ShallEndCurrentDecisionAllies(curDecision);

            if (common != null) return (AICoreActionEndStruct)common;

            return base.ShallEndCurrentDecision(curDecision);
        }

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            previousDecision = prevDecision;
        }

        public override bool ShallUseNow()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                if (
                        botOwner_0.BotRequestController.CurRequest != null &&
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup
                    )
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                }

                coverTries = 0;

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

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            // switch back to primary weapon if enemy is no longer close
            if(goalEnemy != null)
            {
                if(
                    botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.FirstPrimaryWeapon && 
                    Utils.EnemyInfo.DistanceProxy(botOwner_0, botOwner_0.GetPlayer.Transform.position) >= Utils.EnemyInfo.ProxyDistance.Mid
                )
                {
                    botOwner_0.WeaponManager.Selector.TryChangeToMain();
                }
            }

            return base.EndHoldPosition();
        }

        public AICoreActionEndStruct EndGetInClose()
        {

            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            } 
            else if (botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }
            else if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.VeryClose)
            {
                return new AICoreActionEndStruct("enemy.tooClose", true);
            }

            return base.EndRunToCover();
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

        public override AICoreActionEndStruct EndGoToPoint()
        {
            return followerFightLayer.EndGoToPoint();
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

        public override ShootPointClass GetShootPoint()
        {
            return botOwner_0.CurrentEnemyTargetPosition(true);
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0, 100f);


            return customNavigationPoint_0;
        }

        protected virtual void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 =  followerFightLayer.GetClosestCoverPoint(centerPosition, searchRadius);
        }

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = followerFightLayer.GetCoverPoint(centerPosition, searchRadius);
        }

        protected virtual void GetClosestAttackCoverPoint(Vector3 centerPosition,float minDistance = 20f)
        {
            CustomNavigationPoint cover = Utils.Utils.GetClosestAttackCoverPoint(botOwner_0, centerPosition, minDistance,170f,botOwner_0.Memory.GoalEnemy.CurrPosition);
            customNavigationPoint_0 = cover;
            botOwner_0.Memory.SetCoverPoints(cover);
        }
    }
}
