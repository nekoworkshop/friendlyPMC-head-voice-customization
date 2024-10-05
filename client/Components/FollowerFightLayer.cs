
using EFT;
using friendlyPMC.Components.Tactics;
using friendlyPMC.Modules;
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

        private float bossInnerRadius
        {
            get { return commonLayer.bossInnerRadius; }
        }
        public float bossOuterRadius
        {
            get { return commonLayer.bossOuterRadius; }
        }

        private float coverTimer = 0f;
        private float suppressTime = 0f;

        private bool ordersAreHold = false;
        private bool ordersAreAttack = false;
        private bool ordersAreReqroup = false;

        private bool holdTactic = false;
        private bool rushTactic = false;
        private bool allyTactic = false;
        private bool sniperTactic = false;
        private bool guardTactic = false;

        private bool wantsToHeal = false;

        private bool bossUnderAttack = false;

        private string tactic = "default";

        public NavMeshPath NavMeshPath
        {
            get
            {
                return commonLayer.NavMeshPath;
            }
        }

        private FollowerCommonLayer commonLayer;
        private FollowerHolderLayer holderLayer;
        private FollowerPusherLayer pusherLayer;
        private FollowerSniperLayer sniperLayer;
        private FollowerGuard guardLayer;

        private bool ordersChanged
        {
            get
            {
                return commonLayer.OrderHasChangedRecently;
            }
        }

        public FollowerCommonLayer CommonLayer
        {
            get => commonLayer;
        }

        public FollowerPusherLayer PusherLayer
        {
            get => pusherLayer;
        }

        public FollowerHolderLayer HolderLayer
        {
            get => holderLayer;
        }

        public FollowerSniperLayer SniperLayer
        {
            get => sniperLayer;
        }

        public FollowerFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            sniperLayer = new FollowerSniperLayer(bot, priority);
            commonLayer = sniperLayer.CommonLayer;
            holderLayer = new FollowerHolderLayer(bot, priority, commonLayer);
            pusherLayer = new FollowerPusherLayer(bot, priority, commonLayer);
            guardLayer = new FollowerGuard(bot,priority, commonLayer);

        }
        public override void OnActivate()
        {
            base.OnActivate();
            sniperLayer.OnActivate();
            holderLayer.OnActivate();
            pusherLayer.OnActivate();
            guardLayer.OnActivate();
        }

        public override void Dispose()
        {
            base.Dispose();
            sniperLayer.Dispose();
            holderLayer.Dispose();
            pusherLayer.Dispose();
            guardLayer.Dispose();
        }

        public void SetBossFightTactic(string tactic)
        {
            rushTactic = false;
            holdTactic = false;
            allyTactic = false;
            sniperTactic = false;
            guardTactic = false;


            if (tactic != null) tactic = tactic.ToLower();

            if (tactic == "ally" || tactic == "assist")
            {
                allyTactic = true;
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Assist");
            }
            else if (tactic == "push")
            {
                rushTactic = true;
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Push");
            }
            else if (tactic == "defend")
            {
                holdTactic = true;
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Hold");
            }
            else if (tactic == "marksman")
            {
                sniperTactic = true;
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Marksman");
            }
            else if (tactic == "guard")
            {
                guardTactic = true;
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Guard");
            }
            else
            {
                (botOwner_0.Brain.BaseBrain as FollowerBrain).SetTactic("Default");
            }
        }


        public void CoverType(string type)
        {
            commonLayer.CoverType(type);
        }
        public override string Name()
        {
            if (holdTactic) tactic = "defend";
            else if (rushTactic) tactic = "push";
            else if (allyTactic) tactic = "assist";
            else if (sniperTactic) tactic = "marksman";
            else if (guardTactic) tactic = "guard";
            else tactic = "default";
            if (ordersAreAttack) tactic += ":atk";
            else if (ordersAreHold) tactic += ":hld";

            return "FBPFight" + tactic;
        }
        public override bool ShallUseNow()
        {
            if (wantsToHeal) return true;

            if (!botOwner_0.Memory.HaveEnemy)
            {
                if (ordersAreAttack || ordersAreHold)
                {
                    ordersAreAttack = false;
                    ordersAreHold = false;

                    if (
                        botOwner_0.BotRequestController.CurRequest != null &&
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.attackClose
                    )
                    {
                        botOwner_0.BotRequestController.CurRequest.Complete();
                    }
                }

                return false;
            }

            if (InteractableObjects.IsTaker(botOwner_0)) return false;

            return true;
        }

        public bool HasBoss()
        {
            return commonLayer.HasBoss();
        }

        public pitAIBossPlayer GetBoss()
        {
            return commonLayer.GetBoss();
        }

        public bool ShallGoNearBoss()
        {
            return commonLayer.ShallGoNearBoss();
        }

        public void OrdersChanged()
        {
            commonLayer.OrdersChanged();
        }

        public AICoreActionResultStruct<BotLogicDecision> DefendPosition(Vector3 interestPosition)
        {
            AICoreActionResultStruct<BotLogicDecision> holder = holderLayer.DefendPosition(interestPosition);
            customNavigationPoint_0 = holderLayer.NavigationPoint;

            return holder;
        }

        public AICoreActionResultStruct<BotLogicDecision> EngageEnemy(bool pushOrdered = false)
        {
            AICoreActionResultStruct<BotLogicDecision> engage = pusherLayer.EngageEnemy(pushOrdered);
            customNavigationPoint_0 = pusherLayer.NavigationPoint;

            return engage;
        }

        public AICoreActionResultStruct<BotLogicDecision> GuardTactic()
        {
            AICoreActionResultStruct<BotLogicDecision> engage = guardLayer.GetDecision();
            customNavigationPoint_0 = pusherLayer.NavigationPoint;

            return engage;
        }

        public AICoreActionResultStruct<BotLogicDecision> DecideTactic()
        {
            Vector3 interestPosition = HasBoss() ? GetBoss().Position : botOwner_0.GetPlayer.Transform.position;

            if(commonLayer.coverType == "far")
            {
                interestPosition = botOwner_0.GetPlayer.Transform.position;
            }


            if(guardTactic)
            {

                if (ordersAreAttack) return EngageEnemy(true);

                if (commonLayer.coverType == "far")
                {
                    if (Utils.Enemy.Distance(botOwner_0) <= Utils.Enemy.EnemyDistance.Mid)
                    {
                        return EngageEnemy();
                    }
                    else
                    {
                        return pusherLayer.EnemySearch();
                    }
                }

                return GuardTactic();
            }

            if (allyTactic)
            {
                if (botOwner_0.Memory.AttackImmediately && commonLayer.IsEnemyLowThreat())
                {
                    if(Enemy.Distance(botOwner_0) <= Enemy.EnemyDistance.Mid)
                        return EngageEnemy();
                    else
                        return pusherLayer.EnemySearch();
                }

                if (Enemy.Distance(botOwner_0) > Enemy.EnemyDistance.Mid || !commonLayer.IsEnemyLowThreat()) return sniperLayer.GetDecision();

                return pusherLayer.EnemySearch();
            }
            else if ((ordersAreHold || holdTactic) && !ordersAreAttack)
            {

                if (!ordersAreHold && !ordersAreAttack && holdTactic)
                {

                    if (botOwner_0.Memory.AttackImmediately && commonLayer.IsEnemyLowThreat() && Utils.Enemy.Distance(botOwner_0) <= Utils.Enemy.EnemyDistance.Close)
                        return EngageEnemy();

                }

                return DefendPosition(interestPosition);
            }
            else if (ordersAreAttack || rushTactic) return EngageEnemy(ordersAreAttack);


            // do not go after distant enemies
            /*if (Utils.Enemy.Distance(botOwner_0) > Utils.Enemy.EnemyDistance.Mid)
            {
                return pusherLayer.EnemySearch();
            }*/

            if (botOwner_0.Memory.AttackImmediately && Utils.Enemy.Distance(botOwner_0) <= Utils.Enemy.EnemyDistance.Mid)
            {
                return EngageEnemy();
            }
            else
            {
                return pusherLayer.EnemySearch();
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

            // assist and sniper do not do push
            if (allyTactic || sniperTactic)
            {
                ordersAreAttack = false;
                if (allyTactic) ordersAreHold = false;
            }

            // is in dogfight
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = commonLayer.DogFight(out customNavigationPoint_0);

            if (aicoreActionResultStruct != null)
            {
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }
            // needs healing?
            aicoreActionResultStruct = commonLayer.NeedHeal(out customNavigationPoint_0);
            if (aicoreActionResultStruct != null)
            {
                wantsToHeal = true;
                if (request != null && request.BotRequestType != BotRequestType.wait) request.Complete(); // cancel requests when needing to heal
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }

            wantsToHeal = false;

            AIBossPlayerLogic gclass363_0 = HasBoss() ? GetBoss().GetBossLogic() : null;
            bossUnderAttack = gclass363_0 != null && gclass363_0.IsHitted;

            // Check if the bot has received the regroup command
            if (ordersAreReqroup && GetNavDistance(bossPosition) > commonLayer.regroupMinDistance && (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible))
            {
                aicoreActionResultStruct = commonLayer.GetCloserToBoss(out customNavigationPoint_0);
                return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;
            }


            if (!allyTactic)
            {
                // Check if the boss is under attack
                if (bossUnderAttack && (commonLayer.coverType == "close") && (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible))
                {
                    // - switch the bot's enemy to the one attacking the boss
                    var closestEnemy = GetBoss().ClosestEnemy();
                    if (closestEnemy != null)
                    {
                        GetBoss().PrioritizeEnemy(botOwner_0, closestEnemy);
                    }
                    // - guard tries to get in front of the boss
                    if(guardTactic)
                    {
                        customNavigationPoint_0 = Utils.Covers.GetClosestCoverPointBetween(botOwner_0,GetBoss().realPlayer.Transform.position, closestEnemy.GetPlayer.Transform.position);
                        if(customNavigationPoint_0 != null)
                        {
                            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);

                            if (GetNavDistance(customNavigationPoint_0.Position) > commonLayer.sprintDistance)
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
                            GetClosestCoverPoint(GetBoss().realPlayer.Transform.position, bossInnerRadius);

                            if (customNavigationPoint_0 != null)
                            {
                                if (GetNavDistance(customNavigationPoint_0.Position) > commonLayer.sprintDistance)
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
                                return EngageEnemy();
                            }
                        }
                    }
                    // - sniper tries to find shooting spot
                    if (sniperTactic || holdTactic)
                    {
                        GetClosestAttackCoverPoint(bossPosition, bossOuterRadius);
                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "relocateFast");
                        }
                        else
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
                        }
                    }

                    // - try and get back to the boss
                    GetClosestCoverPoint(bossPosition, bossOuterRadius);

                    if (customNavigationPoint_0 != null)
                    {
                        if (GetNavDistance(customNavigationPoint_0.Position) > commonLayer.sprintDistance)
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
            }

            // suppression fire request
            if (botOwner_0.Memory.HaveEnemy && request != null && request.BotRequestType == BotRequestType.suppressionFire)
            {
                botOwner_0.BotTalk.TrySay(EPhraseTrigger.Covering, true);
                suppressTime = Time.time + 2f;
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "suppressFire");
            }

            // throw grenade request
            if (!sniperTactic && request != null && request.BotRequestType == BotRequestType.throwGrenade)
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.throwGrenadeFromPlace, "throwGrenadeRequest");

            // come here request during fights
            if(request != null && request.BotRequestType == BotRequestType.followMe && !allyTactic)
                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.MoveToPoint, "req:comeHere");

            // go there request during fights
            if (request != null && request.BotRequestType == BotRequestType.goToPoint && !allyTactic)
            {
                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.MoveToPoint, "req:goCheck");
            }

            // spread out request
            if (
                request != null &&
                (request.BotRequestType == BotRequestType.getInCover || request.BotRequestType == BotRequestType.hide)
             )
            {
                Modules.Logger.LogInfo("Spread Out received");
                if (botOwner_0.Memory.HaveEnemy && botOwner_0.Memory.GoalEnemy.CanShoot)
                {
                    request.Complete();
                }
                else
                {
                    GetCoverPoint(botOwner_0.GetPlayer.Transform.position, 50f);

                    if (customNavigationPoint_0 != null)
                    {
                        Utils.Utils.SetTimeout(() =>
                        {
                            if (
                                botOwner_0 != null && !botOwner_0.IsDead && botOwner_0.BotState == EBotState.Active && request != null &&
                                (request.BotRequestType == BotRequestType.hide || request.BotRequestType == BotRequestType.getInCover)
                            )
                            {
                                request.Complete();
                            }

                        }, 4000);
                        Modules.Logger.LogInfo("Execute Spread Out");
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runToCover");
                    } else
                    {
                        request.Complete();
                    }
                }
            }

            if (botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman))
                return commonLayer.MarksManFight(out customNavigationPoint_0);

            // ally tactic will make the bot always fight in hold mode
            if (allyTactic)
            {
                ordersAreAttack = false;
                ordersAreHold = false;
                return DecideTactic();
            }

            if (sniperTactic)
            {
                AICoreActionResultStruct<BotLogicDecision> decision = sniperLayer.GetDecision();
                customNavigationPoint_0 = sniperLayer.NavigationPoint;
                return decision;
            }

            return DecideTactic();
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            if (ordersAreHold || allyTactic || (holdTactic && !ordersAreAttack)) return holderLayer.EndHoldPosition();

            return pusherLayer.EndHoldPosition();
        }

        public override AICoreActionEndStruct EndSuppressFire()
        {
            BotRequest curRequest = botOwner_0.BotRequestController.CurRequest;
            if (curRequest != null && curRequest.BotRequestType == BotRequestType.suppressionFire)
            {
                if (suppressTime < Time.time)
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
            return commonLayer.EndRunToEnemy();
        }

        public override AICoreActionEndStruct EndGoToPoint()
        {

            return commonLayer.EndGoToPoint();
        }

        public override AICoreActionEndStruct EndHeal()
        {
            AICoreActionEndStruct result =  commonLayer.EndHeal();

            if (result.Value) wantsToHeal = false;

            return result;
        }

        public override AICoreActionEndStruct EndTakeItem()
        {
            return commonLayer.EndTakeItem();
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

        public override AICoreActionEndStruct EndDoorOpenRequest()
        {
            InteractableObjects.SetCurDoor(null);
            return new AICoreActionEndStruct("enemy.Present", true);
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (curDecision.Action == BotLogicDecision.heal
            )
            {
                return commonLayer.EndHeal();
            }

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
                        !commonLayer.ordersIgnoreReasons.Contains(curDecision.Reason) &&
                        !commonLayer.ordersIgnoreDecisions.Contains(curDecision.Action) &&
                        (
                            !botOwner_0.Memory.HaveEnemy ||
                            !botOwner_0.Memory.GoalEnemy.IsVisible
                        )
                    )
                )
            )
            {
                if(!ordersAreAttack && !ordersAreHold) commonLayer.OrderReset();
                return new AICoreActionEndStruct("orders.Received", true);
            }

            AICoreActionEndStruct? shallEndCommon = commonLayer.ShallEndCurrentDecisionCommon(curDecision);

            if (shallEndCommon.HasValue) return shallEndCommon.Value;

            if(curDecision.Action == (BotLogicDecision)CustomBotDecisions.MoveToPoint)
            {
                if(!botOwner_0.Memory.HaveEnemy) return new AICoreActionEndStruct("enemy.None", true);
                if (!botOwner_0.Memory.GoalEnemy.CanShoot) return new AICoreActionEndStruct("enemy.Shoot", true);

                if (!(botOwner_0.BotRequestController.CurRequest != null && 
                        (botOwner_0.BotRequestController.CurRequest.BotRequestType ==  BotRequestType.goToPoint ||
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.followMe)
                     )
                   )
                    return aICoreActionEndStruct;

                return aICoreActionEndStruct_1;
            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            commonLayer.DecisionChanged(prevDecision, nextDecision);
        }
        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            if (coverTimer > Time.time) return customNavigationPoint_0;

            coverTimer = 1f + Time.time;

            customNavigationPoint_0 = Covers.FindPoint(botOwner_0, customNavigationPoint_0, 100f);

            return customNavigationPoint_0;
        }

        public void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool> extraChecks = null)
        {
            customNavigationPoint_0 = commonLayer.GetClosestCoverPoint(centerPosition, searchRadius, safeDistance, extraChecks);
        }
        /** Find the closest safe cover point to the given position, within the given radius **/
        public void GetClosestSafeCoverPoint(Vector3 centerPosition, float safeDistance = 10f)
        {
            customNavigationPoint_0 = commonLayer.GetClosestSafeCoverPoint(centerPosition, safeDistance);
        }

        /** Find closest cover point to pointA between pointA and pointB ensuring it is at minimum safeDistance from danger **/

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

        public void GetApproachablePoint()
        {
            customNavigationPoint_0 = commonLayer.GetApproachableCover();

        }
        /** Find a shoot positionm that is closest to the enemy but at a minimum distance and maximum from the enemy **/
        public void GetClosestAttackCoverPoint(Vector3 centerPosition, float minDistance = 5f, float maxDistance = 150f)
        {
            customNavigationPoint_0 = commonLayer.GetClosestShootCover(centerPosition, minDistance, maxDistance);
        }

        private void GetClosestCoverPointGroup(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = commonLayer.GetClosestCoverPointGroup(centerPosition, searchRadius);
        }
        /** Is point free by the followers group **/
        private bool IsPointFreeGroup(CustomNavigationPoint point)
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

        public float GetNavDistance(Vector3 point)
        {
            return commonLayer.GetNavDistance(point);
        }
    }
}
