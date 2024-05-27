using EFT;
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
            bool hasBossRequest = botOwner_0.BotFollower.HaveBoss && botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.Requester == botOwner_0.BotFollower.BossToFollow.Player();
            BotRequest currRequest = hasBossRequest ? botOwner_0.BotRequestController.CurRequest : null;

            // partial re-creation of fight decisions in GClass47
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = InFightLogic();

            if (aicoreActionResultStruct != null)
            {
                coverTimeRunner = 0f;
                return aicoreActionResultStruct.Value;
            }

            // weapons has no bullets
            if (!botOwner_0.WeaponManager.HaveBullets)
            {
                // - reload while moving to cover
                if (!botOwner_0.Memory.IsInCover)
                {
                    coverTimeRunner = 0f;
                    botOwner_0.WeaponManager.Reload.TryReload();
                    GetClosestCoverPoint(botOwner_0.Position);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "doRun");
                }
            }


            AIBossPlayerLogic gclass363_0 = HasBoss() ? botOwner_0.BotFollower.BossToFollow.GetBossLogic() as AIBossPlayerLogic : null;
            bool bossUnderAttack = gclass363_0 != null && gclass363_0.IsHitted;


            if (method_2())
            {
                coverTimeRunner = 0f;
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "cdg");

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
                        GetCoverPoint(botOwner_0.Position, searchRadius);
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "doRun");
                    }
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal1");
                }
            }

            // TO DO : implement check currRequest

            if (!botOwner_0.Memory.HaveEnemy)
            {
                coverTimeRunner = 0f;
                if (HasBoss())
                {
                    botOwner_0.GoToSomePointData.SetPoint(GetBoss().Position);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, "comeToBoss-noEnemy");
                } else
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.simplePatrol, "roamaround");
                }

            }
            else
            {
                EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
                // in cover, but enemy not visible
                if (botOwner_0.Memory.IsInCover && !goalEnemy.IsVisible)
                {
                    // - boss under attack, get close to boss
                    if (bossUnderAttack)
                    {
                        coverTimeRunner = 0f;
                        GetClosestCoverPoint(GetBoss().Position);
                        // -- is close enough, try to provide suppression
                        if(customNavigationPoint_0 != null && (botOwner_0.Position - customNavigationPoint_0.Position).sqrMagnitude < 10f)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "interuptAttack");
                        }

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "comeToBoss-underAttack");
                    }
                    // - else stay in cover until time runs out
                    if (coverTimeRunner == 0f)
                    {
                        coverTimeRunner = 1f;

                        return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(UnityEngine.Random.Range(2f, 5f)), "waitabit");
                    }
                    // - then proceed to approach the enemy
                    else
                    {
                        coverTimeRunner = 0f;
                        // -- find closes cover the enemy and aproach him from that way
                        if (HasCloseCoverToEnemy())
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "closeIn1");
                        // -- else just rush him
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "confrontEnemey");
                    }
                   
                }
                // not in cover and enemy not visible
                else if (!goalEnemy.IsVisible)
                {
                    coverTimeRunner = 0f;
                    // - boss under attack, get back to him
                    if (bossUnderAttack)
                    {
                        GetClosestCoverPoint(GetBoss().Position);
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "backToBoss");
                    }
                    // - else search for the enemy
                    Vector3 enemyLastSeenPos = goalEnemy.EnemyLastPosition;
                    Vector3 myPOs = botOwner_0.Position;

                    if ((enemyLastSeenPos - myPOs).sqrMagnitude < 15f)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.search, "searchEnemy");
                    }
                    else
                    {
                        // - if enemy is close enough, go after him
                        if ((enemyLastSeenPos - myPOs).sqrMagnitude < 70f)
                        {
                            GetClosestCoverPoint(enemyLastSeenPos);
                            if (customNavigationPoint_0 != null)
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getCloser");
                            }
                            else
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToEnemy, "rushEnemyLastPosition");
                            }
                        // - else stick to boss
                        } else 
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "stickCloseToBoss");
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
                        // -- find cover if can't shoot
                        if ( (HasBoss() && HasCloseCoverToBoss()) || this.customNavigationPoint_0 == null)
                        {
                            if(!HasBoss()) GetClosestCoverPoint(botOwner_0.Position);
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToCoverPoint, "CantShootFindCover");
                            // -- no cover, still can't shoot, just provide suppression
                        }
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "intimidate");
                        }

                    // can shoot
                    }
                    else
                    {
                        // - shoot while moving to cover
                        GetClosestCoverPoint(botOwner_0.Position);
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
                else if (HasCloseCoverToEnemy())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "closeIn2");
                // - can't get a new cover, provide supression
                }
                else
                {
                    // - shoot while moving to cover
                    GetClosestCoverPoint(botOwner_0.Position);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "findCoverToShoot");
                    }
                    // - supression fire
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "intimidate");
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

            GetCoverPoint(botOwner_0.Position,searchRadius);

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


        public new void method_14()
        {
            if (float_2 < Time.time)
            {
                if (this.customNavigationPoint_0 != null && ((!this.customNavigationPoint_0.CanIShootToEnemy && this.botOwner_0.Memory.HaveEnemy) || !this.customNavigationPoint_0.IsFreeById(this.botOwner_0.Id) || this.customNavigationPoint_0.IsSpotted))
                {
                    this.customNavigationPoint_0 = null;
                }
                float_2 = 1f + Time.time;
                GetCoverPoint(HasBoss() ? GetBoss().Position : botOwner_0.Position, searchRadius);
            }
        }

        private void GetClosestCoverPoint(Vector3 centerPosition)
        {

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 2f + Time.time;

            List<CustomNavigationPoint> customNavigationPoints = BossPlayer.Instance.GetCovers();

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
                                    point.IsDangerPositionFarEnough(new Vector3[] { botOwner_0.Memory.GoalEnemy.CurrPosition },5f)
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
            if(customNavigationPoint_0 != null && (botOwner_0.Position - customNavigationPoint_0.Position).sqrMagnitude > 10f)
            {
                customNavigationPoint_0 = null;
            }

            return customNavigationPoint_0 != null;
        }

        private void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            List<CustomNavigationPoint> customNavigationPoints = BossPlayer.Instance.GetCovers();

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
