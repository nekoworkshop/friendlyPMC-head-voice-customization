using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightEnemyBuildingLayer : GClass32
    {

        protected CustomNavigationPoint customNavigationPoint_0 = null;
        protected float coverTimer = 0f;
        public KnightEnemyBuildingLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            if (baseDecision.Action == BotLogicDecision.runToCover)
            {
                GetCoverPoint(HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position, friendlyPMC.fightOuterRadius.Value);

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }

            if(baseDecision.Action == BotLogicDecision.holdPosition && baseDecision.Reason == "hold2")
            {
                HoldFor(GClass760.Random(1f, 3f));
            }

            return baseDecision;
        }

        public override bool ShallUseNow()
        {
            List<BotRequestType> fightdRequests = new List<BotRequestType>
            {
                BotRequestType.warnPlayer // this is need help or regroup from the player
            };

            if(botOwner_0.BotRequestController.CurRequest != null && fightdRequests.Contains(botOwner_0.BotRequestController.CurRequest.BotRequestType))
            {
                return false;
            }

            return base.ShallUseNow();
        }

        protected virtual bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        protected virtual pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }


        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {

            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0);


            return customNavigationPoint_0;
        }

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            CustomNavigationPoint point1 = Utils.Utils.GetCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);

        }
    }
}
