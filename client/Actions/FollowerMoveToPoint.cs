using EFT;
using friendlyPMC.Components;
using friendlyPMC.Requests;
using HarmonyLib;
using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.AI;

using Comfort.Common;

namespace friendlyPMC.Actions
{
    public class FollowerMoveToPoint: GClass196
    {
        private bool _shouldSprint = true;

        private float float_0 = 0f;

        private bool bool_0 = false;
        private bool bool_1 = false;
        private bool bool_2 = false;

        private bool ischecking = false;
        private float checkTime = 0f;

        private bool _init = false;

        private Vector3? _point;
        public FollowerMoveToPoint(BotOwner bot) : base(bot)
        {

        }

        private void Init()
        {
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnUpdate += OnAgentUpdate;
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnDispose += OnAgentDispose;
        }
        private void OnAgentUpdate(AICoreActionResultStruct<BotLogicDecision> decision)
        {
            if (decision.Action != (BotLogicDecision)CustomBotDecisions.MoveToPoint || (decision.Reason != "req:comeHere" && decision.Reason != "req:goCheck"))
            {
                bool_0 = false;
                bool_1 = false;
                bool_2 = false;
                ischecking = false;
            }
        }

        protected void OnAgentDispose(object sender, EventArgs e)
        {
            _init = false;

            bool_0 = false;
            bool_1 = false;
            bool_2 = false;

            ischecking = false;

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

            // cancel movement if we see the enemy
            if (botOwner_0.Memory.HaveEnemy && botOwner_0.Memory.GoalEnemy.CanShoot)
            {
                botOwner_0.BotRequestController.CurRequest.Complete();
                botOwner_0.BotRequestController.CurRequest = null;
                checkTime = 0f;
                bool_0 = false;
                bool_1 = false;
                bool_2 = false;
                ischecking = false;
                return;
            }

            if (ischecking)
            {
                if (checkTime < Time.time)
                {
                    BotRequest r = botOwner_0.BotRequestController.CurRequest;
                    if (r != null)
                    {
                        r.Complete();
                        botOwner_0.BotRequestController.CurRequest = null;
                        checkTime = 0f;
                        bool_0 = false;
                        bool_1 = false;
                        bool_2 = false;
                        ischecking = false;
                    }
                }
                return;
            }

            if (bool_2)
            {
                BotRequest r = botOwner_0.BotRequestController.CurRequest;
                if (r != null)
                {
                    r.Complete();
                    botOwner_0.BotRequestController.CurRequest = null;
                }
                return;
            }

            
            if (botOwner_0.BotRequestController.CurRequest == null) return;


            if (botOwner_0.Brain.Agent.LastReason == "req:goCheck" && !bool_0)
            {
                FollowerGoCheck req = botOwner_0.BotRequestController.CurRequest as FollowerGoCheck;

                if (req.HasPoint) bool_0 = true;
                else req.Complete();
            }
            else if (botOwner_0.Brain.Agent.LastReason == "req:comeHere" && !bool_1)
            {
                IPlayer requester = botOwner_0.BotRequestController.CurRequest.Requester;

                Vector3 requestPos = requester.Position;
                Vector3 dir01 = requester.LookDirection;

                float offset = GClass824.RandomSing() * GClass824.Random(1f, 2f);
                Vector3 direction = Vector3.Cross(Vector3.up, dir01).normalized;

                Vector3 finPos = requestPos + direction * offset;

                Vector3 point = new Vector3(finPos.x, requestPos.y, finPos.z);

                _point = point;

                if (botOwner_0.GoToPoint(point, true, 0.5f,true,true,true) == NavMeshPathStatus.PathComplete)
                {
                    bool_1 = true;
                }
                else
                {
                    botOwner_0.BotRequestController.CurRequest.Complete();
                    return;
                }
            }


            if (botOwner_0.Mover.IsComeTo(0.5f, false))
            {
                ischecking = bool_0;

                bool_0 = false;
                bool_1 = false;
                bool_2 = true;

                if (ischecking) checkTime = Time.time + GClass824.Random(4f, 6f);

                return;

            }
            else if (float_0 < Time.time && (bool_0 || bool_1))
            {
                if (bool_0) _shouldSprint = false;
                else if (_point.HasValue)
                    _shouldSprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, _point.Value) > 15f;

                float_0 = Time.time + 2f;

                var brain = botOwner_0.Brain.BaseBrain as FollowerBrain;
                bool wasHit = false;
                // let the bot turn to the direction he was hit from
                if (brain != null && brain.WasHit)
                {
                    wasHit = true;
                }

                if(!wasHit && !botOwner_0.Memory.HaveEnemy) botOwner_0.Steering.LookToMovingDirection(60f);
                botOwner_0.Mover.Sprint(_shouldSprint && !wasHit);
            }
        }
    }
}
