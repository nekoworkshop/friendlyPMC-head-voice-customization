using Comfort.Common;
using EFT;
using friendlyPMC.Components.Tactics;
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

        private CustomNavigationPoint customNavigationPoint_0;

        private FollowerCommonLayer commonLayer;

        float heal_time = 0f;
        public FollowerRequestLayer(BotOwner bot, int priority) : base(bot, priority)
        {
            commonLayer = new FollowerCommonLayer(bot, priority);
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
            if (botOwner_0.Memory.HaveEnemy)
            {
                if (botOwner_0.BotRequestController.CurRequest?.BotRequestType == BotRequestType.wait)
                    botOwner_0.BotRequestController.CurRequest.Complete();

                return false;
            }

            var brain = botOwner_0.Brain.BaseBrain as FollowerBrain;

            if (brain != null && brain.UnderFire) return false;

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

            if (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork || botOwner_0.Medecine.Using) return false;


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

        public override AICoreActionResultStruct<BotLogicDecision> GetDecision()
        {
            BotRequest request = botOwner_0.BotRequestController.CurRequest;

            if(request == null)
            {
                return new AICoreActionResultStruct<BotLogicDecision>(HasBoss() ? BotLogicDecision.followerPatrol : HoldOrCover(botOwner_0), "req:Error");
            }

            switch (request.BotRequestType)
            {
                // on follow me request from the boss, just come closer to the boss or get out of hold position
                case BotRequestType.followMe:

                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.MoveToPoint, "req:comeHere");

                case (BotRequestType)CustomBotRequestType.Regroup:
                    Utils.Utils.SetTimeout(() =>
                    {
                        BotRequest req = botOwner_0.BotRequestController.CurRequest;

                        if (botOwner_0 != null && !botOwner_0.IsDead && botOwner_0.BotState == EBotState.Active && req != null && req.BotRequestType == (BotRequestType)CustomBotRequestType.Regroup)
                        {
                            req.Complete();
                        }

                    }, 2000);
                    return BotLogicDecisions.RegroupToBoss(botOwner_0);

                // stay in place
                case BotRequestType.wait:
                    if(heal_time + 30f < Time.time  && (botOwner_0.Medecine.FirstAid.Have2Do || botOwner_0.Medecine.SurgicalKit.HaveWork))
                    {
                        heal_time = Time.time;
                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.heal, "heal");
                    }
                    return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:holdPos");

                // spread out requests
                case BotRequestType.getInCover:
                case BotRequestType.hide:

                    GetCoverPoint(botOwner_0.GetPlayer.Transform.position, 50f);
                    if (customNavigationPoint_0 != null)
                    {
                        Utils.Utils.SetTimeout(() =>
                        {
                            if (
                                botOwner_0 != null && !botOwner_0.IsDead && botOwner_0.BotState == EBotState.Active && request != null && 
                                ( request.BotRequestType == BotRequestType.hide || request.BotRequestType == BotRequestType.getInCover)
                            )
                            {
                                request.Complete();
                            }

                        }, 4000);

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.runToCover, "req:runHide");
                    } else
                    {
                        request.Complete();

                        return new AICoreActionResultStruct<BotLogicDecision>(BotLogicDecision.holdPosition, "req:cantHide");
                    }

                case BotRequestType.goToPoint:
                    return new AICoreActionResultStruct<BotLogicDecision>((BotLogicDecision)CustomBotDecisions.MoveToPoint, "req:goCheck");
            }

            
            botOwner_0.BotTalk.TrySay(EPhraseTrigger.Negative, false);
            request.Complete();
            return new AICoreActionResultStruct<BotLogicDecision>(HasBoss() ? BotLogicDecision.followerPatrol : HoldOrCover(botOwner_0), "req:Unhandled");
        }

        public override AICoreActionEndStruct ShallEndCurrentDecision(AICoreActionResultStruct<BotLogicDecision> curDecision)
        {

            if(curDecision.Action == BotLogicDecision.goToPoint && botOwner_0.Mover.IsComeTo(0.5f, false))
            {
                return new AICoreActionEndStruct("point.Reached", true);
            }

            return base.ShallEndCurrentDecision(curDecision);
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

        public override AICoreActionEndStruct EndHeal()
        {
            if (heal_time + 30f < Time.time)
            {
                if (botOwner_0.Medecine.FirstAid.Using) botOwner_0.Medecine.FirstAid.CancelCurrent();
                else if (botOwner_0.Medecine.SurgicalKit.Using) botOwner_0.Medecine.SurgicalKit.CancelCurrent();

                botOwner_0.AIData.Player.ActiveHealthController.RestoreFullHealth();

                return new AICoreActionEndStruct("EndHealTimer", true);
            }

            return aICoreActionEndStruct_1;
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
