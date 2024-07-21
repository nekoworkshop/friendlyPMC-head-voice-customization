using Cysharp.Threading.Tasks;
using EFT;
using friendlyPMC.Components;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    internal class FollowerSniperSearch : GClass159
    {
        private Vector3? spotPosition;

        private bool sprint = false;

        private float float_4 = 0f;
        private float float_5 = 0f;
        
        private Vector3? _lastTarget;
        private Vector3? _lastPosition;
        private Vector3? _lastCover;
        private Vector3? _lastSpot;

        private float _nextShootPositionUpdateTime = 0f;

        private bool covering = false;

        private bool _hasCome = false;

        protected float _minDist = 10f;

        protected float _maxDist = 100f;

        private Queue<Action> _actionsQueue = new Queue<Action>();
        public FollowerSniperSearch(BotOwner bot) : base(bot)
        {

        }

        public override void Update()
        {
            botOwner_0.DoorOpener.Update();

            if (!botOwner_0.Memory.HaveEnemy) return;

            while (_actionsQueue.Count > 0)
            {
                Action action = _actionsQueue.Dequeue();
                action();
            }

            if (spotPosition.HasValue)
            {
                if (botOwner_0.GoToSomePointData.IsCome())
                {
                    if (!_hasCome)
                    {
                        botOwner_0.SetPose(0.5f);
                        botOwner_0.StopMove();
                        botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
                        _hasCome = true;
                    }
                    ReachSearchPoint();
                    return;
                }

                botOwner_0.GoToSomePointData.UpdateToGo(sprint);

                _hasCome = false;

                if (!covering)
                    botOwner_0.Steering.LookToDirection(botOwner_0.Memory.GoalEnemy.CurrPosition - botOwner_0.GetPlayer.Transform.position, 90f);
                else
                    botOwner_0.LookData.SetLookPointByHearing(null);

                if (float_4 < Time.time)
                {
                    float_4 = Time.time + 2f;
                    sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, spotPosition.Value) > 20f;
                }
                return;
            }

            if (_lastTarget.HasValue && float_5 < Time.time)
            {
                float_5 = Time.time + GClass761.Random(3f, 4f);

                UpdateShootPosition();


                if (!_lastTarget.HasValue) return;

                if (_lastSpot.HasValue)
                {
                    spotPosition = _lastSpot;
                    covering = false;
                }
                // else find a position from where we can see the enemy
                else if (_lastPosition.HasValue)
                {
                    spotPosition = _lastPosition.Value;
                    covering = false;
                }
                // else find a position from where we can cover boss
                else if (_lastCover.HasValue)
                {
                    spotPosition = _lastCover.Value;
                    covering = true;

                }
                // nothing found - stay in place
                if (!spotPosition.HasValue)
                {
                    SetSearchPosition();
                    return;
                }

                botOwner_0.GoToSomePointData.SetPoint((Vector3)spotPosition);
                bool sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, (Vector3)spotPosition) > 20f;
                botOwner_0.GoToSomePointData.UpdateToGo(sprint);

                if (covering)
                    botOwner_0.Steering.LookToMovingDirection();
                else
                    botOwner_0.Steering.LookToDirection(botOwner_0.Memory.GoalEnemy.CurrPosition - botOwner_0.GetPlayer.Transform.position, 90f);

            }
            else
            {
                _hasCome = false;
                if (!_lastTarget.HasValue)
                {
                    UpdateShootPosition();
                }
                else SetSearchPosition();
            }
        }

        private void RefreshSearchPoint()
        {
            Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;

            Vector3 targetSpot = new Vector3(
                Mathf.Floor(enemyPosition.x / 20f) * 20f,
                Mathf.Floor(enemyPosition.y / 2f) * 2f,
                Mathf.Floor(enemyPosition.z / 20f) * 20f
            );

            if(targetSpot != _lastTarget)
            {
                _lastTarget = targetSpot;
                spotPosition = null;
                covering = false;;
            }
        }

        private void ReachSearchPoint()
        {
            SetSearchPosition();
            spotPosition = null;
            covering = false;
        }

        private void SetSearchPosition()
        {
            botOwner_0.SetPose(0.5f);
            botOwner_0.StopMove();
            if(botOwner_0.Memory.HaveEnemy)
                botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
        }

        private void UpdateShootPosition()
        {
            if (_nextShootPositionUpdateTime > Time.time) return;

            _nextShootPositionUpdateTime = Time.time + 1.5f;

            Vector3[] carePosition = new Vector3[] { };

            foreach (var item in botOwner_0.EnemiesController.EnemyInfos)
            {
                try
                {
                    carePosition = carePosition.AddItem(item.Value.CurrPosition).ToArray();
                }
                catch
                {
                }
            }

            List<CustomNavigationPoint> areaCovers = botOwner_0.BotFollower.HaveBoss ? (botOwner_0.BotFollower.BossToFollow as pitAIBossPlayer).GetAreaCovers() : new List<CustomNavigationPoint>();

            if (!botOwner_0.Memory.HaveEnemy)
            {
                _lastTarget = null;
                return;
            }

            RefreshSearchPoint();


            if (_lastTarget.HasValue)
            {
                Vector3 enemySpot = botOwner_0.Memory.GoalEnemy.CurrPosition;

                CustomNavigationPoint Spot = Utils.Covers.GetClosestAttackCoverPoint(
                    botOwner_0.Id,
                    botOwner_0.GetPlayer.Transform.position,
                    enemySpot,
                    areaCovers,
                    _minDist, 
                    _maxDist,
                    carePosition,
                    false
                );

                if (Spot != null) _lastSpot = Spot.Position;
                else _lastSpot = null;


                if (!_lastSpot.HasValue)
                {
                    _actionsQueue.Enqueue(() => {

                        ShootPointClass shootTarget = new ShootPointClass(enemySpot, 1f);
                        _lastPosition = Utils.Covers.FindShootPosition(
                            botOwner_0.GetPlayer.Transform.position,
                            shootTarget,
                            botOwner_0.LookSensor.Mask,
                            _minDist,
                            _maxDist
                        );

                        if (!_lastPosition.HasValue && botOwner_0.BotFollower.HaveBoss)
                        {
                            Vector3 bossPos = botOwner_0.BotFollower.BossToFollow.Position;

                            _actionsQueue.Enqueue(() =>
                            {
                                CustomNavigationPoint cover = Utils.Covers.GetClosestCoverPoint(
                                    botOwner_0.Id,
                                    botOwner_0.GetPlayer.Transform.position,
                                    bossPos,
                                    areaCovers,
                                    30f,
                                    5f,
                                    carePosition
                                );

                                if (cover != null)
                                {
                                    _lastCover = cover.Position;

                                }
                                else
                                {
                                    _lastCover = null;
                                }
                            });
                        }

                    });
                }
            }
            else
            {
                _lastPosition = null;
            }
        }
    }
}
