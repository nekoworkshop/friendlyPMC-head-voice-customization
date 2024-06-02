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

        private readonly float searchRadius = 70f;
        private readonly float enemySearchRadius = 50f;
        private readonly float nearSearchRadius = 30f;


        private float coverTimer = 0f;
        private float coverTimeRunner = 0f;

        private float suppressTime = 0f;

        private bool ordersAreHold = false;
        private bool ordersAreAttack = false;
        private bool ordersAreCoverMe = false;

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

        private AICoreActionResultStruct<BotLogicDecision> EngageEnemy()
        {

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;

            if (goalEnemy.IsVisible && goalEnemy.CanShoot)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");
            }

            if (botOwner_0.Memory.IsInCover)
            {
                if (goalEnemy.IsVisible)
                {
                    if (botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "sfc1");
                    }
                }

                if(ordersAreAttack)
                {
                    GetClosestCoverPoint(enemyPosition, enemySearchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        if ((customNavigationPoint_0.Position - botPosition).sqrMagnitude > 20f)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast1");
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow1");
                    } else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "noCloseCoverEnemy");
                    }
                }

                if (ordersAreHold)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "doNotEngage1");
                }

                if(ordersAreCoverMe)
                {
                    GetClosetCoverPointGroup(GetBoss().Position, nearSearchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        if ((customNavigationPoint_0.Position - botPosition).sqrMagnitude > 15f)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getToBossFast1");
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getToBossSlow1");
                    } else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "getToBossFast2");
                    }
                }

                float flag = Mathf.Round(UnityEngine.Random.Range(0f, 1f));

                if (flag == 0f)
                {
                    GetClosetCoverPointGroup(botPosition, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "nextCover1a");
                    }
                }
                else if (flag == 1f)
                {
                    GetClosestCoverPoint(enemyPosition, enemySearchRadius);
                    if (customNavigationPoint_0 != null && (customNavigationPoint_0.Position - botPosition).sqrMagnitude < 20f)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "nextCover1b");
                    } else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "runToEnemy");
                    }
                }


            } else
            {
                if (ordersAreAttack)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemyZigZag, "getInCloseFast2");
                }
                if (ordersAreHold)
                {
                    GetClosetCoverPointGroup(botPosition, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "findCover1");
                    }
                    else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dgf-nocover2");
                    }
                }

                if (ordersAreCoverMe)
                {
                    GetClosetCoverPointGroup(GetBoss().Position, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        if ((customNavigationPoint_0.Position - botPosition).sqrMagnitude > 15f)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getToBossFast2");
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getToBossSlow2");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dgf-nocover2");
                }

                if (goalEnemy.IsVisible)
                {
                    GetClosetCoverPointGroup(botPosition, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        if((customNavigationPoint_0.Position - botPosition).sqrMagnitude < 20f)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "nextCover2a");
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "nextCover2b");
                    } else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dgf-nocover1");
                    }
                }
            }

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "noEngage");
        }


        private AICoreActionResultStruct<BotLogicDecision> RoundProtect()
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            
            if(!botOwner_0.Memory.HaveEnemy && HasBoss()) GetBoss().PrioritizeEnemy(botOwner_0);

            if (HasBoss())
            {
                GetClosetCoverPointGroup(GetBoss().Position, searchRadius);
            } else
            {
                GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position, searchRadius);
            }

            if (customNavigationPoint_0 == null)
            {
                if ((customNavigationPoint_0.Position - botPosition).sqrMagnitude > 15f)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "aroundBossFast");
                } else
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "aroundBossSlow");
                }
            } else if (HasBoss())
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "comeToBoss2");
            }

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.standBy, "noProtection");
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

            if (method_2())
            {
                coverTimeRunner = 0f;
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "cdg");

            }

            // damaged and has healers
            if (botOwner_0.Medecine.Stimulators.Using)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "usingStims");
            }

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

            BotRequest request = botOwner_0.BotRequestController.CurRequest;
            // accept requests only from teammates and boss
            if (request != null && request.Requester != botOwner_0.BotFollower.BossToFollow.Player() && !botOwner_0.BotsGroup.Contains(request.Requester.AIData.BotOwner))
            {
                request = null;
            }

            if (request != null && (request.BotRequestType == BotRequestType.hold || request.BotRequestType == BotRequestType.wait))
            {
                ordersAreHold = true;
            }
            else
            {
                ordersAreHold = false;
            }

            if (request != null && request.BotRequestType == BotRequestType.attackClose)
            {
                ordersAreAttack = true;
            }
            else
            {
                ordersAreAttack = false;
            }

            if (request != null && request.BotRequestType == BotRequestType.goToPoint)
            {
                ordersAreCoverMe = true;
                ordersAreAttack = false;
                ordersAreHold = false;
            }

            AIBossPlayerLogic gclass363_0 = HasBoss() ? GetBoss().GetBossLogic() : null;
            bool bossUnderAttack = gclass363_0 != null && gclass363_0.IsHitted;

            if (!botOwner_0.Memory.HaveEnemy)
            {
                if(bossUnderAttack)
                {
                    GetBoss().PrioritizeEnemy(botOwner_0);

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "comeToBoss1");
                }

                if(!HasBoss())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.simplePatrol, "simplePatrol");
                } else
                {
                    GetClosetCoverPointGroup(GetBoss().Position,searchRadius);
                    if(customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "comeToBoss2");

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "comeToBoss1");
                }
            }


            var EngageDecision = EngageEnemy();
            if (EngageDecision.Reason != "noEngage") return EngageDecision;

            var ProtectDecision = RoundProtect();
            if(ProtectDecision.Reason != "noProtection") return ProtectDecision;

            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dogfight-fallback");



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

        public override AICoreActionEndStruct EndSuppressFire()
        {
            BotRequest curRequest = this.botOwner_0.BotRequestController.CurRequest;
            if (curRequest != null && curRequest.BotRequestType == BotRequestType.suppressionFire)
            {
                if(suppressTime < Time.time)
                {
                    suppressTime = 0;
                    curRequest.Complete();

                    return this.gstruct7_0;
                }
                return this.gstruct7_1;
            }
            return this.gstruct7_0;
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

        private void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
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
                            IsPointFreeGroup(point) &&
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

            GetClosetCoverPointGroup(centerPosition, searchRadius);

            return customNavigationPoint_0 != null;
        }

        private bool HasCloseCoverToEnemy()
        {
            Vector3 centerPosition = botOwner_0.Memory.GoalEnemy.Person.Position;

            GetClosetCoverPointGroup(centerPosition,enemySearchRadius);
            // not too far
            if (customNavigationPoint_0 != null && (botOwner_0.GetPlayer.Transform.position - customNavigationPoint_0.Position).magnitude > 10f)
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
                    if (IsPointFreeGroup(point) && !point.IsSpotted)
                    {
                        range = (centerPosition - point.Position).magnitude;
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

        private void GetClosetCoverPointGroup(Vector3 centerPosition, float searchRadius)
        {

            List<CustomNavigationPoint> customNavigationPoints = BossPlayers.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;

                int minFollowers = 1;
                int maxFollowers = HasBoss() ? GetBoss().Followers.Count : 1;
                float maxInnerRadius = searchRadius;
                float minInnerRadius = 0f;
                int numFollowers = HasBoss() ? GetBoss().Followers.Count : 1;

                float innerRadius = ((numFollowers - minFollowers) / (float)(maxFollowers - minFollowers)) * (maxInnerRadius - minInnerRadius) + minInnerRadius;

                List<CustomNavigationPoint> availablePoints = new List<CustomNavigationPoint>();

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (IsPointFreeGroup(point) && !point.IsSpotted)
                    {
                        float range = (centerPosition - point.Position).sqrMagnitude;
                        if (range < maxInnerRadius)
                        {
                            maxInnerRadius = range;
                            point1 = point;
                            if (numFollowers > 1 && range < innerRadius)
                            {
                                availablePoints.Add(point);
                            }
                            

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

        private bool IsPointFreeGroup(CustomNavigationPoint point)
        {
            if (!HasBoss()) return point.IsFreeById(botOwner_0.Id);
            
            bool isfree = true;

            foreach (var follower in GetBoss().Followers)
            {
                if(!point.IsFreeById(follower.Id))
                {
                    isfree = false;
                    break;
                } 
            }
            return isfree;

        }
    }
}
