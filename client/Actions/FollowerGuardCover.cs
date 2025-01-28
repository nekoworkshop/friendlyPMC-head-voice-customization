using EFT;
using EFT.InventoryLogic;
using friendlyPMC.Components;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace friendlyPMC.Actions
{
    /**
     * Alternative action to enemy search for the follower with "Guard" tactic
     */
    public class FollowerGuardCover : FollowerSniperSearch
    {
        public FollowerGuardCover(BotOwner owner)
        : base(owner)
        {
            maxDist = 60f;
            searchPose = 0.1f;
            Action = (BotLogicDecision)CustomBotDecisions.GuardToCover;
            searchType = "GuardCover";
        }

        protected override void UpdateShootPosition()
        {
            if (_nextShootPositionUpdateTime > Time.time) return;

            _nextShootPositionUpdateTime = Time.time + 3f;

            if (!botOwner_0.Memory.HaveEnemy)
            {
                _lastTarget = null;
                return;
            }

            RefreshSearchPoint();


            if (_lastTarget.HasValue)
            {

                Vector3 enemySpot = botOwner_0.Memory.GoalEnemy.CurrPosition;
                Vector3 bossPos = botOwner_0.BotFollower.BossToFollow.Player().Transform.position;
                Vector3 botPosition = botOwner_0.GetPlayer.Transform.position;
                bool protectBoss = (botOwner_0.Brain.BaseBrain as FollowerBrain).bossNeedsProtection;

                // get closest attack point from the bot's position
                CustomNavigationPoint Spot = Utils.Covers.GetCover(
                    botOwner_0,
                    protectBoss ? bossPos : botPosition,
                    CoverSearchType.shoot_toCover_toBot_Distances,
                    50f
                );

                if (Spot != null)
                {
                    _lastSpot = Spot.Position;
                    return;
                }

                _lastSpot = null;

                _actionsQueue.Enqueue(() =>
                {
                    // else get the next cover between the bot and the enemy
                    CustomNavigationPoint Spot2 = Utils.Covers.GetCover(
                        botOwner_0,
                        (botOwner_0.Position + enemySpot) / 2f,
                        CoverSearchType.closerToSelectedPoint
                    );

                    if (Spot2 != null)
                    {
                        _lastSpot = Spot2.Position;
                        return;
                    }
                    _lastSpot = null;

                    _actionsQueue.Enqueue(() =>
                    {
                        // else find the next available spot from where the bot can shoot the enemy, relative to his position
                        if (botOwner_0.IsDead || botOwner_0.BotState != EBotState.Active || !botOwner_0.Memory.HaveEnemy) return;
                        _lastPosition = Utils.Covers.FindShootPosition(
                            botOwner_0,
                            minDist,
                            maxDist
                        );

                        if (_lastPosition != null)
                        {
                            return;
                        }
                        _lastPosition = null;

                        _actionsQueue.Enqueue(() =>
                        {
                            // if boss needs protection
                            if (protectBoss)
                            {
                                // - get closest attack point to the boss
                                CustomNavigationPoint Spot3 = Utils.Covers.GetCover(
                                    botOwner_0,
                                    bossPos,
                                    CoverSearchType.shoot_toCover_toBot_Distances
                                );
                                if (Spot3 != null)
                                {
                                    _lastSpot = Spot3.Position;
                                    return;
                                }

                                _lastSpot = null;

                                // - else get closest cover to the boss and cover him
                                _actionsQueue.Enqueue(() =>
                                {
                                    CustomNavigationPoint Spot5 = Utils.Covers.GetCover(
                                        botOwner_0,
                                        (bossPos + enemySpot) / 2f,
                                        CoverSearchType.closerToSelectedPoint,
                                        35f
                                    );

                                    if (Spot5 != null)
                                    {
                                        _lastCover = Spot5.Position;
                                        return;
                                    }

                                    _lastCover = null;
                                });
                            }
                            else
                            {
                                // else get closest cover
                                CustomNavigationPoint Spot4 = Utils.Covers.GetCover(
                                    botOwner_0,
                                    botPosition,
                                    CoverSearchType.closerToSelectedPoint,
                                    35f
                                );

                                if (Spot4 != null)
                                {
                                    _lastCover = Spot4.Position;
                                    return;
                                }

                                _lastCover = null;
                            }
                        });

                    });
                });
            }
            else
            {
                _lastPosition = null;
            }
        }
    }
}
