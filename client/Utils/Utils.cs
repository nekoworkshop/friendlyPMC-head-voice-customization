using EFT;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Components;
using System;
using System.Timers;

namespace friendlyPMC.Utils
{
    internal class Utils
    {
        public static List<WildSpawnType> BossFollowersRoles = new List<WildSpawnType> { 
            WildSpawnType.bossKnight, 
            WildSpawnType.followerBigPipe,
            WildSpawnType.followerBirdEye
        };

        /** Get distance between 2 points via navigation path **/
        public static float GetNavDistance(Vector3 point1, Vector3 point2, NavMeshPath existingMesh = null)
        {
            NavMeshPath navMeshPath = existingMesh != null ? existingMesh :  new NavMeshPath();
            navMeshPath.ClearCorners();
            bool resut = NavMesh.CalculatePath(point1, point2, -1, navMeshPath);

            if (resut && navMeshPath.status == NavMeshPathStatus.PathComplete)
            {
                return navMeshPath.CalculatePathLength();
            }
            else
            {
                return Vector3.Distance(point2, point1);
            }
        }
        /** Shortcut to check if bot has boss **/
        public static bool HasBoss(BotOwner botOwner)
        {
            return botOwner.BotFollower.HaveBoss;
        }
        /** Shortcut to get the boss the follower has **/
        public static pitAIBossPlayer GetBoss(BotOwner botOwner)
        {
            return (pitAIBossPlayer)botOwner.BotFollower.BossToFollow;
        }
        /** Recreation of javascript SetTimeout **/
        public static GClass552.IBotTimer SetTimeout(Action func, float timer, bool isLopped = false)
        {
            Timer time = new Timer(timer);

            GClass552.Class261 @class = new GClass552.Class261();

            var duration = TimeSpan.FromSeconds(timer / 1000);
            
            float num = (float)duration.TotalSeconds;

            @class.gclass552_0 = null;
            @class.timer = new GClass552.Class260();
            @class.timer.Init(() => { }, () =>
            {
                try
                {
                    func();
                }
                catch (Exception ex)
                {
                    Components.Logger.LogInfo($"Exception in SetTimeout: {ex.Message}");
                    Components.Logger.LogInfo($"StackTrace in SetTimeout: {ex.StackTrace}");
                }
            });

            @class.timer.Start(Time.time + num, num, isLopped);
            return @class.timer;
        }
        /** Shortcut to EFT method of doign MakeTimer in relation to bot activity **/
        public static GClass552.IBotTimer SetBotTimer(Action func,float seconds)
        {
            var Timer = StaticManager.Instance.TimerManager.MakeTimer(TimeSpan.FromSeconds(seconds), false);

            Timer.OnTimer += () =>
            {
                try
                {
                    func();
                }
                catch (Exception ex)
                {
                    Components.Logger.LogInfo($"Exception in SetBotTimer: {ex.Message}");
                    Components.Logger.LogInfo($"StackTrace in SetBotTimer: {ex.StackTrace}");
                }
            };

            return Timer;
        }
    }
}
