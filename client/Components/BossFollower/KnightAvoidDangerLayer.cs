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

        private FollowerFightLayer followerFightLayer;

        public KnightAvoidDangerLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            followerFightLayer = new FollowerFightLayer(bot, priority);
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            float searchRadius = 70f;
            GetCoverPoint(HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position, searchRadius);

            return base.GetDecision();
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = followerFightLayer.FindPoint(data, p, checkCurrent);
            return customNavigationPoint_0;
        }

        protected virtual bool HasBoss()
        {
            return followerFightLayer.HasBoss();
        }

        protected virtual pitAIBossPlayer GetBoss()
        {
            return followerFightLayer.GetBoss();
        }

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            customNavigationPoint_0 = followerFightLayer.GetCoverPoint(centerPosition, searchRadius);

        }
    }
}
