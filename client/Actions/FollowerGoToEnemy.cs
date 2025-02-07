using EFT;
using HarmonyLib;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    /**
     * A combination of the attackMoving and goToEnemy to use our cover system for "goToEnemy" decision
     */
    public class FollowerGoToEnemy : GClass185
    {
        private bool shouldSprint = false;

        private float float_0 = 0f;
        private float float_1 = 0f;

        public FollowerGoToEnemy(BotOwner bot) : base(bot)
        {
        }

        public override void Update()
        {
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            botOwner_0.DoorOpener.Update();
            botOwner_0.Sprint(shouldSprint, true);
            NotMovingCheck();

            bool flag = false;
            if (goalEnemy.IsVisible && goalEnemy.CanShoot)
            {
                flag = true;
                botOwner_0.Steering.LookToPoint(goalEnemy.GetCenterPart());
                botOwner_0.StopMove();
                gclass158_0.Update();
                return;
            }
            else if (!goalEnemy.IsVisible && Time.time - goalEnemy.GroupInfo.EnemyLastSeenTimeSense >= 5f)
            {
                botOwner_0.LookData.SetLookPointByHearing(null);
            }
            else
            {
                botOwner_0.Steering.LookToPoint(goalEnemy.CurrPosition);
            }

            if (botOwner_0.Mover.HasPathAndNoComplete)
            {

                bool flag2 = botOwner_0.Mover.IsComeTo(botOwner_0.Settings.FileSettings.Move.REACH_DIST, false);
                if (!botOwner_0.WeaponManager.HaveBullets)
                {
                    botOwner_0.WeaponManager.Reload.TryReload();
                }
                if (flag2 && goalEnemy.IsVisible && goalEnemy.CanShoot)
                {
                    AimAndMove();
                    return;
                }
                if (flag2)
                {
                    botOwner_0.StopMove();
                    botOwner_0.Steering.LookToPoint(goalEnemy.GetCenterPart());

                    return;
                }
                else if (!shouldSprint)
                {
                    AimAndMove();
                }
            }
            else
            {
                if (!flag)
                {
                    botOwner_0.LookData.SetLookPointByHearing(null);
                }
                botOwner_0.StopMove();
                botOwner_0.SetPose(0f);
            }
        }

        public void NotMovingCheck()
        {
            if (float_0 > Time.time)
            {
                return;
            }
            float_0 = Time.time + 3f;
            Vector3 currPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;
            TryMoveToEnemy(currPosition);
        }

        public void AimAndMove()
        {

            bool flag;
            Vector3 centerPos;
            Vector3 enemyPos = botOwner_0.Memory.GoalEnemy.EnemyLastPosition;
            if (botOwner_0.Memory.IsInCover && !botOwner_0.LookSensor.EnoughDistToShoot(out flag))
            {
                centerPos = (botOwner_0.Transform.position + enemyPos) / 2f;
            }
            else
            {
                centerPos = enemyPos;
            }


            if (float_1 < Time.time)
            {
                float_1 = Time.time + 2f;
                var shootPointClass = botOwner_0.CurrentEnemyTargetPosition(true);
                var point = Utils.Covers.GetClosestCoverPoint(botOwner_0, centerPos, 50f, 0.5f, cover =>
                {
                    if (GClass344.CanShootToTarget(shootPointClass, cover, botOwner_0.LookSensor.Mask, false))
                    {
                        cover.CanIShootToEnemy = true;
                        return true;
                    }

                    return false;
                });

                if(point != null)
                {
                    botOwner_0.GoToPoint(point);
                } 
                else
                {
                    botOwner_0.GoToPoint(centerPos);
                }
            }

            botOwner_0.BotAttackManager.UpdateNextTick();

            AimingAndShoot();
        }

        // replication of MoveToEnemyData.TryToMoveToEnemy, but adapted to use our cover system
        public bool TryMoveToEnemy(Vector3 targetPoint)
        {
            Vector3 position;
            if (botOwner_0.MoveToEnemyData.method_0(targetPoint, out position) && botOwner_0.GoToPoint(position, true, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
            {
                shouldSprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, position) >= 20f;
                return true;
            }

            Vector3 vector;
            if (!botOwner_0.MoveToEnemyData.ShallRecalWay(out vector) && Time.time - botOwner_0.Mover.LastPathSetTime < 10f)
            {
                return true;
            }
            if (botOwner_0.GoToPoint(targetPoint, true, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
            {
                Vector3 curPathLastPoint = botOwner_0.Mover.TargetPoint.Value;
                if ((targetPoint - curPathLastPoint).magnitude < 2f)
                {
                    return true;
                }
            }

            NavMeshHit navMeshHit;
            if (NavMesh.SamplePosition(targetPoint, out navMeshHit, 2.6f, -1) && botOwner_0.GoToPoint(navMeshHit.position, false, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
            {
                shouldSprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, navMeshHit.position) >= 20f;
                return true;
            }

            CustomNavigationPoint customNavigationPoint = null;

            List<CustomNavigationPoint> closePoints = Utils.Covers.GetCoverPoints(botOwner_0, targetPoint, 20f);
            if (closePoints.Count > 0)
            {
                customNavigationPoint = closePoints.RandomElement();
            }

            if (customNavigationPoint == null)
            {
                CustomNavigationPoint freeClosePoint = Utils.Covers.GetClosestCoverPoint(botOwner_0, targetPoint, 30f, 1f);
                if (freeClosePoint != null)
                {
                    customNavigationPoint = freeClosePoint;
                }
            }

            if (customNavigationPoint != null && Mathf.Abs(customNavigationPoint.Position.y - targetPoint.y) < 1f && this.botOwner_0.GoToPoint(customNavigationPoint.Position, true, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
            {
                shouldSprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, customNavigationPoint.Position) >= 20f;
                return true;
            }

            return false;
        }
    }
}
