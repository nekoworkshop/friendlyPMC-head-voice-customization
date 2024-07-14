using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    internal class FollowerSniperSearch : GClass160
    {

        private CustomNavigationPoint Spot = null;

        private Vector3? spotPosition;

        private bool sprint = false;

        private float float_4 = 0f;
        private float float_5 = 0f;

        private float _nextPosibleCheckTime = 0f;
        private Vector3? _lastTarget;

        private NavMeshPath navMeshPath;
        public FollowerSniperSearch(BotOwner bot) : base(bot)
        {
            navMeshPath = new NavMeshPath();
        }

        public override void Update()
        {            
            botOwner_0.DoorOpener.Update();

            if (!botOwner_0.Memory.HaveEnemy) return;

            RefreshSearchPoint();

            if(spotPosition.HasValue)
            {
                botOwner_0.GoToSomePointData.UpdateToGo(sprint);
                botOwner_0.Steering.LookToMovingDirection();
                if (float_4 < Time.time)
                {
                    float_4 = Time.time + 2f;
                    sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, spotPosition.Value) > 20f;
                }
                return;
            }

            if (_lastTarget.HasValue && Spot == null && float_5 < Time.time)
            {
                float_5 = Time.time + GClass760.Random(2f, 4f);

                Components.Logger.LogInfo("sniperSearch: look for a spot");
                // find a cover from where we can shoot the enemy
                Spot = Utils.Covers.GetClosestAttackCoverPoint(botOwner_0, _lastTarget.Value, 20f, 100f, null, true);

                if (Spot != null)
                {
                    spotPosition = Spot.Position;
                }
                // else find a position from where we can see the enemy
                else
                {
                    ShootPointClass shootTarget = new ShootPointClass(_lastTarget.Value, 1f);

                    spotPosition = Utils.Covers.FindShootPosition(botOwner_0, shootTarget, 15f, 100f);
                }

                if (!spotPosition.HasValue)
                {
                    Components.Logger.LogInfo("sniperSearch: no sniping spot found");
                    this.botOwner_0.SetPose(0.01f);
                    this.botOwner_0.StopMove();
                }
                else
                {
                    Components.Logger.LogInfo("sniperSearch: have spot");

                    botOwner_0.LookData.SetLookPointByHearing(null);

                    botOwner_0.GoToSomePointData.SetPoint((Vector3)spotPosition);
                    bool sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, (Vector3)spotPosition) > 20f;
                    botOwner_0.GoToSomePointData.UpdateToGo(sprint);
                    botOwner_0.Steering.LookToMovingDirection();
                }
            } else
            {

                if(!_lastTarget.HasValue) RearchSearchPoint();
            }
        }

        private void RefreshSearchPoint()
        {
            if (this._nextPosibleCheckTime > Time.time)
            {
                return;
            }
            
            this._nextPosibleCheckTime = Time.time + 7f;
            Vector3 enemyPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;

            Vector3 targetSpot = new Vector3(
                Mathf.Floor(enemyPosition.x / 20f) * 20f,
                Mathf.Floor(enemyPosition.y / 20f) * 20f,
                Mathf.Floor(enemyPosition.z / 20f) * 20f
            );

            if(targetSpot != _lastTarget)
            {
                _lastTarget = targetSpot;
                spotPosition = null;
                Spot = null;
                botOwner_0.Mover.Sprint(false, true);
            }
        }

        private void RearchSearchPoint()
        {
            botOwner_0.SetPose(0.01f);
            botOwner_0.StopMove();
            botOwner_0.Steering.LookToPoint(this.botOwner_0.Memory.GoalEnemy.GetCenterPart());
            _nextPosibleCheckTime = Time.time + 1f;
            spotPosition = null;
        }
    }
}
