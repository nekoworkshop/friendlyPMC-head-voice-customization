using EFT;
using friendlyPMC.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components.Tactics
{
    /** This class is not meant to be used directly as a brain layer, but within one **/
    internal class FollowerSniperLayer : GClass61
    {
  
        protected float coverTimer = 0f;
        protected float holdTimer = 0f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        private FollowerCommonLayer commonLayer;

        public CustomNavigationPoint NavigationPoint
        {
            get
            {
                return customNavigationPoint_0;
            }
        }

        public FollowerCommonLayer CommonLayer { get { return commonLayer; } }
        public FollowerSniperLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            commonLayer = new FollowerCommonLayer(bot, priority);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            commonLayer?.OnActivate();
        }
        public override void Dispose()
        {
            base.Dispose();
            commonLayer?.Dispose();
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

        // dummy 
        public override bool ShallUseNow()
        {
            return true;
        }

        public override ShootPointClass GetShootPoint()
        {
            return botOwner_0.CurrentEnemyTargetPosition(true);
        }
        // dummy 
        public override string Name()
        {
            return "FBSniper";
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;

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
                    // - found nothing, fallback
                    if (holdTimer < Time.time)
                    {
                        float timer = GClass761.Random(2f, 5f);
                        holdTimer = Time.time + timer + GClass761.Random(2f, 3f);
                        return commonLayer.HoldPositionFor(timer);
                    }

                    // - alternative fallback
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dgf");

                }
                // enemy visible and in cover
                else
                {
                    // - try to shot enemy
                    if (botOwner_0.Memory.CurCustomCoverPoint != null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                    // - else find better spot
                    else
                    {
                        GetClosestAttackCoverPoint(botPosition, 20f);
                        if (customNavigationPoint_0 != null && coverTimer < Time.time)
                        {
                            coverTimer = Time.time + GClass761.Random(3f, 5f);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "relocate");
                        }
                    }

                    // -- fallback #1, just wait
                    if (holdTimer < Time.time)
                    {
                        float timer = GClass761.Random(2f, 5f);
                        holdTimer = Time.time + timer + GClass761.Random(3f, 5f);
                        return commonLayer.HoldPositionFor(timer);
                    }

                    // - fallback #2
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.SniperSearch, "sniper.Search");
                }
            }
            // enemy not visible
            else
            {
                // - look for a shooting spot
                GetClosestAttackCoverPoint(botPosition);

                if (customNavigationPoint_0 != null && coverTimer < Time.time)
                {
                    coverTimer = Time.time + GClass761.Random(3f, 5f);
                    botOwner_0.GoToSomePointData.SetPoint(customNavigationPoint_0.Position);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, "reposition");
                }
                // -- fallback #1, just wait
                if (holdTimer < Time.time)
                {
                    float timer = GClass761.Random(2f, 5f);
                    holdTimer = Time.time + timer + GClass761.Random(3f, 5f);
                    return commonLayer.HoldPositionFor(timer);
                }

                // - fallback #2, search for a sniping spot
                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.SniperSearch, "sniper.Search");
            }
        }
        public AICoreActionEndStruct EndSniperSearch()
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

            if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.VeryClose)
            {
                return new AICoreActionEndStruct("enemy.Close", true);
            }

            return aICoreActionEndStruct;
        }

        public override AICoreActionEndStruct EndGoToPoint()
        {

            if (commonLayer.CurrentDecision.HasValue && commonLayer.CurrentDecision.Value.Reason == "relocateFast")
            {
                if (botOwner_0.Memory.GoalEnemy.CanShoot)
                {
                    return new AICoreActionEndStruct("enemy.canSh", true);
                }

                return base.EndGoToPoint();
            }
            return commonLayer.EndGoToPoint();
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
