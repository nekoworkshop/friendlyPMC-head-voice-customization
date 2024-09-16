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
    internal class FollowerGuardCover : FollowerSniperSearch
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

            Vector3[] carePosition = new Vector3[] { };

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

                // get closest attack point from the bot's position
                CustomNavigationPoint Spot = Utils.Covers.GetClosestAttackCoverPoint(
                    botOwner_0.Id,
                    botPosition,
                    botPosition,
                    enemySpot,
                    areaCovers,
                    5f,
                    300f,
                    carePosition
                );

                if (Spot != null)
                {
                    _lastSpot = Spot.Position;
                    return;
                }



                _actionsQueue.Enqueue(() =>
                {
                    // else get the next cover between the boss and the enemy
                    CustomNavigationPoint Spot2 = Utils.Covers.GetClosestCoverPointBetween(
                        botOwner_0,
                        protectBoss ? bossPos : enemySpot,
                        enemySpot
                    );

                    if (Spot2 != null)
                    {
                        _lastSpot = Spot2.Position;
                        return;
                    }
                    
                    _actionsQueue.Enqueue(() =>
                    {
                        // else find the next available spot from where the bot can shoot the enemy, relative to his position
                        ShootPointClass shootTarget = new ShootPointClass(enemySpot, 1f);
                        _lastPosition = Utils.Covers.FindShootPosition(
                            botOwner_0.GetPlayer.Transform.position,
                            botOwner_0.ShootData.WeaponRootOffset,
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

                        if (_lastPosition != null)
                        {
                            return;
                        }

                        _actionsQueue.Enqueue(() =>
                        {
                            // if boss needs protection
                            if (protectBoss)
                            {
                                // - get closest attack point to the boss
                                CustomNavigationPoint Spot3 = Utils.Covers.GetClosestAttackCoverPoint(
                                    botOwner_0.Id,
                                    botPosition,
                                    bossPos,
                                    enemySpot,
                                    areaCovers,
                                    5f,
                                    300f,
                                    carePosition
                                );
                                if (Spot3 != null)
                                {
                                    _lastSpot = Spot3.Position;
                                    return;
                                }
                                // - else get closest cover to the boss and cover him
                                _actionsQueue.Enqueue(() =>
                                {
                                    CustomNavigationPoint Spot5 = Utils.Covers.GetClosestCoverPoint(
                                        botOwner_0.Id,
                                        botOwner_0.GetPlayer.Transform.position,
                                        bossPos,
                                        areaCovers,
                                        30f,
                                        5f,
                                        carePosition
                                    );

                                    if (Spot5 != null)
                                    {
                                        _lastCover = Spot5.Position;
                                        return;
                                    }
                                });
                            }
                            else
                            {
                                // else get closest cover
                                CustomNavigationPoint Spot4 = Utils.Covers.GetClosestCoverPoint(
                                    botOwner_0.Id,
                                    botOwner_0.GetPlayer.Transform.position,
                                    botPosition,
                                    areaCovers,
                                    30f,
                                    5f,
                                    carePosition
                                );

                                if (Spot4 != null)
                                {
                                    _lastCover = Spot4.Position;
                                    return;
                                }
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
