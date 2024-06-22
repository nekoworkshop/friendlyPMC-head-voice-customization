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

        protected CustomNavigationPoint customNavigationPoint_0;

        protected float sprintDistance = 15f;
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
            if (HasBoss() && botOwner_0.Memory.HaveEnemy && Vector3.Distance(GetBoss().Position, botOwner_0.Memory.GoalEnemy.CurrPosition) >= friendlyPMC.maximumCoverDistance.Value)
            {
                return false;
            }

            return base.ShallUseNow();
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            if (baseDecision.Action == BotLogicDecision.runToEnemy && Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Position, botOwner_0.Memory.GoalEnemy.CurrPosition) > 25f)
            {
                customNavigationPoint_0 = Utils.Utils.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition);
                if (customNavigationPoint_0 == null)
                {
                    botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);
                    if (Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Position, customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }
            }

            return baseDecision;
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            return Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0);
        }

    }
}
