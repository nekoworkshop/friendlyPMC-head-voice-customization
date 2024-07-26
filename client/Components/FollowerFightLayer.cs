
using EFT;
using friendlyPMC.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Components
{
    // GClass47 is followerBoar Fight layer
    internal class FollowerFightLayer : GClass47
    {

        public float bossInnerRadius
        {
            get
            {
                return friendlyPMC.fightInnerRadius;
            }
        }

        public float bossOuterRadius
        {
            get
            {
                return friendlyPMC.fightOuterRadius;
            }
        }

        private readonly float coverSearchRadius = 80f;
        private readonly float sprintDistance = 15f;

        private readonly float searchRadius = 50f;
        private readonly float nearSearchRadius = 30f;

        private float regroupMinDistance = friendlyPMC.regroupMinDistance;

        private float coverTimer = 0f;
        private float holdTimer = 0f;
        private float suppressTime = 0f;

        private float dangerTimer = 0f;
        private float dangerIgnoreEquipTimer = 0f;
        private bool dangerResult = false;
        private bool dangerIgnoreEquipResult = false;

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

        protected CustomNavigationPoint customNavigationPoint_1;

        protected NavMeshPath _navMeshPath;

        public NavMeshPath NavMeshPath
        {
            get { return _navMeshPath; }
        }

        private float _lastHitTime = 0f;
        public float LastTimeHit
        {
            get
            {
                return _lastHitTime;
            }
        }

        private bool _isTakingHeavyDamage = false;
        public bool TakingHeavyDamage
        {
            get => _isTakingHeavyDamage;
        }

        private GClass552.Class260 _damageTimer;


        private AICoreActionResultStruct<BotLogicDecision>? previousDecision = null;

        public readonly List<string> ordersIgnoreReasons = new List<string>
        {
            "healInCover",
            "heal",
            "usingStims",
            "runToHeal",
            "IsDamaged",
            "DogFight"
        };

        public readonly List<string> closeInDecisions = new List<string>
        {
            "getInCloseFast",
            "getInCloseSlow",
            "approachEnemy",
            "repositionFast",
            "reposition",
            "enemy.Search"
        };

        public readonly List<BotLogicDecision> ordersIgnoreDecisions = new List<BotLogicDecision>
        {
            BotLogicDecision.dogFight,
            BotLogicDecision.suppressFire,
            BotLogicDecision.shootFromPlace,
            BotLogicDecision.shootFromCover,
            BotLogicDecision.healStimulators,
            BotLogicDecision.heal
        };

        public FollowerFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            _navMeshPath = new NavMeshPath();
        }
        public override void OnActivate()
        {
            botOwner_0.GetPlayer.BeingHitAction += BeingHitAction;
            base.OnActivate();
        }

        public override void Dispose()
        {
            botOwner_0.GetPlayer.BeingHitAction -= BeingHitAction;
            _damageTimer = null;
            base.Dispose();
        }

        private void BeingHitAction(DamageInfo info, EBodyPart part, float arg3)
        {
            if (info.Player == null) return;

            _lastHitTime = Time.time;
                        
            if(part == EBodyPart.Stomach || part == EBodyPart.Chest || part == EBodyPart.Head)
            {
                if(botOwner_0.Profile.Health.BodyParts.TryGetValue(part, out var bodyPart))
                {
                    float hp = bodyPart.Health.Current * 100 / bodyPart.Health.Maximum;

                    if (
                        (hp <= 65 && part == EBodyPart.Head) ||
                        (hp <= 55 && part == EBodyPart.Chest) ||
                        (hp > 0 && hp <= 35 && part == EBodyPart.Stomach)
                    )
                    {
                        _isTakingHeavyDamage = true;
                        if (_damageTimer != null) _damageTimer.Stop();

                        _damageTimer = Utils.Utils.SetTimeout(() =>
                        {
                            _isTakingHeavyDamage = false;
                            _damageTimer = null;

                        }, 100);
                        
                    }

                }
            }
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
            return Utils.Utils.HasBoss(botOwner_0);
        }

        public pitAIBossPlayer GetBoss()
        {
            return Utils.Utils.GetBoss(botOwner_0);
        }

        public bool ShallGoNearBoss()
        {
            if (!HasBoss()) return false;

            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            float bossDist = Vector3.Distance(botOwner_0.Position, GetBoss().Position);

            if(allyTactic && goalEnemy != null && goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime < friendlyPMC.maximumCover) {
                return false;
            }

            return bossDist > Mathf.Min(friendlyPMC.maximumCoverDistance, nearSearchRadius) && (goalEnemy == null || !goalEnemy.HaveSeen || (goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime > friendlyPMC.maximumCover));
        }

        private bool TimeToHeal()
        {
            return !botOwner_0.Memory.HaveEnemy || (Time.time - this.botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime >= 15f && (this.botOwner_0.Medecine.FirstAid.Have2Do || this.botOwner_0.Medecine.SurgicalKit.HaveWork));
        }

        public void OrdersChanged()
        {
            ordersChanged = true;

            Utils.Utils.SetTimeout(() =>
            {
                ordersChanged = false;
            },1000);
        }

        public bool IsEnemyLowThreat(bool ignoreEquip = false)
        {
            if (!ignoreEquip && dangerTimer > Time.time) return dangerResult;
            else if (ignoreEquip && dangerIgnoreEquipTimer > Time.time ) return dangerIgnoreEquipResult;

            if(!ignoreEquip)
            {
                dangerTimer = Time.time + 1f;
                dangerResult = botOwner_0.Memory.AttackImmediately && Utils.EnemyInfo.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId,botOwner_0.Memory.GoalEnemy.CurrPosition) < 2;

                return dangerResult;
            } 
            else
            {
                dangerIgnoreEquipTimer = Time.time + 1f;
                dangerIgnoreEquipResult = Utils.EnemyInfo.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, botOwner_0.Memory.GoalEnemy.CurrPosition) < 3;

                return dangerIgnoreEquipResult;
            }
        }

        public AICoreActionResultStruct<BotLogicDecision> EngageEnemy(bool pushOrdered = false, bool isaiboss = false)
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            float lastEnemySeenTime = botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime;
            bool inCover = botOwner_0.Memory.IsInCover;

            Utils.EnemyInfo.EnemyDistance distanceToEnemy = Utils.EnemyInfo.Distance(botOwner_0);
            float enemiesAtLocation = 0;
            if (botOwner_0.Memory.GoalEnemy.ProfileId != null) 
                Utils.EnemyInfo.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, enemyPos);

            // PUSH CASE
            if (botOwner_0.Memory.AttackImmediately || pushOrdered) 
            {
                if (
                    // - go for it if enemy is already close
                    distanceToEnemy == Utils.EnemyInfo.EnemyDistance.Close ||
                    // - go for it if enemy is just 1
                    (enemiesAtLocation < 2) ||
                    // - go for it if there is strength in numbers
                    (pushOrdered && enemiesAtLocation < 4)
                )
                {
                    // -- push if not visible
                    if (!enemyVisible)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "rushEnemy");
                    else
                    {
                        // -- cover push if visible
                        GetClosestCoverPoint(enemyPos, searchRadius);
                        
                        if (customNavigationPoint_0 != null)
                        {
                            if (distanceToEnemy >= Utils.EnemyInfo.EnemyDistance.Mid && previousDecision?.Reason != "getInCloseFast")
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                            }
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                        }
                        // -- no cover, go for it
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "rushEnemy");
                    }
                }

                // - enemy visible and push conditions not met
                if(enemyVisible)
                {   
                    if (distanceToEnemy <= Utils.EnemyInfo.EnemyDistance.Mid)
                    {
                        // -- in cover
                        if (inCover)
                        {
                            // --- try to shot enemy
                            if(enemiesAtLocation < 4 || ( botOwner_0.Memory.CurCustomCoverPoint !=null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)) 
                            {
                                if (botOwner_0.Memory.CurCustomCoverPoint != null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                                // --- else find better spot
                                else {
                                    GetClosestAttackCoverPoint(botPosition,10f);
                                    if (customNavigationPoint_0 != null)
                                    {
                                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "relocate");
                                    }
                                }
                            }
                            // --- too many enemies, find better spot or hold out
                            else
                            {
                                if (holdTimer < Time.time)
                                {
                                    float timer = GClass761.Random(2f, 5f);
                                    holdTimer = Time.time + timer + GClass761.Random(2f, 3f);
                                    return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                                } 
                                else
                                {
                                    GetApproachablePoint(); 
                                    
                                    if(customNavigationPoint_0 == null) GetClosestAttackCoverPoint(botPosition,10f);
                                    
                                    if(customNavigationPoint_0 == null) GetClosestCoverPointBetween(botPosition,enemyPos,20f);

                                    if (customNavigationPoint_0 != null)
                                    {
                                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "relocate");
                                    }
                                } 
                            }
    

                        }
                        // -- not in cover
                        else
                        {
                            GetClosestCoverPointBetween(botPosition, enemyPos, 20f);
                            GetClosestCoverPoint(botPosition,coverSearchRadius);

                            if (customNavigationPoint_0 != null)
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "findCover");
                            }
                        }
                    }
                    // -- enemy is distant but visible
                    else
                    {
                        GetClosestAttackCoverPoint(enemyPos,10f);
                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "approachEnemyFast");
                        }
                        else
                        {
                            // No cover point found, hold position temporarily
                            if (holdTimer < Time.time)
                            {
                                float timer = GClass761.Random(2f, 5f);
                                holdTimer = Time.time + timer + GClass761.Random(2f, 3f);
                                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                            }
                        }
                    }
                }
                // - enemy not visible and push conditions not met
                else
                {

                    GetClosestCoverPointBetween(enemyPos, botPosition, 7f);
                    if (customNavigationPoint_0 != null)
                    {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "approachEnemyFast");
                    }

                    return EnemySearch();
                }
            // play the intimidation game 
            } else {
                // - enemy is visible
                if (enemyVisible)
                {
                    // -- if the bot is in cover
                    if (inCover)
                    {
                        // --- find an approachable point towards the enemy
                        GetApproachablePoint();
                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "approachEnemy");
                        }
                        else
                        {
                        // --- no approachable point found, hold position temporarily
                            if (holdTimer < Time.time)
                            {
                                float timer = GClass761.Random(2f, 5f);
                                holdTimer = Time.time + timer + GClass761.Random(2f, 3f);
                                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                            }
                        }
                    }
                    // -- if the bot is not in cover
                    else
                    {
                        // --- find a cover point closer to the enemy
                        GetClosestAttackCoverPoint(enemyPos, 5f, 120f);

                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                        }
                        else
                        {
                            // --- no cover point found, hold position temporarily
                            if (holdTimer < Time.time)
                            {
                                float timer = GClass761.Random(2f, 5f);
                                holdTimer = Time.time + timer + GClass761.Random(2f, 3f);
                                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "wait4it");
                            } else
                                return EnemySearch();
                        }
                    }
                }
                // - if the enemy is not visible
                else if(Time.time - lastEnemySeenTime < GClass761.Random(2f, 5f))
                {
                    // -- find a cover point closer to the enemy's last known position
                    GetApproachablePoint();
                    if (customNavigationPoint_0 != null && previousDecision?.Reason != "getInCloseFast" && previousDecision?.Reason != "waitAbit")
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }
                    else
                    {
                        // -- no cover point found, approach the enemy
                        return EnemySearch();
                    }
                }
            }

            // --  fallback If no conditions are met
            if (holdTimer < Time.time)
            {
                float timer = GClass761.Random(2f, 5f);
                holdTimer = Time.time + timer + GClass761.Random(2f, 3f);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), "waitAbit");
            }
            else
            {
                if (enemyVisible)
                {
                    //botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dgf");
                }
                else
                {
                    if(Utils.EnemyInfo.Distance(botOwner_0) > Utils.EnemyInfo.EnemyDistance.Mid && previousDecision?.Reason != "getInCloseFast" && previousDecision?.Reason != "waitAbit")
                    {
                        GetClosestCoverPointBetween(botPosition, enemyPos, 10f);
                        if(customNavigationPoint_0 != null)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "getInCloseFast");
                    }

                    return EnemySearch();
                }

            }
        }

        public AICoreActionResultStruct<BotLogicDecision> DefendPosition(Vector3 interestPosition)
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;


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
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
                }
                // Otherwise, hold position
                return HoldPositionFor(GClass761.Random(2f,3f), "holdPositionInCover");
            }

            // If the bot is not in cover, find the closest cover and move to it
            if(HasBoss())
            {
                GetClosestCoverPointGroup(interestPosition, coverSearchRadius);
                if (customNavigationPoint_0 != null)
                {
                    if(GetNavDistance(customNavigationPoint_0.Position) < sprintDistance)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToBoss");
                    else
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "regroupToBossFast");
                }

                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
            } 
            else 
            { 
                GetClosestCoverPoint(botPosition, coverSearchRadius);
            }

            if (customNavigationPoint_0 != null)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "moveToCover");
            }

            // Fallback decision if no cover is found
            //if (!botOwner_0.Memory.GoalEnemy.IsVisible) botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
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
            if (enemyVisible && botOwner_0.Memory.IsInCover)
            {
                if(canShoot)
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "sfc");

                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass761.Random(2f, 5f)), "wait4it");
            }

            // If the enemy is a sniper and visible, try to find a cover point from which you can shoot
            if (enemyVisible)
            {
                GetClosestAttackCoverPoint(botPosition,5,200); // Find cover close to the bot's position

                if (customNavigationPoint_0 != null)
                {
                    if (GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "relocateFast");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "reposition");
                }
            }

            // If the sniper is not visible, try to find a cover point closer to the bot's position
            if (!enemyVisible)
            {
                GetClosestCoverPoint(botPosition, coverSearchRadius);

                if (customNavigationPoint_0 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "relocateFast");
                }
            }

            return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
        }
        
        public AICoreActionResultStruct<BotLogicDecision> EnemySearch(string reason = null)
        {
            return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.EnemySearch, reason != null ? reason : "enemy.Search");
        }

        public AICoreActionResultStruct<BotLogicDecision> HoldPositionFor(float timer, string reason = "wait4it")
        {
            Utils.Utils.SetTimeout(() =>
            {
                if (botOwner_0.BotState == EBotState.Active && !botOwner_0.IsDead && botOwner_0.Memory.HaveEnemy && !botOwner_0.Memory.GoalEnemy.IsVisible) 
                    botOwner_0.Steering.LookToDirection(botOwner_0.Memory.GoalEnemy.CurrPosition - botOwner_0.GetPlayer.Transform.position,90f);
            },100);
            return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), reason);
        }

        public AICoreActionResultStruct<BotLogicDecision> GetCloserToBoss()
        {
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position;
            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            GetClosestCoverPointGroup(bossPosition, bossInnerRadius);
            if (customNavigationPoint_0 != null)
            {
                if(request != null) request.Complete();

                if (GetNavDistance(customNavigationPoint_0.Position) < sprintDistance)
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "moveCloserToBoss");
                else
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "moveCloserToBossFast");

            }
            return BotLogicDecisions.RegroupToBoss(botOwner_0);
        }
        public AICoreActionResultStruct<BotLogicDecision>?  DogFight()
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;

            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = InFightLogic();

            if (aicoreActionResultStruct != null)
            {
                return aicoreActionResultStruct.Value;
            }

            if (botOwner_0.DogFight.DogFightState == BotDogFightStatus.dogFight)
            {
                //if(!botOwner_0.Memory.GoalEnemy.IsVisible) botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "cdg");
            }

            // Check if the enemy is visible and can be shot
            if (botOwner_0.Memory.GoalEnemy.IsVisible && botOwner_0.Memory.GoalEnemy.CanShoot)
            {

                Utils.EnemyInfo.EnemyDistance enemyDistance = Utils.EnemyInfo.Distance(botOwner_0);

                float common = 100f * botOwner_0.HealthController.GetBodyPartHealth(EBodyPart.Common, false).Normalized;
                float headnum = 100f * botOwner_0.HealthController.GetBodyPartHealth(EBodyPart.Head, false).Normalized;
                float chestnum = 100f * botOwner_0.HealthController.GetBodyPartHealth(EBodyPart.Chest, false).Normalized;

                float health;

                if (common <= headnum && common <= chestnum)
                {
                    health = common;
                }
                else if (headnum <= common && headnum <= chestnum)
                {
                    health = headnum;
                }
                else
                {
                    health = chestnum;
                }

                //  - retreat as we are getting damaged
                if ( Time.time - LastTimeHit < 2f && ((health < 70f && enemyDistance < Utils.EnemyInfo.EnemyDistance.Mid) || health < 60f))
                {
                    // -- find cover point behind
                    GetClosestCoverPoint(botPosition - (botOwner_0.LookDirection * coverSearchRadius), coverSearchRadius);
                    // -- found nothing, fallback to any cover
                    if(customNavigationPoint_0 == null)
                    {
                        GetClosestCoverPoint(botPosition, coverSearchRadius);
                    }

                    if (customNavigationPoint_0 != null)
                    {
                        // -- critical damage and enemy has enough distance, run for cover
                        if(health < 50f && Utils.EnemyInfo.Distance(botOwner_0) > Utils.EnemyInfo.EnemyDistance.VeryClose)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "damageCritical");
                        }
                        // -- else move while shooting
                        botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "backOff");
                    }
                }
                // - nowhere to retreat, keep shooting
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "cdg");
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
                float lastSeen = Time.time - botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime;
                if (!botOwner_0.Memory.GoalEnemy.IsVisible && lastSeen > 3f)
                {
                    // - close to the enemy, but safe enough to apply meds
                    if(botOwner_0.Memory.IsInCover &&  Utils.EnemyInfo.DistanceProxy(botOwner_0,botPosition) > Utils.EnemyInfo.ProxyDistance.VeryClose)
                    {
                        heal_time = Time.time;
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "healInCover");
                    // find some new cover, 
                    } else
                    {
                        // - look for a safe cover
                        GetClosestSafeCoverPoint(botPosition);
                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                        }
                        // - nothing found, just heal and pray
                        else
                        {
                            heal_time = Time.time;
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal");
                        }
                    }
                } else if (lastSeen <=3f)
                {
                    // not seeing the enemy and we are far enough
                    if(Utils.EnemyInfo.DistanceProxy(botOwner_0, botPosition) > Utils.EnemyInfo.ProxyDistance.Close)
                    {
                        // - heal if already in cover
                        if(botOwner_0.Memory.IsInCover)
                        {
                            heal_time = Time.time;
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "healInCover");
                        }
                        // - else look for cover with enough distance
                        GetClosestSafeCoverPoint(botPosition);
                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                        }
                        // - nothing found, just heal and pray
                        else
                        {
                            heal_time = Time.time;
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal");
                        }
                    }
                    // not seeing the enemy but we are close
                    else
                    {
                        // - look for a spot to heal
                        float safeDist = 10f;
                        GetClosestSafeCoverPoint(botPosition, safeDist);

                        if (customNavigationPoint_0 != null)
                        {
                            if (GetNavDistance(customNavigationPoint_0.Position) > sprintDistance)
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                            }
                            else
                            {
                                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "moveToHeal");
                            }
                        }
                        // - nothing found, just heal and pray
                        else
                        {
                            heal_time = Time.time;
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal");
                        }
                    }
                }
                // we need to heal, but are seeing the enemy
                else
                {
                    // - look for a spot to heal
                    float safeDist = 10f;
                    if (customNavigationPoint_0 == null)
                        GetClosestCoverPoint(botPosition, coverSearchRadius, safeDist);

                    if (customNavigationPoint_0 != null)
                    {
                        if (GetNavDistance(customNavigationPoint_0.Position) > sprintDistance && Utils.EnemyInfo.DistanceProxy(botOwner_0, botPosition) > Utils.EnemyInfo.ProxyDistance.VeryClose)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToHeal");
                        }
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "moveToHeal");
                        }
                    }
                    // - nothing found, do not heal
                }
                
            }

            return null;
        }

        public AICoreActionResultStruct<BotLogicDecision> DecideTactic()
        {

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
                GetClosestCoverPoint(bossPosition, bossOuterRadius);

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
                    return BotLogicDecisions.RegroupToBoss(botOwner_0);
                }
            }

            Vector3 interestPosition = HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position;

            if (allyTactic) return DefendPosition(interestPosition);
            else if ((ordersAreHold || holdTactic) && !ordersAreAttack) return DefendPosition(interestPosition);
            else if (ordersAreAttack || rushTactic) return EngageEnemy(ordersAreAttack);


            // do not go after distant enemies
            if (Utils.EnemyInfo.Distance(botOwner_0) >= Utils.EnemyInfo.EnemyDistance.Distant)
            {
                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
            }

            if (botOwner_0.Memory.AttackImmediately && Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.Mid)
            {
                return EngageEnemy();
            }
            else
            {
                return DefendPosition(interestPosition);
            }
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

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
                return GetCloserToBoss();
            }

            
            if (!botOwner_0.Memory.HaveEnemy && !allyTactic)
            {
                if(bossUnderAttack)
                {
                    var closestEnemy = GetBoss().ClosestEnemy();
                    if (closestEnemy != null)
                    {
                        GetBoss().PrioritizeEnemy(botOwner_0, closestEnemy);
                    }

                    return BotLogicDecisions.RegroupToBoss(botOwner_0);
                }

                if(!HasBoss())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.simplePatrol, "simplePatrol");
                } else
                {
                    GetClosestCoverPointGroup(GetBoss().Position,bossInnerRadius);
                    if(customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToBoss");

                    return BotLogicDecisions.RegroupToBoss(botOwner_0);
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

            // ally tactic will make the bot always fight in hold mode
            if (allyTactic)
            {
                ordersAreAttack = false;
                ordersAreHold = false;
                return DefendPosition(bossPosition);
            }

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
            try
            {
                if (ordersChanged)
                {
                    return new AICoreActionEndStruct("EndHol", true);
                }


                AIBossPlayerLogic gclass363_0 = HasBoss() ? GetBoss().GetBossLogic() : null;

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
                    GClass362 gclass = gclass363_0;
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


                //GetCoverPoint(botOwner_0.GetPlayer.Transform.position, nearSearchRadius);

                return aICoreActionEndStruct_1;
            } catch (Exception e)
            {
                Components.Logger.LogInfo("HoldPosition Error:" + e.StackTrace);
                return  new AICoreActionEndStruct("hpError", true);
            }
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

                    return aICoreActionEndStruct;
                }
                return aICoreActionEndStruct_1;
            }
            return aICoreActionEndStruct;
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

        public override AICoreActionEndStruct EndGoToPoint()
        {
            if (ordersChanged || !botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("EndGoTo", true);
            }

            if(botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if(botOwner_0.GoToSomePointData.IsCome())
            {
                return new AICoreActionEndStruct("point.Reached", true);
            }

            return base.EndGoToPoint();
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

            return aICoreActionEndStruct_1;
        }

        public override AICoreActionEndStruct EndTakeItem()
        {
            return new AICoreActionEndStruct("enemy.Present", true);
        }
        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            if (!botOwner_0.Memory.HaveEnemy)
                return new AICoreActionEndStruct("enemy.None", true);
            
            else if (!botOwner_0.Memory.GoalEnemy.IsVisible)
                return base.EndFollowerPatrolItem();
            else 
                return new AICoreActionEndStruct("enemy.Present", true);
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

        public AICoreActionEndStruct EndSniperSearch()
        {
            if (ordersChanged)
                return new AICoreActionEndStruct("search.End", true);

            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            if (botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if (Time.time - LastTimeHit <= 0.5f)
            {
                return new AICoreActionEndStruct("enemy.ShotMe", true);
            }

            if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.VeryClose)
            {
                return new AICoreActionEndStruct("enemy.Close", true);
            }

            return aICoreActionEndStruct;
        }

        public AICoreActionEndStruct EndCoverToCover()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            if (botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            return aICoreActionEndStruct;
        }

        public AICoreActionEndStruct? ShallEndCurrentDecisionCommon(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (
                curDecision.Action == (BotLogicDecision)CustomBotDecisions.EnemySearch ||
                curDecision.Action == (BotLogicDecision)CustomBotDecisions.SniperSearch
            )
            {
                return EndSniperSearch();
            }

            if (curDecision.Action == (BotLogicDecision)CustomBotDecisions.CoverToCover)
            {
                return EndCoverToCover();
            }

            if (closeInDecisions.Contains(curDecision.Reason))
            {
                return EndGetInClose();
            }

            return null;
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return aICoreActionEndStruct;
            }
            
            // orders changed
            if (
                ordersChanged &&
                (
                    
                    ordersAreAttack ||
                    ordersAreHold ||
                    (
                        !ordersIgnoreReasons.Contains(curDecision.Reason) &&
                        !ordersIgnoreDecisions.Contains(curDecision.Action) &&
                        (
                            !botOwner_0.Memory.HaveEnemy ||
                            !botOwner_0.Memory.GoalEnemy.IsVisible
                        )
                    )
                )
            )
            {
                return new AICoreActionEndStruct("orders.Received", true);
            }

            AICoreActionEndStruct? shallEndCommon = ShallEndCurrentDecisionCommon(curDecision);

            if (shallEndCommon.HasValue) return shallEndCommon.Value;

            return base.ShallEndCurrentDecision(curDecision);
        }


        public AICoreActionEndStruct? ShallEndCurrentDecisionAllies(AICoreActionResultStruct<BotLogicDecision> curDecision, bool ordersChanged)
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return aICoreActionEndStruct;
            }

            List<BotLogicDecision> breakOffContactDecision = new List<BotLogicDecision>
            {
                BotLogicDecision.holdPosition,
                BotLogicDecision.lay,
                BotLogicDecision.shootFromPlace,
                BotLogicDecision.shootFromCover
            };

            if(breakOffContactDecision.Contains(curDecision.Action) && _isTakingHeavyDamage && botOwner_0.Memory.HaveEnemy && Vector3.Distance(botOwner_0.GetPlayer.Transform.position,botOwner_0.Memory.GoalEnemy.CurrPosition) > 25f)
            {
                return new AICoreActionEndStruct("contact.Break", true);
            }

            // orders changed
            if (ordersChanged &&
                !ordersIgnoreReasons.Contains(curDecision.Reason) &&
                !ordersIgnoreDecisions.Contains(curDecision.Action) &&
                (
                    !botOwner_0.Memory.HaveEnemy ||
                    !botOwner_0.Memory.GoalEnemy.IsVisible
                )
            )
            {
                return new AICoreActionEndStruct("orders.Received", true);
            }

            AICoreActionEndStruct? shallEndCommon = ShallEndCurrentDecisionCommon(curDecision);

            if (shallEndCommon.HasValue) return shallEndCommon.Value;

            return null;
        }

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            previousDecision = prevDecision;

            base.DecisionChanged(prevDecision, nextDecision);
        }
        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            if (this.coverTimer > Time.time) return customNavigationPoint_0;

            this.coverTimer = 1f + Time.time;

            customNavigationPoint_0 = Covers.FindPoint(botOwner_0, customNavigationPoint_0, 100f);

            return customNavigationPoint_0;
        }
        /** Find the closest cover point to the given position, within the given radius and ensuring it is at minimum safeDistance from danger **/
        public CustomNavigationPoint GetClosestCoverPoint(Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool> extraChecks = null)
        {
            if (this.coverTimer > Time.time) return customNavigationPoint_0;

            this.coverTimer = 1f + Time.time;

            CustomNavigationPoint point = Covers.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius, safeDistance, extraChecks);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);

            return customNavigationPoint_0;
        }
        /** Find the closest safe cover point to the given position, within the given radius **/
        public CustomNavigationPoint GetClosestSafeCoverPoint(Vector3 centerPosition, float safeDistance = 10f)
        {
            Vector3 dangerPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            
            NavMeshPath navMeshPath = new NavMeshPath();

            CustomNavigationPoint point = Covers.ClosestPoint(botOwner_0, centerPosition, (CustomNavigationPoint pt) => {
                bool good = true;
                // should not be seen by any enemy
                foreach (var enemy in botOwner_0.EnemiesController.EnemyInfos)
                {
                    if (enemy.Value.Person.HealthController.IsAlive && !Covers.CheckCoverVisibility(pt.Position, enemy.Value.Person.Transform.position))
                    {
                        good = false;
                    }
                }

                if (good)
                {
                    navMeshPath.ClearCorners();
                    bool result = NavMesh.CalculatePath(centerPosition, pt.Position, -1, navMeshPath);
                    if (result && navMeshPath.status == NavMeshPathStatus.PathComplete)
                    {
                        float dist = navMeshPath.CalculatePathLength();

                        if(dist > Vector3.Distance(botPosition,pt.Position) + 20f) good = false;
                    }
                    else good = false;
                }

                return good;

            },safeDistance);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);

            return customNavigationPoint_0;
        }

        /** Find closest cover point to pointA between pointA and pointB ensuring it is at minimum safeDistance from danger **/
        public CustomNavigationPoint GetClosestCoverPointBetween(Vector3 pointA, Vector3 pointB, float safeDistance = 5f)
        {
            if (this.coverTimer > Time.time) return customNavigationPoint_0;

            this.coverTimer = 1f + Time.time;

            CustomNavigationPoint point = Covers.GetClosestCoverPointBetween(botOwner_0,pointA,pointB, safeDistance);

            customNavigationPoint_0 = point;
            botOwner_0.Memory.SetCoverPoints(point);

            return customNavigationPoint_0;
        }
        /** Find a random cover point at the given position, within the given radius **/
        public CustomNavigationPoint GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (this.coverTimer > Time.time) return customNavigationPoint_0;

            this.coverTimer = 1f + Time.time;

            CustomNavigationPoint point1 = Covers.GetCoverPoint(botOwner_0, centerPosition, searchRadius);


            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);
            return customNavigationPoint_0;

        }
        /** Find a shoot position between bot and enemy, that is the closest to the middle point between bot and enemy **/
        public virtual CustomNavigationPoint GetApproachablePoint()
        {
            if (this.coverTimer > Time.time) return customNavigationPoint_1;

            this.coverTimer = 1f + Time.time;

            customNavigationPoint_1 = Covers.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition);

            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_1);
            return customNavigationPoint_1;
        }
        /** Find a shoot positionm that is closest to the enemy but at a minimum distance and maximum from the enemy **/
        public virtual CustomNavigationPoint GetClosestAttackCoverPoint(Vector3 centerPosition, float minDistance = 5f, float maxDistance = 150f)
        {
            if (this.coverTimer > Time.time) return customNavigationPoint_1;

            this.coverTimer = 1f + Time.time;

            customNavigationPoint_1 = Covers.GetClosestAttackCoverPoint(botOwner_0, centerPosition, minDistance, maxDistance);

            customNavigationPoint_0 = customNavigationPoint_1;
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_1);
            return customNavigationPoint_1;
        }
        /** Find closest cover point at the given position taking into cosideration the rest of the followers **/
        private void GetClosestCoverPointGroup(Vector3 centerPosition, float searchRadius)
        {
            if(!HasBoss() || GetBoss().Followers.Count < 2)
            {
                GetClosestCoverPoint(centerPosition, searchRadius);
                return;
            }

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1.5f + Time.time;

            float maxInnerRadius = searchRadius;

            Vector3 botPosition = botOwner_0.Transform.position;

            customNavigationPoint_0 = Covers.ClosestPoint(botOwner_0, centerPosition, (CustomNavigationPoint point) =>
            {
                if (IsPointFreeGroup(point)) return false;

                float range = Vector3.Distance(centerPosition, point.Position);
                if (range < maxInnerRadius)
                {
                    _navMeshPath.ClearCorners();
                    bool resut = NavMesh.CalculatePath(botPosition, point.Position, -1, _navMeshPath);
                    if (resut && _navMeshPath.status == NavMeshPathStatus.PathComplete)
                    {

                        float dist = _navMeshPath.CalculatePathLength();
                        if (dist > maxInnerRadius)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                return true;

            }, 10f);

            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);

        }
        /** Is point free by the followers group **/
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

        public float GetNavDistance(Vector3 point)
        {
            return Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, point, _navMeshPath);
        }
    }
}
