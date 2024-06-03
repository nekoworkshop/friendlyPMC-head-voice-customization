using EFT;

namespace friendlyPMC.Components
{
    internal class FollowerBrain : BaseBrain
    {
        public FollowerBrain(BotOwner owner) : base(owner)
        {
            AddLayers();
        }
        /** Exposed method for adding brain layers so it can be patched by addons **/
        public virtual void AddLayers()
        {
            // order matters for which layer get the initial priority
            // - follow
            FollowerLayer followLayer = new FollowerLayer(_owner, 51);
            method_0(1, followLayer, true);
            // - requests
            FollowerRequestLayer layer4 = new FollowerRequestLayer(_owner, 55);
            method_0(2, layer4, true);
            // - fight
            FollowerFightLayer layer6 = new FollowerFightLayer(_owner, 60);
            method_0(3, layer6, true);
            // - grenade
            GClass36 layer = new GClass36(_owner, 130);
            method_0(4, layer, true);
            // - weapon malfunction
            GClass98 layer3 = new GClass98(_owner, 88);
            method_0(5, layer3, true);
            // - ExURequest - what is that?
            GClass68 layer2 = new GClass68(_owner, 90);
            method_0(6, layer2, true);
            // - stay at position in prone mode
            GClass104 layer8 = new GClass104(_owner, 10, false, CoverLevel.Lay);
            method_0(7, layer8, true);
            // - item taker
            FollowerLootTaker layer9 = new FollowerLootTaker(_owner, 40);
            method_0(8, layer9, true);
        }

        public override string ShortName()
        {
            return "FLBPlayer";
        }

        public override GClass578 EventsPriority()
        {
            return new GClass578(1, 75, 45, 76);
        }
    }
}
