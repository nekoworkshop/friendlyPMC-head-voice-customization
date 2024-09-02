using EFT;
using friendlyPMC.Components;
using HarmonyLib;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    internal class FollowerGoToPoint : GClass173
    {
        private bool _shouldSprint = true;

        private float float_0 = 0f;
        
        private bool bool_0 = false;
        private bool bool_1 = false;
        private bool bool_2 = false;

        private bool _init = false;

        private Vector3? _point;
        public FollowerGoToPoint(BotOwner bot) : base(bot)
        {

        }

        private void Init()
        {
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnUpdate += OnAgentUpdate;
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnDispose += OnAgentDispose;
        }
        private void OnAgentUpdate(AICoreActionResultStruct<BotLogicDecision> decision)
        {
            if(decision.Action != BotLogicDecision.goToPoint || (decision.Reason != "req:comeHere" && decision.Reason != "req:goCheck"))
            {
                bool_0 = false;
                bool_1 = false;
                bool_2 = false;
            }
        }

        protected void OnAgentDispose(object sender, EventArgs e)
        {
            _init = false;
            
            bool_0 = false;
            bool_1 = false;
            bool_2 = false;

            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnUpdate -= OnAgentUpdate;
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnDispose -= OnAgentDispose;
        }

        public override void Update()
        {
            if (!_init)
            {
                Init();
                _init = true;
            }

            base.method_0();
            
            Components.Logger.LogInfo("Point bool_2 is " + (bool_2 ? "true" : "false"));

            if (bool_2)
            {
                return;
            }

            if (botOwner_0.Brain.Agent.LastReason == "req:goCheck" && !bool_0)
            {

                IPlayer requester = botOwner_0.BotRequestController.CurRequest.Requester;
                Vector3 dir02 = requester.LookDirection;
                float forwardDistance = GClass761.Random(3f, 5f);

                Vector3 forwardPosition = requester.Position + dir02.normalized * forwardDistance;
                float lateralOffset = GClass761.RandomSing() * GClass761.Random(0.5f, 1.5f);
                Vector3 lateralDirection = Vector3.Cross(Vector3.up, dir02).normalized;

                Vector3 finalPosition = forwardPosition + lateralDirection * lateralOffset;
                
                _point = finalPosition;

                if (botOwner_0.GoToPoint(finalPosition,true,0.5f) == NavMeshPathStatus.PathComplete)
                {
                    Components.Logger.LogInfo("Point Go");
                    bool_0 = true;
                }
                else
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                    return;
                }
            }
            else if(botOwner_0.Brain.Agent.LastReason == "req:comeHere" && !bool_1)
            {
                IPlayer requester = botOwner_0.BotRequestController.CurRequest.Requester;

                Vector3 requestPos = requester.Position;
                Vector3 dir01 = requester.LookDirection;

                float offset = GClass761.RandomSing() * GClass761.Random(1f, 2f);
                Vector3 direction = Vector3.Cross(Vector3.up, dir01).normalized;

                Vector3 finPos = requestPos + direction * offset;

                Vector3 point = new Vector3(finPos.x, requestPos.y, finPos.z);

                _point = point;

                if (botOwner_0.GoToPoint(point,true,0.5f) == NavMeshPathStatus.PathComplete)
                {
                    bool_1 = true;
                }
                else
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                    return;
                }
            }

  
            if(botOwner_0.Mover.IsComeTo(0.5f, false))
            {
                
                bool_0 = false;
                bool_1 = false;
                bool_2 = true;

                Components.Logger.LogInfo("Point Reached");

                if (botOwner_0.BotRequestController.CurRequest == null) Components.Logger.LogInfo("request is NULL");
                else Components.Logger.LogInfo("request is " + botOwner_0.BotRequestController.CurRequest?.BotRequestType);

                if (
                    botOwner_0.BotRequestController.CurRequest?.BotRequestType == BotRequestType.followMe ||
                    botOwner_0.BotRequestController.CurRequest?.BotRequestType == BotRequestType.goToPoint
                )
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                    botOwner_0.BotRequestController.CurRequest = null;
                }
                // switch back to hold position if available
                bool hasHold = false;
                try
                {
                    var _listOfRequests = AccessTools.Field(typeof(BotGroupRequestController), "_listOfRequests").GetValue(botOwner_0.BotsGroup.RequestsController) as List<BotRequest>;
                    if (_listOfRequests != null)
                    {
                        foreach (BotRequest botRequest2 in _listOfRequests)
                        {
                            if (
                                botRequest2.CanExecuteByMyself && 
                                botOwner_0.GetPlayer.Id != botRequest2.Requester.Id && 
                                botRequest2.CanStartExecute(botOwner_0) &&
                                botRequest2.BotRequestType == BotRequestType.wait
                            )
                            {
                                botOwner_0.BotRequestController.SetCurrentRequest(botRequest2);
                                hasHold = true;
                                break;
                            }
                        }
                    }
                } catch
                {
                }

                if(!hasHold) botOwner_0.BotsGroup.RequestsController.FindForMe(botOwner_0);

                return;

            } 
            else if (float_0 < Time.time && (bool_0 || bool_1))
            {
                if (_point.HasValue)
                    _shouldSprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, _point.Value) > 15f;

                float_0 = Time.time + 2f;

                botOwner_0.Steering.LookToMovingDirection(30f);
                botOwner_0.Mover.Sprint(_shouldSprint);
            }
        }
    }
}