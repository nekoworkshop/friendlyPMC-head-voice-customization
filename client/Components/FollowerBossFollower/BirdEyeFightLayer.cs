using EFT;
using EFT.InventoryLogic;
using friendlyPMC.Components.Tactics;
using friendlyPMC.Utils;
using friendlyPMC.Modules;
using System;
using UnityEngine;

namespace friendlyPMC.Components.FollowerBossFollower
{
    /**
     * Overwrite of BirdEye's fight layer
     */
    public class BirdEyeFightLayer : GClass63
    {
        private FollowerSniperLayer followerSniperLayer;
        private FollowerCommonLayer followerCommonLayer;
        private FollowerHolderLayer holderLayer;

        protected bool ordersChanged
        {
            get
            {
                return (bool)followerCommonLayer?.OrderHasChangedRecently;
            }
        }

        public BirdEyeFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {

            followerSniperLayer = new FollowerSniperLayer(bot, priority);
            followerCommonLayer = followerSniperLayer.CommonLayer;
            holderLayer = new FollowerHolderLayer(bot, priority, followerCommonLayer);
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

                return false;
            }

            return true;
        }


        public override void OnActivate()
        {
            base.OnActivate();
            followerSniperLayer?.OnActivate();
            followerCommonLayer?.OnActivate();
            holderLayer?.OnActivate();
        }

        public override void Dispose()
        {
            base.Dispose();
            followerSniperLayer?.Dispose();
            followerCommonLayer?.Dispose();
            holderLayer?.Dispose();
        }

        public override string Name()
        {
            return "BirdEyeFight";
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            try
            {
                BotRequest request = botOwner_0.BotRequestController.CurRequest;

                Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
                Vector3 bossPosition = request != null ? botOwner_0.BotRequestController.CurRequest.Requester.Position : botPosition;

                EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

                // is in dogfight?
                AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = followerCommonLayer.DogFight(out customNavigationPoint_0);
                if (aicoreActionResultStruct.HasValue)
                {
                    return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
                }
                // needs healing?
                aicoreActionResultStruct = followerCommonLayer.NeedHeal(out customNavigationPoint_0);
                if (aicoreActionResultStruct != null)
                {
                    return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
                }

                // player needs help or has call for a regroup
                if (
                    request != null &&
                    request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup &&
                    Utils.Utils.GetNavDistance(botPosition, bossPosition) > followerCommonLayer.regroupMinDistance
                )
                {
                    if (!botOwner_0.Memory.HaveEnemy || !goalEnemy.IsVisible)
                    {
                        aicoreActionResultStruct = followerCommonLayer.GetCloserToBoss(out customNavigationPoint_0);
                        if (aicoreActionResultStruct != null)
                            return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
                    }
                    else
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.DontKnow, true);
                        request.Complete();
                    }
                }

                // come here request during fights
                if (request != null && request.BotRequestType == BotRequestType.followMe)
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.MoveToPoint, "req:comeHere");

                // go there request during fights
                if (request != null && request.BotRequestType == BotRequestType.goToPoint)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.MoveToPoint, "req:goCheck");
                }

                // do not pursue a marksman
                if (botOwner_0.Memory.HaveEnemy && goalEnemy.Owner.IsRole(WildSpawnType.marksman))
                {
                    return followerCommonLayer.MarksManFight(out customNavigationPoint_0);
                }

                AICoreActionResultStruct<BotLogicDecision> decision;

                // default to hold tactic if enemy is too close
                Utils.Enemy.EnemyDistance enemyDistance = Utils.Enemy.Distance(botOwner_0);

                if (enemyDistance <= Utils.Enemy.EnemyDistance.VeryClose)
                {
                    decision = new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "getReady");
                }
                else if (enemyDistance <= Utils.Enemy.EnemyDistance.Close)
                {
                    if (followerCommonLayer.HasBoss() && Vector3.Distance(followerCommonLayer.GetBoss().Position, botPosition) <= 35f)
                    {
                        decision = holderLayer.DefendPosition(bossPosition);
                    }
                    else
                    {
                        decision = holderLayer.DefendPosition(botPosition);
                    }

                    customNavigationPoint_0 = holderLayer.NavigationPoint;
                }
                else
                {
                    decision = followerSniperLayer.GetDecision();
                    customNavigationPoint_0 = followerSniperLayer.NavigationPoint;
                }
                // enemy very close, switch to close combat ASAP
                followerSniperLayer.CheckCanSwitchToSecondary(decision, enemyDistance);

                return decision;
            }
            catch (Exception ex)
            {
                Modules.Logger.LogInfo("BirdEye Decision Error: " + ex.Message);
                Modules.Logger.LogInfo("Trace: " + ex.StackTrace);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass824.Random(1f, 2f)), "decision.Error");
            }
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            AICoreActionEndStruct? common = followerCommonLayer.ShallEndCurrentDecisionAllies(curDecision);

            if (common != null) return (AICoreActionEndStruct)common;

            if (
                    curDecision.Action == (BotLogicDecision)CustomBotDecisions.EnemySearch ||
                    curDecision.Action == (BotLogicDecision)CustomBotDecisions.SniperSearch
                )
            {
                return followerSniperLayer.EndSniperSearch();
            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            followerSniperLayer.DecisionChanged(prevDecision, nextDecision);
        }


        public override AICoreActionEndStruct EndHoldPosition()
        {
            AICoreActionEndStruct endHold;

            if (followerCommonLayer.OrderHasChangedRecently)
            {
                endHold = new AICoreActionEndStruct("EndHol", true);
            }
            else
            {
                endHold = holderLayer.EndHoldPosition();
            }

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            // switch back to primary weapon if enemy is no longer close
            if (goalEnemy != null && !goalEnemy.IsVisible && endHold.Value)
            {
                if (
                    botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.FirstPrimaryWeapon &&
                    Enemy.DistanceProxy(botOwner_0, botOwner_0.GetPlayer.Transform.position) >= Enemy.ProxyDistance.Mid
                )
                {
                    botOwner_0.WeaponManager.Selector.TryChangeToMain();
                }
            }

            return endHold;
        }

        public override AICoreActionEndStruct EndHeal()
        {
            return followerCommonLayer.EndHeal();
        }

        public override AICoreActionEndStruct EndTakeItem()
        {
            return followerCommonLayer.EndTakeItem();
        }

        public override AICoreActionEndStruct EndGoToPoint()
        {
            return followerSniperLayer.EndGoToPoint();
        }

        protected bool HasBoss()
        {
            return followerCommonLayer.HasBoss();
        }

        protected pitAIBossPlayer GetBoss()
        {
            return followerCommonLayer.GetBoss();
        }

        public void OrdersChanged()
        {
            followerSniperLayer.OrdersChanged();
        }

        public override ShootPointClass GetShootPoint()
        {
            return followerSniperLayer.GetShootPoint();
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Covers.FindPoint(botOwner_0, customNavigationPoint_0, 100f);
            return customNavigationPoint_0;
        }
    }
}
