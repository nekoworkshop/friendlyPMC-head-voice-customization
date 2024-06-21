using EFT;
using friendlyPMC.Components.BossFollower;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BirdEyeFollowerBrain : FollowerBrain
    {
        public BirdEyeFollowerBrain(BotOwner owner, pitAIBossPlayer boss) : base(owner, boss)
        {
        }

        public override void AddLayers()
        {
            // order matters for which layer get the initial priority
            // - follow
            BossFollowLayer followLayer = new BossFollowLayer(_owner, 51);
            method_0(1, followLayer, true);
            // avoid danger
            KnightAvoidDangerLayer layer = new KnightAvoidDangerLayer(_owner, 80);
            base.method_0(2, layer, true);
            // weapon maintenance during combat
            KnightWeaponMtnLayer layer2 = new KnightWeaponMtnLayer(_owner, 78);
            base.method_0(3, layer2, true);
            // assault building
            KnightAssaultBuildingLayer layer3 = new KnightAssaultBuildingLayer(_owner, 72);
            base.method_0(4, layer3, true);
            // enemy building
            KnightEnemyBuildingLayer layer4 = new KnightEnemyBuildingLayer(_owner, 70);
            base.method_0(5, layer4, true);
            // sniper hold
            BirdEyeSniperLayer layer5 = new BirdEyeSniperLayer(_owner, 55);
            method_0(6, layer5, true);
            // sniper fight
            BirdEyeFightLayer layer6 = new BirdEyeFightLayer(_owner, 50);
            method_0(7, layer6, true);
            // - item taker
            FollowerLootLayer layer7 = new FollowerLootLayer(_owner, 40);
            method_0(8, layer7, true);
        }

        public override string ShortName()
        {
            return "BirdEyeFLW";
        }
    }
}
