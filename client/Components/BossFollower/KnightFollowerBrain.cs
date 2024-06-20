using EFT;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightFollowerBrain : FollowerBrain
    {

        GClass65 gclass65_0_1;

        public KnightFollowerBrain(BotOwner owner, pitAIBossPlayer boss) : base(owner, boss)
        {
            
        }

        public override void AddLayers()
        {
            // follow layer
            BossFollowLayer layer6 = new BossFollowLayer(_owner, 45);
            base.method_0(1, layer6, true);
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
            // fight logic
            this.gclass65_0_1 = new KnightFightLayer(_owner, 62);
            base.method_0(6, this.gclass65_0_1, true);
            // assault have enemy
            KnightAssaultFightLayer layer5 = new KnightAssaultFightLayer(_owner, 50);
            base.method_0(7, layer5, true);
            // - item taker
            FollowerLootLayer layer9 = new FollowerLootLayer(_owner, 40);
            method_0(8, layer9, true);
        }

        public override string ShortName()
        {
            return "KnightFLW";
        }

        public void ForceRecalcShootPos()
        {
            this.gclass65_0_1.ForceRecalcShootPos();
        }
    }
}
