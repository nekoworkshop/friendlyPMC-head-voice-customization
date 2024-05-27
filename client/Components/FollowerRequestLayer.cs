using EFT;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Components
{
    // GClass69 is generic request receiver layer
    internal class FollowerRequestLayer : GClass69
    {
        float coverTimer = 0f;

        private CustomNavigationPoint customNavigationPoint_0;
        public FollowerRequestLayer(BotOwner bot, int priority) : base(bot, priority)
        {

        }

        public override string Name()
        {
            if (botOwner_0.BotRequestController.CurRequest != null)
            {
                return "FBPReq:" + botOwner_0.BotRequestController.CurRequest.BotRequestType.ToString();
            }
            return "FollowReqNull";
        }

        public override bool ShallUseNow()
        {
            if (botOwner_0.BotRequestController.CurRequest == null)
            {
                return false;
            }

            if (
                    (
                        // boss can throw all types of requests
                        botOwner_0.BotRequestController.CurRequest.Requester == botOwner_0.BotFollower.BossToFollow.Player() &&
                        botOwner_0.BotRequestController.CurRequest.BotRequestType != BotRequestType.followMe
                    ) ||
                    (
                        // teammates only some
                        botOwner_0.BotsGroup.Contains(botOwner_0.BotRequestController.CurRequest.Requester.AIData.BotOwner) &&
                        (
                            botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.getInCover ||
                            botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.hide ||
                            botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.throwGrenade ||
                            botOwner_0.BotRequestController.CurRequest.BotRequestType == BotRequestType.throwGrenadeFromPlace
                        )
                    )
                )
            {
                return true;
            }

            return false;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            if (botOwner_0.BotRequestController.CurRequest != null)
            {

                Logger.Instance.LogInfo("BotRequestController was " + botOwner_0.BotRequestController.CurRequest.BotRequestType);
            }

            switch (botOwner_0.BotRequestController.CurRequest.BotRequestType)
            {
                // on follow me request from the boss, just come closer to the boss or get out of hold position
                case BotRequestType.followMe:

                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "flwMRF");

                // stay in place
                case BotRequestType.hold:
                case BotRequestType.wait:
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:holdPos");

                // spread out requests
                case BotRequestType.getInCover:
                case BotRequestType.hide:
                    if (botOwner_0.Memory.IsInCover)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:stayHidden");
                    }

                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Going, false);

                    GetCoverPoint(botOwner_0.Position, 30f);

                    if (!botOwner_0.CanSprintPlayer)
                    {
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToCoverPoint, "req:goHide");
                    }
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "req:runHide");

                case BotRequestType.suppressionFire:
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Covering, true);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "req:suppressFire");

                case BotRequestType.attackClose:
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, true);
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToEnemy, "req:attackClose");
            }

            return new AICoreActionResultStruct<BotLogicDecision>(BaseLogicLayerClass.HoldOrCover(botOwner_0), "Error");
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

            if (this.coverTimer > Time.time) return;

            this.coverTimer = 2f + Time.time;



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
