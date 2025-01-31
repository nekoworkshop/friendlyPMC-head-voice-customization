using EFT;
using System;
using UnityEngine;
using EFT.InventoryLogic;
using friendlyPMC.Components.Tactics;

namespace friendlyPMC.Components.BossFollower
{
    /**
     * Ovewrite the fight layer for Knight to user our cover system and stay around the player boss
     */
    internal class KnightFightLayer : GClass67
    {

        protected float coverTimer = 0f;

        protected readonly float sprintDistance = 15f;

        protected readonly float fightRange = 50f;
        protected readonly float fightLongRange = 100f;

        protected bool bool_14;

        protected bool bool_8;

        protected bool bool_10;

        protected float float_43 = 0f;
        protected float float_52 = 0f;
        protected float float_29 = 0f;
        protected float float_32 = 0f;

        protected int int_16 = 0;

        protected FollowerCommonLayer commonLayer;
        protected FollowerPusherLayer pusherLayer;
        protected FollowerGuard guardLayer;

        public KnightFightLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            pusherLayer = new FollowerPusherLayer(bot, priority);
            commonLayer = pusherLayer.CommonLayer;
            guardLayer = new FollowerGuard(bot, priority, pusherLayer);
        }
        public override void OnActivate()
        {
            pusherLayer?.OnActivate();
            base.OnActivate();

            if (botOwner_0.WeaponManager.Grenades != null)
            {
                botOwner_0.WeaponManager.Grenades.OnGrenadeThrowStart += OnThrowGrenade;
            }

            if (botOwner_0.WeaponManager.Grenades != null) botOwner_0.WeaponManager.Grenades.OnGrenadeThrowStart -= OnThrowGrenade;
        }

        public override void Dispose()
        {
            pusherLayer?.Dispose();
            base.Dispose();
        }

        public void OnThrowGrenade()
        {
            float killa_AFTER_GRENADE_SUPPRESS_DELAY = botOwner_0.Settings.FileSettings.Boss.KILLA_AFTER_GRENADE_SUPPRESS_DELAY;
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            if (killa_AFTER_GRENADE_SUPPRESS_DELAY > 0f && goalEnemy != null && !goalEnemy.CanShoot)
            {
                nullable_0 = new BotLogicDecision?(BotLogicDecision.holdPosition);
                HoldFor(killa_AFTER_GRENADE_SUPPRESS_DELAY);
            }
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
            return commonLayer.HasBoss();
        }

        protected pitAIBossPlayer GetBoss()
        {
            return commonLayer.GetBoss();
        }

        public void OrdersChanged()
        {
            pusherLayer.OrdersChanged();
        }

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            pusherLayer.DecisionChanged(prevDecision, nextDecision);

            base.DecisionChanged(prevDecision, nextDecision);
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            // is in dogfight?
            AICoreActionResultStruct<BotLogicDecision>? aicoreActionResultStruct = commonLayer.DogFight(out customNavigationPoint_0);
            if (aicoreActionResultStruct != null) return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;

            // needs healing?
            aicoreActionResultStruct = commonLayer.NeedHeal(out customNavigationPoint_0);
            if (aicoreActionResultStruct != null) return (AICoreActionResultStruct<BotLogicDecision>)aicoreActionResultStruct;

            // player requests?
            AICoreActionResultStruct<BotLogicDecision>? preFightDecision = KnightPreFight();
            if (preFightDecision != null) return (AICoreActionResultStruct<BotLogicDecision>)preFightDecision;

            // do not go after distant enemies
            if (Utils.Enemy.Distance(botOwner_0) >= Utils.Enemy.EnemyDistance.Distant)
            {
                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.CoverToCover, "coverBoss");
            }

            try
            {
                return KnightFight();
            }
            catch (Exception ex)
            {
                Modules.Logger.LogInfo("KnightFight Error: " + ex.Message);
                Modules.Logger.LogInfo("Trace: " + ex.StackTrace);
                return new AICoreActionResultStruct<BotLogicDecision>(HoldFor(GClass824.Random(1f, 2f)), "decision.Error");
            }
        }

        public AICoreActionResultStruct<BotLogicDecision> KnightAssault()
        {
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            Utils.Enemy.EnemyDistance distanceToEnemy = Utils.Enemy.Distance(botOwner_0);

            // If the enemy is visible
            if (enemyVisible)
            {
                // If the enemy is close or mid-range
                if (distanceToEnemy <= Utils.Enemy.EnemyDistance.Mid && commonLayer.IsEnemyLowThreat(false, 2))
                {
                    // Rush towards the enemy
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "assaultRush");
                }
                else
                {
                    // Find a cover point closer to the enemy
                    GetClosestAttackCoverPoint((botOwner_0.Position + botOwner_0.Memory.GoalEnemy.EnemyLastPosition) / 2f);
                    if (customNavigationPoint_0 != null)
                    {
                        // Move towards the cover point while suppressing the enemy
                        botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMovingWithSuppress, "assaultApproach");
                    }
                    else if (commonLayer.IsEnemyLowThreat(false, 2))
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
                GetClosestAttackCoverPoint((botOwner_0.Position + botOwner_0.Memory.GoalEnemy.EnemyLastPosition) / 2f);
                if (customNavigationPoint_0 != null)
                {
                    // Move towards the cover point while suppressing
                    botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMovingWithSuppress, "assaultApproach");
                }
                else if (commonLayer.IsEnemyLowThreat(false, 2))
                {
                    // No cover point found, move towards the enemy's last known position while suppressing
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToEnemy, "assaultRush");
                }


            }

            return pusherLayer.EngageEnemy();
        }

        public AICoreActionResultStruct<BotLogicDecision>? KnightPreFight()
        {

            BotRequest request = botOwner_0.BotRequestController.CurRequest;


            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            Vector3 bossPosition = request != null ? botOwner_0.BotRequestController.CurRequest.Requester.Position : botPosition;

            // player needs help or has call for a regroup
            if (
                request != null &&
                request.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup &&
                Utils.Utils.GetNavDistance(botPosition, bossPosition) > commonLayer.regroupMinDistance
            )
            {
                if (!botOwner_0.Memory.HaveEnemy || !botOwner_0.Memory.GoalEnemy.IsVisible)
                {
                    return commonLayer.GetCloserToBoss(out customNavigationPoint_0);
                }
                else
                {
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.DontKnow, true);
                    request.Complete();
                }
            }
            // player requested a suppression fire

            if (request != null && request.BotRequestType == BotRequestType.suppressionFire)
            {
                AICoreActionResultStruct<BotLogicDecision> decision = guardLayer.method_29(false, BotLogicDecision.debugGrenade);

                if (decision.Action != BotLogicDecision.debugGrenade)
                {
                    return decision;
                }
            }

            // do not pursue a marksman
            if (botOwner_0.Memory.GoalEnemy.Owner.IsRole(WildSpawnType.marksman))
            {
                return commonLayer.MarksManFight(out customNavigationPoint_0);
            }

            // player suggested to do a push
            if (request != null && request.BotRequestType == BotRequestType.attackClose)
            {
                AICoreActionResultStruct<BotLogicDecision> forcePush = pusherLayer.EngageEnemy(true);
                customNavigationPoint_0 = pusherLayer.NavigationPoint;
                return forcePush;
            }

            return null;
        }

        /** Adaptation of original GetDecision from GClass65 */
        public AICoreActionResultStruct<BotLogicDecision>? KnightBaseDecision()
        {
            if (method_23() && botOwner_0.Brain.LastDecision != null)
            {
                BotLogicDecision? lastDecision = botOwner_0.Brain.LastDecision;
                if (!(lastDecision.GetValueOrDefault() == (BotLogicDecision)CustomBotDecisions.attackRetreat & lastDecision != null))
                {
                    if (botOwner_0.Memory.GoalEnemy != null && botOwner_0.Memory.GoalEnemy.CanShoot && botOwner_0.Memory.GoalEnemy.IsVisible)
                    {
                        return commonLayer.DogFight(out customNavigationPoint_0);
                    }

                    GetClosestAttackCoverPoint((botOwner_0.Position + botOwner_0.Memory.GoalEnemy.EnemyLastPosition) / 2f);
                    if (customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.attackRetreat, "enemyNear");
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
                return commonLayer.HoldPositionFor(7f, "bad covers");
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
                    var brain = (botOwner_0.Brain.BaseBrain as FollowerBrain);
                    if (!brain.IsThrowingGrenade) brain.OnThrow();

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
                    return commonLayer.DogFight(out customNavigationPoint_0);
                }

                return KnightAssault();
            }

            return null;
        }


        public AICoreActionResultStruct<BotLogicDecision> KnightFight()
        {
            AICoreActionResultStruct<BotLogicDecision>? baseDecision = KnightBaseDecision();

            if (baseDecision.HasValue) return baseDecision.Value;

            bool useGrenade = botOwner_0.Settings.FileSettings.Core.CanGrenade && GClass824.Random(0f, 2f) > 1f && Utils.Enemy.Distance(botOwner_0) <= Utils.Enemy.EnemyDistance.Close;

            AICoreActionResultStruct<BotLogicDecision> decision = guardLayer.method_29(useGrenade, BotLogicDecision.debugGrenade);

            if (decision.Action != BotLogicDecision.debugGrenade)
            {
                return decision;
            }

            AICoreActionResultStruct<BotLogicDecision> push = pusherLayer.EngageEnemy();
            customNavigationPoint_0 = pusherLayer.NavigationPoint;

            return push;
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            AICoreActionEndStruct? common = commonLayer.ShallEndCurrentDecisionAllies(curDecision);

            if (common != null) return (AICoreActionEndStruct)common;

            AICoreActionEndStruct? push = pusherLayer.ShallEndDecision(curDecision);

            if (push.HasValue) return push.Value;

            if (curDecision.Reason == "assaultRush" && Utils.Enemy.Distance(botOwner_0) <= Utils.Enemy.EnemyDistance.VeryClose)
            {
                return new AICoreActionEndStruct("assault.closeEnough", true);
            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = commonLayer.FindPoint(data, p, checkCurrent);
            return customNavigationPoint_0;
        }

        public AICoreActionEndStruct EndGetInClose()
        {
            return commonLayer.EndGetInClose();
        }
        public override AICoreActionEndStruct EndGoToPoint()
        {
            return commonLayer.EndGoToPoint();
        }

        public override AICoreActionEndStruct EndHoldPosition()
        {
            return pusherLayer.EndHoldPosition();
        }

        public override AICoreActionEndStruct EndRunToEnemy()
        {
            return pusherLayer.EndRunToEnemy();
        }

        public override AICoreActionEndStruct EndHeal()
        {
            return commonLayer.EndHeal();
        }
        public override AICoreActionEndStruct EndTakeItem()
        {
            return commonLayer.EndTakeItem();
        }

        public override AICoreActionEndStruct EndFollowerPatrolItem()
        {
            if (botOwner_0.Memory.HaveEnemy) return new AICoreActionEndStruct("enemy.Present", true);
            return base.EndFollowerPatrolItem();
        }

        protected void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = commonLayer.GetClosestCoverPoint(centerPosition, searchRadius);
        }
        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            customNavigationPoint_0 = commonLayer.GetCoverPoint(centerPosition, searchRadius);
        }

        protected void GetApproachablePoint()
        {
            customNavigationPoint_0 = commonLayer.GetApproachableCover();
        }

        protected void GetClosestAttackCoverPoint(Vector3 centerPosition)
        {
            customNavigationPoint_0 = commonLayer.GetClosestShootCover(centerPosition);
        }
    }
}
