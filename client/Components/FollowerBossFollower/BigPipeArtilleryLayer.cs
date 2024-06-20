using EFT;
using friendlyPMC.Modules;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Components.FollowerBossFollower
{
    internal class BigPipeArtilleryLayer : GClass51
    {
        private float coverTimer = 0f;
        public BigPipeArtilleryLayer([NotNull] BotOwner owner, int priority) : base(owner, priority)
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

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            AICoreActionResultStruct<BotLogicDecision> baseDecision = base.GetDecision();

            if(HasBoss() && baseDecision.Action == BotLogicDecision.runToCover) {
                GetClosestCoverPoint(GetBoss().Position, friendlyPMC.fightOuterRadius.Value);
                if(customNavigationPoint_0 == null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
                }
            }

            return baseDecision;
        }

        private void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
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
    }
}
