using EFT;
using friendlyPMC.Components.BossFollower;
using friendlyPMC.Modules;
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
            supportLayer = new GClass51(owner,priority);
        }

        public override string Name()
        {
            return "PipeFight";
        }

        public override void OnActivate()
        {
            supportLayer?.OnActivate();
            base.OnActivate();
        }

        public override void Dispose()
        {
            supportLayer?.Dispose();
            base.Dispose();
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            // is in dogfight?
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = commonLayer.DogFight(out customNavigationPoint_0);
            if (aicoreActionResultStruct != null) return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;

            // needs healing?
            aicoreActionResultStruct = commonLayer.NeedHeal(out customNavigationPoint_0);
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
                Modules.Logger.LogInfo("baseDecision Error: " + ex.Message);
                Modules.Logger.LogInfo("Trace: " + ex.StackTrace);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass761.Random(1f, 2f)), "baseDecision.Error");
            }

            if (
                baseDecision.Reason == "regroupToBossFast" || 
                baseDecision.Reason == "regroupToBoss" ||
                (commonLayer.OrderHasChangedRecently && request != null && request.BotRequestType == BotRequestType.attackClose) ||
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
                Modules.Logger.LogInfo("supportDecision Error: " + ex.Message);
                Modules.Logger.LogInfo("Trace: " + ex.StackTrace);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass761.Random(1f, 2f)), "supportDecision.Error");
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
                //GetClosestCoverPoint(bossPosition, fightRange);
                if (customNavigationPoint_0 != null)
                {
                    return supportDecision;
                }
            }

            return baseDecision;
        }
        
    }
}
