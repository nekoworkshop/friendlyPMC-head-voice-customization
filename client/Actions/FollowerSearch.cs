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
            searchPose = 1f;
            Action = (BotLogicDecision)CustomBotDecisions.EnemySearch;
        }

        protected override void UpdateShootPosition()
        {
            if (_nextShootPositionUpdateTime > Time.time) return;

            _nextShootPositionUpdateTime = Time.time + 2f;

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
                // get the closet cover to the bot from where he can shoot the enemy
                CustomNavigationPoint Spot = Utils.Covers.GetApproachableCoverPoint(
                    botOwner_0,
                    enemySpot
                );

                if (Spot != null) _lastSpot = Spot.Position;
                else
                {
                    _lastSpot = null;
                    _actionsQueue.Enqueue(() =>
                    {
                        // else get the next cover between the bot and the enemy
                        CustomNavigationPoint Spot2 = Utils.Covers.GetClosestCoverPointBetween(
                            botOwner_0,
                            botPosition,
                            enemySpot
                        );

                        if (Spot2 != null) _lastSpot = Spot2.Position;
                        else _lastSpot = null;

                        if (!_lastSpot.HasValue)
                        {
                            _actionsQueue.Enqueue(() =>
                            {
                                // else find a spot from where the bot can shoot the enemy, relative to his position
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
                                        // else get closest cover to the boss and cover him
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
