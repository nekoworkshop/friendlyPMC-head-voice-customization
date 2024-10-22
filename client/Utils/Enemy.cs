using Comfort.Common;
using EFT;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

using friendlyPMC.Modules;

namespace friendlyPMC.Utils
{
    internal class Enemy
    {
        private struct CachedEnemyInfo
        {
            public float EnemyCount;
            public Vector3 CachedPosition;

            public CachedEnemyInfo(float enemyCount, Vector3 cachedPosition)
            {
                EnemyCount = enemyCount;
                CachedPosition = cachedPosition;
            }
        }

        private static Dictionary<(Vector3, string), CachedEnemyInfo> enemyLocationCache = new Dictionary<(Vector3, string), CachedEnemyInfo>();
        private static object enemyLocationCacheLock = new object();

        private static List<string> enemies = new List<string>();

        public enum EnemyDistance
        {
            VeryClose = 0,
            Close = 1,
            Mid = 2,
            Distant = 3,
            Far = 4
        }

        public enum ProxyDistance
        {
            VeryClose = 0,
            Close = 1,
            Mid = 2,
            Distant = 3,
            Far = 4
        }
        public static bool IsClose(BotOwner bot)
        {
            if (!bot.Memory.HaveEnemy) return false;

            return (Distance(bot) <= EnemyDistance.Close && bot.Memory.GoalEnemy.IsVisible) || Distance(bot) == EnemyDistance.VeryClose;
        }

        public static ProxyDistance DistanceProxy(BotOwner bot, Vector3 position)
        {
            Vector3 enemyPosition = bot.Memory.GoalEnemy.CurrPosition;

            float distance = Vector3.Distance(position, enemyPosition);

            if (distance < 12f) return ProxyDistance.VeryClose;

            if (distance < 30f)
            {
                return ProxyDistance.Close;
            }

            if (distance < 55f)
            {
                return ProxyDistance.Mid;
            }

            if (distance < 110f)
            {
                return ProxyDistance.Distant;
            }

            return ProxyDistance.Far;
        }
        public static EnemyDistance Distance(BotOwner bot)
        {
            if (!bot.Memory.HaveEnemy) return EnemyDistance.Far;

            Vector3 botPosition = bot.GetPlayer.Transform.position;
            Vector3 enemyPosition = bot.Memory.GoalEnemy.CurrPosition;

            float distance = Utils.GetNavDistance(botPosition, enemyPosition);

            if (distance < 15f) return EnemyDistance.VeryClose;

            if (distance < 31f)
            {
                return EnemyDistance.Close;
            }

            if (distance < 55f)
            {
                return EnemyDistance.Mid;
            }

            if (distance < 110f)
            {
                return EnemyDistance.Distant;
            }

            return EnemyDistance.Far;

        }

        public static float NavDistance(BotOwner bot, NavMeshPath navMesh = null)
        {
            if (!bot.Memory.HaveEnemy) return Mathf.Infinity;

            Vector3 botPosition = bot.GetPlayer.Transform.position;
            Vector3 enemyPosition = bot.Memory.GoalEnemy.CurrPosition;

            return Utils.GetNavDistance(botPosition, enemyPosition, navMesh);
        }

        public static float GetEnemiesAtLocation(BotOwner bot, string enemyId, Vector3 position, float radius = 30f)
        {
            try
            {
                if (!enemies.Contains(enemyId))
                {
                    Player enemy = Singleton<GameWorld>.Instance.GetAlivePlayerByProfileID(enemyId);

                    enemy.OnIPlayerDeadOrUnspawn += (IPlayer pl) =>
                    {
                        ClearEnemyLocations(enemyId);
                        enemies.Remove(enemyId);
                    };
                }

                Vector3 cacheKey = new Vector3(
                    Mathf.Floor(position.x / 10f) * 10f,
                    Mathf.Floor(position.y / 10f) * 10f,
                    Mathf.Floor(position.z / 10f) * 10f
                );
                (Vector3, string) cacheKeyWithId = (cacheKey, enemyId);


                lock (enemyLocationCacheLock)
                {

                    if (enemyLocationCache.TryGetValue(cacheKeyWithId, out CachedEnemyInfo cachedInfo))
                    {
 
                        if (cachedInfo.CachedPosition == cacheKey)
                        {

                            return cachedInfo.EnemyCount;
                        }
                        else
                        { 
                            enemyLocationCache.Remove(cacheKeyWithId);
                        }
                    }
                }

                int nr = 0;

                Collider[] hits = new Collider[20];

                int numHits = Physics.OverlapSphereNonAlloc(
                    position,
                    radius,
                    hits,
                    LayerMaskClass.PlayerMask
                );

                if (numHits == 0)
                {
                    lock (enemyLocationCacheLock)
                    {
                        enemyLocationCache[cacheKeyWithId] = new CachedEnemyInfo(0f, position);
                    }

                    return 0;
                }
 
                for (int i = 0; i < numHits; i++)
                {
                    var enemy = bot.ShootData.method_4(hits[i]);

                    if (enemy != null &&
                        enemy.HealthController.IsAlive &&
                        (bot.EnemiesController.IsEnemy(enemy) ||
                         bot.Settings.FileSettings.Mind.ENEMY_BOT_TYPES.Contains(enemy.GetPlayer.Profile.Info.Settings.Role)) &&
                        bot.GetPlayer.ProfileId != enemy.ProfileId &&
                        !(enemy.IsAI && bot.BotsGroup.Contains(enemy.AIData.BotOwner)) &&
                        !bot.BotsGroup.IsAlly(enemy))
                    {
                        nr++;
                    }
                }

                float result = nr;

                lock (enemyLocationCacheLock)
                {
                    enemyLocationCache[cacheKeyWithId] = new CachedEnemyInfo(result, cacheKey);
                }

                return result;

            }
            catch (Exception ex)
            {
                Modules.Logger.LogError("GetEnemiesAtLocation Error");
                Modules.Logger.LogError(ex);
                return 1;
            }
        }

        public static EnemyInfo MakeEnemy(BotOwner bot, Player enemy, EBotEnemyCause cause = EBotEnemyCause.addPlayerToBoss)
        {
            BotSettingsClass groupInfo;
            bot.BotsGroup.Enemies.TryGetValue(enemy, out groupInfo);

            if (groupInfo == null)
            {
                bot.BotsGroup.AddEnemy(enemy, cause);
                bot.BotsGroup.Enemies.TryGetValue(enemy, out groupInfo);
            }

            if (groupInfo == null)
            {
                groupInfo = new BotSettingsClass(enemy, bot.BotsGroup, cause);

                bot.Memory.AddEnemy(enemy, groupInfo, false);
            }

            EnemyInfo info;

            bot.EnemiesController.EnemyInfos.TryGetValue(enemy, out info);

            if (info == null)
            {
                info = bot.EnemiesController.AddNew(bot.BotsGroup, enemy, groupInfo);

                info.SetVisible(true);

                bot.EnemiesController.SetInfo(enemy, info);
            }

            return info;

        }

        public static void ClearEnemyLocations(string enemyId)
        {
            lock (enemyLocationCacheLock)
            {
                var keysToRemove = enemyLocationCache.Keys.Where(key => key.Item2 == enemyId).ToList();
                foreach (var key in keysToRemove)
                {
                    enemyLocationCache.Remove(key);
                }
            }
        }

        public static void ClearEnemiesLocations()
        {
            enemyLocationCache.Clear();
            enemies.Clear();
        }

        public static bool IsClosestEnemy(BotOwner botOwner_0)
        {
            bool result = true;
            EnemyInfo enemyInfo = botOwner_0.Memory.GoalEnemy;
            foreach (EnemyInfo enemy in botOwner_0.EnemiesController.EnemyInfos.Values)
            {
                if (enemy.Distance < enemyInfo.Distance)
                {
                    result = false;
                    break;
                }
            }

            return result;
        }
    }
}
