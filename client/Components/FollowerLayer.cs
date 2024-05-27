using EFT;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Components
{
    // GClass103 is a generic follower layer
    internal class FollowerLayer : GClass103
    {
        private float float_2;

        private CustomNavigationPoint customNavigationPoint_0;

        public FollowerLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            float_2 = Time.time + 60f;
        }

        public override string Name()
        {
            return "FollowerBossPlayer";
        }
        private bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {

            if (!botOwner_0.Medecine.FirstAid.Have2Do && !botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
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
                if (botOwner_0.SecondWeaponData.HaveActions())
                {
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.watchSecondWeapon, "Look2ndWeap");
                }

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
                    if (botOwner_0.SecondWeaponData.HaveActions())
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.watchSecondWeapon, "Look2ndWeap");
                    }
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


                bool hasBossRequest = botOwner_0.BotFollower.HaveBoss && botOwner_0.BotRequestController.CurRequest != null && botOwner_0.BotRequestController.CurRequest.Requester == botOwner_0.BotFollower.BossToFollow.Player();
                BotRequest currRequest = hasBossRequest ? botOwner_0.BotRequestController.CurRequest : null;

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
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "first aid");
                }
                if (method_11(20f))
                {
                    GetCoverPoint(botOwner_0.Position, 20f);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "goforheal");
                }
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
            return gstruct7_1;
        }

        public override AICoreActionEndStruct EndHeal()
        {
            if (!botOwner_0.Medecine.FirstAid.Have2Do && !botOwner_0.Medecine.SurgicalKit.HaveWork)
            {
                return new AICoreActionEndStruct("EndHeal", true);
            }
            return gstruct7_1;
        }
        public override bool ShallUseNow()
        {
            botOwner_0.PriorityAxeTarget.FindTarget();
            return true;
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

        private void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            List<CustomNavigationPoint> customNavigationPoints = BossPlayer.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;
                float range = 0;

                List<CustomNavigationPoint> availablePoints = new List<CustomNavigationPoint>();

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (point.IsFreeById(botOwner_0.Id) && !point.IsSpotted)
                    {
                        range = (centerPosition - point.Position).sqrMagnitude;
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
    }
}
