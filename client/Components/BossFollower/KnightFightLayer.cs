using EFT;
using System;
using UnityEngine;
using EFT.InventoryLogic;
using System.Reflection.Emit;

namespace friendlyPMC.Components.BossFollower
{
    internal class KnightFightLayer : GClass65
    {

        protected float coverTimer = 0f;

        protected readonly float sprintDistance = 15f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        protected bool ordersChanged = false;

        protected FollowerFightLayer followerFightLayer;

        protected bool bool_14;

        protected bool bool_8;

        protected bool bool_10;

        protected float float_43 = 0f;
        protected float float_52 = 0f;
        protected float float_29 = 0f;
        protected float float_32 = 0f;

        protected int int_16 = 0;

        public KnightFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            followerFightLayer = new FollowerFightLayer(bot, priority);
        }
        public override void OnActivate()
        {
            followerFightLayer?.OnActivate();
            base.OnActivate();
        }

        public override void Dispose()
        {
            base.Dispose();
            followerFightLayer?.Dispose();
        }

        public override bool ShallUseNow()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                if (
                        botOwner_0.BotRequestController.CurRequest != null &&
                        (botOwner_0.BotRequestController.CurRequest.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup ||
                        botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.attackClose)
                    )
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                }

                return false;
            }

            if (!bool_14)
            {
                bool_14 = true;
                botOwner_0.Brain.BaseBrain.OnLayerChangedTo += OnLayerChanged;
            }

            return true;
        }

        public new void OnLayerChanged(AICoreLayerClass<BotLogicDecision> layer)
        {
            this.int_16 = 0;
            if (layer == this)
            {
                this.float_43 = Time.time;
                return;
            }
            this.float_43 = -1000f;
        }
        protected bool HasBoss()
        {
            return followerFightLayer.HasBoss();
        }

        protected pitAIBossPlayer GetBoss()
        {
            return followerFightLayer.GetBoss();
        }

        public void OrdersChanged()
        {
            ordersChanged = true;
            Utils.Utils.SetTimeout(() =>
            {
                ordersChanged = false;
            },1000f);

            followerFightLayer.OrdersChanged();
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            // is in dogfight?
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = followerFightLayer.DogFight();
            if (aicoreActionResultStruct != null) return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;

            // needs healing?
            aicoreActionResultStruct = followerFightLayer.NeedHeal();
            if (aicoreActionResultStruct != null) return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;

            // player requests?
            AICoreActionResultStruct<BotLogicDecision>? preFightDecision = KnightPreFight();
            if (preFightDecision != null) return (AICoreActionResultStruct<BotLogicDecision>)preFightDecision;

            if (Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.VeryClose && !followerFightLayer.IsEnemyLowThreat(true))
            {
                return followerFightLayer.DefendPosition(botOwner_0.GetPlayer.Transform.position);
            }

            try
            {
                return KnightFight();
            } catch (Exception ex)
            {
                Components.Logger.LogInfo("KnightFight Error: " + ex.Message);
                Components.Logger.LogInfo("Trace: " + ex.StackTrace);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass761.Random(1f, 2f)), "decision.Error");
            }
        }

        public AICoreActionResultStruct<BotLogicDecision> KnightAssault()
        {
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            Utils.EnemyInfo.EnemyDistance distanceToEnemy = Utils.EnemyInfo.Distance(botOwner_0);

            // If the enemy is visible
            if (enemyVisible)
            {
                // If the enemy is close or mid-range
                if (distanceToEnemy <= Utils.EnemyInfo.EnemyDistance.Mid)
                {
                    // Rush towards the enemy while suppressing
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "assaultRush");
                }
                else
                {
                    // Find a cover point closer to the enemy
                    GetClosestAttackCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, 5f);
                    if (customNavigationPoint_0 != null)
                    {
                        // Move towards the cover point while suppressing the enemy
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMovingWithSuppress, "assaultApproach");
                    }
                    else
                    {
                        // No cover point found, move towards the enemy while suppressing
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "assaultRush");
                    }
                }
            }
            // If the enemy is not visible
            else
            {
                // Find a cover point closer to the enemy's last known position
                GetClosestAttackCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition, 5f);
                if (customNavigationPoint_0 != null)
                {
                    // Move towards the cover point while suppressing
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMovingWithSuppress, "assaultApproach");
                }
                else
                {
                    // No cover point found, move towards the enemy's last known position while suppressing
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "assaultRush");
                }
            }
        }

        public AICoreActionResultStruct<BotLogicDecision>? KnightPreFight()
        {

            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = request != null ? botOwner_0.BotRequestController.CurRequest.Requester.Position : botPosition;

            // player needs help or has call for a regroup
            if (
                ordersChanged && request != null &&
                request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup &&
                Utils.Utils.GetNavDistance(botPosition, bossPosition) > friendlyPMC.regroupMinDistance
            )
            {
                Components.Logger.LogInfo("Player asked for help");
                if (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible)
                {
                    Components.Logger.LogInfo("move closer to Player");
                    followerFightLayer.GetCloserToBoss();

                }
                else
                {
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.OnFight, true);
                    request.Complete();
                }
            }

            // do not pursue a marksman
            if (botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman))
            {
                return followerFightLayer.MarksManFight();
            }

            // player suggested to do a push
            if (ordersChanged && request != null && request.BotRequestType == BotRequestType.attackClose)
            {

                Utils.Utils.SetTimeout(() =>
                {
                    if(botOwner_0 != null && !botOwner_0.IsDead && botOwner_0.BotState == EBotState.Active && botOwner_0.BotRequestController.CurRequest != null)
                    {
                        botOwner_0.BotRequestController.CurRequest.Complete();
                    }
                },2000f);

                return followerFightLayer.EngageEnemy(true);
            }

            return null;
        }
        
        /** Adaptation of original GetDecision from GClass65 */
        public AICoreActionResultStruct<BotLogicDecision>? KnightBaseDecision()
        {
            if (method_23() && botOwner_0.Brain.LastDecision != null)
            {
                BotLogicDecision? lastDecision = botOwner_0.Brain.LastDecision;
                if (!(lastDecision.GetValueOrDefault() == BotLogicDecision.attackMoving & lastDecision != null))
                {
                    if (botOwner_0.Memory.GoalEnemy != null && botOwner_0.Memory.GoalEnemy.CanShoot && botOwner_0.Memory.GoalEnemy.IsVisible)
                    {
                        return followerFightLayer.DogFight();
                    }

                    GetClosestAttackCoverPoint(botOwner_0.Memory.GoalEnemy.CurrPosition,10f);
                    if(customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "enemyNear");
                }
            }

            bool_8 = false;
            bool_10 = false;
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            if (float_43 + 10f < Time.time && botOwner_0.Memory.GoalEnemy != null && !botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                if (goalEnemy != null)
                {
                    if (goalEnemy.IsVisible)
                    {
                        goto IL_19D;
                    }
                    BotLogicDecision? lastDecision = botOwner_0.Brain.LastDecision;
                    if (lastDecision.GetValueOrDefault() == BotLogicDecision.heal & lastDecision != null)
                    {
                        goto IL_19D;
                    }
                }
                if (botOwner_0.Medecine.FirstAid.Have2Do && botOwner_0.Memory.IsInCover && botOwner_0.Memory.LastEnemyTimeSeen + 6f < Time.time)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "HealInCover");
                }

                IL_19D:
                return KnightAssault();
            }

            int num = (botOwner_0.BotsGroup.MembersCount > 1) ? 1 : 2;
            if (int_16 >= num)
            {
                int_16 = 0;
                return followerFightLayer.HoldPositionFor(7f,"bad covers");
            }
            float_52 = Time.time;


            if (method_13())
            {
                if (method_16(true))
                {
                    GetClosestCoverPoint(botOwner_0.GetPlayer.Transform.position, fightRange);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "loseTarget");
                }

                if (botOwner_0.WeaponManager.Grenades.HaveGrenadeOfType(ThrowWeapType.smoke_grenade))
                {
                    AIGreanageThrowData aigreanageThrowData = new AIGreanageThrowData();
                    aigreanageThrowData.Direction = botOwner_0.LookDirection;
                    aigreanageThrowData.Ang = 30f;
                    aigreanageThrowData.Force = 6f;
                    aigreanageThrowData.GrenadeType = new ThrowWeapType?(ThrowWeapType.smoke_grenade);
                    botOwner_0.WeaponManager.Grenades.SetThrowData(aigreanageThrowData);
                    botOwner_0.WeaponManager.Grenades.DoThrow();
                    float_29 = Time.time;
                    float_32 = Time.time;
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "suppress1");
                }
            }

            if (method_21())
            {
                if (botOwner_0.Memory.IsInCover)
                {
                    botOwner_0.Memory.Spotted(false, null, new float?(32f));
                    int_16++;
                }
                
                if (method_12() && botOwner_0.Memory.GoalEnemy.CanShoot && botOwner_0.Memory.GoalEnemy.IsVisible)
                {
                    return followerFightLayer.DogFight();
                }

                if (followerFightLayer.IsEnemyLowThreat() && Utils.EnemyInfo.Distance(botOwner_0) < Utils.EnemyInfo.EnemyDistance.Mid)
                {
                    return KnightAssault();
                }
            }

            return null;
        }


        public AICoreActionResultStruct<BotLogicDecision> KnightFight()
        {
            AICoreActionResultStruct<BotLogicDecision>? baseDecision = KnightBaseDecision();

            if (baseDecision.HasValue) return baseDecision.Value;

            return followerFightLayer.EngageEnemy(false,true);
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            AICoreActionEndStruct? common = followerFightLayer.ShallEndCurrentDecisionAllies(curDecision, ordersChanged);

            if (common != null) return (AICoreActionEndStruct)common;

            if(curDecision.Reason == "assaultRush" && Utils.EnemyInfo.Distance(botOwner_0) <= Utils.EnemyInfo.EnemyDistance.VeryClose)
            {
                return new AICoreActionEndStruct("assault.closeEnough", true);
            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            followerFightLayer.DecisionChanged(prevDecision, nextDecision);

            base.DecisionChanged(prevDecision, nextDecision);
        }
        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = followerFightLayer.FindPoint(data, p, checkCurrent);
            return customNavigationPoint_0;
        }

        public AICoreActionEndStruct EndGetInClose()
        {
            return followerFightLayer.EndGetInClose();
        }
        public override AICoreActionEndStruct EndGoToPoint()
        {
            return followerFightLayer.EndGoToPoint();
        }

        public override AICoreActionEndStruct EndSearch()
        {
            return followerFightLayer.EndSearch();
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            return followerFightLayer.EndHoldPosition();
        }

        public override AICoreActionEndStruct EndRunToEnemy()
        {
            return followerFightLayer.EndRunToEnemy();
        }

        public override AICoreActionEndStruct EndHeal()
        {
            return followerFightLayer.EndHeal();
        }
        public override AICoreActionEndStruct EndTakeItem()
        {
            return followerFightLayer.EndTakeItem();
        }

        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            if(botOwner_0.Memory.HaveEnemy) return new AICoreActionEndStruct("enemy.Present", true);
            return base.EndFollowerPatrolItem();
        }

        protected void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = followerFightLayer.GetClosestCoverPoint(centerPosition, searchRadius);
        }
        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = followerFightLayer.GetCoverPoint(centerPosition, searchRadius);
        }

        protected void GetApproachablePoint()
        {
            customNavigationPoint_0 = followerFightLayer.GetApproachablePoint();
        }

        protected void GetClosestAttackCoverPoint(Vector3 centerPosition, float minDistance = 5f)
        {
            customNavigationPoint_0 = followerFightLayer.GetClosestAttackCoverPoint(centerPosition, minDistance);
        }
    }
}
