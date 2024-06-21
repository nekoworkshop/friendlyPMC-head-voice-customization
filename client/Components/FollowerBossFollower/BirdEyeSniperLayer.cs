using EFT;
using System;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BirdEyeSniperLayer : GClass62
    {
        public BirdEyeSniperLayer(BotOwner bot, int priority) : base(bot, priority) { 
        }


        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0,100f);


            return customNavigationPoint_0;
        }
    }
}
