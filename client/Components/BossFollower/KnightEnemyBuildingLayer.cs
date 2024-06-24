using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using Aki.Common.Http;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightEnemyBuildingLayer : GClass32
    {

        protected CustomNavigationPoint customNavigationPoint_0 = null;

        private float sprintDistance = 15f;
        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;
        public KnightEnemyBuildingLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        public override bool ShallUseNow()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return false;
            }


            if (
                   botOwner_0.BotRequestController.CurRequest != null &&
                   (botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.attackClose ||
                   botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer)
               )
            {

                return false;
            }


            return (this.botOwner_0.Memory.HaveEnemy && this.botOwner_0.Memory.GoalEnemy.Person.AIData.EnvironmentId > 0 && Time.time - this.botOwner_0.Memory.GoalEnemy.GroupInfo.EnemyLastSeenTimeReal < 900f) || (this.botOwner_0.Memory.LastEnemy != null && this.botOwner_0.Memory.LastEnemy.Person.AIData.EnvironmentId > 0 && Time.time - this.botOwner_0.Memory.LastEnemy.GroupInfo.EnemyLastSeenTimeReal < 900f);
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return gstruct7_0;
            }


            return base.ShallEndCurrentDecision(curDecision);
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;

            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            if (baseDecision.Action == BotLogicDecision.runToCover)
            {
                GetClosestCoverPoint(botPosition, fightRange);

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }

            if (baseDecision.Action == BotLogicDecision.holdPosition)
            {
               
                if(
                    botOwner_0.Memory.IsInCover && (!HasBoss() || 
                    Utils.Utils.GetNavDistance(GetBoss().Position, botOwner_0.GetPlayer.Position) < fightRange)
                )
                {
                    HoldFor(GClass760.Random(1f, 3f));
                } else
                {
                    GetCoverPoint(bossPosition, fightRange);

                    if (customNavigationPoint_0 == null)
                    {
                        GetClosestCoverPoint(bossPosition, fightLongRange);
                    }

                    if (customNavigationPoint_0 == null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                    }

                    if (Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Position, customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "moveToCoverFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "moveToCoverSlow");
                }
            }

            return baseDecision;
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {

            if (ShallGoNearBoss()) return new AICoreActionEndStruct("goNearPlayer", true);
            return base.EndHoldPosition();
        }

        protected virtual bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        protected virtual pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }

        private bool ShallGoNearBoss()
        {
            if (!HasBoss()) return false;
            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            float bossDist = Vector3.Distance(botOwner_0.Position, GetBoss().Position);

            return bossDist > Mathf.Min(friendlyPMC.maximumCoverDistance.Value, friendlyPMC.regroupMinDistance.Value) && (goalEnemy == null || !goalEnemy.HaveSeen || (goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime > friendlyPMC.maximumCover.Value));
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {

            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0);


            return customNavigationPoint_0;
        }

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            CustomNavigationPoint point1 = Utils.Utils.GetCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);

        }

        protected virtual void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            CustomNavigationPoint point = Utils.Utils.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);
        }
    }
}
