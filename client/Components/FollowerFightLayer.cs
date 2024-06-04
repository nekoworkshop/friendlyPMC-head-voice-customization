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

        private float suppressTime = 0f;

        private bool ordersAreHold = false;
        private bool ordersAreAttack = false;
        private bool ordersAreReqroup = false;

        private bool holdTactic = false;
        private bool rushTactic = true;

        private bool bossUnderAttack = false;

        private string tactic = "balance";

        public FollowerFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            
        }

        public void SetBossFightTactic()
        {
            if (HasBoss())
            {
                if (GetBoss().GetBossLogic().GetTactic() == "push") rushTactic = true;
                else if (GetBoss().GetBossLogic().GetTactic() == "defend") holdTactic = true;
                else
                {
                    rushTactic = false;
                    holdTactic = false;
                }
            }
            else
            {
                rushTactic = false;
                holdTactic = false;
            }
        }

        public override string Name()
        {
            if (holdTactic) tactic = "defend";
            else if (rushTactic) tactic = "push";
            else tactic = "balance";

            return "FBPFight:" + tactic;
        }
        public override bool ShallUseNow()
        {
            if(
                botOwner_0.BotRequestController.CurRequest != null && HasBoss() && GetBoss().Player().ProfileId == botOwner_0.BotRequestController.CurRequest.Requester.ProfileId && 
                (
                    botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer
                )
             )
            {
                return true;
            }

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
            // Check if the bot needs to heal
            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                // If the bot is not in cover, find the closest cover and move to it
                if (!botOwner_0.Memory.IsInCover)
                {
                    GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                    }
                }
                // If the bot is in cover, heal
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "healInCover");
            }

            // If the enemy is visible and can be shot, shoot from the current position
            if (botOwner_0.Memory.GoalEnemy.IsVisible && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");
            }

            // If the bot is in cover
            if (botOwner_0.Memory.IsInCover)
            {
                // If the enemy is visible, shoot from cover
                if (botOwner_0.Memory.GoalEnemy.IsVisible)
                {
                    if (botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                    }
                }

                // Move to a closer cover while attacking
                GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, enemySearchRadius);
                if (customNavigationPoint_0 != null)
                {
                    float dist = 20f;
                    if ((customNavigationPoint_0.Position - botOwner_0.GetPlayer.Transform.position).magnitude > dist)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }
                    else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }
                }
                else
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "noCloseCoverEnemy");
                }
            }

            // If the bot is not in cover, find the closest cover near the enemy and move to it
            GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, enemySearchRadius);
            if (customNavigationPoint_0 != null)
            {
                float dist = 20f;
                if ((customNavigationPoint_0.Position - botOwner_0.GetPlayer.Transform.position).magnitude > dist)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                }
                else
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }
            }

            // Fallback decision if no cover is found
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "fallbackRunToEnemy");
        }

        private AICoreActionResultStruct<BotLogicDecision> DefendPosition()
        {
            // Check if the bot needs to heal
            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                // If the bot is not in cover, find the closest cover and move to it
                if (!botOwner_0.Memory.IsInCover)
                {
                    GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                    }
                }
                // If the bot is in cover, heal
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "healInCover");
            }

            // If the bot is already in cover
            if (botOwner_0.Memory.IsInCover)
            {
                // If the enemy is visible and can be shot, shoot from cover
                if (botOwner_0.Memory.GoalEnemy.IsVisible && botOwner_0.Memory.GoalEnemy.CanShoot)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                }
                // Otherwise, hold position
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "holdPositionInCover");
            }

            // If the bot is not in cover, find the closest cover and move to it
            if(HasBoss())
            {
                GetClosetCoverPointGroup(GetBoss().Position, searchRadius);
            } 
            else 
            { 
                GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position, searchRadius);
            }

            if (customNavigationPoint_0 != null)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "moveToCover");
            }

            // Fallback decision if no cover is found
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "fallbackDogFight");
        }


        public AICoreActionResultStruct<BotLogicDecision> DecideTactic()
        {
            // Check if the bot needs to heal
            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                return DefendPosition();
            }

            // Check if the enemy is visible and can be shot
            if (botOwner_0.Memory.GoalEnemy.IsVisible && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return EngageEnemy();
            }

            // Check if the bot has received the regroup command
            if (ordersAreReqroup)
            {
                Vector3 bossPosition = GetBoss().Position;
                GetClosetCoverPointGroup(bossPosition, searchRadius); // Adjust the radius as needed

                if (customNavigationPoint_0 != null)
                {
                    float dist = 15f;
                    if ((customNavigationPoint_0.Position - botOwner_0.GetPlayer.Transform.position).magnitude > dist)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "regroupToBossFast");
                    }
                    else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToBossSlow");
                    }
                }
                else
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "regroupFallback");
                }
            }

            if (ordersAreAttack || rushTactic) return EngageEnemy();
            else if (ordersAreHold || holdTactic) return DefendPosition();

            // Check if the boss is under attack
            if (bossUnderAttack)
            {
                // Switch the bot's enemy to the one attacking the boss
                var closestEnemy = GetBoss().ClosestEnemy();
                if (closestEnemy != null)
                {
                    GetBoss().PrioritizeEnemy(botOwner_0, closestEnemy);
                }

                // Get back to the boss
                Vector3 bossPosition = GetBoss().Position;
                GetClosetCoverPointGroup(bossPosition, searchRadius); // Adjust the radius as needed

                if (customNavigationPoint_0 != null)
                {
                    float dist = 20f;
                    if ((customNavigationPoint_0.Position - botOwner_0.GetPlayer.Transform.position).magnitude > dist)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "protectBossFast");
                    }
                    else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "protectBossSlow");
                    }
                }
                else
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "protectBossFallback");
                }
            }

            // Check the distance to the enemy if we can rush him (exclude snipers since they cannot be reached)
            float distanceToEnemySqr = (botOwner_0.Memory.GoalEnemy.CurrPosition - botOwner_0.GetPlayer.Transform.position).magnitude;
            float closeDistanceThresholdSqr = 50f;

            if (distanceToEnemySqr < closeDistanceThresholdSqr && !botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman))
            {
                return EngageEnemy();
            }
            else
            {
                return DefendPosition();
            }
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            rushTactic = false;
            holdTactic = false;

            BotRequest request = botOwner_0.BotRequestController.CurRequest;
            // accept requests only from teammates and boss
            if (request != null && !(request.Requester.ProfileId == botOwner_0.BotFollower.BossToFollow.Player().ProfileId || botOwner_0.BotsGroup.Contains(request.Requester.AIData.BotOwner)))
            {
                request = null;
            }

            if (request != null)
            {
                Logger.LogInfo("Fight request received is " + request.BotRequestType.ToString());
            }


            if (request != null && request.BotRequestType == BotRequestType.wait)
            {
                ordersAreHold = true;
            }
            else
            {
                ordersAreHold = false;
            }

            if (request != null && request.BotRequestType == BotRequestType.goToPoint)
            {
                ordersAreAttack = true;
            }
            else
            {
                ordersAreAttack = false;
            }
            // warn request is actually regroup
            if (request != null && request.BotRequestType == BotRequestType.warnPlayer)
            {
                ordersAreReqroup = true;
            }
            else
            {
                ordersAreReqroup = false;
            }

            if (HasBoss() && GetBoss().GetBossLogic().GetTactic() == "push")
            {
                rushTactic = true;
            }

            if (HasBoss() && GetBoss().GetBossLogic().GetTactic() == "defend")
            {
                holdTactic = true;
            }

            // partial re-creation of fight decisions in GClass47
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = InFightLogic();

            if (aicoreActionResultStruct != null)
            {
                return aicoreActionResultStruct.Value;
            }

            if (method_2())
            {
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
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal2");
                }
                if (!CheckMedsToStop(botOwner_0))
                {
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


            AIBossPlayerLogic gclass363_0 = HasBoss() ? GetBoss().GetBossLogic() : null;
            bossUnderAttack = gclass363_0 != null && gclass363_0.IsHitted;

            if (!botOwner_0.Memory.HaveEnemy)
            {
                if(bossUnderAttack)
                {
                    var closestEnemy = GetBoss().ClosestEnemy();
                    if (closestEnemy != null)
                    {
                        GetBoss().PrioritizeEnemy(botOwner_0, closestEnemy);
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "comeToBoss1");
                }

                if(!HasBoss())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.simplePatrol, "simplePatrol");
                } else
                {
                    GetClosetCoverPointGroup(GetBoss().Position,nearSearchRadius);
                    if(customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "comeToBoss2");

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "comeToBoss1");
                }
            }


            return DecideTactic();


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

        public override AICoreActionEndStruct EndRunToEnemy()
        {
            return base.EndRunToEnemy();
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
                        float range = (centerPosition - point.Position).magnitude;
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

        private void GetClosetCoverPointGroup(Vector3 centerPosition, float searchRadius)
        {
            if(!HasBoss() || GetBoss().Followers.Count < 2)
            {
                GetClosestCoverPoint(centerPosition, searchRadius);
                return;
            }

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

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
                        Vector3 delta = (centerPosition - point.Position);
                        float range = 0;
                        
                        range = delta.magnitude;

                        if (range < maxInnerRadius)
                        {
                            maxInnerRadius = range;
                            point1 = point;
                            if (numFollowers > 1 && delta.magnitude < innerRadius)
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

        private void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            List<CustomNavigationPoint> customNavigationPoints = BossPlayers.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;

                List<CustomNavigationPoint> availablePoints = new List<CustomNavigationPoint>();

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (IsPointFreeGroup(point) && !point.IsSpotted)
                    {
                        float range = (centerPosition - point.Position).magnitude;
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
