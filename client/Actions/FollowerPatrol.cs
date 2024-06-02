using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Modules;

namespace friendlyPMC.Actions
{
    /** A blend between GClass427 (simple follow) and GClass484 (close cover with stop) **/
    internal class FollowerPatrol : GClass362
    {

        private readonly Player player_0;

        private float float_3;

        private float float_4;

        private Vector3 vector3_0;

        private bool bool_0;

        private bool bool_1;

        private GClass424 gclass424_0;

        public FollowerPatrol(Player player, BotOwner owner) : base(owner)
        {
            this.vector3_0 = owner.Position;
            this.player_0 = player;

            this.gclass424_0 = this.botOwner_0.Memory.botObserveData;
        }

        public void Update()
        {
            Components.Logger.LogInfo("Follower Patrol Update");
            this.botOwner_0.LookData.SetLookPointByHearing(null);

            if (this.float_3 < Time.time)
            {
                this.float_3 = Time.time + GClass760.Random(1f, 2f);
                float num = Mathf.Abs((this.bool_0 ? this.vector3_0 : (this.player_0.Position - this.botOwner_0.Position)).magnitude);
                bool flag2;
                bool flag = (flag2 = (num < 10f)) != this.bool_1;
                this.bool_1 = flag2;
                if (flag2)
                {
                    this.botOwner_0.Mover.Sprint(false, true);
                    if (this.bool_0)
                    {
                        this.botOwner_0.StopMove();
                        return;
                    }
                    if (this.float_4 < Time.time || flag)
                    {
                        this.float_4 = Time.time + 8f;

                        /*List<CustomNavigationPoint> nearPoints = BossPlayers.Instance.GetCovers();

                        float maxDist = 10f;
                        float radius = maxDist;

                        CustomNavigationPoint nearPoint = null;
                        nearPoints.ForEach((point) =>
                        {
                            float dist = (botOwner_0.BotFollower.BossToFollow.Player().Transform.position - point.Position).magnitude;
                            if (dist < radius && point.IsFreeById(botOwner_0.Id))
                            {
                                nearPoint = point;
                                radius = dist;
                            }
                        });

                        if (nearPoint != null)
                        {
                            botOwner_0.Memory.SetCoverPoints(nearPoint);

                            var status = this.botOwner_0.Mover.GoToPoint(nearPoint, true, true);
                            if (status != NavMeshPathStatus.PathComplete)
                            {
                                this.botOwner_0.StopMove();
                                return;
                            }
                        }*/

                        float num2 = (float)GClass760.RandomSing() * GClass760.Random(0.3f, 3.5f);
                        float num3 = (float)GClass760.RandomSing() * GClass760.Random(0.3f, 3.5f);
                        float x = num2 + this.player_0.Position.x;
                        float z = num3 + this.player_0.Position.z;
                        NavMeshHit navMeshHit;
                        if (!NavMesh.SamplePosition(new Vector3(x, this.player_0.Position.y, z), out navMeshHit, 2f, -1))
                        {
                            this.botOwner_0.StopMove();
                            return;
                        }
                        if (this.botOwner_0.GoToPoint(navMeshHit.position, true, -1f, false, true, true, false) != NavMeshPathStatus.PathComplete)
                        {
                            this.botOwner_0.StopMove();
                            return;
                        }
                    }
                }
                else
                {
                    this.method_0();
                    bool val = num > 10.5f;
                    this.botOwner_0.Mover.Sprint(val, true);
                }
            }
        }

        public void method_0()
        {
            this.bool_0 = false;
            NavMeshHit navMeshHit;
            if (this.method_1(this.player_0.Position) == NavMeshPathStatus.PathComplete)
            {
                this.bool_0 = false;
            }
            else if (NavMesh.SamplePosition(this.player_0.Position, out navMeshHit, 2f, -1) && this.method_1(this.player_0.Position) != NavMeshPathStatus.PathComplete)
            {
                this.bool_0 = true;
            }
            if (this.bool_0)
            {
                CustomNavigationPoint freeClosePoint = this.botOwner_0.Covers.GetFreeClosePoint(this.player_0.Position, 0f, false);
                if (freeClosePoint != null)
                {
                    this.bool_0 = true;
                    this.method_1(freeClosePoint.Position);
                }
            }
        }

        public NavMeshPathStatus method_1(Vector3 v)
        {
            NavMeshPathStatus navMeshPathStatus = this.botOwner_0.GoToPoint(v, true, -1f, false, true, true, false);
            if (navMeshPathStatus == NavMeshPathStatus.PathComplete)
            {
                this.vector3_0 = v;
            }
            return navMeshPathStatus;
        }


    }
}
