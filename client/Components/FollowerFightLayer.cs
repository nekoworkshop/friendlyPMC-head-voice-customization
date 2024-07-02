using Aki.Common.Http;
using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Components
{
    // GClass47 is followerBoar Fight layer
    internal class FollowerFightLayer : GClass47
    {
        private float searchRadius = 50f;
        private float nearSearchRadius = 30f;
        private float regroupMinDistance = 7f;

        private readonly float coverSearchRadius = 80f;
        private readonly float sprintDistance = 15f;


        private float coverTimer = 0f;

        private float suppressTime = 0f;

        private bool ordersAreHold = false;
        private bool ordersAreAttack = false;
        private bool ordersAreReqroup = false;

        private bool holdTactic = false;
        private bool rushTactic = false;
        private bool allyTactic = false;

        private bool ordersChanged = false;

        private bool bossUnderAttack = false;

        private string tactic = "balance";

        private float heal_time = 0f;
        public FollowerFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            
        }

        public void SetBossFightTactic(string tactic)
        {
            rushTactic = false;
            holdTactic = false;
            allyTactic = false;
            if (tactic == "ally")
            {
                allyTactic = true;
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Assist");
            }
            else if(tactic == "push")
            {
                rushTactic = true;
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Push");
            }
            else if (tactic == "defend") 
            {
                holdTactic = true;
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Hold");
            } else
            {
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Balance");
            }
        }

        public override string Name()
        {
            if (holdTactic) tactic = "defend";
            else if (rushTactic) tactic = "push";
            else tactic = "balance";
            if (ordersAreAttack) tactic += ":atk";
            else if (ordersAreHold) tactic += ":hld";

            return "FBPFight:" + tactic;
        }
        public override bool ShallUseNow()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                if(ordersAreAttack || ordersAreHold)
                {
                    ordersAreAttack = false;
                    ordersAreHold = false;

                    if (
                        botOwner_0.BotRequestController.CurRequest != null &&
                        (botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.goToPoint ||
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.attackClose)
                    )
                    {
                        botOwner_0.BotRequestController.CurRequest.Complete();
                    }
                }

                return false;
            }

            return true;
        }

        public bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        public pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }

        public bool ShallGoNearBoss()
        {
            if (!HasBoss()) return false;

            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            float bossDist = Vector3.Distance(botOwner_0.Position, GetBoss().Position);

            if(allyTactic && goalEnemy != null && goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime < friendlyPMC.maximumCover.Value) {
                return false;
            }

            return bossDist > Mathf.Min(friendlyPMC.maximumCoverDistance.Value, nearSearchRadius) && (goalEnemy == null || !goalEnemy.HaveSeen || (goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime > friendlyPMC.maximumCover.Value));
        }

        private bool TimeToHeal()
        {
            return Time.time - this.botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime >= 15f && (this.botOwner_0.Medecine.FirstAid.Have2Do || this.botOwner_0.Medecine.SurgicalKit.HaveWork);
        }

        public void OrdersChanged()
        {
            ordersChanged = true;
            var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(1), false);
            Timer.OnTimer += () =>
            {
                ordersChanged = false;
            };
        }

        public AICoreActionResultStruct<BotLogicDecision> EngageEnemy()
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPoition = HasBoss() ? GetBoss().Position : botPosition;
            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            // we put this again here because other layers might use this function
            searchRadius = friendlyPMC.fightOuterRadius.Value;
            nearSearchRadius = friendlyPMC.fightInnerRadius.Value;

            Utils.EnemyInfo.EnemyDistance distanceToEnemy = Utils.EnemyInfo.Distance(botOwner_0);

            // we have the power, go to the enemy
            if(botOwner_0.Memory.AttackImmediately) 
            {
                if(distanceToEnemy <= Utils.EnemyInfo.EnemyDistance.Close)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "rushEnemy");
                }

                if(distanceToEnemy <= Utils.EnemyInfo.EnemyDistance.Mid) {
                    GetClosestAttackCoverPoint(enemyPos,true);
                    
                    if(customNavigationPoint_0 == null) {
                        GetClosestCoverPoint(enemyPos,searchRadius);
                    }

                    if (customNavigationPoint_0 != null)
                    {
                        if(GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                        }

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }

                } else {
                    
                    GetApproachablePoint();
                    if (customNavigationPoint_0 == null)
                    {
                        if (GetNavDistance(enemyPos) < searchRadius)

                            GetClosestCoverPoint(enemyPos, nearSearchRadius);
                        else
                            GetClosestCoverPoint(enemyPos, searchRadius);
                    }

                    if (customNavigationPoint_0 != null)
                    {
                        if(GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                        }

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }
                }


                // - fallback
                if(distanceToEnemy <= Utils.EnemyInfo.EnemyDistance.Mid) {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "rushEnemy");
                } else {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemyZigZag, "rushEnemyZigZag");
                }
            // play the intimidation game 
            } else {
                if(Vector3.Distance(botPosition, enemyPos) >= 25f)
                {
                    GetApproachablePoint();
                    if (customNavigationPoint_0 == null)
                    {
                        if (GetNavDistance(enemyPos) < searchRadius)

                            GetClosestCoverPoint(enemyPos, nearSearchRadius);
                        else
                            GetClosestCoverPoint(enemyPos, searchRadius);
                    }

                    if (customNavigationPoint_0 != null)
                    {
                        if(GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                        }

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }
                } else 
                {
                    if (botOwner_0.Memory.IsInCover)
                    {
                        // - enemy is visible, shoot from cover
                        if (enemyVisible && (botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy || botOwner_0.Memory.GoalEnemy.CanShoot))
                        {   
                            if( botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                            else 
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");

                        } else if(!botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy && !botOwner_0.Memory.GoalEnemy.CanShoot)
                        {
                            GetCoverPoint(botPosition,searchRadius);

                            if (customNavigationPoint_0 != null)
                            {
                                if(GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
                                {
                                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                                }

                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                            }
                        }
                    // - resposition
                    } else {
                        GetClosestCoverPoint(bossPoition, searchRadius);
                        if(customNavigationPoint_0 == null) 
                            GetCoverPoint(bossPoition, searchRadius);

                        if (customNavigationPoint_0 != null)
                        {
                            if(GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "repositionFast");
                            }

                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "reposition");
                        }

                    }
                }
                // - fallback
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
            }
        }

        public AICoreActionResultStruct<BotLogicDecision> DefendPosition()
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            // we put this again here because other layers might use this function
            searchRadius = friendlyPMC.fightOuterRadius.Value;
            nearSearchRadius = friendlyPMC.fightInnerRadius.Value;


            // If the bot is already in cover
            if (botOwner_0.Memory.IsInCover)
            {
                // If the enemy is visible and can be shot, shoot from cover
                if (enemyVisible && (botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy || botOwner_0.Memory.GoalEnemy.CanShoot))
                {   
                    if( botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                    else 
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");

                }
                // Else check if the bot needs to get close to the boss
                if(HasBoss() && ShallGoNearBoss())
                {
                    GetClosestCoverPointGroup(bossPosition, nearSearchRadius);

                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "moveCloserToBoss");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "moveCloserToBossFallback");
                }
                // Otherwise, hold position
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "holdPositionInCover");
            }

            // If the bot is not in cover, find the closest cover and move to it
            if(HasBoss())
            {
                GetClosestCoverPointGroup( allyTactic ? botPosition :  bossPosition, nearSearchRadius);
                if (customNavigationPoint_0 != null)
                {
                    if(GetNavDistance(customNavigationPoint_0.Position) < sprintDistance)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "moveCloserToBoss");
                    else
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "moveCloserToBossFast");
                }

                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "moveCloserToBossFallback");
            } 
            else 
            { 
                GetClosestCoverPoint(botPosition, searchRadius);
            }

            if (customNavigationPoint_0 != null)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "moveToCover");
            }

            // Fallback decision if no cover is found
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "DogFight");
        }

        public AICoreActionResultStruct<BotLogicDecision> MarksManFight()
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            bool canShoot = botOwner_0.Memory.GoalEnemy.CanShoot;
            bool haveSeen = botOwner_0.Memory.GoalEnemy.HaveSeen;
            float lastSeenTime = botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime;

            // If the enemy is a sniper and visible, and the bot is in cover, shoot from cover
            if (enemyVisible && botOwner_0.Memory.IsInCover && canShoot)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootSniper");
            }

            // If the enemy is a sniper and visible, try to find a cover point from which you can shoot
            if (enemyVisible)
            {
                GetClosestAttackCoverPoint(botPosition, false); // Find cover close to the bot's position

                if (customNavigationPoint_0 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }
            }

            // If the sniper was recently seen, try to find a cover point from which you can approach
            if (haveSeen && Time.time - lastSeenTime < 6f)
            {
                GetApproachablePoint();

                if (customNavigationPoint_0 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                }
            }

            // If the sniper is not visible, try to find a cover point closer to the bot's position
            if (!enemyVisible)
            {
                GetClosestCoverPoint(botPosition, nearSearchRadius);

                if (customNavigationPoint_0 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "repositionFast");
                }
            }

            return DefendPosition();
        }
        public AICoreActionResultStruct<BotLogicDecision>?  DogFight()
        {
            // partial re-creation of fight decisions in GClass47
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = InFightLogic();

            if (aicoreActionResultStruct != null)
            {
                return aicoreActionResultStruct.Value;
            }

            if (method_2() || Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.VeryClose)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "cdg");
            }

            // Check if the enemy is visible and can be shot
            if (botOwner_0.Memory.GoalEnemy.IsVisible && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromPlace, "shootEnemy");
            }

            return null;
        }

        public AICoreActionResultStruct<BotLogicDecision>? NeedHeal()
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            // damaged and has healers
            if (botOwner_0.Medecine.Stimulators.Using)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "usingStims");
            }

            // Check if the bot needs to heal
            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                // If the bot is not in cover, find the closest cover and move to it
                if (!botOwner_0.Memory.IsInCover)
                {
                    GetClosestCoverPoint(botPosition, coverSearchRadius);

                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                    }
                    // - just heal
                    else
                    {
                        heal_time = Time.time;
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal");
                    }
                }
                // If the bot is in cover, heal
                else
                {
                    heal_time = Time.time;
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "healInCover");
                }
            }

            return null;
        }


        public AICoreActionResultStruct<BotLogicDecision> DecideTactic()
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;

            // Check if the boss is under attack
            if (bossUnderAttack && (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible))
            {
                // - switch the bot's enemy to the one attacking the boss
                var closestEnemy = GetBoss().ClosestEnemy();
                if (closestEnemy != null)
                {
                    GetBoss().PrioritizeEnemy(botOwner_0, closestEnemy);
                }

                // - try and get back to the boss
                Vector3 bossPosition = GetBoss().Position;
                GetClosestCoverPoint(bossPosition, nearSearchRadius);

                if (customNavigationPoint_0 != null)
                {
                    if (GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
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

            

            if(allyTactic) return DefendPosition();
            else if ((ordersAreHold || holdTactic) && !ordersAreAttack) return DefendPosition();
            else if (ordersAreAttack || rushTactic) return EngageEnemy();

            

            if (botOwner_0.Memory.AttackImmediately && Utils.EnemyInfo.Distance(botOwner_0) < Utils.EnemyInfo.EnemyDistance.Mid)
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
            searchRadius = friendlyPMC.fightOuterRadius.Value;
            nearSearchRadius = friendlyPMC.fightInnerRadius.Value;
            regroupMinDistance = friendlyPMC.regroupMinDistance.Value;

            BotRequest request = botOwner_0.BotRequestController.CurRequest;
            
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botPosition;

            // accept requests only from the boss
            if (request != null && (botOwner_0.BotFollower.BossToFollow == null || request.Requester.ProfileId != botOwner_0.BotFollower.BossToFollow.Player().ProfileId))
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

            if (request != null && request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup)
            {
                ordersAreReqroup = true;
            }
            else
            {
                ordersAreReqroup = false;
            }

            // is in dogfight/
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = DogFight();

            if (aicoreActionResultStruct != null)
            {
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }
            // needs healing?
            aicoreActionResultStruct = NeedHeal();
            if (aicoreActionResultStruct != null)
            {
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }


            AIBossPlayerLogic gclass363_0 = HasBoss() ? GetBoss().GetBossLogic() : null;
            bossUnderAttack = gclass363_0 != null && gclass363_0.IsHitted;

            // Check if the bot has received the regroup command
            if (ordersAreReqroup && GetNavDistance(bossPosition) > regroupMinDistance && (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible))
            {
                GetClosestCoverPoint(bossPosition, nearSearchRadius);

                if (customNavigationPoint_0 != null)
                {

                    if (GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
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

            // suppression fire request
            if((!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible) && request != null && request.BotRequestType == BotRequestType.suppressionFire)
            {
                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Covering, true);
                suppressTime = Time.time + 2f;
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "suppressFire");
            }
            // throw grenate request
            if(request != null && request.BotRequestType == BotRequestType.throwGrenade)
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.throwGrenadeFromPlace, "throwGrenadeRequest");

            if (botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman)) return MarksManFight();

            if (request != null && request.BotRequestType == BotRequestType.goToPoint)
            {

                if (botOwner_0.Memory.HaveEnemy)
                {
                    Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;

                    GetApproachablePoint();
                    if (customNavigationPoint_0 == null)
                    {
                        GetClosestCoverPoint(enemyPos, nearSearchRadius);
                    }

                    if (customNavigationPoint_0 == null)
                    {
                        float diste = GetNavDistance(enemyPos);
                        if (diste < sprintDistance)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToEnemy, "getInCloseSlow");
                        }
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "getInCloseFast");
                        }
                    }

                    float dist = GetNavDistance(customNavigationPoint_0.Position);
                    if (dist < sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }
                    else
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }
                }
            }

            return DecideTactic();
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            if(ordersChanged)
            {
                return new AICoreActionEndStruct("EndHol", true);
            }

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


            if (ShallGoNearBoss()) return new AICoreActionEndStruct("goNearBoss", true);

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
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            if (botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }


            return base.EndRunToEnemy();
        }

        public AICoreActionEndStruct EndGetInClose()
        {

            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            if (botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if (botOwner_0.Mover.IsComeTo(0.5f, false))
            {

                return new AICoreActionEndStruct("point.Reached", true);
            }


            return base.EndRunToCover();
        }

        public override AICoreActionEndStruct EndGoToPoint()
        {
            if (ordersChanged || !botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("EndGoTo", true);
            }

            if(!botOwner_0.Memory.HaveEnemy || (botOwner_0.Memory.GoalEnemy.CanShoot && botOwner_0.Memory.GoalEnemy.IsVisible))
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if (botOwner_0.GoToSomePointData.IsCome() || botOwner_0.Mover.IsComeTo(0.5f, false))
            {

                return new AICoreActionEndStruct("point.Reached", true);
            }

            return gstruct7_1;
        }

        public override AICoreActionEndStruct EndHeal()
        {
            if (!botOwner_0.Medecine.FirstAid.Have2Do && !botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                return new AICoreActionEndStruct("EndHeal", true);
            }
            else if (heal_time + 20f < Time.time)
            {
                botOwner_0.AIData.Player.ActiveHealthController.RestoreFullHealth();
                return new AICoreActionEndStruct("EndHealTimer", true);
            }

            return gstruct7_1;
        }

        public override AICoreActionEndStruct EndTakeItem()
        {
            return new AICoreActionEndStruct("enemy.Present", true);
        }
        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            return new AICoreActionEndStruct("enemy.Present", true);
        }
        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {

            List<string> ordersIgnoreDecisions = new List<string>
            {
                "moveCloserToBossFallback",
                "runToHeal",
                "healInCover",
                "DogFight",
                "shootEnemy",
                "shootFromCover"
            };

            if (!botOwner_0.Memory.HaveEnemy)
            {
                return gstruct7_0;
            }

            // orders changed
            if (ordersChanged && !ordersIgnoreDecisions.Contains(curDecision.Reason))
            {
                return gstruct7_0;
            }

            // boss recall
            if(
                !ordersIgnoreDecisions.Contains(curDecision.Reason) &&
                botOwner_0.BotRequestController.CurRequest != null &&
                botOwner_0.BotRequestController.CurRequest.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup &&
                botOwner_0.Memory.HaveEnemy && !botOwner_0.Memory.GoalEnemy.IsVisible
            )
            {
                return gstruct7_0;
            }

            if (curDecision.Reason == "getInCloseFast" || curDecision.Reason == "getInCloseSlow" || curDecision.Reason == "repositionFast")
            {
                return EndGetInClose();
            }

            return base.ShallEndCurrentDecision(curDecision);
        }


        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Utils.Utils.FindPoint(botOwner_0, customNavigationPoint_0, 100f);


            return customNavigationPoint_0;
        }

        public CustomNavigationPoint GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return customNavigationPoint_0;

            this.coverTimer = 1f + Time.time;

            CustomNavigationPoint point = Utils.Utils.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);

            return customNavigationPoint_0;
        }

        public CustomNavigationPoint GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            CustomNavigationPoint point1 = Utils.Utils.GetCoverPoint(botOwner_0, centerPosition, searchRadius, true);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);
            return customNavigationPoint_0;

        }

        public virtual CustomNavigationPoint GetApproachablePoint()
        {
            customNavigationPoint_0 = Utils.Utils.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition);
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);
            return customNavigationPoint_0;
        }

        public virtual CustomNavigationPoint GetClosestAttackCoverPoint(Vector3 centerPosition, bool useFullCover = false, float minDistance = 5f)
        {
            CustomNavigationPoint cover = Utils.Utils.GetClosestAttackCoverPoint(botOwner_0, centerPosition, useFullCover, minDistance);
            customNavigationPoint_0 = cover;
            botOwner_0.Memory.SetCoverPoints(cover);
            return cover;
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

            List<CustomNavigationPoint> customNavigationPoints = HasBoss() ? GetBoss().GetAreaCovers() : BossPlayers.GetAICovers();

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

        private float GetNavDistance(Vector3 point)
        {
            return Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, point);
        }
    }
}
