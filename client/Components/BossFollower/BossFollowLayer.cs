using EFT;

namespace friendlyPMC.Components.BossFollower
{
    public class BossFollowLayer : FollowerLayer
    {
        public BossFollowLayer(BotOwner bot, int priority) : base(bot, priority)
        {
        }
        public override string Name()
        {
            return "BossFLP";
        }
    }
}