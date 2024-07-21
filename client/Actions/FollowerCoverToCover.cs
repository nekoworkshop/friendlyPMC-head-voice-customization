using EFT;
using UnityEngine;

namespace friendlyPMC.Actions
{
    internal class FollowerCoverToCover : GClass159
    {
        private Vector3? _coverTarget;
        private Vector3? coverPosition;
        private IPlayer _coverPerson;

        private CustomNavigationPoint _closestPoint;

        private float _nextPosibleCheckTime = 0f;

        private bool coverChange = true;

        private float float_4 = 0f;

        private bool sprint = false;
        public FollowerCoverToCover(BotOwner bot) : base(bot)
        {

        }

        public override void Update()
        {
            botOwner_0.DoorOpener.Update();

            if (!botOwner_0.Memory.HaveEnemy || !botOwner_0.BotFollower.HaveBoss) return;

            if (botOwner_0.BotFollower.HaveBoss) _coverPerson = botOwner_0.BotFollower.BossToFollow.Player();

            

            if (botOwner_0.GoToSomePointData.IsCome() || !coverChange)
            {
                botOwner_0.SetPose(0.01f);
                botOwner_0.StopMove();
                botOwner_0.Steering.LookToPoint(this.botOwner_0.Memory.GoalEnemy.GetCenterPart());
                coverChange = false;
                RefreshCoverPoint();
                return;
            }

            if (coverPosition.HasValue)
            {

                botOwner_0.GoToSomePointData.UpdateToGo(sprint);
                botOwner_0.LookData.SetLookPointByHearing(_closestPoint);
                if (float_4 < Time.time)
                {
                    float_4 = Time.time + 2f;
                    sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, coverPosition.Value) > 20f;
                }
                return;
            } else
                RefreshCoverPoint();

        }

        private void RefreshCoverPoint()
        {
            if (_nextPosibleCheckTime > Time.time || !botOwner_0.Memory.HaveEnemy)
            {
                return;
            }

            _nextPosibleCheckTime = Time.time + 4f;

            Vector3 bossPos = _coverPerson.Transform.position;
            Vector3 targetSpot = new Vector3(
                Mathf.Floor(bossPos.x / 20f) * 20f,
                Mathf.Floor(bossPos.y / 3f) * 3f,
                Mathf.Floor(bossPos.z / 20f) * 20f
            );

            if (targetSpot != _coverTarget)
            {
                _coverTarget = targetSpot;
                CustomNavigationPoint cover = Utils.Covers.GetClosestCoverPoint(botOwner_0, bossPos, 50f, 10f);

                if (cover != null)
                {
                    coverPosition = cover.Position;
                    _closestPoint = cover;
                    botOwner_0.GoToSomePointData.SetPoint((Vector3)coverPosition);
                    
                    bool sprint = Utils.Utils.GetNavDistance(botOwner_0.GetPlayer.Transform.position, (Vector3)coverPosition) > 20f;
                    botOwner_0.GoToSomePointData.UpdateToGo(sprint);
                    botOwner_0.LookData.SetLookPointByHearing(null);

                    coverChange = true;
                }
            }
        }
    }
}
