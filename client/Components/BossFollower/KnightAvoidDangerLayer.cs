using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Components.Tactics;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightAvoidDangerLayer : GClass35
    {
        protected CustomNavigationPoint customNavigationPoint_0;

        private FollowerCommonLayer commonLayer;

        public KnightAvoidDangerLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            commonLayer = new FollowerCommonLayer(bot, priority);
        }

        public override void OnActivate()
        {
            base.OnActivate();
            commonLayer.OnActivate();
        }

        public override void Dispose()
        {
            base.Dispose();
            commonLayer.Dispose();
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            float searchRadius = 70f;
            GetCoverPoint(HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position, searchRadius);

            return base.GetDecision();
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = commonLayer.FindPoint(data, p, checkCurrent);
            return customNavigationPoint_0;
        }

        protected virtual bool HasBoss()
        {
            return commonLayer.HasBoss();
        }

        protected virtual pitAIBossPlayer GetBoss()
        {
            return commonLayer.GetBoss();
        }

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            customNavigationPoint_0 = commonLayer.GetCoverPoint(centerPosition, searchRadius);

        }
    }
}
