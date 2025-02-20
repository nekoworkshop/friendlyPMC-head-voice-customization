using friendlyPMC.Components;
using EFT;
using UnityEngine;

namespace friendlyPMC.Actions
{
    /**
     * Enemy search action for a bot follower
     * @notused
     */
    public class FollowerSearch : FollowerSniperSearch
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
                ShootPointClass shootPointClass = botOwner_0.CurrentEnemyTargetPosition(true);
                // get the closet cover to the bot from where he can shoot the enemy
                CustomNavigationPoint Spot = Utils.Covers.GetClosestCoverPoint(
                    botOwner_0,
                    (botPosition + enemySpot) / 2f,
                    150f,
                    5f,
                    point =>
                    {
                        if (GClass344.CanShootToTarget(shootPointClass, point, botOwner_0.LookSensor.Mask, false))
                        {
                            point.CanIShootToEnemy = true;
                            return true;
                        }
                        return false;
                    }
                );

                if (Spot != null) _lastSpot = Spot.Position;
                else
                {
                    _lastSpot = null;
                    _actionsQueue.Enqueue(() =>
                    {
                        // else get the next cover between the bot and the enemy
                        CustomNavigationPoint Spot2 = Utils.Covers.GetCover(
                            botOwner_0,
                            (botPosition + enemySpot) / 2f,
                            CoverSearchType.closerToSelectedPoint
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
                                        CustomNavigationPoint Spot3 = Utils.Covers.GetCover(botOwner_0, (botPosition + enemySpot) / 2f, CoverSearchType.shoot_toCover_toBot_Distances, 60f);
                                        if (Spot3 != null) _lastSpot = Spot3.Position;
                                        else if (botOwner_0.BotFollower.HaveBoss)
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
                                                    (CustomNavigationPoint point) =>
                                                    {
                                                        if (!GClass369.IsDangerPositionFarEnough(point.Position, new Vector3[] { bossPos }, 0.5f * 0.5f)) return false;
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
