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

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        private float float_6;

        private GClass409 gclass409_0;

        private FollowerFightLayer followerFightLayer;
        public BirdEyeFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            gclass409_0 = this.botOwner_0.FindPlaceToShoot.Register(60, 40, 0.2f);

            followerFightLayer = new FollowerFightLayer(bot, priority);
        }

        public override string Name()
        {
            return "BirdEyeFight";
        }

        private AICoreActionResultStruct<BotLogicDecision>  MakeDecision()
        {
            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;

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
    
            // player needs cover
            if (ordersChanged && request != null && request.BotRequestType == BotRequestType.warnPlayer)
            {
                if (Utils.Utils.GetNavDistance(botPosition, bossPosition) > friendlyPMC.regroupMinDistance.Value && (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible))
                {
                    if (!botOwner_0.Memory.HaveEnemy)
                    {
                        GetClosestCoverPoint(bossPosition, friendlyPMC.fightOuterRadius.Value);
                    }
                    else
                    {
                        GetClosestAttackCoverPoint(bossPosition);
                    }
                    
                    coverTries = 0;

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
            
            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;

            if (botOwner_0.Memory.GoalEnemy.IsVisible)
            {
                if (!botOwner_0.Memory.IsInCover)
                {
                    // enemy visbile but cannot shoot him
                    GetClosestAttackCoverPoint(botPosition);
                    // no attack cover, just find a cover 
                    if (customNavigationPoint_0 == null) GetCoverPoint(enemyPos, fightLongRange);

                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "repositionFast");
                    }
                    // found nothing, fallback
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
                else
                {
                    if (botOwner_0.Memory.GoalEnemy.CanShoot)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                    }

                    if (coverTimer > Time.time)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass760.Random(3f, 7f)), "wait4it");
                    }

                    coverTimer = Time.time + coverTries + GClass760.Random(2f, 5f);
                    coverTries++;

                    // enemy visbile but cannot shoot him
                    GetClosestAttackCoverPoint(botPosition);
                    // no attack cover, just find a new cover 
                    if (customNavigationPoint_0 == null) GetCoverPoint(bossPosition, fightLongRange);

                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "repositionFast");
                    }
                    coverTimer = 0f;
                    return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass760.Random(2f, 5f)), "wait4it");
                }

            }
            else
            {
                if (gclass409_0.LastGoodPoint != null)
                {
                    ShootPointClass shootToPoint = new ShootPointClass(botOwner_0.Memory.GoalEnemy.GetPartToShoot(), 1f);

                    float sqrMagnitude = (gclass409_0.LastGoodPoint.Value - botOwner_0.Position).sqrMagnitude;
                    Vector3 firePos = gclass409_0.LastGoodPoint.Value + botOwner_0.ShootData.WeaponRootOffset;
                    if (GClass301.CanShootToTarget(shootToPoint, firePos, botOwner_0.LookSensor.Mask, false))
                    {
                        if (sqrMagnitude < 2f)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "2some");
                        }
                        else
                        {
                            botOwner_0.GoToSomePointData.SetPoint(gclass409_0.LastGoodPoint.Value);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, "reposition");
                        }
                    }
                    else
                    {
                        gclass409_0.Drop();
                    }
                }

                if (coverTimer > Time.time)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass760.Random(3f, 7f)), "wait4it");
                }

                coverTimer = Time.time + coverTries + GClass760.Random(3f, 6f);
                coverTries++;

                // enemy not visible
                GetClosestAttackCoverPoint(botPosition);

                // don't get closer now
                if (customNavigationPoint_0 != null &&
                    Utils.EnemyInfo.DistanceProxy(botOwner_0, botPosition) >= Utils.EnemyInfo.ProxyDistance.Mid && 
                    (Utils.EnemyInfo.DistanceProxy(botOwner_0, customNavigationPoint_0.Position) <= Utils.EnemyInfo.ProxyDistance.Close)
                )
                {
                    customNavigationPoint_0 = null;
                }

                // no attack cover, just find a cover 
                if (customNavigationPoint_0 == null) GetCoverPoint(bossPosition, fightLongRange);

                if (customNavigationPoint_0 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "repositionFast");
                }

                // nothing to do
                coverTimer = 0f;
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass760.Random(2f, 5f)), "wait4it");
            }
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            AICoreActionResultStruct<BotLogicDecision>  decision = MakeDecision();

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            if(goalEnemy != null)
            {
                // switch to secondary weapon if we are getting closer
                var proxydist = Utils.EnemyInfo.DistanceProxy(botOwner_0, customNavigationPoint_0.Position);
                if (
                    decision.Action == BotLogicDecision.runToCover &&
                    decision.Reason != "runToHeal" &&
                    botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.SecondPrimaryWeapon &&
                    botOwner_0.WeaponManager.Selector.CanChangeToSecondWeapons &&
                    proxydist < Utils.EnemyInfo.ProxyDistance.Mid && proxydist > Utils.EnemyInfo.ProxyDistance.VeryClose
                )
                {
                    botOwner_0.WeaponManager.Selector.TryChangeWeapon(true);

                } 
                // switch back to sniper if we are moving for a sniper shot
                else if(
                    decision.Action == BotLogicDecision.goToPoint &&
                    botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.FirstPrimaryWeapon &&
                    Utils.EnemyInfo.DistanceProxy(botOwner_0, gclass409_0.LastGoodPoint.Value) >= Utils.EnemyInfo.ProxyDistance.Mid
                )
                {
                    botOwner_0.WeaponManager.Selector.TryChangeToMain();
                }
            }

            return decision;

        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            List<string> getInClose = new List<string>
            {
                "regroupToBossFast",
                "regroupToBossSlow",
                "getInCloseFast",
                "getInCloseSlow",
                "repositionFast"
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
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer
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

            if (customNavigationPoint_0 != null && !customNavigationPoint_0.CanIShootToEnemy)
            {
                ShootPointClass shootPoint = this.GetShootPoint();
                Vector3 vector;
                if (this.gclass409_0.ManualUpdateSearch(shootPoint, 20f, out vector))
                {
                    return new AICoreActionEndStruct("havePoint", true);
                }
                if (this.float_6 < Time.time)
                {
                    this.float_6 = Time.time + 10f;
                    if (this.gclass409_0.ShootPositionType == EShootPositionType.stand)
                    {
                        this.gclass409_0.Set(EShootPositionType.lay);
                    }
                    else
                    {
                        this.gclass409_0.Set(EShootPositionType.stand);
                    }
                }
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
            return this.botOwner_0.CurrentEnemyTargetPosition(true);
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

        protected virtual void GetClosestAttackCoverPoint(Vector3 centerPosition, bool useFullCover = false, float minDistance = 10f)
        {
            customNavigationPoint_0 = followerFightLayer.GetClosestAttackCoverPoint(centerPosition, useFullCover, minDistance);
        }

        protected virtual void GetApproachablePoint()
        {
            customNavigationPoint_0 = followerFightLayer.GetApproachablePoint();
        }
    }
}
