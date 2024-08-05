using EFT;
using friendlyPMC.Utils;
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Components.Tactics
{
    /** This class is not meant to be used directly as a brain layer, but within one **/
    internal class FollowerCommonLayer : BaseLogicLayerSimpleAbstractClass
    {

        private CustomNavigationPoint customNavigationPoint_0;
        private CustomNavigationPoint customNavigationPoint_1;
        private CustomNavigationPoint customNavigationPoint_2;
        private CustomNavigationPoint customNavigationPoint_3;

        public CustomNavigationPoint NavigationPoint2
        {
            get
            {
                return customNavigationPoint_2;
            }
        }

        public CustomNavigationPoint NavigationPoint1
        {
            get
            {
                return customNavigationPoint_1;
            }
        }
        public CustomNavigationPoint NavigationPoint3
        {
            get
            {
                return customNavigationPoint_3;
            }
        }
        public CustomNavigationPoint NavigationPoint
        {
            get
            {
                return customNavigationPoint_0;
            }
        }

        private float coverTimer_0 = 0f;
        private float coverTimer_1 = 0f;
        private float coverTimer_2 = 0f;

        private float heal_time = 0f;

        private float dangerTimer = 0f;
        private float dangerIgnoreEquipTimer = 0f;
        private bool dangerResult = false;
        private bool dangerIgnoreEquipResult = false;

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

        private bool ordersChanged = false;

        public bool OrderHasChangedRecently
        {
            get
            {
                return ordersChanged;
            }
        }

        private NavMeshPath _navMeshPath;

        public NavMeshPath NavMeshPath
        {
            get { return _navMeshPath; }
        }

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

        public float coverSearchRadius
        {
            get
            {
                return Props.coverSearchRadius;
            }
        }
        public float sprintDistance
        {
            get
            {
                return Props.sprintDistance;
            }
        }
        public float regroupMinDistance
        {
            get => Props.regroupMinDistance;
        }
        public float searchRadius
        {
            get { return Props.searchRadius; }
        }
        public float nearSearchRadius
        {
            get { return Props.nearSearchRadius; }
        }
        

        public float bossInnerRadius
        {
            get
            {
                return Props.bossInnerRadius;
            }
        }

        public float bossOuterRadius
        {
            get
            {
                return Props.bossOuterRadius;
            }
        }

        public float bossMaxCoverDistance
        {
            get
            {
                return Props.bossMaxCoverDistance;
            }
        }
        public float bossMinCoverDistance
        {
            get
            {
                return Props.bossMinCoverDistance;
            }
        }

        private AICoreActionResultStruct<BotLogicDecision>? previousDecision = null;


        public AICoreActionResultStruct<BotLogicDecision>? CurrentDecision
        {
            get
            {
                return previousDecision;
            }
        }

        public FollowerCommonLayer(BotOwner bot, int priority) : base(bot,priority)
        {
            botOwner_0 = bot;
            _navMeshPath = new NavMeshPath();
        }
        public override bool ShallUseNow()
        {
            return true;
        }
        public override string Name()
        {
            return "FBCommon";
        }
        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.standBy, "decision.None");
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

            if (part == EBodyPart.Stomach || part == EBodyPart.Chest || part == EBodyPart.Head)
            {
                if (botOwner_0.Profile.Health.BodyParts.TryGetValue(part, out var bodyPart))
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

        public bool HasBoss()
        {
            return Utils.Utils.HasBoss(botOwner_0);
        }

        public pitAIBossPlayer GetBoss()
        {
            return Utils.Utils.GetBoss(botOwner_0);
        }
        public bool ShallGoNearBoss(bool assist = false)
        {
            if (!HasBoss()) return false;

            EnemyInfo goalEnemy = this.botOwner_0.Memory.GoalEnemy;
            float bossDist = Vector3.Distance(botOwner_0.Position, GetBoss().Position);

            return bossDist > Mathf.Min(bossMaxCoverDistance, nearSearchRadius) && (goalEnemy == null || !goalEnemy.HaveSeen || (goalEnemy.HaveSeen && Time.time - goalEnemy.PersonalLastSeenTime > bossMinCoverDistance));
        }

        public void ResetTimer(string timer)
        {
            if (timer == "coverTimer_0")
                coverTimer_0 = 0f;
            else if (timer == "coverTimer_1")
                coverTimer_1 = 0f;
            else if (timer == "coverTimer_2")
                coverTimer_2 = 0f;
        }

        public void OrdersChanged()
        {
            ordersChanged = true;

            Utils.Utils.SetTimeout(() =>
            {
                ordersChanged = false;
            }, 1000);
        }

        public void OrderReset()
        { 
            ordersChanged = false;
        }

        public bool IsEnemyLowThreat(bool ignoreEquip = false)
        {
            if (!ignoreEquip && dangerTimer > Time.time) return dangerResult;
            else if (ignoreEquip && dangerIgnoreEquipTimer > Time.time) return dangerIgnoreEquipResult;

            if (!ignoreEquip)
            {
                dangerTimer = Time.time + 1f;
                dangerResult = botOwner_0.Memory.AttackImmediately && Utils.EnemyInfo.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, botOwner_0.Memory.GoalEnemy.CurrPosition) < 2;

                return dangerResult;
            }
            else
            {
                dangerIgnoreEquipTimer = Time.time + 1f;
                dangerIgnoreEquipResult = Utils.EnemyInfo.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, botOwner_0.Memory.GoalEnemy.CurrPosition) < 3;

                return dangerIgnoreEquipResult;
            }
        }

        /** Find a shoot positionm that is closest to the enemy but at a minimum distance and maximum from the enemy **/
        // customNavigationPoint_1
        public CustomNavigationPoint GetClosestAttackCoverPoint(Vector3 centerPosition, float minDistance = 5f, float maxDistance = 150f)
        {
            if (coverTimer_1 > Time.time) return customNavigationPoint_1;

            coverTimer_1 = 1f + Time.time;

            customNavigationPoint_1 = Covers.GetClosestAttackCoverPoint(botOwner_0, centerPosition, minDistance, maxDistance);
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_1);
            return customNavigationPoint_1;
        }
        /** Find a shoot position between bot and enemy, that is the closest to the middle point between bot and enemy **/
        // customNavigationPoint_1
        public CustomNavigationPoint GetApproachablePoint()
        {
            if (coverTimer_1 > Time.time) return customNavigationPoint_1;

            coverTimer_1 = 1f + Time.time;

            customNavigationPoint_1 = Covers.GetApproachableCoverPoint(botOwner_0, botOwner_0.Memory.GoalEnemy.CurrPosition);

            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_1);
            return customNavigationPoint_1;
        }
        /** Find the closest cover point to the given position, within the given radius and ensuring it is at minimum safeDistance from danger **/
        // customNavigationPoint_2
        public CustomNavigationPoint GetClosestCoverPoint(Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool> extraChecks = null)
        {
            if (coverTimer_2 > Time.time) return customNavigationPoint_2;

            coverTimer_2 = 1f + Time.time;

            CustomNavigationPoint point = Covers.GetClosestCoverPoint(botOwner_0, centerPosition, searchRadius, safeDistance, extraChecks);

            customNavigationPoint_2 = point;
            
            botOwner_0.Memory.SetCoverPoints(point);

            return customNavigationPoint_2;
        }
        /** Find closest cover point at the given position taking into cosideration the rest of the followers **/
        public CustomNavigationPoint GetClosestCoverPointGroup(Vector3 centerPosition, float searchRadius)
        {
            if (!HasBoss() || GetBoss().Followers.Count < 2)
            {
                return GetClosestCoverPoint(centerPosition, searchRadius);
            }

            if (this.coverTimer_2 > Time.time) return customNavigationPoint_2;

            this.coverTimer_2 = 1.5f + Time.time;

            float maxInnerRadius = searchRadius;

            Vector3 botPosition = botOwner_0.Transform.position;

            NavMeshPath _navMeshPath = new NavMeshPath();

            customNavigationPoint_2 = Covers.ClosestPoint(botOwner_0, centerPosition, (CustomNavigationPoint point) =>
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

            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_2);

            return customNavigationPoint_2;

        }
        /** Find a random cover point at the given position, within the given radius **/
        // customNavigationPoint_0
        public CustomNavigationPoint GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            if (coverTimer_0 > Time.time) return customNavigationPoint_0;

            coverTimer_0 = 1f + Time.time;

            CustomNavigationPoint point1 = Covers.GetCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);
            return customNavigationPoint_0;

        }
        /** Find the closest safe cover point to the given position, within the given radius **/
        // customNavigationPoint_3
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

                        if (dist > Vector3.Distance(botPosition, pt.Position) + 20f) good = false;
                    }
                    else good = false;
                }

                return good;

            }, safeDistance);

            customNavigationPoint_3 = point;
            botOwner_0.Memory.SetCoverPoints(point);
  
            return customNavigationPoint_3;
        }

        /** Find closest cover point to pointA between pointA and pointB ensuring it is at minimum safeDistance from danger **/
        public CustomNavigationPoint GetClosestCoverPointBetween(Vector3 pointA, Vector3 pointB, float safeDistance = 5f)
        {
            CustomNavigationPoint point = Covers.GetClosestCoverPointBetween(botOwner_0, pointA, pointB, safeDistance);

            customNavigationPoint_3 = point;
            botOwner_0.Memory.SetCoverPoints(point);

            return customNavigationPoint_3;
        }
        /** Is point free by the followers group **/
        public bool IsPointFreeGroup(CustomNavigationPoint point)
        {
            if (!HasBoss()) return point.IsFreeById(botOwner_0.Id);

            bool isfree = true;

            foreach (var follower in GetBoss().Followers)
            {
                if (follower.Id != botOwner_0.Id && !point.IsFreeById(follower.Id))
                {
                    isfree = false;
                    break;
                }
            }
            return isfree;

        }

        public bool TimeToHeal()
        {
            return !botOwner_0.Memory.HaveEnemy || (Time.time - this.botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime >= 15f && (this.botOwner_0.Medecine.FirstAid.Have2Do || this.botOwner_0.Medecine.SurgicalKit.HaveWork));
        }

        public float GetNavDistance(Vector3 point)
        {
            return Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, point, _navMeshPath);
        }

        public AICoreActionResultStruct<BotLogicDecision>? DogFight(out CustomNavigationPoint navpoint)
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;

            navpoint = null;

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
                if (Time.time - LastTimeHit < 2f && ((health < 70f && enemyDistance < Utils.EnemyInfo.EnemyDistance.Mid) || health < 60f))
                {
                    // -- find cover point behind
                    GetClosestCoverPoint(botPosition - (botOwner_0.LookDirection * coverSearchRadius), coverSearchRadius);
                    // -- found nothing, fallback to any cover
                    if (customNavigationPoint_2 == null)
                    {
                        ResetTimer("coverTimer_2");
                        GetClosestCoverPoint(botPosition, coverSearchRadius);
                    }
                    
                    navpoint = customNavigationPoint_2;

                    if (customNavigationPoint_2 != null)
                    {
                        // -- critical damage and enemy has enough distance, run for cover
                        if (health < 50f && Utils.EnemyInfo.Distance(botOwner_0) > Utils.EnemyInfo.EnemyDistance.VeryClose)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "damageCritical");
                        }
                        // -- else move while shooting
                        if(!botOwner_0.Memory.GoalEnemy.IsVisible) botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "backOff");
                    }
                }
                // - nowhere to retreat, keep shooting
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "cdg");
            }

            return null;
        }

        public AICoreActionResultStruct<BotLogicDecision>? NeedHeal(out CustomNavigationPoint navpoint)
        {
            navpoint = null;

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
                    if (botOwner_0.Memory.IsInCover && Utils.EnemyInfo.DistanceProxy(botOwner_0, botPosition) > Utils.EnemyInfo.ProxyDistance.VeryClose)
                    {
                        heal_time = Time.time;
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "healInCover");
                        // find some new cover, 
                    }
                    else
                    {
                        // - look for a safe cover
                        GetClosestSafeCoverPoint(botPosition);
                        
                        navpoint = customNavigationPoint_3;

                        if (customNavigationPoint_3 != null)
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
                }
                else if (lastSeen <= 3f)
                {
                    // not seeing the enemy and we are far enough
                    if (Utils.EnemyInfo.DistanceProxy(botOwner_0, botPosition) > Utils.EnemyInfo.ProxyDistance.Close)
                    {
                        // - heal if already in cover
                        if (botOwner_0.Memory.IsInCover)
                        {
                            heal_time = Time.time;
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "healInCover");
                        }
                        // - else look for cover with enough distance
                        GetClosestSafeCoverPoint(botPosition);

                        navpoint = customNavigationPoint_3;

                        if (customNavigationPoint_3 != null)
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

                        navpoint = customNavigationPoint_3;

                        if (customNavigationPoint_3 != null)
                        {
                            if (GetNavDistance(customNavigationPoint_3.Position) > sprintDistance)
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

                    GetClosestCoverPoint(botPosition, coverSearchRadius, safeDist);

                    navpoint = customNavigationPoint_2;

                    if (customNavigationPoint_2 != null)
                    {
                        if (GetNavDistance(customNavigationPoint_2.Position) > sprintDistance && Utils.EnemyInfo.DistanceProxy(botOwner_0, botPosition) > Utils.EnemyInfo.ProxyDistance.VeryClose)
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

        public AICoreActionResultStruct<BotLogicDecision> HoldPositionFor(float timer, string reason = "wait4it")
        {
            Utils.Utils.SetTimeout(() =>
            {
                if (botOwner_0.BotState == EBotState.Active && !botOwner_0.IsDead && botOwner_0.Memory.HaveEnemy && !botOwner_0.Memory.GoalEnemy.IsVisible)
                    botOwner_0.Steering.LookToDirection(botOwner_0.Memory.GoalEnemy.CurrPosition - botOwner_0.GetPlayer.Transform.position, 90f);
            }, 100);
            return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(timer), reason);
        }

        public AICoreActionResultStruct<BotLogicDecision> GetCloserToBoss(out CustomNavigationPoint navpoint)
        {
            Vector3 bossPosition = HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position;
            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            if (request != null)
            {
                Utils.Utils.SetTimeout(() =>
                {
                    if (botOwner_0 != null && !botOwner_0.IsDead && botOwner_0.BotState == EBotState.Active && request != null && request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup)
                    {
                        request.Complete();
                    }

                }, 2000);
                ResetTimer("coverTimer_2");
            }

            GetClosestCoverPointGroup(bossPosition, bossInnerRadius);

            navpoint = customNavigationPoint_2;

            if (customNavigationPoint_2 != null)
            {
                if (GetNavDistance(customNavigationPoint_2.Position) < sprintDistance)
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "moveCloserToBoss");
                else
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "moveCloserToBossFast");

            }
            return BotLogicDecisions.RegroupToBoss(botOwner_0);
        }

        public AICoreActionResultStruct<BotLogicDecision> MarksManFight(out CustomNavigationPoint navpoint)
        {

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            bool canShoot = botOwner_0.Memory.GoalEnemy.CanShoot;
            bool haveSeen = botOwner_0.Memory.GoalEnemy.HaveSeen;
            float lastSeenTime = botOwner_0.Memory.GoalEnemy.PersonalLastSeenTime;

            // If the enemy is a sniper and visible, and the bot is in cover, shoot from cover
            if (enemyVisible && botOwner_0.Memory.IsInCover)
            {
                navpoint = null;

                if (canShoot)
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "sfc");

                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass761.Random(2f, 5f)), "wait4it");
            }

            // If the enemy is a sniper and visible, try to find a cover point from which you can shoot
            if (enemyVisible)
            {
                GetClosestAttackCoverPoint(botPosition,5,200); // Find cover close to the bot's position
                
                navpoint = customNavigationPoint_1;

                if (customNavigationPoint_1 != null)
                {
                    if (GetNavDistance(customNavigationPoint_1.Position) > sprintDistance)
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

                navpoint = customNavigationPoint_2;

                if (customNavigationPoint_2 != null)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "relocateFast");
                }
            }

            navpoint = null;

            return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
        }

        public AICoreActionEndStruct? ShallEndCurrentDecisionCommon(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (
                curDecision.Action == (BotLogicDecision)CustomBotDecisions.EnemySearch
            )
            {
                return EndEnemySearch();
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

        public AICoreActionEndStruct? ShallEndCurrentDecisionAllies(AICoreActionResultStruct<BotLogicDecision> curDecision, bool? ordersChanged = null)
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

            if (breakOffContactDecision.Contains(curDecision.Action) && _isTakingHeavyDamage && botOwner_0.Memory.HaveEnemy && Vector3.Distance(botOwner_0.GetPlayer.Transform.position, botOwner_0.Memory.GoalEnemy.CurrPosition) > 25f)
            {
                return new AICoreActionEndStruct("contact.Break", true);
            }

            bool ordchanged = ordersChanged.HasValue ? ordersChanged.Value : this.ordersChanged;

            // orders changed
            if (ordchanged &&
                !ordersIgnoreReasons.Contains(curDecision.Reason) &&
                !ordersIgnoreDecisions.Contains(curDecision.Action) &&
                (
                    !botOwner_0.Memory.HaveEnemy ||
                    !botOwner_0.Memory.GoalEnemy.IsVisible
                )
            )
            {
                OrderReset();
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

        public AICoreActionEndStruct EndEnemySearch()
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
            if(ordersChanged)
                return new AICoreActionEndStruct("orders.Received", true);

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

        public override AICoreActionEndStruct EndGoToPoint()
        {
            if (ordersChanged || !botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("EndGoTo", true);
            }

            if (botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                return new AICoreActionEndStruct("enemy.canSh", true);
            }

            if (botOwner_0.GoToSomePointData.IsCome())
            {
                return new AICoreActionEndStruct("point.Reached", true);
            }

            return base.EndGoToPoint();
        }

    }
}
