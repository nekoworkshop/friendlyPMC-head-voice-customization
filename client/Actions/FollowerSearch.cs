using EFT;
using JetBrains.Annotations;

namespace friendlyPMC.Actions
{
    internal class FollowerSearch : FollowerSniperSearch
    {
        public FollowerSearch(BotOwner owner)
        : base(owner)
        {
            _minDist = 5f;
            _maxDist = 60f;
        }
    }
}
