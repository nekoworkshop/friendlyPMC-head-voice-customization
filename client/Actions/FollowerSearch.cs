using friendlyPMC.Components;

using EFT;
using HarmonyLib;

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace friendlyPMC.Actions
{
    internal class FollowerSearch : FollowerSniperSearch
    {
        public FollowerSearch(BotOwner owner)
        : base(owner)
        {
            minDist = 5f;
            maxDist = 60f;
            Action = (BotLogicDecision)CustomBotDecisions.EnemySearch;
        }

        protected override void UpdateShootPosition()
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
                Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;

                CustomNavigationPoint Spot = Utils.Covers.GetClosestCoverPointBetween(
                    botOwner_0,
                    botPosition,
                    enemySpot
                );

                if (Spot != null) _lastSpot = Spot.Position;
                else _lastSpot = null;

                if (!_lastSpot.HasValue)
                {
                    _actionsQueue.Enqueue(() =>
                    {
                        ShootPointClass shootTarget = new ShootPointClass(enemySpot, 1f);
                        _lastPosition = Utils.Covers.FindShootPosition(
                            botOwner_0.GetPlayer.Transform.position,
                            shootTarget,
                            botOwner_0.LookSensor.Mask,
                            minDist,
                            maxDist,
                            (Vector3 position) =>
                            {
                        
                                if (!Utils.Covers.IsPointBetween(position, botPosition, enemySpot)) return false;

                                return true;
                            }
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
