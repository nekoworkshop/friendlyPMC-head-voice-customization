using EFT;
namespace friendlyPMC.Actions
{
    internal class FollowerGoCheck : BotRequest
    {
        private bool _fromWait = false;

        public bool FromWait
        {
            get
            {
                return _fromWait;
            }
        }
        public FollowerGoCheck(IPlayer requester, BotRequestType request = BotRequestType.goToPoint, bool fromWait = false) : base(requester, request)
        {
            _fromWait = fromWait;
        }

        public override bool CanProceed()
        {
            if (Executor == null) return false;

            return true;
        }

        public override bool CanRequest(BotOwner owner)
        {


            if (owner.Memory.HaveEnemy) return false;

            /*if (
                owner.IsRole(WildSpawnType.followerBirdEye)
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
            }*/

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
