using EFT;
namespace friendlyPMC.Actions
{
    internal class FollowerGoCheck : BotRequest
    {
        public FollowerGoCheck(IPlayer requester, BotRequestType request = BotRequestType.goToPoint) : base(requester, request)
        {
        }

        public override bool CanProceed()
        {
            if (Executor == null) return false;

            return true;
        }

        public override bool CanRequest(BotOwner owner)
        {

            if(owner.IsRole(WildSpawnType.followerBigPipe) && BotRequestType == BotRequestType.goToPoint)
                return true;

            if (
                owner.IsRole(WildSpawnType.followerBirdEye) ||
                owner.IsRole(WildSpawnType.followerBigPipe)
            )
            {
                if (Requester.IsAI && Requester.Profile.Info.Settings.Role == WildSpawnType.bossKnight)
                {
                    return true;
                }
                if (
                    owner.BotFollower.BossToFollow != null &&
                    !owner.BotFollower.BossToFollow.Followers.Exists((BotOwner follower) => follower.IsRole(WildSpawnType.bossKnight))
                )
                {
                    return true;
                }
                return false;
            }

            return true;
        }

        public override EBotRequestMode RequestMode
        {
            get
            {
                return EBotRequestMode.Fight;
            }
        }
    }
}
