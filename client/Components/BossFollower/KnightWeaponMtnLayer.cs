using EFT;
using System;
using UnityEngine;

namespace friendlyPMC.Components.BossFollower
{
    /**
     * Ovewrite Weapon Malfunction Layer for Knight to use our cover system
     */
    internal class KnightWeaponMtnLayer : GClass105
    {
        protected CustomNavigationPoint customNavigationPoint_0;

        private FollowerFightLayer followerFightLayer;

        public KnightWeaponMtnLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            followerFightLayer = new FollowerFightLayer(bot, priority);
        }


        private bool HasBoss()
        {
            return followerFightLayer.HasBoss();
        }

        private pitAIBossPlayer GetBoss()
        {
            return followerFightLayer.GetBoss();
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = followerFightLayer.FindPoint(data, p, checkCurrent);


            return customNavigationPoint_0;
        }

        private void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            customNavigationPoint_0 = followerFightLayer.GetCoverPoint(centerPosition, searchRadius);

        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            float searchRadius = 70f;
            GetCoverPoint(HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position, searchRadius);

            return base.GetDecision();
        }
    }
}
