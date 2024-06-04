using Aki.Common.Http;
using EFT;
using friendlyPMC.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Components
{
    // GClass69 is generic request receiver layer
    internal class FollowerRequestLayer : GClass69
    {
        float coverTimer = 0f;
        float suppressTime = 0f;
        float doorOpenTimer = 0f;

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
            return "FBPReq:Null";
        }

        public override bool ShallUseNow()
        {
            BotRequest currRequest = botOwner_0.BotRequestController.CurRequest;
            List<BotRequestType> enemyAllowedRequests = new List<BotRequestType>
            {
                BotRequestType.getInCover,
                BotRequestType.hide,
                BotRequestType.suppressionFire,
            };

            List<BotRequestType> allyAllowedRequest = new List<BotRequestType>
            {
                BotRequestType.getInCover,
                BotRequestType.hide,
                BotRequestType.throwGrenade,
                BotRequestType.throwGrenadeFromPlace,
                BotRequestType.suppressionFire
            };

            List<BotRequestType> generalRequests = new List<BotRequestType>
            {
               BotRequestType.wait,
               BotRequestType.followMe
            };

            if (currRequest == null)
            {
                return false;
            }

            if(currRequest.BotRequestType == BotRequestType.doorOpen)
            {
                return currRequest.CanProceed();
            }

            if (
                    (
                        // boss can throw all types of requests
                        botOwner_0.BotFollower.BossToFollow != null && currRequest.Requester == botOwner_0.BotFollower.BossToFollow.Player() &&
                        // - the rest is handled by followerfight layer
                        (botOwner_0.Memory.HaveEnemy && enemyAllowedRequests.Contains(currRequest.BotRequestType)) ||
                        (!botOwner_0.Memory.HaveEnemy && generalRequests.Contains(currRequest.BotRequestType))
                    ) ||
                    (
                        // teammates only some
                        botOwner_0.BotsGroup.Contains(currRequest.Requester.AIData.BotOwner) && allyAllowedRequest.Contains(currRequest.BotRequestType)
                    )
                )
            {
                return true;
            }

            return false;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            if (request != null)
            {
                Logger.LogInfo("General request received is " + request.BotRequestType.ToString());
            }

            switch (request.BotRequestType)
            {
                // on follow me request from the boss, just come closer to the boss or get out of hold position
                case BotRequestType.followMe:
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                    request.Complete();
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.followerPatrol, "backToFLB");

                // stay in place
                case BotRequestType.wait:
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                    botOwner_0.Gesture.TryGestus(EGesture.Good,false);

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:holdPos");

                // spread out requests
                case BotRequestType.getInCover:
                case BotRequestType.hide:

                    if (botOwner_0.Memory.IsInCover)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Going, false);
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:stayHidden");
                    }
                    GetCoverPoint(botOwner_0.GetPlayer.Transform.position, 30f);
                    if (customNavigationPoint_0 != null)
                    {
                        botOwner_0.BotTalk.TrySay(EPhraseTrigger.Going, false);
                        request.Complete();
                        if (!botOwner_0.CanSprintPlayer)
                        {
                            return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToCoverPoint, "req:goHide");
                        }
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "req:runHide");
                    } else
                    {
                        request.Complete();

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:cantHide");
                    }

                case BotRequestType.suppressionFire:
                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Covering, true);
                    suppressTime = Time.time + 2f;
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.suppressFire, "req:suppressFire");

                case BotRequestType.doorOpen:
                    doorOpenTimer = Time.time + 5f;
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.doorOpen, "doorOpen");
            }

            botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, false);
            return new AICoreActionResultStruct<BotLogicDecision>(HoldOrCover(botOwner_0), "req:Error");
        }


        public override AICoreActionEndStruct EndDoorOpenRequest()
        {
            BotRequest curRequest = this.botOwner_0.BotRequestController.CurRequest;

            if(doorOpenTimer < Time.time)
            {
                curRequest.Complete();
                return this.gstruct7_0;
            }

            if (curRequest != null && curRequest.BotRequestType == BotRequestType.doorOpen && !botOwner_0.DoorOpener.Interacting)
            {
                return this.gstruct7_1;
            }
            return this.gstruct7_0;
        }
        public override AICoreActionEndStruct EndSuppressFire()
        {
            BotRequest curRequest = this.botOwner_0.BotRequestController.CurRequest;
            if (curRequest != null && curRequest.BotRequestType == BotRequestType.suppressionFire)
            {
                if (suppressTime < Time.time)
                {
                    suppressTime = 0;
                    curRequest.Complete();

                    return this.gstruct7_0;
                }
                return this.gstruct7_1;
            }
            return this.gstruct7_0;
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



            List<CustomNavigationPoint> customNavigationPoints = BossPlayers.Instance.GetCovers();

            if (customNavigationPoints.Count > 0)
            {
                CustomNavigationPoint point1 = null;
                float distance = searchRadius;

                List<CustomNavigationPoint> availablePoints = new List<CustomNavigationPoint>();

                foreach (CustomNavigationPoint point in customNavigationPoints)
                {
                    if (point.IsFreeById(botOwner_0.Id) && !point.IsSpotted)
                    {
                        float range = (centerPosition - point.Position).magnitude;
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
