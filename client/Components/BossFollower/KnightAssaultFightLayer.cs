using EFT;
using friendlyPMC.Modules;
using System.Collections.Generic;
using System;
using UnityEngine.AI;
using UnityEngine;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightAssaultFightLayer : GClass34
    {
        protected CustomNavigationPoint customNavigationPoint_0 = null;
        protected float coverTimer = 0f;

        private readonly float sprintDistance = 15f;
        public KnightAssaultFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            if (baseDecision.Action == BotLogicDecision.runToCover)
            {
                Vector3 center = botOwner_0.GetPlayer.Transform.position;

                if (baseDecision.Reason == "runToNtg")
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, friendlyPMC.fightInnerRadius.Value);
                }
                else if (baseDecision.Reason == "runIfCoverF" || baseDecision.Reason == "Ambush")
                {
                    GetClosestCoverPoint(center, friendlyPMC.fightOuterRadius.Value);
                }
                else if (baseDecision.Reason == "ShallRunIfNoAmmo" || baseDecision.Reason == "LastDamageDataActive")
                {
                    GetCoverPoint(center, friendlyPMC.fightOuterRadius.Value);
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }
            else if (baseDecision.Action == BotLogicDecision.attackMoving)
            {
                if (baseDecision.Reason == "attackNtg" || baseDecision.Reason == "Last")
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, friendlyPMC.fightInnerRadius.Value);
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }
            else if (baseDecision.Action == BotLogicDecision.holdPosition && baseDecision.Reason == "hold2")
            {
                HoldFor(GClass760.Random(1f, 3f));
            }
            else if (baseDecision.Action == BotLogicDecision.search)
            {
                GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.EnemyLastPosition, friendlyPMC.fightOuterRadius.Value);

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "followerFallback");
                }
                else
                {
                    if (Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Position, customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }

            }

            return baseDecision;
        }

        protected virtual bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        protected virtual pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }


        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0);


            return customNavigationPoint_0;
        }

        protected virtual void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1f + Time.time;

            CustomNavigationPoint point = Utils.Utils.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);
        }

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            CustomNavigationPoint point1 = Utils.Utils.GetCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);

        }
    }
}
