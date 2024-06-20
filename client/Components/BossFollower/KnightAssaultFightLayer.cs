using EFT;
using friendlyPMC.Modules;
using System.Collections.Generic;
using System;
using UnityEngine.AI;
using UnityEngine;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightAssaultFightLayer : GClass34
    {
        protected CustomNavigationPoint customNavigationPoint_0 = null;
        protected float coverTimer = 0f;
        public KnightAssaultFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            if (baseDecision.Action == BotLogicDecision.runToCover)
            {
                Vector3 center = botOwner_0.GetPlayer.Transform.position;

                if (baseDecision.Reason == "runToNtg")
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, friendlyPMC.fightInnerRadius.Value);
                }
                else if (baseDecision.Reason == "runIfCoverF" || baseDecision.Reason == "Ambush")
                {
                    GetClosestCoverPoint(center, friendlyPMC.fightOuterRadius.Value);
                }
                else if (baseDecision.Reason == "ShallRunIfNoAmmo" || baseDecision.Reason == "LastDamageDataActive")
                {
                    GetCoverPoint(center, friendlyPMC.fightOuterRadius.Value);
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }

            if (baseDecision.Action == BotLogicDecision.attackMoving)
            {
                if (baseDecision.Reason == "attackNtg" || baseDecision.Reason == "Last")
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, friendlyPMC.fightInnerRadius.Value);
                }

                if (customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }

            if (baseDecision.Action == BotLogicDecision.holdPosition && baseDecision.Reason == "hold2")
            {
                HoldFor(GClass760.Random(1f, 3f));
            }

            return baseDecision;
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
            if (this.customNavigationPoint_0 != null && (!this.customNavigationPoint_0.IsFreeById(this.botOwner_0.Id) || this.customNavigationPoint_0.IsSpotted))
            {
                this.customNavigationPoint_0 = null;
            }
            if (this.customNavigationPoint_0 != null)
            {
                return this.customNavigationPoint_0;
            }
            else
            {
                GetCoverPoint(botOwner_0.GetPlayer.Transform.position, 50f);
            }


            return this.customNavigationPoint_0;
        }

        protected virtual void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1f + Time.time;

            List<CustomNavigationPoint> customNavigationPoints = HasBoss() ? GetBoss().GetAreaCovers() : BossPlayers.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;

                NavMeshPath navMeshPath = new NavMeshPath();
                Vector3 botPosition = botOwner_0.Transform.position;

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (
                            point.IsFreeById(botOwner_0.Id) &&
                            !point.IsSpotted &&
                            (
                                !botOwner_0.Memory.HaveEnemy ||
                                (
                                    point.IsFreeById(botOwner_0.Memory.GoalEnemy.Owner.Id) &&
                                    point.IsDangerPositionFarEnough(new Vector3[] { botOwner_0.Memory.GoalEnemy.CurrPosition }, 5f)
                                )
                            )
                        )
                    {
                        float range = Vector3.Distance(centerPosition, point.Position);
                        if (range < distance)
                        {
                            navMeshPath.ClearCorners();
                            bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                            if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                            {

                                float dist = navMeshPath.CalculatePathLength();
                                if (dist > searchRadius)
                                {
                                    continue;
                                }
                            }
                            point1 = point;
                            distance = range;
                        }
                    }
                }
                if (point1 != null)
                {
                    customNavigationPoint_0 = point1;
                    botOwner_0.Memory.SetCoverPoints(point1);
                }
                else
                {
                    customNavigationPoint_0 = null;
                }
            }
        }

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            List<CustomNavigationPoint> customNavigationPoints = HasBoss() ? GetBoss().GetAreaCovers() : BossPlayers.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;

                List<CustomNavigationPoint> availablePoints = new List<CustomNavigationPoint>();

                NavMeshPath navMeshPath = new NavMeshPath();
                Vector3 botPosition = botOwner_0.Transform.position;

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (point.IsFreeById(botOwner_0.Id) && !point.IsSpotted)
                    {

                        float range = Vector3.Distance(centerPosition, point.Position);
                        if (range < distance)
                        {
                            navMeshPath.ClearCorners();
                            bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                            if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                            {

                                float dist = navMeshPath.CalculatePathLength();
                                if (dist > searchRadius)
                                {
                                    continue;
                                }
                            }
                            distance = range;
                            availablePoints.Add(point);

                        }
                    }
                }
                // get a random point
                if (availablePoints.Count > 0)
                {
                    point1 = availablePoints.Random();
                }


                if (point1 != null)
                {
                    customNavigationPoint_0 = point1;
                    botOwner_0.Memory.SetCoverPoints(point1);
                }
                else
                {
                    customNavigationPoint_0 = null;
                }
            }

        }
    }
}
