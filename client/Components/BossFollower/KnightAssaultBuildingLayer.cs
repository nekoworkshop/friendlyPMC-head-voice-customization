using EFT;
using friendlyPMC.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightAssaultBuildingLayer : GClass31
    {
        public KnightAssaultBuildingLayer(BotOwner bot, int priority) : base(bot, priority)
	    {
	    }

        private bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        private pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }

        public override bool ShallUseNow()
        {
            if(HasBoss() && botOwner_0.Memory.HaveEnemy && Vector3.Distance(GetBoss().Position, botOwner_0.Memory.GoalEnemy.CurrPosition) >= friendlyPMC.maximumCoverDistance.Value) 
            {
                return false;
            }

            return base.ShallUseNow();
        }
    }

    
}
