using EFT;
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
    }
}
