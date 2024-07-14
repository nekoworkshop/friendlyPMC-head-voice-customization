using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Modules;
using friendlyPMC.Components;

namespace friendlyPMC.Actions
{
    internal class FollowerPatrol : GClass362
    {

        private readonly Player player_0;

        private readonly pitAIBossPlayer boss_0;

        private float float_3;

        private float float_4;

        private Vector3 vector3_0;

        private bool bool_0;

        private bool bool_1;

        private CustomNavigationPoint lastCoverPoint;
        private bool nocover = false;

        public bool IsInited { get; set; }

        private float reachDist = 10f;

        private bool sprinting = false;

        public readonly BotOwner botOwner;

        public FollowerPatrol(pitAIBossPlayer player, BotOwner owner) : base(owner)
        {
            vector3_0 = owner.Position;
            player_0 = player.realPlayer;
            boss_0 = player;

            IsInited = true;

            botOwner = owner;
        }

        public void Update()
        {

            if (this.float_3 < Time.time)
            {

                Vector3 leaderPosition = player_0.Position;
                
                this.float_3 = Time.time + GClass760.Random(1f, 2f);
                float num = Mathf.Abs((this.bool_0 ? this.vector3_0 : (leaderPosition - this.botOwner_0.Position)).magnitude);
                bool flag2;
                bool flag = (flag2 = (num < reachDist)) != this.bool_1;
                this.bool_1 = flag2;
                if (flag2)
                {
                    if (sprinting)
                    {
                        botOwner_0.Mover.Sprint(false, false);
                        sprinting = false;
                    }
                    
                    if (this.bool_0)
                    {
                        this.botOwner_0.StopMove();
                        return;
                    }

                    if (this.float_4 < Time.time || flag)
                    {
                        this.float_4 = Time.time + 8f;

                        CustomNavigationPoint nearPoint = null;

                        if (lastCoverPoint == null && !nocover)
                        {
                            List<CustomNavigationPoint> coverPoints = boss_0.GetAreaCovers();

                            float maxDist = reachDist;
                            float radius = maxDist;

                            List<CustomNavigationPoint> availCover = new List<CustomNavigationPoint>();
                            coverPoints.ForEach((point) =>
                            {
                                float dist = (leaderPosition - point.Position).magnitude;
                                if (dist < radius && point.IsFreeById(botOwner.Id))
                                {
                                    availCover.Add(point);
                                    radius = dist;
                                }
                            });

                            CustomNavigationPoint cover = availCover.Count > 0 ?  availCover.GetRandomItem() : null;
                            if (cover != null)
                            {
                                nearPoint = cover;
                            }
                        }
                        else
                            nearPoint = lastCoverPoint;

                        if (nearPoint != null)
                        {
                            lastCoverPoint = nearPoint;
                            botOwner.Memory.SetCoverPoints(nearPoint);
                            botOwner_0.Steering.LookToMovingDirection();

                            var status = botOwner.Mover.GoToPoint(nearPoint, true, true);
                            if (status != NavMeshPathStatus.PathComplete)
                            {
                                botOwner.StopMove();
                                botOwner.SetPose(0.5f);
                                bool_0 = true;
                                return;
                            }

                            return;
                        }

                        nocover = true;
                        float minR = Mathf.Min(1f, reachDist * 0.19f);
                        float maxR = Mathf.Min(5f, reachDist * 0.65f);
                        float num2 = (float)GClass760.RandomSing() * GClass760.Random(minR, maxR);
                        float num3 = (float)GClass760.RandomSing() * GClass760.Random(minR, maxR);
                        float x = num2 + leaderPosition.x;
                        float z = num3 + leaderPosition.z;
                        NavMeshHit navMeshHit;
                        if (!NavMesh.SamplePosition(new Vector3(x, leaderPosition.y, z), out navMeshHit, 2f, -1))
                        {
                            botOwner.StopMove();
                            bool_0 = true;
                            return;
                        }
                        if (botOwner.GoToPoint(navMeshHit.position, true, -1f, false, true, true, false) != NavMeshPathStatus.PathComplete)
                        {
                            botOwner.StopMove();
                            bool_0 = true;
                            return;
                        }
                    }
                }
                else
                {
                    lastCoverPoint = null;
                    nocover = false;
                    method_0(leaderPosition);
                    bool val = num > 14f;
                    
                    if(val && !sprinting)
                        botOwner.Mover.Sprint(true, false);
                    else if (!val && !sprinting) botOwner.Mover.Sprint(false, false);

                    sprinting = val;
                }
            }
        }

        public void method_0(Vector3 leaderPosition)
        {
            this.bool_0 = false;
            NavMeshHit navMeshHit;

            

            if (this.method_1(leaderPosition) == NavMeshPathStatus.PathComplete)
            {
                this.bool_0 = false;
            }
            else if (NavMesh.SamplePosition(leaderPosition, out navMeshHit, 2f, -1) && this.method_1(leaderPosition) != NavMeshPathStatus.PathComplete)
            {
                this.bool_0 = true;
            }
            if (this.bool_0)
            {
                CustomNavigationPoint freeClosePoint = this.botOwner_0.Covers.GetFreeClosePoint(leaderPosition, 0f, false);
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

        public void SetReachDist(float dist)
        {
            reachDist = dist;    
        }
    }

    internal class FollowerPatrolInstances
    {
        private List<FollowerPatrol> followerPatrols = new List<FollowerPatrol>();

        private static FollowerPatrolInstances Instance;

        private bool IsDisposed = false;
        public FollowerPatrolInstances()
        {
            if (Instance == null) Instance = this;
        }

        public void Destroy()
        {
            if(IsDisposed) return;

            followerPatrols.Clear();

            IsDisposed = true;
        }

        public static void Dispose()
        {
            if(Instance != null)
            {
                Instance.Destroy();
                Instance = null;
            }
        }

        public static void AddPatrol(FollowerPatrol followerPatrol)
        {
            Instance.followerPatrols.Add(followerPatrol);
        }

        public static void RemovePatrol(FollowerPatrol followerPatrol)
        {
            if(Instance.followerPatrols.Contains(followerPatrol))
            {
                Instance.followerPatrols.Remove(followerPatrol);
            };
        }

        public static List<FollowerPatrol> GetPatrols()
        {
            return Instance.followerPatrols;
        }

        public static FollowerPatrol GetPatrol(BotOwner bot)
        {
            FollowerPatrol patrol = null;
            foreach (var item in Instance.followerPatrols)
            {
                if(item.botOwner.ProfileId ==  bot.ProfileId)
                {
                    patrol = item;
                    break;
                }
            }

            return patrol;
        }

        public static void SetNearPatrol(BotOwner bot)
        {
            var patrol = GetPatrol(bot);
            if (patrol != null)
            {
                patrol.SetReachDist(10f);
            }

            if (!bot.Memory.HaveEnemy)
            {
                bot.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                bot.Gesture.TryGestus(EGesture.Good, false);
            }
        }

        public static void SetFarPatrol(BotOwner bot)
        {
            var patrol = GetPatrol(bot);
            if (patrol != null)
            {
                patrol.SetReachDist(25f);
            }

            if (!bot.Memory.HaveEnemy)
            {
                bot.BotTalk.TrySay(EPhraseTrigger.Roger, false);
                bot.Gesture.TryGestus(EGesture.Good, false);
            }
        }
    }
}
