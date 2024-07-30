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


        protected CustomNavigationPoint customNavigationPoint_1;

        protected float coverTimer = 0f;
        protected float holdTimer = 0f;

        FollowerFightLayer followerFightLayer;
        public FollowerSniperLayer(BotOwner bot, int priority, FollowerFightLayer ffl) : base(bot, priority)
        {
            followerFightLayer = ffl;
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
                        if (followerFightLayer.GetNavDistance(customNavigationPoint_0.Position) < 25f)
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
                        return followerFightLayer.HoldPositionFor(timer);
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
                        return followerFightLayer.HoldPositionFor(timer);
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
                    return followerFightLayer.HoldPositionFor(timer);
                }

                // - fallback #2, search for a sniping spot
                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.SniperSearch, "sniper.Search");
            }
        }

        protected virtual void GetClosestAttackCoverPoint(Vector3 centerPosition, float minDistance = 15f)
        {
            if (this.coverTimer > Time.time) return customNavigationPoint_1;

            this.coverTimer = 1f + Time.time;

            customNavigationPoint_1 = Covers.GetClosestAttackCoverPoint(botOwner_0, centerPosition, minDistance, 150f);

            customNavigationPoint_0 = customNavigationPoint_1;
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_1);
            return customNavigationPoint_1;
        }
    }
}
