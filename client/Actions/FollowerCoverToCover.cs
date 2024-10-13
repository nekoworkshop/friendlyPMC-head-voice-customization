using EFT;
using friendlyPMC.Components;
using friendlyPMC.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace friendlyPMC.Actions
{
    internal class FollowerCoverToCover : GClass159
    {
        private Vector3? _coverTarget;
        private Vector3? coverPosition;
        private IPlayer _coverPerson;

        private CustomNavigationPoint _closestPoint;

        private float _nextPosibleCheckTime = 0f;

        private bool coverChange = true;

        private float float_4 = 0f;

        private bool sprint = false;

        protected bool _init = false;
        public FollowerCoverToCover(BotOwner bot) : base(bot)
        {

        }

        protected virtual void Init()
        {
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnUpdate += OnAgentUpdate;
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnDispose += OnAgentDispose;
        }
        protected void OnAgentUpdate(AICoreActionResultStruct<BotLogicDecision> decision)
        {
            if (
                decision.Action != (BotLogicDecision)CustomBotDecisions.CoverToCover
            )
            {
                _coverPerson = null; _coverTarget = null; coverPosition = null;
            }
        }

        protected void OnAgentDispose(object sender, EventArgs e)
        {
            _coverPerson = null; _coverTarget = null; coverPosition = null;

            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnUpdate -= OnAgentUpdate;
            (botOwner_0.Brain.Agent as FollowerAIAgent<BotLogicDecision>).OnDispose -= OnAgentDispose;
        }

        public override void Update()
        {
            botOwner_0.DoorOpener.Update();

            try
            {
                if (!_init)
                {
                    Init();
                    _init = true;
                }
            }
            catch (Exception ex)
            {
                Modules.Logger.LogError("Failed to init CoverToCover");
                Modules.Logger.LogError(ex);
            }

            if (!botOwner_0.Memory.HaveEnemy || !botOwner_0.BotFollower.HaveBoss) return;

            if (botOwner_0.BotFollower.HaveBoss) _coverPerson = botOwner_0.BotFollower.BossToFollow.Player();
            else _coverPerson = botOwner_0.GetPlayer;



            if (botOwner_0.GoToSomePointData.IsCome() || !coverChange)
            {
                botOwner_0.SetPose(0.01f);
                botOwner_0.StopMove();
                botOwner_0.Steering.LookToPoint(this.botOwner_0.Memory.GoalEnemy.GetCenterPart());
                coverChange = false;
                RefreshCoverPoint();
                return;
            }

            if (coverPosition.HasValue)
            {

                botOwner_0.GoToSomePointData.UpdateToGo(sprint);
                if (float_4 < Time.time)
                {
                    float_4 = Time.time + 2f;
                    sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, coverPosition.Value) > 20f;
                }
                return;
            }
            else
                RefreshCoverPoint();

        }

        private void RefreshCoverPoint()
        {
            if (_nextPosibleCheckTime > Time.time || !botOwner_0.Memory.HaveEnemy)
            {
                return;
            }

            _nextPosibleCheckTime = Time.time + 4f;

            Vector3 bossPos = _coverPerson.Transform.position;
            Vector3 targetSpot = new Vector3(
                Mathf.Floor(bossPos.x / 10f) * 10f,
                Mathf.Floor(bossPos.y / 3f) * 3f,
                Mathf.Floor(bossPos.z / 10f) * 10f
            );

            if (targetSpot != _coverTarget)
            {
                _coverTarget = targetSpot;

                var _members = AccessTools.Field(typeof(BotsGroup), "_members").GetValue(botOwner_0.BotsGroup) as List<BotOwner>;

                CustomNavigationPoint cover = Covers.GetClosestCoverPoint(botOwner_0, bossPos, Props.searchRadius, 5f, (CustomNavigationPoint point) =>
                {
                    if (!GClass326.IsDangerPositionFarEnough(point.Position, new Vector3[] { bossPos }, 0.5f * 0.5f)) return false;

                    if (botOwner_0.BotsGroup.MembersCount == 1) return true;

                    bool isgood = true;
                    foreach (var item in _members)
                    {
                        if (item == null || item.IsDead || item.BotState != EBotState.Active || item.Id == botOwner_0.Id) continue;
                    }
                    return isgood;
                });

                if (cover != null)
                {
                    coverPosition = cover.Position;
                    _closestPoint = cover;
                    botOwner_0.GoToSomePointData.SetPoint((Vector3)coverPosition);

                    sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, (Vector3)coverPosition) > 20f;
                    botOwner_0.GoToSomePointData.UpdateToGo(sprint);
                    botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());

                    coverChange = true;
                }
            }
        }
    }
}
