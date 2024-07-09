using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AI;
using UnityEngine;
using friendlyPMC.Modules;
using friendlyPMC.Components;
using HarmonyLib;

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
        public static float GetNavDistance(Vector3 point1, Vector3 point2)
        {
            NavMeshPath navMeshPath = new NavMeshPath();
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
    }
}
