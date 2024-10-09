using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using friendlyPMC.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Components.Tactics
{
    /** This class is not meant to be used directly as a brain layer, but within one **/
    internal class FollowerGuard : GClass50
    {
        protected float coverTimer = 0f;
        protected float holdTimer = 0f;

        protected readonly float fightRange = 10f;

        private bool existingCommon = false;

        private FollowerCommonLayer commonLayer;

        private float float_2 = 0f;
        private float float_10 = 0f;
        private readonly List<Vector3> list_1 = new List<Vector3>();

        private readonly GClass396 gclass396_0 = null;

        public CustomNavigationPoint NavigationPoint
        {
            get
            {
                return customNavigationPoint_0;
            }
        }

        public FollowerCommonLayer CommonLayer { get { return commonLayer; } }
        public FollowerGuard(BotOwner bot, int priority, FollowerCommonLayer commonLayer = null) : base(bot, priority)
        {
            if (commonLayer != null)
            {
                this.commonLayer = commonLayer;
                existingCommon = true;
            }
            else this.commonLayer = new FollowerCommonLayer(bot, priority);

            gclass396_0 = botOwner_0.WeaponManager.Selector as GClass396;
        }

        public override string Name()
        {
            return "FBGuard";
        }
        // dummy 
        public override bool ShallUseNow()
        {
            return true;
        }

        public override void OnActivate()
        {
            base.OnActivate();
            if (!existingCommon) commonLayer?.OnActivate();

            botOwner_0.GetPlayer.GetPlayer.BeingHitAction += OnHit;
            if (botOwner_0.WeaponManager.Grenades != null)
            {
                botOwner_0.WeaponManager.Grenades.OnGrenadeThrowStart += OnThrowGrenade;
            }
        }
        public override void Dispose()
        {
            base.Dispose();
            if (!existingCommon) commonLayer?.Dispose();

            botOwner_0.GetPlayer.GetPlayer.BeingHitAction -= OnHit;
            if (botOwner_0.WeaponManager.Grenades != null) botOwner_0.WeaponManager.Grenades.OnGrenadeThrowStart -= OnThrowGrenade;
        }
        public void OrdersChanged()
        {
            commonLayer.OrdersChanged();
        }

        public bool ShallGoNearBoss()
        {
            return commonLayer.ShallGoNearBoss();
        }

        public void OnHit(DamageInfo damageInfo, EBodyPart bodyPart, float damageReducedByArmor)
        {
            if (bodyPart == EBodyPart.Head)
            {
                float_2 = Time.time;
            }
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

        public override void DecisionChanged(AICoreActionResultStruct<BotLogicDecision>? prevDecision, AICoreActionResultStruct<BotLogicDecision> nextDecision)
        {
            commonLayer.DecisionChanged(prevDecision, nextDecision);
            base.DecisionChanged(prevDecision, nextDecision);
        }

        public ShootPointClass GetShootPoint()
        {
            return botOwner_0.CurrentEnemyTargetPosition(true);
        }




        public AICoreActionResultStruct<BotLogicDecision>? GetSuppressDecision()
        {
            if (list_1.Count > 0)
            {
                botOwner_0.SuppressShoot.InitToPoints(list_1.ToList<Vector3>(), null);
                float delay = (float)list_1.Count * 2f;
                foreach (Vector3 position in list_1)
                {
                    Singleton<BotEventHandler>.Instance.ArtilleryStart(position, 20f, delay);
                }
                float_10 = Time.time + 60f;
                list_1.Clear();
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "grSuppress");
            }

            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            if (goalEnemy != null && !goalEnemy.IsVisible)
            {
                if (botOwner_0.SmokeGrenade.ShallShoot())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootToSmoke, "StM");
                }
                if (botOwner_0.SmokeGrenade.IsInSmoke)
                {
                    GetClosestCoverPoint(botOwner_0.GetPlayer.Position, 60f);
                    if (customNavigationPoint_0 != null)
                    {
                        botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "IsInSmoke");
                    }
                }
            }

            if (!botOwner_0.Memory.GoalEnemy.IsSuppressed() && goalEnemy.ShallISuppress())
            {
                return base.method_29(true, this.method_31());
            }

            return null;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
            bool enemyVisible = botOwner_0.Memory.GoalEnemy.IsVisible;
            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.CurrPosition;

            if (enemyVisible)
            {
                // enemy visible (can't shoot) and we are not in cover
                if (!botOwner_0.Memory.IsInCover)
                {
                    // - find cover to shoot from
                    // @TODO - needs fixing, bot ends up moving somewhere in the back!
                    GetClosestAttackCoverPoint(botPosition);

                    if (customNavigationPoint_0 != null)
                    {
                        if (commonLayer.GetNavDistance(customNavigationPoint_0.Position) < 25f)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "relocate");
                        }
                        return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.RunToCover, "relocateFast");
                    }

                    // - fallback
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.dogFight, "dgf");

                } else
                {
                    // - try to shot enemy
                    if (botOwner_0.Memory.CurCustomCoverPoint != null && botOwner_0.Memory.CurCustomCoverPoint.CanIShootToEnemy)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.shootFromCover, "shootFromCover");
                    // - else find better spot
                    // @TODO - needs fixing, bot ends up moving somewhere in the back!
                    else
                    {
                        bool getClose = false;
                        if (
                            Enemy.Distance(botOwner_0) <= Enemy.EnemyDistance.Close &&
                            botOwner_0.Memory.AttackImmediately && Enemy.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, enemyPos) < 3)
                        {
                            GetClosestAttackCoverPoint(enemyPos, fightRange);
                            getClose = true;
                        } else
                        {
                            GetClosestAttackCoverPoint(commonLayer.HasBoss()? commonLayer.GetBoss().Position : botPosition, fightRange);
                        }
                            
                        if (customNavigationPoint_0 != null)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, getClose ? "getInCloseSlow" : "relocate");
                        }
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.GuardToCover, "coverBoss");
                }
            }
            // enemy not visible
            else
            {
                // - see if we can do a supress
                AICoreActionResultStruct<BotLogicDecision>? supportDecision = null;

                try
                {
                    supportDecision = GetSuppressDecision();
                }
                catch (Exception ex)
                {
                    Modules.Logger.LogInfo("supportDecision Error: " + ex.Message);
                    Modules.Logger.LogInfo("Trace: " + ex.StackTrace);
                }

                if (supportDecision.HasValue) return supportDecision.Value;

                // - approach enemy if close enough
                if (
                    Enemy.Distance(botOwner_0) <= Enemy.EnemyDistance.Close && Enemy.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, enemyPos) < 3)
                {
                    GetClosestAttackCoverPoint(enemyPos);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "getInCloseSlow");
                    }

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToEnemy, "pushEnemy");
                } 
                else if (Enemy.Distance(botOwner_0) == Enemy.EnemyDistance.Mid)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.GuardToCover, "coverBoss");
                } else if(!(botOwner_0.Memory.AttackImmediately && Enemy.GetEnemiesAtLocation(botOwner_0, botOwner_0.Memory.GoalEnemy.ProfileId, enemyPos) < 3))
                {
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.GuardToCover, "coverBoss");
                }

                // - look for a shooting spot
                GetApproachablePoint();
                if(customNavigationPoint_0 == null)
                    GetClosestAttackCoverPoint(commonLayer.GetBoss().realPlayer.Transform.position,50f);

                if (customNavigationPoint_0 != null && coverTimer < Time.time)
                {
                    coverTimer = Time.time + GClass761.Random(3f, 5f);
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.RunToCover, "relocateFast");
                }

                if(commonLayer.ShallGoNearBoss())
                {
                    customNavigationPoint_0 = commonLayer.GetClosestCoverPointGroup(commonLayer.GetBoss().realPlayer.Transform.position, commonLayer.coverSearchRadius);

                    if (customNavigationPoint_0 != null)
                    {
                        if (commonLayer.GetNavDistance(customNavigationPoint_0.Position) < commonLayer.sprintDistance)
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.attackMoving, "regroupToBoss");
                        else
                            return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.RunToCover, "regroupToBossFast");
                    }
                }

                return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.GuardToCover, "coverBoss");
            }
        }
        public AICoreActionResultStruct<BotLogicDecision> GrenadierDecision()
        {
            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "suppressFireLauncher");
        }
        public AICoreActionResultStruct<BotLogicDecision>? CanDoGrenadierSuppressRequest(Ray rayDirection)
        {
            if(!botOwner_0.WeaponManager.Selector.CanChangeToSecondWeapons) return null;
            GClass396 selector = botOwner_0.WeaponManager.Selector as GClass396;

            if (selector != null && (selector.SecondPrimaryWeapon as Weapon) != null && (selector.SecondPrimaryWeapon as Weapon).IsGrenadeLauncher)
            {
                if(botOwner_0.WeaponManager.Selector.LastEquipmentSlot != EquipmentSlot.SecondPrimaryWeapon)
                    botOwner_0.WeaponManager.Selector.TryChangeWeapon(true);

                RaycastHit[] hits = new RaycastHit[20];
                
                float scanDistance = 120f;

                float sphereRadius = scanDistance / 2;
                float sphereDistance = scanDistance / 2;

                int numHits = Physics.SphereCastNonAlloc(
                    rayDirection,
                    sphereRadius,
                    hits,
                    sphereDistance,
                    LayerMaskClass.PlayerMask
                );

                List<Vector3> list_1 = new List<Vector3>();

                for (int i = 0; i < numHits; i++)
                {
                    RaycastHit hit = hits[i];
                    if (hit.collider != null)
                    {
                        
                        Player enemy = botOwner_0.ShootData.method_4(hit.collider);
                        bool isenemy = false;
                        if(enemy != null && (botOwner_0.BotsGroup.IsEnemy(enemy) || botOwner_0.BotsGroup.IsPlayerEnemy(enemy))) isenemy = true;

                        if (isenemy)
                        {
                            list_1.Add(enemy.Transform.position);
                        }
                    }
                }

                if(list_1.Count < 1) return null;

                botOwner_0.SuppressShoot.InitToPoints(list_1, null);
                float delay = (float)list_1.Count * 2f;
                
                foreach (Vector3 position in list_1)
                {
                    Singleton<BotEventHandler>.Instance.ArtilleryStart(position, 20f,delay);
                }

                return GrenadierDecision();
            }

            
            return null;
        }
        public bool method_35()
        {
            if (float_10 > Time.time)
            {
                return false;
            }
            float_10 = Time.time + 10f;
            if (gclass396_0 != null && gclass396_0.EquipmentSlot != EquipmentSlot.SecondPrimaryWeapon)
            {
                return false;
            }
            int num = 0;
            list_1.Clear();
            foreach (KeyValuePair<IPlayer, EnemyInfo> keyValuePair in botOwner_0.EnemiesController.EnemyInfos)
            {
                if (keyValuePair.Value.IsVisible)
                {
                    num++;
                    list_1.Add(keyValuePair.Value.CurrPosition);
                }
            }
            if (num > botOwner_0.Settings.FileSettings.Boss.BIG_PIPE_ARTILLERY_COUNT)
            {
                return true;
            }
            list_1.Clear();
            return false;
        }

        public BotLogicDecision method_31()
        {
            return (BotLogicDecision)CustomBotDecisions.GuardToCover;
        }
        public override AICoreActionEndStruct EndHoldPosition()
        {
            if (method_35())
            {
                return new AICoreActionEndStruct("massSpr", true);
            }
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;
            if (Time.time - goalEnemy.GroupInfo.EnemyLastSeenTimeReal < 2f)
            {
                bool_2 = false;
                return new AICoreActionEndStruct("smb seen", true);
            }
            return base.EndHoldPosition();
        }


        public override AICoreActionEndStruct EndShootFromCover()
        {
            if (method_35())
            {
                return new AICoreActionEndStruct("massSpr", true);
            }
            return base.EndShootFromCover();
        }


        protected virtual void GetClosestAttackCoverPoint(Vector3 centerPosition, float minDistance = 15f)
        {
            customNavigationPoint_0 = commonLayer.GetClosestShootCover(centerPosition, minDistance);
        }

        protected virtual void GetClosestCoverPoint(Vector3 centerPosition, float searchRadius, float safeDistance = 5f, Func<CustomNavigationPoint, bool> extraChecks = null)
        {
            customNavigationPoint_0 = commonLayer.GetClosestCoverPoint(centerPosition, searchRadius, safeDistance, extraChecks);
        }

        public virtual void GetApproachablePoint()
        {
            customNavigationPoint_0 = commonLayer.GetApproachableCover();
        }
    }
}
