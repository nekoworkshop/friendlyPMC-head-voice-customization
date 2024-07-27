using EFT;
using friendlyPMC.Modules;
using friendlyPMC.Utils;
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

        private readonly float sprintDistance = 15f;

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
            if (botOwner_0.Memory.HaveEnemy) return false;

            BotRequest currRequest = botOwner_0.BotRequestController.CurRequest;

            List<BotRequestType> allyAllowedRequest = new List<BotRequestType>
            {
                BotRequestType.getInCover,
                BotRequestType.hide
            };

            List<BotRequestType> bossRequests = new List<BotRequestType>
            {
               BotRequestType.getInCover,
               BotRequestType.hide,
               BotRequestType.wait,
               BotRequestType.followMe,
               BotRequestType.goToPoint,
               (BotRequestType)CustomBotRequestType.Regroup
            };

            if (currRequest == null)
            {
                return false;
            }

            if (currRequest.BotRequestType == BotRequestType.doorOpen)
            {
                return currRequest.CanProceed();
            }

            pitAIBossPlayer boss = null;
            if (botOwner_0.BotFollower.BossToFollow != null)
            {
                boss = BossPlayers.Instance.GetBossPlayer(botOwner_0.BotFollower.BossToFollow.Player().ProfileId);
            }

            if (
                    (
                        // boss can throw all types of requests
                        boss != null && bossRequests.Contains(currRequest.BotRequestType)
                    ) 
                    ||
                    // teammates only some
                    (
                        boss != null &&
                        boss.Followers.Contains(currRequest.Requester.AIData.BotOwner) && allyAllowedRequest.Contains(currRequest.BotRequestType)
                    ) ||
                    (
                        boss == null &&
                        botOwner_0.BotsGroup.Contains(currRequest.Requester.AIData.BotOwner)
                    )
                )
            {
                return true;
            }

            return false;
        }


        private bool HasBoss()
        {
            return botOwner_0.BotFollower.HaveBoss;
        }

        private pitAIBossPlayer GetBoss()
        {
            return (pitAIBossPlayer)botOwner_0.BotFollower.BossToFollow;
        }

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            IPlayer requester = request != null ? botOwner_0.BotRequestController.CurRequest.Requester : null;

            if(request == null)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(HoldOrCover(botOwner_0), "req:Error");
            }

            switch (request.BotRequestType)
            {
                // on follow me request from the boss, just come closer to the boss or get out of hold position
                case BotRequestType.followMe:
                    botOwner_0.Gesture.TryGestus(EGesture.Good, false);

                    Vector3 requestPos = requester.Position;
                    Vector3 dir01 = requester.LookDirection;

                    float offset = GClass761.RandomSing() * GClass761.Random(1f, 2f);
                    Vector3 direction = Vector3.Cross(Vector3.up, dir01).normalized;

                    Vector3 finPos = requestPos + direction * offset;

                    Vector3 point = new Vector3(finPos.x, requestPos.y, finPos.z);
                    
                    botOwner_0.GoToSomePointData.SetPoint(point);

                    botOwner_0.Steering.LookToMovingDirection();
                    
                    bool shouldSprint01 = Vector3.Distance(point, botOwner_0.GetPlayer.Transform.position) >= sprintDistance;
                    botOwner_0.GoToSomePointData.UpdateToGo(shouldSprint01);
                    if (!shouldSprint01) botOwner_0.Sprint(false);

                    request.Complete();

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, "req:comeHere");

                case (BotRequestType)CustomBotRequestType.Regroup:
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

                    GetCoverPoint(botOwner_0.GetPlayer.Transform.position, 50f);
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

                case BotRequestType.goToPoint:

                    Vector3 dir02 = requester.LookDirection;
                    float forwardDistance = GClass761.Random(3f, 5f);

                    Vector3 forwardPosition = requester.Position + dir02.normalized * forwardDistance;
                    float lateralOffset = GClass761.RandomSing() * GClass761.Random(0.5f, 1.5f);
                    Vector3 lateralDirection = Vector3.Cross(Vector3.up, dir02).normalized;

                    Vector3 finalPosition = forwardPosition + lateralDirection * lateralOffset;

                    botOwner_0.BotTalk.TrySay(EPhraseTrigger.Going, false);

                    botOwner_0.GoToSomePointData.SetPoint(finalPosition);
                    botOwner_0.Steering.LookToMovingDirection();
                    bool shouldSprint02 = Vector3.Distance(finalPosition, botOwner_0.GetPlayer.Transform.position) >= sprintDistance;
                    botOwner_0.GoToSomePointData.UpdateToGo(shouldSprint02);
                    if (!shouldSprint02) botOwner_0.Sprint(false);

                    request.Complete();

                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.goToPoint, "req:goCheck");

                case BotRequestType.doorOpen:
                    doorOpenTimer = Time.time + 5f;
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.doorOpen, "doorOpen");
            }

            botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, false);
            return new AICoreActionResultStruct<BotLogicDecision>(HasBoss() ? BotLogicDecision.followerPatrol : HoldOrCover(botOwner_0), "req:Error");
        }


        public override AICoreActionEndStruct EndDoorOpenRequest()
        {
            BotRequest curRequest = this.botOwner_0.BotRequestController.CurRequest;

            if(doorOpenTimer < Time.time)
            {
                if (curRequest != null && curRequest.BotRequestType == BotRequestType.doorOpen)
                    curRequest.Complete();

                return aICoreActionEndStruct;
            }

            if (curRequest != null && curRequest.BotRequestType == BotRequestType.doorOpen && !botOwner_0.DoorOpener.Interacting)
            {
                return aICoreActionEndStruct_1;
            }
            return aICoreActionEndStruct;
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

                    return aICoreActionEndStruct;
                }
                return aICoreActionEndStruct_1;
            }
            return aICoreActionEndStruct;
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
            } else
            {
                GetCoverPoint(botOwner_0.GetPlayer.Transform.position, 70f);
            }

            return this.customNavigationPoint_0;
        }

        private void GetCoverPoint(Vector3 centerPosition, float searchRadius)
        {
            if (this.coverTimer > Time.time) return;

            this.coverTimer = 1f + Time.time;

            customNavigationPoint_0 = Covers.GetCoverPoint(botOwner_0, centerPosition, searchRadius);
            botOwner_0.Memory.SetCoverPoints(customNavigationPoint_0);
        }
    }
}
