using Aki.Common.Http;
using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Components
{
    // GClass47 is followerBoar Fight layer
    internal class FollowerFightLayer : GClass47
    {

        private readonly float searchRadius = 30f;


        private float coverTimer = 0f;
        private float coverTimeRunner = 0f;

        private float float_2 = 0f;

        private bool ordersAreHold = false;
        private bool ordersAreAttack = false;

        public FollowerFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        public override string Name()
        {
            return "FBPFight";
        }
        public override bool ShallUseNow()
        {
            return botOwner_0.Memory.HaveEnemy;
        }

        private bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        private pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }

        private bool TimeToHeal()
        {
            return Time.time - this.botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime >= 30f && (this.botOwner_0.Medecine.FirstAid.Have2Do || this.botOwner_0.Medecine.SurgicalKit.HaveWork);
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            // partial re-creation of fight decisions in GClass47
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = InFightLogic();

            if (aicoreActionResultStruct != null)
            {
                coverTimeRunner = 0f;
                return aicoreActionResultStruct.Value;
            }


            AIBossPlayerLogic gclass363_0 = HasBoss() ? GetBoss().GetBossLogic() : null;
            bool bossUnderAttack = gclass363_0 != null && gclass363_0.IsHitted;

            if (method_2())
            {
                coverTimeRunner = 0f;
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "cdg");

            }

            BotRequest request = botOwner_0.BotRequestController.CurRequest;
            // accept requests only from teammates and boss
            if (request != null && request.Requester != botOwner_0.BotFollower.BossToFollow.Player() && !botOwner_0.BotsGroup.Contains(request.Requester.AIData.BotOwner))
            {
                request = null;
            }

            // damaged and has healers
            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                if (!botOwner_0.Memory.HaveEnemy)
                {
                    coverTimeRunner = 0f;
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal2");
                }
                if (!CheckMedsToStop(botOwner_0))
                {
                    coverTimeRunner = 0f;
                    if (!botOwner_0.Memory.IsInCover)
                    {
                        GetCoverPoint(botOwner_0.GetPlayer.Transform.position, searchRadius);
                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                        }
                    }
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal1");
                }
            }
            
            if (request != null && (request.BotRequestType == BotRequestType.hold || request.BotRequestType == BotRequestType.wait))
            {
                ordersAreHold = true;
            } else
            {
                ordersAreHold = false;
            }

            if (request != null && request.BotRequestType == BotRequestType.attackClose)
            {
                ordersAreAttack = true;
            } else
            {
                ordersAreAttack = false;
            }

            if (!botOwner_0.Memory.HaveEnemy)
            {

                coverTimeRunner = 0f;
                if (HasBoss() && !ordersAreHold)
                {
                    botOwner_0.GoToSomePointData.SetPoint(GetBoss().Position);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, "comeToBoss-noEnemy");
                }
                else if (!ordersAreHold)
                
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.simplePatrol, "roamaround");
                
                else
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:holdPosFight");

            }
            else
            {
                // if we see the enemy and can shoot him - shoot him!
                EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
                if (goalEnemy.IsVisible && goalEnemy.CanShoot)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");
                }

                // in cover, but enemy not visible
                if (botOwner_0.Memory.IsInCover && !goalEnemy.IsVisible)
                {
                    // - boss under attack, get close to boss
                    if (bossUnderAttack)
                    {
                        coverTimeRunner = 0f;
                        GetClosestCoverPoint(GetBoss().Position);
                        // -- is close enough, try to provide suppression
                        if (customNavigationPoint_0 != null && (botOwner_0.GetPlayer.Transform.position - customNavigationPoint_0.Position).sqrMagnitude < 10f)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "interuptAttack");
                        }
                        if (!ordersAreHold) 
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "comeToBoss-underAttack");
                    }
                    // - else stay in cover until time runs out
                    if (coverTimeRunner == 0f && !ordersAreAttack && !ordersAreHold)
                    {
                        coverTimeRunner = 1f;

                        return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(UnityEngine.Random.Range(1f, 3f)), "waitabit");
                    }
                    // - then proceed to approach the enemy
                    else
                    {
                        coverTimeRunner = 0f;
                        // -- find closes cover the enemy and aproach him from that way
                        if (HasCloseCoverToEnemy() && !ordersAreHold)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "closeIn1");
                        // -- else back to boss while attacking
                        if(HasCloseCoverToBoss() && !ordersAreHold && !ordersAreAttack)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "backToBoss");
                        // -- else just fight
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "justFight3");
                    }

                }
                // not in cover and enemy not visible
                else if (!goalEnemy.IsVisible)
                {
                    coverTimeRunner = 0f;
                    // - boss under attack, get back to him
                    if (bossUnderAttack && !ordersAreAttack)
                    {
                        
                        if (HasCloseCoverToBoss() && !ordersAreHold)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "backToBoss");
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "justFight4");
                    }
                    // - else search for the enemy
                    Vector3 enemyLastSeenPos = goalEnemy.EnemyLastPosition;
                    Vector3 myPOs = botOwner_0.GetPlayer.Transform.position;
                    float distToEnemy = (enemyLastSeenPos - myPOs).sqrMagnitude;

                    if (distToEnemy < 10f && !ordersAreHold)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.search, "searchEnemy");
                    }
                    else if (!ordersAreHold)
                    {
                        // - if enemy is close enough, go after him
                        if (distToEnemy < 20f || ordersAreAttack)
                        {
                            GetClosestCoverPoint(enemyLastSeenPos);
                            if (customNavigationPoint_0 != null)
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getCloser");
                            }
                            else
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "rushEnemyLastPosition");
                            }
                        // - else stick to boss
                        }
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "stickCloseToBoss2");
                        }
                    }
                }

                // not in cover and enemy is visible
                if (!botOwner_0.Memory.IsInCover && goalEnemy.IsVisible)
                {
                    coverTimeRunner = 0f;
                    // can't shoot
                    if (!goalEnemy.CanShoot)
                    {
                        if(ordersAreAttack)
                        {
                            if (HasCloseCoverToEnemy())
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "closeIn3");
                            else
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "justFight4");
                        }
                        // -- find cover if can't shoot
                        if ((HasBoss() && HasCloseCoverToBoss()) || this.customNavigationPoint_0 == null)
                        {
                            if (!HasBoss()) GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position);
                            if(customNavigationPoint_0 != null)
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "cantShootFindCover");
                            // -- no cover, still can't shoot, just provide suppression
                            else
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "justFight1");
                        }   
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "justFight2");
                        }

                    // can shoot
                    }
                    else
                    {
                        // - shoot while moving to cover
                        GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position);
                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "AToCvr");
                            // - can't move to cover or enemy too close, just shoot
                        }
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "attack");
                        }

                    }
                }

                // in cover and enemy is visible
                // - can shoot from cover
                if (goalEnemy.CanShoot)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "sfc");
                    // - get new cover, close to the enemy
                }
                else if (HasCloseCoverToEnemy() && !ordersAreHold)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "closeIn2");
                    // - can't get a new cover, provide supression
                }
                else
                {
                    // - shoot while moving to cover
                    GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "findCoverToShoot");
                    }
                    // - supression fire
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "justFight3");
                }

            }

        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            AIBossPlayerLogic gclass363_0 = HasBoss() ? botOwner_0.BotFollower.BossToFollow.GetBossLogic() as AIBossPlayerLogic : null;

            string text;
            if (base.method_5(out text))
            {
                return new AICoreActionEndStruct("cst", true);
            }

            if (TimeToHeal())
            {
                return new AICoreActionEndStruct("wntHeal", true);
            }

            if (this.customNavigationPoint_0 != null && !this.customNavigationPoint_0.IsFreeById(this.botOwner_0.Id))
            {
                this.customNavigationPoint_0 = null;
            }
            if (this.customNavigationPoint_0 != null && this.customNavigationPoint_0.CanIShootToEnemy && this.botOwner_0.Memory.IsInCover && this.botOwner_0.Memory.BotCurrentCoverInfo.CovPoint.Id != this.customNavigationPoint_0.Id && Time.time - this.botOwner_0.Memory.ComeToCoverTime > 3f && gclass363_0 != null)
            {
                this.botOwner_0.Memory.Spotted(false, null, null);
                this.botOwner_0.Memory.BotCurrentCoverInfo.SetCover(this.customNavigationPoint_0, true);
                GClass363 gclass = gclass363_0;
                if (gclass != null)
                {
                    gclass.StartMoveToAttackPoint(botOwner_0.Id);
                }
                return new AICoreActionEndStruct("betterCover", true);
            }
            if (base.method_6())
            {
                return new AICoreActionEndStruct("EndHol", true);
            }
            if (!this.botOwner_0.Memory.IsInCover)
            {
                return new AICoreActionEndStruct("notInCover", true);
            }
            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            if (goalEnemy != null && goalEnemy.IsVisible && goalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("CanShoot", true);
            }
            if (gclass363_0.IsHitted)
            {
                return new AICoreActionEndStruct("bossHit", true);
            }

            GetCoverPoint(botOwner_0.GetPlayer.Transform.position, searchRadius);

            return this.gstruct7_1;
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

            return base.FindPoint(data, p, checkCurrent);
        }

        private void GetClosestCoverPoint(Vector3 centerPosition)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            List<CustomNavigationPoint> customNavigationPoints = BossPlayers.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;
                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (
                            point.IsFreeById(botOwner_0.Id) &&
                            !point.IsSpotted &&
                            (
                                !botOwner_0.Memory.HaveEnemy ||
                                (
                                    point.IsFreeById(botOwner_0.Memory.GoalEnemy.Owner.Id) &&
                                    point.IsDangerPositionFarEnough(new Vector3[] { botOwner_0.Memory.GoalEnemy.CurrPosition }, 7f)
                                )
                            )
                        )
                    {
                        float range = (centerPosition - point.Position).sqrMagnitude;
                        if (range < distance)
                        {
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

        private bool HasCloseCoverToBoss()
        {
            pitAIBossPlayer playerBoss = (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
            Vector3 centerPosition = playerBoss.Position;

            GetClosestCoverPoint(centerPosition);

            return customNavigationPoint_0 != null;
        }

        private bool HasCloseCoverToEnemy()
        {
            Vector3 centerPosition = botOwner_0.Memory.GoalEnemy.Person.Position;

            GetClosestCoverPoint(centerPosition);
            // not too far
            if (customNavigationPoint_0 != null && (botOwner_0.GetPlayer.Transform.position - customNavigationPoint_0.Position).sqrMagnitude > 10f)
            {
                customNavigationPoint_0 = null;
            }

            return customNavigationPoint_0 != null;
        }

        private void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            List<CustomNavigationPoint> customNavigationPoints = BossPlayers.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;
                float range = 0;

                List<CustomNavigationPoint> availablePoints = new List<CustomNavigationPoint>();

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (point.IsFreeById(botOwner_0.Id) && !point.IsSpotted)
                    {
                        range = (centerPosition - point.Position).sqrMagnitude;
                        if (range < distance)
                        {
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
