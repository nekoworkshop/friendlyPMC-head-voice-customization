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
            maxDist = 200f;
            searchPose = 1f;
            Action = (BotLogicDecision)CustomBotDecisions.EnemySearch;
            searchType = "EnemySearch";
        }

        protected override void UpdateShootPosition()
        {
            if (_nextShootPositionUpdateTime > Time.time) return;

            _nextShootPositionUpdateTime = Time.time + 2f;

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
                CustomNavigationPoint Spot = Utils.Covers.GetClosestShootCover(
                    botOwner_0,
                    botPosition,
                    5f,
                    150f,
                    point =>
                    {
                        return Utils.Covers.IsPointBetween(point.Position, botPosition, enemySpot);
                    }
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
                                if (botOwner_0.IsDead || botOwner_0.BotState != EBotState.Active || !botOwner_0.Memory.HaveEnemy) return;
                                // else find the next available spot from where the bot can shoot the enemy, relative to his position
                                _lastPosition = Utils.Covers.FindShootPosition(
                                    botOwner_0,
                                    minDist,
                                    maxDist,
                                    (Vector3 position) =>
                                    {

                                        if (!Utils.Covers.IsPointBetween(position, botPosition, enemySpot)) return false;

                                        return true;
                                    }
                                );

                                if (!_lastPosition.HasValue)
                                {
                                    // find a cover closer to the enemy - this emulates search
                                    _actionsQueue.Enqueue(() =>
                                    {
                                       CustomNavigationPoint Spot3  = Utils.Covers.GetClosestCoverPoint(botOwner_0, enemySpot, 30f, 5f);
                                        if(Spot3 != null) _lastSpot = Spot3.Position;
                                        else if(botOwner_0.BotFollower.HaveBoss)
                                        {
                                            Vector3 bossPos = botOwner_0.BotFollower.BossToFollow.Position;
                                            Vector3 botPos = botOwner_0.GetPlayer.Transform.position;
                                            bool protectBoss = (botOwner_0.Brain.BaseBrain as FollowerBrain).bossNeedsProtection;

                                            _actionsQueue.Enqueue(() =>
                                            {
                                                // else get closest cover to the boss and cover him
                                                CustomNavigationPoint cover = Utils.Covers.GetClosestCoverPoint(
                                                    botOwner_0,
                                                    protectBoss ? bossPos : botPos,
                                                    30f,
                                                    5f,
                                                    (CustomNavigationPoint point)=>{
                                                        if(!GClass326.IsDangerPositionFarEnough(point.Position, new Vector3[]{ bossPos }, 0.5f * 0.5f)) return false;
                                                        return true;
                                                    }
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
