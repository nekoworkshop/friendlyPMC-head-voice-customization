using EFT;
using EFT.InventoryLogic;
using friendlyPMC.Components.Tactics;
using friendlyPMC.Utils;
using System;
using UnityEngine;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BirdEyeFightLayer : GClass61
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
                    ordersChanged && request != null &&
                    request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup &&
                    Utils.Utils.GetNavDistance(botPosition, bossPosition) > followerCommonLayer.regroupMinDistance
                )
                {

                    if (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible)
                    {
                        return followerCommonLayer.GetCloserToBoss(out customNavigationPoint_0);
                    }
                    else
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.DontKnow, true);
                        request.Complete();
                    }
                }

                // do not pursue a marksman
                if (botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman))
                {
                    return followerCommonLayer.MarksManFight(out customNavigationPoint_0);
                }

                AICoreActionResultStruct<BotLogicDecision> decision;

                // default to hold tactic if enemy is too close
                bool enemyClose = false;
                bool enemyVeryClose = false;
                if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.VeryClose)
                {
                    decision = new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "getReady");
                    enemyVeryClose = true;
                }
                else if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.Close)
                {
                    if (followerCommonLayer.HasBoss() && Vector3.Distance(followerCommonLayer.GetBoss().Position,botPosition) <= 35f)
                    {
                        decision = holderLayer.DefendPosition(bossPosition);
                    }
                    else
                    {
                        decision = holderLayer.DefendPosition(botPosition);
                    }

                    customNavigationPoint_0 = holderLayer.NavigationPoint;

                    enemyClose = true;
                }
                else
                {
                    decision = followerSniperLayer.GetDecision();
                    customNavigationPoint_0 = followerSniperLayer.NavigationPoint;
                }
                // enemy very close, switch to close combat ASAP
                if (goalEnemy != null && enemyVeryClose)
                {
                    if (
                        botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.SecondPrimaryWeapon &&
                        botOwner_0.WeaponManager.Selector.CanChangeToSecondWeapons &&
                        (
                            !botOwner_0.Memory.GoalEnemy.HaveSeen ||
                            Time.time - botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime > 2f
                        )
                    )
                    {
                        botOwner_0.WeaponManager.Selector.TryChangeWeapon(true);
                    }
                }
                else if (goalEnemy != null && customNavigationPoint_0 != null)
                {
                    // switch to secondary weapon if we are getting closer to the enemy
                    var proxydist = Utils.EnemyInfo.DistanceProxy(botOwner_0, customNavigationPoint_0.Position);
                    if (
                        !botOwner_0.Memory.GoalEnemy.IsVisible &&
                            (
                                decision.Reason == "repositionFast" ||
                                decision.Reason == "reposition" ||
                                enemyClose
                            )
                        &&
                        botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.SecondPrimaryWeapon &&
                        botOwner_0.WeaponManager.Selector.CanChangeToSecondWeapons &&
                        proxydist < Utils.EnemyInfo.ProxyDistance.Mid && proxydist > Utils.EnemyInfo.ProxyDistance.VeryClose
                    )
                    {
                        botOwner_0.WeaponManager.Selector.TryChangeWeapon(true);

                    }
                    // switch back to sniper if we are moving to a sniper shot
                    else if (
                        Utils.EnemyInfo.DistanceProxy(botOwner_0, customNavigationPoint_0.Position) >= Utils.EnemyInfo.ProxyDistance.Mid &&
                        !botOwner_0.Memory.GoalEnemy.IsVisible &&
                            (
                                decision.Reason == "repositionFast" ||
                                decision.Reason == "reposition" ||
                                decision.Reason == "relocateFast" ||
                                decision.Reason == "sniper.Search"
                            )
                        &&
                        botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.FirstPrimaryWeapon
                    )
                    {
                        botOwner_0.WeaponManager.Selector.TryChangeToMain();
                    }
                }


                return decision;
            } catch( Exception ex )
            {
                Logger.LogInfo("BirdEye Decision Error: " + ex.Message);
                Logger.LogInfo("Trace: " + ex.StackTrace);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass761.Random(1f, 2f)), "decision.Error");
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
            followerSniperLayer.DecisionChanged(prevDecision,nextDecision);
        }


        public override AICoreActionEndStruct EndHoldPosition()
        {
            if (followerCommonLayer.OrderHasChangedRecently)
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

            return holderLayer.EndHoldPosition();
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
