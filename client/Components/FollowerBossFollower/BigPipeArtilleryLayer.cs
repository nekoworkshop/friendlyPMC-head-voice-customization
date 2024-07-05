using EFT;
using friendlyPMC.Components.BossFollower;
using JetBrains.Annotations;
using System;
using UnityEngine;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BigPipeArtilleryLayer : KnightFightLayer
    {

        protected GClass51 supportLayer;
        public BigPipeArtilleryLayer([NotNull] BotOwner owner, int priority) : base(owner, priority)
        {
            followerFightLayer = new FollowerFightLayer(owner,priority);
            supportLayer = new GClass51(owner,priority);
        }

        public override string Name()
        {
            return "PipeFight";
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;


            AICoreActionResultStruct<BotLogicDecision> baseDecision =  base.GetDecision();

            if(
                baseDecision.Action == BotLogicDecision.shootFromPlace ||
                baseDecision.Reason == "usingStims" ||
                baseDecision.Reason == "runToHeal" ||
                baseDecision.Reason == "heal" ||
                baseDecision.Reason == "healInCover"

            ) 
                return baseDecision;

            if (botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman))
                return baseDecision;


            if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.Close)
            {
                return followerFightLayer.CloseFight();
            }

            AICoreActionResultStruct < BotLogicDecision > supportDecision = supportLayer.GetDecision();

            if(
                supportDecision.Action == BotLogicDecision.suppressFire || 
                supportDecision.Action == BotLogicDecision.shootToSmoke ||
                supportDecision.Action == BotLogicDecision.suppressGrenade
            )
            {
                return supportDecision;
            }

            if (supportDecision.Reason == "IsInSmoke")
            {
                GetClosestCoverPoint(bossPosition, fightRange);
                if (customNavigationPoint_0 != null)
                {
                    return supportDecision;
                }
            }

            return baseDecision;
        }
        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return gstruct7_0;
            }

            if (
                curDecision.Reason == "getInCloseFast" ||
                curDecision.Reason == "getInCloseSlow" ||
                curDecision.Reason == "am" ||
                curDecision.Reason == "repositionFast" ||
                curDecision.Reason == "reposition"
            )
            {
                return EndGetInClose();
            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        
    }
}
