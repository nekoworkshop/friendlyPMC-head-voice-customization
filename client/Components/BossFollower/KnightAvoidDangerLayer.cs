using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightAvoidDangerLayer : GClass35
    {
        protected CustomNavigationPoint customNavigationPoint_0;

        private float coverTimer = 0f;

        public KnightAvoidDangerLayer(BotOwner bot, int priority) : base(bot, priority)
        {
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            float searchRadius = HasBoss() ? friendlyPMC.fightOuterRadius.Value : 50f;
            GetCoverPoint(HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position, searchRadius);

            return base.GetDecision();
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0);


            return customNavigationPoint_0;
        }

        protected virtual bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        protected virtual pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
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
