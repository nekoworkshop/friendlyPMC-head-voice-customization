using Aki.Common.Http;
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

            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            // is in dogfight?
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = followerFightLayer.DogFight();
            if (aicoreActionResultStruct != null) return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;

            // needs healing?
            aicoreActionResultStruct = followerFightLayer.NeedHeal();
            if (aicoreActionResultStruct != null) return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;

            // player requests?
            AICoreActionResultStruct<BotLogicDecision>? preFightDecision = KnightPreFight();
            if (preFightDecision != null) return (AICoreActionResultStruct<BotLogicDecision>)preFightDecision;

            AICoreActionResultStruct<BotLogicDecision> baseDecision;

            try
            {
                baseDecision = base.KnightFight();
            }
            catch (Exception ex)
            {
                Components.Logger.LogInfo("baseDecision Error: " + ex.Message);
                Components.Logger.LogInfo("Trace: " + ex.StackTrace);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass760.Random(1f, 2f)), "baseDecision.Error");
            }

            if (
                baseDecision.Reason == "regroupToBossFast" || 
                baseDecision.Reason == "regroupToBoss" ||
                (ordersChanged && request != null && request.BotRequestType == BotRequestType.attackClose) ||
                botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman) ||
                baseDecision.Action == BotLogicDecision.shootFromPlace
            )
                return baseDecision;


            /*if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.Close)
            {
                return followerFightLayer.CloseFight();
            }*/

            AICoreActionResultStruct<BotLogicDecision> supportDecision;

            try
            {
                supportDecision = supportLayer.GetDecision();
            }
            catch (Exception ex)
            {
                Components.Logger.LogInfo("supportDecision Error: " + ex.Message);
                Components.Logger.LogInfo("Trace: " + ex.StackTrace);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass760.Random(1f, 2f)), "supportDecision.Error");
            }

            if (
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
        
    }
}
