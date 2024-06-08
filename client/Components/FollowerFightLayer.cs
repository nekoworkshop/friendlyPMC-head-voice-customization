using Aki.Common.Http;
using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Components
{
    // GClass47 is followerBoar Fight layer
    internal class FollowerFightLayer : GClass47
    {

        private readonly float searchRadius = 50f;
        private readonly float nearSearchRadius = 30f;


        private float coverTimer = 0f;

        private float suppressTime = 0f;

        private bool ordersAreHold = false;
        private bool ordersAreAttack = false;
        private bool ordersAreReqroup = false;

        private bool holdTactic = false;
        private bool rushTactic = true;

        private bool ordersChanged = false;

        private bool bossUnderAttack = false;

        private string tactic = "balance";


        public FollowerFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            
        }

        public void SetBossFightTactic(string tactic)
        {
            if (tactic == "push")
            {
                rushTactic = true;
                holdTactic = false;
            }
            else if (tactic == "defend") 
            {
                holdTactic = true;
                rushTactic |= false;
            
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
                    botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer ||
                    (botOwner_0.Memory.HaveEnemy && botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.goToPoint)
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

        private bool ShallGoNearBoss()
        {
            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            return Vector3.Distance(botOwner_0.Position, GetBoss().Position) > 20f && goalEnemy == null || (goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime > 10f);
        }

        private bool TimeToHeal()
        {
            return Time.time - this.botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime >= 30f && (this.botOwner_0.Medecine.FirstAid.Have2Do || this.botOwner_0.Medecine.SurgicalKit.HaveWork);
        }

        public void OrdersChanged()
        {
            ordersChanged = true;
            Task.Delay(1000).ContinueWith(t =>
            {
                ordersChanged = false;
            });
        }

        private AICoreActionResultStruct<BotLogicDecision> EngageEnemy()
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            // Check if the bot needs to heal
            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                // If the bot is not in cover, find the closest cover and move to it
                if (!botOwner_0.Memory.IsInCover)
                {
                    GetClosestCoverPoint(botPosition, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                    }
                }
                // If the bot is in cover, heal
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, botOwner_0.Memory.IsInCover ? "healInCover" : "healNoCover");
            }

            // If the enemy is visible and can be shot, shoot from the current position
            if (botOwner_0.Memory.GoalEnemy.IsVisible && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");
            }

            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;

            // If the bot is in cover:
            if (botOwner_0.Memory.IsInCover)
            {
                
                // - enemy is visible, shoot from cover
                if (enemyVisible && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                }

                // - enemy is visible, can't shoot, find new cover
                else if(!botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, searchRadius);
                    if (customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                // - enemy not visible
                } else
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        float dist = 15f;
                        // -- move in fast
                        if (Vector3.Distance(customNavigationPoint_0.Position, botPosition) > dist)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                        }
                        // -- move in slow
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                        }
                    }
                }

                // - fallback
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
            }

            // If the bot is not in cover:
            if (enemyVisible)
            {
                // - enemy visible and clsoe, move in
                if (Vector3.Distance(botPosition, enemyPos) < 30f)
                {
                    GetClosestCoverPoint(enemyPos, nearSearchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }
                }
                // - enemy visible and far, find close cover
                else
                {
                    GetClosestCoverPoint(botPosition, nearSearchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "DodgeToCover");
                    }
                }
                // - fallback
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
            }
            else
            {
                // - enemy not visible, get in close
                GetClosestCoverPoint(enemyPos, searchRadius);
                if (customNavigationPoint_0 != null)
                {
                    // -- slow
                    if (Vector3.Distance(botPosition, enemyPos) < 15f)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }
                    // -- fast
                    else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }
                }
                // - fallback
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "fallbackRunToEnemy");
            }
        }

        private AICoreActionResultStruct<BotLogicDecision> DefendPosition()
        {
            // Check if the bot needs to heal
            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                // If the bot is not in cover, find the closest cover and move to it
                if (!botOwner_0.Memory.IsInCover)
                {
                    GetClosestCoverPoint(GetBoss().Position, nearSearchRadius);
                    
                    if (customNavigationPoint_0 != null)
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

                if(HasBoss() && ShallGoNearBoss())
                {
                    GetClosestCoverPointGroup(GetBoss().Position, searchRadius);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "moveCloserToBoss");
                    }
                }
                // Otherwise, hold position
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "holdPositionInCover");
            }

            // If the bot is not in cover, find the closest cover and move to it
            bool nearBoss = false;
            if(HasBoss())
            {
                GetClosestCoverPointGroup(GetBoss().Position, searchRadius);
                nearBoss = true;
            } 
            else 
            { 
                GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position, nearSearchRadius);
            }

            if (customNavigationPoint_0 != null)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, nearBoss ? "moveCloserToBoss" : "moveToCover");
            }

            // Fallback decision if no cover is found
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
        }


        public AICoreActionResultStruct<BotLogicDecision> DecideTactic()
        {
            
            // Check if the bot has received the regroup command
            if (ordersAreReqroup)
            {
                Vector3 bossPosition = GetBoss().Position;
                GetClosestCoverPoint(bossPosition, searchRadius); // Adjust the radius as needed

                if (customNavigationPoint_0 != null)
                {
                    float dist = 20f;
                    if (Vector3.Distance(customNavigationPoint_0.Position,botOwner_0.GetPlayer.Transform.position) > dist)
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

           
            if (ordersAreHold || holdTactic) return DefendPosition();
            else if (ordersAreAttack || rushTactic) return EngageEnemy();

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;

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
                GetClosestCoverPoint(bossPosition, searchRadius);

                if (customNavigationPoint_0 != null)
                {
                    float dist = 20f;
                    if (Vector3.Distance(customNavigationPoint_0.Position, botPosition) > dist)
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
            float distanceToEnemySqr = Vector3.Distance(botOwner_0.Memory.GoalEnemy.CurrPosition, botPosition);
            float closeDistanceThresholdSqr = 35f;

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

            BotRequest request = botOwner_0.BotRequestController.CurRequest;
            // accept requests only from teammates and boss
            if (request != null && !(request.Requester.ProfileId == botOwner_0.BotFollower.BossToFollow.Player().ProfileId || botOwner_0.BotsGroup.Contains(request.Requester.AIData.BotOwner)))
            {
                request = null;
            }

            if (request != null && request.BotRequestType == BotRequestType.wait)
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
            // warn request is actually regroup
            if (request != null && request.BotRequestType == BotRequestType.warnPlayer)
            {
                ordersAreReqroup = true;
            }
            else
            {
                ordersAreReqroup = false;
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

            // Check if the enemy is visible and can be shot
            if (botOwner_0.Memory.GoalEnemy.IsVisible && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");
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

            if(request != null && request.BotRequestType == BotRequestType.goToPoint)
            {
                if(botOwner_0.Memory.HaveEnemy)
                {
                    GetClosestCoverPoint(botOwner_0.Memory.GoalEnemy.EnemyLastPosition, nearSearchRadius);
                    if (customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    else
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "getInCloseFast");
                }
            }

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
                    GetClosestCoverPointGroup(GetBoss().Position,nearSearchRadius);
                    if(customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "comeToBoss2");

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "comeToBoss3");
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

            GetCoverPoint(botOwner_0.GetPlayer.Transform.position, nearSearchRadius);

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
            if (botOwner_0.Memory.HaveEnemy && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            return base.EndRunToEnemy();
        }

        public AICoreActionEndStruct EndGetInClose()
        {
            if (botOwner_0.Memory.HaveEnemy && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if(!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            return base.EndRunToCover();
        }

        public override AICoreActionEndStruct EndGoToPoint()
        {
            BotRequest curRequest = this.botOwner_0.BotRequestController.CurRequest;
            if (curRequest != null && curRequest.BotRequestType != BotRequestType.goToPoint)
            {
                return this.gstruct7_0;
            }

            if(botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                if (botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.goToPoint)
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                }

                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            return this.gstruct7_1;
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {

            List<string> ordersIgnoreDecisions = new List<string>
            {
                "moveCloserToBoss",
                "runToHeal",
                "healInCover",
                "DogFight",
                "shootEnemy",
                "shootFromCover"
            };

            // orders changed
            if(ordersChanged && !ordersIgnoreDecisions.Contains(curDecision.Reason))
            {
                return gstruct7_0;
            }

            // boss recall
            if(
                !ordersIgnoreDecisions.Contains(curDecision.Reason) &&
                botOwner_0.BotRequestController.CurRequest != null &&
                botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.warnPlayer &&
                botOwner_0.Memory.HaveEnemy && !botOwner_0.Memory.GoalEnemy.IsVisible
            )
            {
                return gstruct7_0;
            }

            List<string> enemyDecisions= new List<string>{
                "getInCloseSlow",
                "getInCloseFast",
                "shootEnemy",
                "shootFromCover",
                "fallbackRunToEnemy",
                "holdPositionInCover",
                "DodgeToCover",
                "moveCloserToBoss"
            };

            if (enemyDecisions.Contains(curDecision.Reason) && !botOwner_0.Memory.HaveEnemy)
            {
                return gstruct7_0;
            }

            if (curDecision.Reason == "getInCloseFast" || curDecision.Reason == "getInCloseSlow")
            {
                return EndGetInClose();
            }

            return base.ShallEndCurrentDecision(curDecision);
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
                        float range = Vector3.Distance(centerPosition,point.Position);
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

        private void GetCoverPoint(Vector3 centerPosition, float searchRadius)
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

        private void GetClosestCoverPointGroup(Vector3 centerPosition, float searchRadius)
        {
            if(!HasBoss() || GetBoss().Followers.Count < 2)
            {
                GetClosestCoverPoint(centerPosition, searchRadius);
                return;
            }

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            List<CustomNavigationPoint> customNavigationPoints = HasBoss() ? GetBoss().GetAreaCovers() : BossPlayers.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;

                float maxInnerRadius = searchRadius;

                Vector3 botPosition = botOwner_0.Transform.position;

                NavMeshPath navMeshPath = new NavMeshPath();

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (IsPointFreeGroup(point) && !point.IsSpotted)
                    {
                        float range = Vector3.Distance(centerPosition,point.Position);
                        if (range < maxInnerRadius)
                        {
                            navMeshPath.ClearCorners();
                            bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, navMeshPath);
                            if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
                            {
                                
                                float dist = navMeshPath.CalculatePathLength();
                                if(dist > maxInnerRadius)
                                {
                                    continue;
                                }
                            }
                            maxInnerRadius = range;
                            point1 = point;


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

        private bool IsPointFreeGroup(CustomNavigationPoint point)
        {
            if (!HasBoss()) return point.IsFreeById(botOwner_0.Id);
            
            bool isfree = true;

            foreach (var follower in GetBoss().Followers)
            {
                if(follower.Id != botOwner_0.Id && !point.IsFreeById(follower.Id))
                {
                    isfree = false;
                    break;
                } 
            }
            return isfree;

        }
    }
}
