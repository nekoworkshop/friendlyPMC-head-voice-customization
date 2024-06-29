using EFT;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightFollowerBrain : FollowerBrain
    {

        KnightFightLayer gclass65_0_1;
        public KnightFollowerBrain(BotOwner owner, pitAIBossPlayer boss) : base(owner, boss)
        {
            owner.Tactic.IsCurTactic(BotsGroup.BotCurrentTactic.Attack);
        }

        public override void AddLayers()
        {
            // follow layer
            BossFollowLayer layer6 = new BossFollowLayer(_owner, 50);
            base.method_0(3, layer6, true);
            // avoid danger
            KnightAvoidDangerLayer layer = new KnightAvoidDangerLayer(_owner, 80);
            base.method_0(4, layer, true);
            // weapon maintenance during combat
            KnightWeaponMtnLayer layer2 = new KnightWeaponMtnLayer(_owner, 78);
            base.method_0(5, layer2, true);
            // assault building
            KnightAssaultBuildingLayer layer3 = new KnightAssaultBuildingLayer(_owner, 72);
            base.method_0(6, layer3, true);
            // enemy building
            //buildingLayer = new KnightEnemyBuildingLayer(_owner, 70);
            //base.method_0(7, buildingLayer, true);
            // fight logic
            this.gclass65_0_1 = new KnightFightLayer(_owner, 65);
            base.method_0(1, this.gclass65_0_1, true);
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

        public override void BossOrdersChanged()
        {
            gclass65_0_1.OrdersChanged();
        }
    }
}
