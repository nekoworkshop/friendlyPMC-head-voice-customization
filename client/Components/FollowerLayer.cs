using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Components
{
    // GClass103 is a generic follower layer
    internal class FollowerLayer : GClass103
    {
        protected float float_2;

        protected CustomNavigationPoint customNavigationPoint_0;

        protected float heal_time = 0f;

        public FollowerLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            float_2 = Time.time + 60f;
        }
        public override bool ShallUseNow()
        {
            botOwner_0.PriorityAxeTarget.FindTarget();
            var brain = botOwner_0.Brain.BaseBrain as FollowerBrain;
            
            if (brain != null && brain.UnderFire && !botOwner_0.Memory.HaveEnemy) return true;

            return HasBoss() && !InteractableObjects.IsTaker(botOwner_0) && !InteractableObjects.IsOpener(botOwner_0);
        }

        public override string Name()
        {
            return "FLBPlayer";
        }
        protected virtual bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        protected virtual pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }

        protected virtual Vector3 GetBossPosition()
        {
            return  HasBoss() ?  GetBoss().Position : botOwner_0.GetPlayer.Transform.position;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            if (!botOwner_0.Medecine.FirstAid.Have2Do && !botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                var brain = botOwner_0.Brain.BaseBrain as FollowerBrain;

                if (brain != null && brain.UnderFire && !botOwner_0.Memory.IsInCover)
                {
                    GetCoverPoint(botOwner_0.GetPlayer.Transform.position, 50f);
                    if (customNavigationPoint_0 != null)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "runHide");
                    }
                }

                if (botOwner_0.SmokeGrenade.IsInSmoke)
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToCoverPoint, "PeaceSmoke");
                }
                if (botOwner_0.PeaceHardAim.HaveActions())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.peaceHardAim, "PeaceHardAi");
                }
                if (botOwner_0.PeaceLook.HaveActions())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.peaceLook, "PeaceLook");
                }
                /*if (botOwner_0.SecondWeaponData.HaveActions())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.watchSecondWeapon, "Look2ndWeap");
                }*/

                if (!HasBoss())
                {
                    if (botOwner_0.FriendlyTilt.HaveActions())
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.friendlyTilt, "FriendlyTil");
                    }
                    if (botOwner_0.EatDrinkData.HaveActions())
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.eatDrink, "EatDrinkDat");
                    }
                    /*if (botOwner_0.SecondWeaponData.HaveActions())
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.watchSecondWeapon, "Look2ndWeap");
                    }*/
                    if (botOwner_0.Gesture.HaveRequest())
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.gesture, "Gesture");
                    }
                    if (botOwner_0.PeacefulActions.HaveActions())
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.peaceful, "Peaceful");
                    }
                }

                botOwner_0.PatrollingData.SetTargetMoveSpeed();
                botOwner_0.PatrollingData.PointChooser.ShallChangeWay(false);


                if (botOwner_0.PatrollingData.Way == null)
                {
                    botOwner_0.PatrollingData.PointChooser.ChooseStartWay();
                }

                PatrolWay way = botOwner_0.PatrollingData.Way;


                if (HasBoss())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "BossFollow");
                }


                if ((float)botOwner_0.WeaponManager.Reload.BulletCount / (float)botOwner_0.WeaponManager.Reload.MaxBulletCount < 0.6f && float_2 < Time.time)
                {
                    float_2 = Time.time + 30f;
                    botOwner_0.WeaponManager.Reload.TryReload();
                }
                if (way != null && way.PatrolType == PatrolType.reserved && botOwner_0.Settings.FileSettings.Patrol.CAN_CHOOSE_RESERV)
                {
                    botOwner_0.PatrollingData.ComeToPoint();
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.alternativePatrol, "RESER");
                }
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.simplePatrol, "Basic");
            }
            else
            {
                if (botOwner_0.Memory.IsInCover)
                {
                    heal_time = Time.time;
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "first aid");
                }
                if (method_11(20f))
                {
                    GetCoverPoint(botOwner_0.GetPlayer.Transform.position, 50f);
                    if (this.customNavigationPoint_0 != null)
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "goforheal");
                }
                heal_time = Time.time;
                return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal now");

            }

        }

            
        public override AICoreActionEndStruct EndSimplePatrol()
        {
            string reason;
            if (method_13(out reason))
            {
                return new AICoreActionEndStruct(reason, true);
            }
            if (botOwner_0.PatrollingData.Way.PatrolType == PatrolType.reserved)
            {
                return new AICoreActionEndStruct("way is alt", true);
            }
            if (HasBoss())
            {
                return new AICoreActionEndStruct("has boss", true);
            }
            return aICoreActionEndStruct_1;
        }

        public override AICoreActionEndStruct EndHeal() 
        {
            if (!botOwner_0.Medecine.FirstAid.Have2Do && !botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                if (botOwner_0.Medecine.FirstAid.Using) botOwner_0.Medecine.FirstAid.CancelCurrent();
                else if (botOwner_0.Medecine.SurgicalKit.Using) botOwner_0.Medecine.SurgicalKit.CancelCurrent();
                
                return new AICoreActionEndStruct("EndHeal", true);
            } else if(heal_time + 30f < Time.time) 
            {
                if (botOwner_0.Medecine.FirstAid.Using) botOwner_0.Medecine.FirstAid.CancelCurrent();
                else if (botOwner_0.Medecine.SurgicalKit.Using) botOwner_0.Medecine.SurgicalKit.CancelCurrent();

                botOwner_0.AIData.Player.ActiveHealthController.RestoreFullHealth();

                return new AICoreActionEndStruct("EndHealTimer", true);
            }

            return aICoreActionEndStruct_1;
        }

        public override AICoreActionEndStruct EndSuppressFire()
        { 
            return new AICoreActionEndStruct("enemy.None", true);
        }

        public override AICoreActionEndStruct EndRunToEnemy()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            return base.EndRunToEnemy();
        }

        public override AICoreActionEndStruct EndAttackMoving()
        {
            if (!botOwner_0.Memory.HaveEnemy)
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            return base.EndAttackMoving();
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {
            if (curDecision.Action == BotLogicDecision.heal
            )
            {
                return EndHeal();
            }

            if (curDecision.Action == BotLogicDecision.runToCover && curDecision.Reason == "runToHeal")
            {
                return new AICoreActionEndStruct("enemy.None", true);
            }

            return base.ShallEndCurrentDecision(curDecision);
        }

        /*public override CustomNavigationPoint FindPoint(CoverSearchData data, Func<CoverSearchData, CustomNavigationPoint> p, bool checkCurrent)
        {
            customNavigationPoint_0 = Utils.Covers.FindPoint(botOwner_0, customNavigationPoint_0);
            return customNavigationPoint_0;
        }*/

        protected virtual void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {

            CustomNavigationPoint point1 = Utils.Covers.GetCoverPoint(botOwner_0, centerPosition, searchRadius);

            customNavigationPoint_0 = point1;
            botOwner_0.Memory.SetCoverPoints(point1);
        }
    }
}
