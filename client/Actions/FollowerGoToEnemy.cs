using EFT;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace friendlyPMC.Actions
{
    // replication of GClass177 to have the bot do sprinting when needed
    internal class FollowerGoToEnemy : GClass176
    {
        private bool shouldSprint = false;
        private readonly GClass136 gclass136_0;
        public FollowerGoToEnemy(BotOwner bot) : base(bot)
        {
            gclass136_0 = new GClass141(botOwner_0);
        }

        public override void Update()
        {
            EnemyInfo goalEnemy = botOwner_0.Memory.GoalEnemy;

            base.method_0();
            botOwner_0.Sprint(shouldSprint, true);
            NotMovingCheck();
            base.method_5();

            bool flag = false;
            if (goalEnemy.IsVisible && goalEnemy.CanShoot)
            {
                flag = true;
                gclass136_0.Update();
            }
            else if (!goalEnemy.IsVisible && Time.time - goalEnemy.GroupInfo.EnemyLastSeenTimeSense >= 5f)
            {
                botOwner_0.LookData.SetLookPointByHearing(null);
            }
            else
            {
                botOwner_0.Steering.LookToPoint(botOwner_0.Memory.GoalEnemy.GetCenterPart());
            }
            if (botOwner_0.Mover.HasPathAndNoComplete)
            {
                botOwner_0.SetTargetMoveSpeed(1f);
                botOwner_0.SetPose(1f);
                bool flag2 = botOwner_0.Mover.IsComeTo(botOwner_0.Settings.FileSettings.Move.REACH_DIST, false);
                if (!botOwner_0.WeaponManager.HaveBullets)
                {
                    botOwner_0.WeaponManager.Reload.TryReload();
                }
                if (flag2 && goalEnemy.IsVisible && goalEnemy.CanShoot)
                {
                    return;
                }
                if (flag2)
                {
                    method_6();
                    return;
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

        public void method_6()
        {
            botOwner_0.StopMove();
        }

        public override void NotMovingCheck()
        {
            if (float_0 > Time.time)
            {
                return;
            }
            float_0 = Time.time + 3f;
            Vector3 currPosition = botOwner_0.Memory.GoalEnemy.CurrPosition;
            TryMoveToEnemy(currPosition);
        }

        // replication of MoveToEnemyData.TryToMoveToEnemy, but adapted to use our cover system
        public bool TryMoveToEnemy(Vector3 targetPoint)
        {
            Vector3 position;
            if (botOwner_0.MoveToEnemyData.method_0(targetPoint, out position) && botOwner_0.GoToPoint(position, true, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
            {
                return true;
            }
            Vector3 vector;
            if (!botOwner_0.MoveToEnemyData.ShallRecalWay(out vector) && Time.time - botOwner_0.Mover.LastPathSetTime < 10f)
            {
                return true;
            }
            if (botOwner_0.GoToPoint(targetPoint, true, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
            {
                Vector3 curPathLastPoint = botOwner_0.Mover.CurPathLastPoint;
                if ((targetPoint - curPathLastPoint).magnitude < 2f)
                {
                    return true;
                }
            }
            NavMeshHit navMeshHit;
            if (NavMesh.SamplePosition(targetPoint, out navMeshHit, 2.6f, -1) && botOwner_0.GoToPoint(navMeshHit.position, false, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
            {
                return true;
            }
            List<CustomNavigationPoint> closePoints = Utils.Covers.GetCoverPoints(botOwner_0, targetPoint, 25f);
            if (closePoints.Count > 0)
            {
                CustomNavigationPoint customNavigationPoint = closePoints.RandomElement();
                if (customNavigationPoint != null && Mathf.Abs(customNavigationPoint.Position.y - targetPoint.y) < 1f && this.botOwner_0.GoToPoint(customNavigationPoint.Position, true, -1f, false, false, true, false) == NavMeshPathStatus.PathComplete)
                {
                    return true;
                }
                customNavigationPoint = closePoints.RandomElement();
                if (customNavigationPoint != null)
                {
                    shouldSprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, customNavigationPoint.Position) >= 20f;
                    return true;
                }
            }

            CustomNavigationPoint freeClosePoint = Utils.Covers.GetClosestCoverPoint(botOwner_0, targetPoint, 30f, 1f);
            if (freeClosePoint != null)
            {
                shouldSprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, freeClosePoint.Position) >= 20f;
            }

            return freeClosePoint != null;
        }
    }
}
