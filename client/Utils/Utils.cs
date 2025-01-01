using EFT;
using System.Collections.Generic;
using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Components;
using friendlyPMC.Modules;
using System;

namespace friendlyPMC.Utils
{
    public class Utils
    {
        
        private static Dictionary<string,bool> flags = new Dictionary<string,bool>();

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
        public static GClass605.Class290 SetTimeout(Action func, int timer, bool isLopped = false)
        {
            float num = timer / 1000;

            GClass605.Class291 @class = new GClass605.Class291();
            @class.gclass605_0 = StaticManager.Instance.TimerManager;
            @class.timer = new GClass605.Class290();
            @class.timer.Init(new Action(@class.method_0), new Action(@class.method_1));
            @class.timer.Start(Time.time + num, num, isLopped);

            @class.timer.OnTimer += () =>
            {
                try
                {
                    func();
                }
                catch (Exception ex)
                {
                    Modules.Logger.LogError("Exception in SetTimeout");
                    Modules.Logger.LogError(ex);
                }
            };


            return @class.timer;
        }
        /** Shortcut to EFT method of doign MakeTimer in relation to bot activity **/
        public static GClass605.IBotTimer SetBotTimer(Action func,float seconds)
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
                    Modules.Logger.LogError("Exception in SetBotTimer");
                    Modules.Logger.LogError(ex);
                }
            };

            return Timer;
        }


        public static void FlagSet(string flag, bool value)
        {
            flags[flag] = value;
        }

        public static bool FlagGet(string flag)
        {
            flags.TryGetValue(flag, out var value);
            return value || false;
        }

        public static void FlagsClear()
        {
            flags.Clear();
        }

        public static float GetScaledValue(float baseValue, float increment, int level, float maxValue)
        {
            return Math.Min(baseValue + (increment * level), maxValue);
        }
    }
}
